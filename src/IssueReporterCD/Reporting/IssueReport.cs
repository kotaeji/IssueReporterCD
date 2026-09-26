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
    /// What the field engineer entered, plus a summary of the collected data.
    /// </summary>
    public sealed class IssueReport
    {
        public DateTime CreatedAt { get; set; }

        public string EquipmentId { get; set; }

        public string Site { get; set; }

        public string Author { get; set; }

        public string OccurredAt { get; set; }

        public Severity Severity { get; set; }

        public string Symptom { get; set; }

        public string ReproSteps { get; set; }

        public string ActionsTaken { get; set; }

        public IList<CollectionSummary> Collections { get; set; }
    }
}
