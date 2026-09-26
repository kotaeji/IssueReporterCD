using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IssueReporterCD.Infrastructure;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;

namespace IssueReporterCD.Collectors
{
    /// <summary>
    /// Writes OS, hardware, disk, and running-process details to system_info.txt.
    /// </summary>
    public sealed class SystemInfoCollector : ICollector
    {
        public string Name
        {
            get { return "시스템 정보"; }
        }

        public Task<CollectorResult> CollectAsync(string workDir, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                string path = Path.Combine(workDir, "system_info.txt");
                File.WriteAllText(path, BuildReport(), new UTF8Encoding(true));
                return CollectorResult.Done("OK");
            }, cancellationToken);
        }

        private static string BuildReport()
        {
            var sb = new StringBuilder();

            sb.AppendLine("[General]");
            sb.AppendLine("Collected at   : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));
            sb.AppendLine("Machine name   : " + Environment.MachineName);
            sb.AppendLine("User           : " + Environment.UserDomainName + "\\" + Environment.UserName);
            sb.AppendLine("OS             : " + GetOsDescription());
            sb.AppendLine("64-bit OS      : " + Environment.Is64BitOperatingSystem);
            sb.AppendLine("Processors     : " + Environment.ProcessorCount);
            sb.AppendLine("Uptime         : " + FormatUptime());
            Try(sb, () =>
            {
                var computer = new ComputerInfo();
                sb.AppendLine("Memory total   : " + SizeFormatter.Format((long)computer.TotalPhysicalMemory));
                sb.AppendLine("Memory free    : " + SizeFormatter.Format((long)computer.AvailablePhysicalMemory));
            });
            sb.AppendLine();

            sb.AppendLine("[Drives]");
            Try(sb, () =>
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    sb.AppendLine(string.Format("{0,-4} {1,-8} free {2,10} / {3,10}",
                        drive.Name, drive.DriveType, SizeFormatter.Format(drive.AvailableFreeSpace), SizeFormatter.Format(drive.TotalSize)));
                }
            });
            sb.AppendLine();

            sb.AppendLine("[Processes]");
            Try(sb, () =>
            {
                foreach (var process in Process.GetProcesses().OrderBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase))
                {
                    using (process)
                    {
                        long memory = 0;
                        try { memory = process.WorkingSet64; } catch (InvalidOperationException) { }
                        sb.AppendLine(string.Format("{0,-40} pid {1,-7} mem {2,10}",
                            process.ProcessName, process.Id, SizeFormatter.Format(memory)));
                    }
                }
            });

            return sb.ToString();
        }

        private static string GetOsDescription()
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        return string.Format("{0} {1} (build {2})",
                            key.GetValue("ProductName"), key.GetValue("DisplayVersion"), key.GetValue("CurrentBuildNumber"));
                    }
                }
            }
            catch (Exception)
            {
                // Fall back to the (less precise) runtime value below.
            }
            return Environment.OSVersion.ToString();
        }

        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

        private static string FormatUptime()
        {
            var uptime = TimeSpan.FromMilliseconds(GetTickCount64());
            return string.Format("{0}d {1:00}:{2:00}:{3:00}", uptime.Days, uptime.Hours, uptime.Minutes, uptime.Seconds);
        }

        private static void Try(StringBuilder sb, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                sb.AppendLine("(error: " + ex.Message + ")");
            }
        }
    }
}
