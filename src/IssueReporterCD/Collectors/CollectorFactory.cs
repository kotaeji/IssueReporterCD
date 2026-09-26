using System.Collections.Generic;
using IssueReporterCD.Settings;

namespace IssueReporterCD.Collectors
{
    public static class CollectorFactory
    {
        public static IList<ICollector> Create(ReporterSettings settings, string screenshotDir, string screenshotError)
        {
            return new List<ICollector>
            {
                new ScreenshotCollector(screenshotDir, screenshotError),
                new SystemInfoCollector(),
                new DirectoryCollector("Aurora 로그", settings.LogDir.Value, "aurora_logs", settings.LogMaxAgeDays),
                new DirectoryCollector("Aurora sys-error", settings.SysErrorDir.Value, "aurora_syserror", settings.LogMaxAgeDays),
                new DirectoryCollector("Aurora 설정", settings.ConfigDir.Value, "aurora_config", 0),
            };
        }
    }
}
