namespace IssueReporterCD.Reporting
{
    /// <summary>
    /// Korean labels for schema choice fields, shared by the form and issue_report.txt.
    /// </summary>
    public static class FieldLabels
    {
        public const string Unknown = "모름";

        public static string Of(Frequency value)
        {
            switch (value)
            {
                case Frequency.Always: return "항상";
                case Frequency.Intermittent: return "간헐적";
                case Frequency.Once: return "1회";
                default: return Unknown;
            }
        }

        public static string Of(Reproducible value)
        {
            switch (value)
            {
                case Reproducible.Site: return "현장에서 재현됨";
                case Reproducible.None: return "재현 안 됨";
                default: return Unknown;
            }
        }

        public static string Of(OperatingMode value)
        {
            return value == OperatingMode.Unknown ? Unknown : value.ToString();
        }

        public static string Of(LifecyclePhase value)
        {
            return value == LifecyclePhase.Unknown ? Unknown : value.ToString();
        }

        public static string Of(Severity value)
        {
            switch (value)
            {
                case Severity.High: return "상 (라인 정지)";
                case Severity.Low: return "하";
                default: return "중";
            }
        }
    }
}
