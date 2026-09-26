using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace IssueReporterCD.Reporting
{
    /// <summary>
    /// Machine-readable report.json for the planned analysis service.
    /// Bump SchemaVersion when fields are renamed or removed; adding fields is backward compatible.
    /// </summary>
    [DataContract]
    public sealed class ReportManifest
    {
        public const string FileName = "report.json";

        [DataMember(Name = "schemaVersion", Order = 0)]
        public int SchemaVersion { get; set; }

        [DataMember(Name = "reporterVersion", Order = 1)]
        public string ReporterVersion { get; set; }

        [DataMember(Name = "createdAt", Order = 2)]
        public string CreatedAt { get; set; }

        [DataMember(Name = "equipmentId", Order = 3)]
        public string EquipmentId { get; set; }

        [DataMember(Name = "site", Order = 4)]
        public string Site { get; set; }

        [DataMember(Name = "author", Order = 5)]
        public string Author { get; set; }

        [DataMember(Name = "occurredAt", Order = 6)]
        public string OccurredAt { get; set; }

        [DataMember(Name = "severity", Order = 7)]
        public string Severity { get; set; }

        [DataMember(Name = "symptomType", Order = 8)]
        public string SymptomType { get; set; }

        [DataMember(Name = "symptom", Order = 9)]
        public string Symptom { get; set; }

        [DataMember(Name = "reproSteps", Order = 10)]
        public string ReproSteps { get; set; }

        [DataMember(Name = "actionsTaken", Order = 11)]
        public string ActionsTaken { get; set; }

        [DataMember(Name = "newestLogTime", Order = 12)]
        public string NewestLogTime { get; set; }

        [DataMember(Name = "logGapWarningAcknowledged", Order = 13)]
        public bool LogGapWarningAcknowledged { get; set; }

        [DataMember(Name = "collections", Order = 14)]
        public List<ManifestCollection> Collections { get; set; }

        public static ReportManifest From(IssueReport report)
        {
            return new ReportManifest
            {
                SchemaVersion = 1,
                ReporterVersion = report.ReporterVersion,
                CreatedAt = report.CreatedAt.ToString("o"),
                EquipmentId = report.EquipmentId,
                Site = report.Site,
                Author = report.Author,
                OccurredAt = report.OccurredAt,
                Severity = report.Severity.ToString(),
                SymptomType = report.SymptomType,
                Symptom = report.Symptom,
                ReproSteps = report.ReproSteps,
                ActionsTaken = report.ActionsTaken,
                NewestLogTime = report.NewestLogTime.HasValue ? report.NewestLogTime.Value.ToString("o") : null,
                LogGapWarningAcknowledged = report.LogGapWarningAcknowledged,
                Collections = (report.Collections ?? new List<CollectionSummary>())
                    .Select(c => new ManifestCollection { Name = c.Name, Status = c.Status.ToString(), Detail = c.Detail })
                    .ToList(),
            };
        }

        public void WriteTo(string path)
        {
            using (var stream = File.Create(path))
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true))
            {
                new DataContractJsonSerializer(typeof(ReportManifest)).WriteObject(writer, this);
            }
        }
    }

    [DataContract]
    public sealed class ManifestCollection
    {
        [DataMember(Name = "name", Order = 0)]
        public string Name { get; set; }

        [DataMember(Name = "status", Order = 1)]
        public string Status { get; set; }

        [DataMember(Name = "detail", Order = 2)]
        public string Detail { get; set; }
    }
}
