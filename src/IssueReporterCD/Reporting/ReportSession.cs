using System;
using System.IO;

namespace IssueReporterCD.Reporting
{
    /// <summary>
    /// Temporary working folder for one run; everything in it goes into the zip.
    /// </summary>
    public sealed class ReportSession
    {
        private ReportSession(string workDir, DateTime startedAt)
        {
            WorkDir = workDir;
            StartedAt = startedAt;
        }

        public string WorkDir { get; }

        public DateTime StartedAt { get; }

        public string ScreenshotDir
        {
            get { return Path.Combine(WorkDir, "screenshots"); }
        }

        public bool HasReport { get; set; }

        public static ReportSession Create()
        {
            DateTime startedAt = DateTime.Now;
            string workDir = Path.Combine(Path.GetTempPath(), "IssueReporterCD", startedAt.ToString("yyyyMMdd_HHmmss_fff"));
            Directory.CreateDirectory(workDir);
            return new ReportSession(workDir, startedAt);
        }

        /// <summary>
        /// Deletes the working folder once a zip exists. Without a zip it is kept so nothing is lost.
        /// </summary>
        public void CleanupIfReported()
        {
            if (!HasReport)
            {
                return;
            }

            try
            {
                Directory.Delete(WorkDir, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
