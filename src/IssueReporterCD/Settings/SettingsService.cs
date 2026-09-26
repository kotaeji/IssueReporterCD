using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using IssueReporterCD.Infrastructure;

namespace IssueReporterCD.Settings
{
    /// <summary>
    /// Resolves settings from three layers and saves user overrides.
    /// <list type="number">
    /// <item>User overrides: %ProgramData%\IssueReporterCD\settings.ini (shared by every account on the equipment PC)</item>
    /// <item>Aurora discovery file (App.config AuroraDiscoveryFile): key=value paths written by Aurora</item>
    /// <item>App.config defaults</item>
    /// </list>
    /// </summary>
    public sealed class SettingsService
    {
        public const string LogDirKey = "LogDir";
        public const string SysErrorDirKey = "SysErrorDir";
        public const string ConfigDirKey = "ConfigDir";
        public const string OutputDirKey = "OutputDir";
        public const string LogMaxAgeDaysKey = "LogMaxAgeDays";
        public const string LogGapWarningHoursKey = "LogGapWarningHours";

        private readonly NameValueCollection _defaults;
        private readonly string _userFile;
        private readonly string _discoveryFile;

        public SettingsService()
            : this(ConfigurationManager.AppSettings, DefaultUserFile())
        {
        }

        public SettingsService(NameValueCollection defaults, string userFile)
        {
            _defaults = defaults;
            _userFile = userFile;
            _discoveryFile = Expand(defaults["AuroraDiscoveryFile"]);
        }

        public ReporterSettings Load()
        {
            Dictionary<string, string> user = SafeRead(_userFile);
            Dictionary<string, string> aurora = SafeRead(_discoveryFile);

            return new ReporterSettings
            {
                LogDir = ResolvePath(LogDirKey, "AuroraLogDir", user, aurora),
                SysErrorDir = ResolvePath(SysErrorDirKey, "AuroraSysErrorDir", user, aurora),
                ConfigDir = ResolvePath(ConfigDirKey, "AuroraConfigDir", user, aurora),
                OutputDir = ResolveOutputDir(user),
                LogMaxAgeDays = ResolveInt(LogMaxAgeDaysKey, user, 3),
                LogGapWarningHours = ResolveInt(LogGapWarningHoursKey, user, 24),
                UserSettingsFile = _userFile,
                AuroraDiscoveryFile = _discoveryFile,
            };
        }

        /// <summary>
        /// Stores only values that differ from the automatic ones, so later Aurora path changes still apply.
        /// </summary>
        /// <returns>The reloaded settings.</returns>
        public ReporterSettings Save(SettingsEdits edits)
        {
            ReporterSettings current = Load();
            var overrides = new Dictionary<string, string>();
            AddPathOverride(overrides, LogDirKey, edits.LogDir, current.LogDir);
            AddPathOverride(overrides, SysErrorDirKey, edits.SysErrorDir, current.SysErrorDir);
            AddPathOverride(overrides, ConfigDirKey, edits.ConfigDir, current.ConfigDir);
            AddPathOverride(overrides, OutputDirKey, edits.OutputDir, current.OutputDir);
            if (edits.LogMaxAgeDays != DefaultInt(LogMaxAgeDaysKey, 3))
            {
                overrides[LogMaxAgeDaysKey] = edits.LogMaxAgeDays.ToString();
            }
            if (edits.LogGapWarningHours != DefaultInt(LogGapWarningHoursKey, 24))
            {
                overrides[LogGapWarningHoursKey] = edits.LogGapWarningHours.ToString();
            }

            KeyValueFile.Write(_userFile, overrides);
            return Load();
        }

        public static bool SamePath(string a, string b)
        {
            return string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);
        }

        private PathSetting ResolvePath(string key, string defaultKey, Dictionary<string, string> user, Dictionary<string, string> aurora)
        {
            string autoValue;
            SettingSource autoSource;
            string auroraValue;
            if (aurora.TryGetValue(key, out auroraValue) && !string.IsNullOrWhiteSpace(auroraValue))
            {
                autoValue = Expand(auroraValue);
                autoSource = SettingSource.Aurora;
            }
            else
            {
                autoValue = Expand(_defaults[defaultKey]);
                autoSource = SettingSource.Default;
            }

            return WithUserOverride(key, user, autoValue, autoSource);
        }

        private PathSetting ResolveOutputDir(Dictionary<string, string> user)
        {
            string autoValue = Expand(_defaults["OutputDir"]);
            if (string.IsNullOrWhiteSpace(autoValue))
            {
                autoValue = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "IssueReports");
            }
            return WithUserOverride(OutputDirKey, user, autoValue, SettingSource.Default);
        }

        private static PathSetting WithUserOverride(string key, Dictionary<string, string> user, string autoValue, SettingSource autoSource)
        {
            string userValue;
            if (user.TryGetValue(key, out userValue) && !string.IsNullOrWhiteSpace(userValue))
            {
                return new PathSetting(Expand(userValue), SettingSource.User, autoValue, autoSource);
            }
            return new PathSetting(autoValue, autoSource, autoValue, autoSource);
        }

        private int ResolveInt(string key, Dictionary<string, string> user, int fallback)
        {
            string userValue;
            int parsed;
            if (user.TryGetValue(key, out userValue) && int.TryParse(userValue, out parsed) && parsed >= 0)
            {
                return parsed;
            }
            return DefaultInt(key, fallback);
        }

        private int DefaultInt(string key, int fallback)
        {
            int parsed;
            return int.TryParse(_defaults[key], out parsed) && parsed >= 0 ? parsed : fallback;
        }

        private static void AddPathOverride(Dictionary<string, string> overrides, string key, string edited, PathSetting current)
        {
            if (!string.IsNullOrWhiteSpace(edited) && !SamePath(edited, current.AutoValue))
            {
                overrides[key] = edited.Trim();
            }
        }

        private static Dictionary<string, string> SafeRead(string path)
        {
            try
            {
                return KeyValueFile.Read(path);
            }
            catch (Exception)
            {
                // An unreadable file behaves like a missing one; the next layer takes over.
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static string DefaultUserFile()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "IssueReporterCD",
                "settings.ini");
        }

        private static string Normalize(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim().TrimEnd('\\', '/');
        }

        private static string Expand(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : Environment.ExpandEnvironmentVariables(value.Trim());
        }
    }
}
