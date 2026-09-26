using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IssueReporterCD.Infrastructure;

namespace IssueReporterCD.Collectors
{
    /// <summary>
    /// Copies files from a folder (recursively) into the report, optionally only recent ones.
    /// Files held open by the equipment software are read with shared access.
    /// </summary>
    public sealed class DirectoryCollector : ICollector
    {
        private readonly string _sourceDir;
        private readonly string _destName;
        private readonly int _maxAgeDays;

        /// <param name="maxAgeDays">Only files modified within this many days; 0 copies everything.</param>
        public DirectoryCollector(string name, string sourceDir, string destName, int maxAgeDays)
        {
            Name = name;
            _sourceDir = sourceDir;
            _destName = destName;
            _maxAgeDays = maxAgeDays;
        }

        public string Name { get; }

        public Task<CollectorResult> CollectAsync(string workDir, CancellationToken cancellationToken)
        {
            return Task.Run(() => Collect(workDir, cancellationToken), cancellationToken);
        }

        private CollectorResult Collect(string workDir, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_sourceDir) || !Directory.Exists(_sourceDir))
            {
                return CollectorResult.Warning("경로 없음: " + _sourceDir);
            }

            string sourceRoot = Path.GetFullPath(_sourceDir).TrimEnd('\\', '/');
            string destRoot = Path.Combine(workDir, _destName);
            DateTime? cutoff = _maxAgeDays > 0 ? DateTime.Now.AddDays(-_maxAgeDays) : (DateTime?)null;

            int copied = 0;
            int failed = 0;
            long bytes = 0;

            try
            {
                foreach (string file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var info = new FileInfo(file);
                        if (cutoff.HasValue && info.LastWriteTime < cutoff.Value)
                        {
                            continue;
                        }

                        string relative = file.Substring(sourceRoot.Length).TrimStart('\\', '/');
                        string target = Path.Combine(destRoot, relative);
                        Directory.CreateDirectory(Path.GetDirectoryName(target));

                        using (var source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                        using (var destination = new FileStream(target, FileMode.Create, FileAccess.Write))
                        {
                            source.CopyTo(destination);
                        }

                        copied++;
                        bytes += info.Length;
                    }
                    catch (IOException)
                    {
                        failed++;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        failed++;
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return CollectorResult.Failed("접근 권한 없음: " + ex.Message);
            }

            string detail = copied + "개, " + SizeFormatter.Format(bytes);
            if (failed > 0)
            {
                return CollectorResult.Warning(detail + " (실패 " + failed + "개)");
            }
            if (copied == 0)
            {
                return CollectorResult.Warning(_maxAgeDays > 0
                    ? "최근 " + _maxAgeDays + "일 내 파일 없음"
                    : "파일 없음");
            }
            return CollectorResult.Done(detail);
        }
    }
}
