using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IssueReporterCD.Infrastructure;
using WinForms = System.Windows.Forms;

namespace IssueReporterCD.Collectors
{
    /// <summary>
    /// The "environment fingerprint" layer of the issue schema (docs/issue-schema.md):
    /// values the field engineer can't type but the analysis depends on.
    /// Aurora-owned values come from the environment file Aurora/Boot Loader writes;
    /// the rest is read from this PC.
    /// </summary>
    public sealed class EnvironmentFingerprint
    {
        public const string ModuleKeyPrefix = "module.";

        public EnvironmentFingerprint()
        {
            Aurora = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ModuleVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Keys from the Aurora environment file, except module.* entries.</summary>
        public Dictionary<string, string> Aurora { get; }

        /// <summary>module.&lt;name&gt;=&lt;version&gt; entries from the Aurora environment file.</summary>
        public Dictionary<string, string> ModuleVersions { get; }

        public string AuroraFilePath { get; set; }

        public bool AuroraFileFound { get; set; }

        /// <summary>Last write time of the Aurora file; an old value means the info may be stale.</summary>
        public DateTime? AuroraFileWrittenAt { get; set; }

        public string MachineName { get; set; }

        public string OsVersion { get; set; }

        public string OsArchitecture { get; set; }

        public string ProcessArchitecture { get; set; }

        public string ScreenResolution { get; set; }

        /// <summary>"sha256:..." over every file in the config folder; null if the folder is missing.</summary>
        public string ConfigHash { get; set; }

        public int ConfigFileCount { get; set; }

        public string GetAurora(string key)
        {
            string value;
            return Aurora.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value) ? value : null;
        }
    }

    public sealed class EnvironmentCollector : ICollector
    {
        private readonly string _auroraEnvironmentFile;
        private readonly string _configDir;

        public EnvironmentCollector(string auroraEnvironmentFile, string configDir)
        {
            _auroraEnvironmentFile = auroraEnvironmentFile;
            _configDir = configDir;
        }

        public string Name
        {
            get { return "환경 지문"; }
        }

        /// <summary>Set after CollectAsync completes; included in report.json.</summary>
        public EnvironmentFingerprint Result { get; private set; }

        public Task<CollectorResult> CollectAsync(string workDir, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                EnvironmentFingerprint fingerprint = Collect(_auroraEnvironmentFile, _configDir);
                Result = fingerprint;

                if (!fingerprint.AuroraFileFound)
                {
                    return CollectorResult.Warning("Aurora 정보 파일 없음 (PC 정보만)");
                }
                return CollectorResult.Done("Aurora 정보 " + (fingerprint.Aurora.Count + fingerprint.ModuleVersions.Count) + "개");
            }, cancellationToken);
        }

        public static EnvironmentFingerprint Collect(string auroraEnvironmentFile, string configDir)
        {
            var fingerprint = new EnvironmentFingerprint
            {
                AuroraFilePath = auroraEnvironmentFile,
                MachineName = Environment.MachineName,
                OsVersion = SystemInfoCollector.GetOsDescription(),
                OsArchitecture = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(),
            };

            ReadAuroraFile(fingerprint, auroraEnvironmentFile);
            fingerprint.ScreenResolution = DescribeScreens();
            ComputeConfigHash(fingerprint, configDir);
            return fingerprint;
        }

        private static void ReadAuroraFile(EnvironmentFingerprint fingerprint, string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    return;
                }

                fingerprint.AuroraFileFound = true;
                fingerprint.AuroraFileWrittenAt = File.GetLastWriteTime(path);
                foreach (KeyValuePair<string, string> entry in KeyValueFile.Read(path))
                {
                    if (entry.Key.StartsWith(EnvironmentFingerprint.ModuleKeyPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        fingerprint.ModuleVersions[entry.Key.Substring(EnvironmentFingerprint.ModuleKeyPrefix.Length)] = entry.Value;
                    }
                    else
                    {
                        fingerprint.Aurora[entry.Key] = entry.Value;
                    }
                }
            }
            catch (Exception)
            {
                // A broken file is treated as missing; the PC-side values are still useful.
                fingerprint.AuroraFileFound = false;
            }
        }

        private static string DescribeScreens()
        {
            try
            {
                return string.Join("; ", WinForms.Screen.AllScreens.Select(s => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}x{1}{2}",
                    s.Bounds.Width, s.Bounds.Height, s.Primary ? " (primary)" : string.Empty)));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Hashes relative path + content hash of every config file in a stable order, so the same
        /// configuration always gives the same value regardless of file times or enumeration order.
        /// </summary>
        private static void ComputeConfigHash(EnvironmentFingerprint fingerprint, string configDir)
        {
            if (string.IsNullOrWhiteSpace(configDir) || !Directory.Exists(configDir))
            {
                return;
            }

            try
            {
                string root = Path.GetFullPath(configDir).TrimEnd('\\', '/');
                List<string> files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                using (SHA256 total = SHA256.Create())
                using (SHA256 perFile = SHA256.Create())
                {
                    var manifest = new StringBuilder();
                    foreach (string file in files)
                    {
                        string relative = file.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/').ToLowerInvariant();
                        using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                        {
                            manifest.Append(relative).Append('\n').Append(ToHex(perFile.ComputeHash(stream))).Append('\n');
                        }
                    }

                    fingerprint.ConfigHash = "sha256:" + ToHex(total.ComputeHash(Encoding.UTF8.GetBytes(manifest.ToString())));
                    fingerprint.ConfigFileCount = files.Count;
                }
            }
            catch (Exception)
            {
                // Leave the hash empty rather than report one that doesn't cover every file.
                fingerprint.ConfigHash = null;
            }
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }
    }
}
