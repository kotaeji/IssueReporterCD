namespace IssueReporterCD.Settings
{
    /// <summary>
    /// Where a setting's value came from, in priority order: User &gt; Aurora &gt; Default.
    /// </summary>
    public enum SettingSource
    {
        Default,
        Aurora,
        User,
    }

    public sealed class PathSetting
    {
        public PathSetting(string value, SettingSource source, string autoValue, SettingSource autoSource)
        {
            Value = value;
            Source = source;
            AutoValue = autoValue;
            AutoSource = autoSource;
        }

        /// <summary>The path actually used.</summary>
        public string Value { get; }

        public SettingSource Source { get; }

        /// <summary>The path that would be used without a user override.</summary>
        public string AutoValue { get; }

        public SettingSource AutoSource { get; }
    }

    /// <summary>
    /// Effective settings after merging user overrides, the Aurora discovery file, and App.config defaults.
    /// </summary>
    public sealed class ReporterSettings
    {
        public PathSetting LogDir { get; set; }

        public PathSetting SysErrorDir { get; set; }

        public PathSetting ConfigDir { get; set; }

        public PathSetting OutputDir { get; set; }

        public int LogMaxAgeDays { get; set; }

        public int LogGapWarningHours { get; set; }

        /// <summary>Where user overrides are stored, shown in the settings window.</summary>
        public string UserSettingsFile { get; set; }

        public string AuroraDiscoveryFile { get; set; }

        /// <summary>Environment fingerprint file written by Aurora / Boot Loader (docs/aurora-integration.md).</summary>
        public string AuroraEnvironmentFile { get; set; }
    }

    /// <summary>
    /// Values entered in the settings window.
    /// </summary>
    public sealed class SettingsEdits
    {
        public string LogDir { get; set; }

        public string SysErrorDir { get; set; }

        public string ConfigDir { get; set; }

        public string OutputDir { get; set; }

        public int LogMaxAgeDays { get; set; }

        public int LogGapWarningHours { get; set; }
    }
}
