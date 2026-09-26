using System;
using System.IO;

namespace IssueReporterCD.Collectors
{
    public sealed class LogFreshnessResult
    {
        public DateTime? NewestWriteTime { get; set; }

        public TimeSpan? Gap { get; set; }

        public bool IsSuspicious { get; set; }

        /// <summary>Explanation for the engineer; null when nothing looks wrong.</summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Detects a log folder that probably isn't the one Aurora is writing to:
    /// the newest log is far from the current time, or there are no logs at all.
    /// </summary>
    public static class LogFreshness
    {
        public static LogFreshnessResult Check(string logDir, int warningHours, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(logDir) || !Directory.Exists(logDir))
            {
                return new LogFreshnessResult
                {
                    IsSuspicious = true,
                    Message = "로그 폴더가 없습니다.\n로그 폴더 설정이 올바른지 확인하세요.\n\n로그 폴더: " + logDir,
                };
            }

            DateTime? newest = FindNewestWriteTime(logDir);
            if (!newest.HasValue)
            {
                return new LogFreshnessResult
                {
                    IsSuspicious = true,
                    Message = "로그 폴더에서 로그 파일을 찾지 못했습니다.\n로그 폴더 설정이 올바른지 확인하세요.\n\n로그 폴더: " + logDir,
                };
            }

            TimeSpan gap = now - newest.Value;
            var result = new LogFreshnessResult { NewestWriteTime = newest, Gap = gap };
            if (warningHours > 0 && Math.Abs(gap.TotalHours) >= warningHours)
            {
                result.IsSuspicious = true;
                result.Message = string.Format(
                    "가장 최근 로그 기록 시각은 {0:yyyy-MM-dd HH:mm}으로, 지금과 {1} 차이가 납니다.\n로그 폴더 설정이 올바른지 확인하세요.\n\n로그 폴더: {2}",
                    newest.Value, FormatDuration(gap), logDir);
            }
            return result;
        }

        public static string FormatDuration(TimeSpan span)
        {
            span = span.Duration();
            if (span.TotalDays >= 1)
            {
                return string.Format("{0}일 {1}시간", (int)span.TotalDays, span.Hours);
            }
            return string.Format("{0}시간 {1}분", (int)span.TotalHours, span.Minutes);
        }

        private static DateTime? FindNewestWriteTime(string dir)
        {
            DateTime? newest = null;
            try
            {
                foreach (string file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    DateTime time = File.GetLastWriteTime(file);
                    if (!newest.HasValue || time > newest.Value)
                    {
                        newest = time;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Use whatever was found before the inaccessible folder.
            }
            catch (IOException)
            {
            }
            return newest;
        }
    }
}
