using System;
using System.Collections.Generic;
using IssueReporterCD.Collectors;

namespace IssueReporterCD.Reporting
{
    public enum Severity
    {
        High,
        Medium,
        Low,
    }

    // Field values for the schema (docs/issue-schema.md). Unknown is written as null.

    public enum Frequency
    {
        Unknown,
        Always,
        Intermittent,
        Once,
    }

    /// <summary>Where the issue could be reproduced. "lab" is set later during triage, not on site.</summary>
    public enum Reproducible
    {
        Unknown,
        Site,
        None,
    }

    public enum OperatingMode
    {
        Unknown,
        Auto,
        Manual,
        Teaching,
        Simulation,
    }

    public enum LifecyclePhase
    {
        Unknown,
        Boot,
        Load,
        Run,
        Stop,
        Shutdown,
    }

    public sealed class CollectionSummary
    {
        public CollectionSummary(string name, CollectionStatus status, string detail)
        {
            Name = name;
            Status = status;
            Detail = detail;
        }

        public string Name { get; }

        public CollectionStatus Status { get; }

        public string Detail { get; }
    }

    /// <summary>
    /// One report, grouped by the schema layers in docs/issue-schema.md.
    /// </summary>
    public sealed class IssueReport
    {
        // ① Identification
        public string IssueId { get; set; }

        public DateTime CreatedAt { get; set; }

        public string ReporterVersion { get; set; }

        public string Author { get; set; }

        // ② Environment (human-entered parts; the rest is in Environment)
        public string EquipmentId { get; set; }

        public string Site { get; set; }

        public string Line { get; set; }

        public EnvironmentFingerprint Environment { get; set; }

        // ③ Symptom
        public string Title { get; set; }

        public string SymptomType { get; set; }

        public string Expected { get; set; }

        /// <summary>What actually happened (the symptom text, possibly from a template).</summary>
        public string Actual { get; set; }

        public string ReproSteps { get; set; }

        public Frequency Frequency { get; set; }

        public Reproducible Reproducible { get; set; }

        public string OccurredAt { get; set; }

        public Severity Severity { get; set; }

        public string ActionsTaken { get; set; }

        // ④ Location
        public OperatingMode OperatingMode { get; set; }

        public LifecyclePhase LifecyclePhase { get; set; }

        // ⑤ Evidence
        public IList<string> AlarmCodes { get; set; }

        public DateTime? NewestLogTime { get; set; }

        /// <summary>True when the engineer was warned about the log time gap and chose to continue.</summary>
        public bool LogGapWarningAcknowledged { get; set; }

        public IList<CollectionSummary> Collections { get; set; }
    }
}
