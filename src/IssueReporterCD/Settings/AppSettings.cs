using System;
using System.Configuration;
using System.IO;

namespace IssueReporterCD.Settings
{
    /// <summary>
    /// Deployment settings from IssueReporterCD.exe.config (appSettings).
    /// </summary>
    public sealed class AppSettings
    {
        public string AuroraLogDir { get; private set; }

        public string AuroraConfigDir { get; private set; }

        public int LogMaxAgeDays { get; private set; }

        public string OutputDir { get; private set; }

        public static AppSettings Load()
        {
            var values = ConfigurationManager.AppSettings;

            int maxAgeDays;
            if (!int.TryParse(values["LogMaxAgeDays"], out maxAgeDays) || maxAgeDays < 0)
            {
                maxAgeDays = 3;
            }

            string outputDir = Expand(values["OutputDir"]);
            if (string.IsNullOrWhiteSpace(outputDir))
            {
                outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "IssueReports");
            }

            return new AppSettings
            {
                AuroraLogDir = Expand(values["AuroraLogDir"]),
                AuroraConfigDir = Expand(values["AuroraConfigDir"]),
                LogMaxAgeDays = maxAgeDays,
                OutputDir = outputDir,
            };
        }

        private static string Expand(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : Environment.ExpandEnvironmentVariables(value.Trim());
        }
    }
}
