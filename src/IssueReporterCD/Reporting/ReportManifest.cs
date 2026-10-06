using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using IssueReporterCD.Collectors;

namespace IssueReporterCD.Reporting
{
    /// <summary>
    /// Machine-readable report.json for the planned analysis service. Field catalog: docs/issue-schema.md.
    /// Bump SchemaVersion when fields are renamed or removed; adding fields is backward compatible.
    /// Reserved fields the reporter can't fill yet are written as null so the schema stays visible.
    /// </summary>
    [DataContract]
    public sealed class ReportManifest
    {
        public const string FileName = "report.json";
        public const int CurrentSchemaVersion = 2;

        [DataMember(Name = "schema_version", Order = 0)]
        public int SchemaVersion { get; set; }

        [DataMember(Name = "identification", Order = 1)]
        public IdentificationLayer Identification { get; set; }

        [DataMember(Name = "environment", Order = 2)]
        public EnvironmentLayer Environment { get; set; }

        [DataMember(Name = "symptom", Order = 3)]
        public SymptomLayer Symptom { get; set; }

        [DataMember(Name = "location", Order = 4)]
        public LocationLayer Location { get; set; }

        [DataMember(Name = "evidence", Order = 5)]
        public EvidenceLayer Evidence { get; set; }

        public static ReportManifest From(IssueReport report)
        {
            EnvironmentFingerprint env = report.Environment ?? new EnvironmentFingerprint();
            return new ReportManifest
            {
                SchemaVersion = CurrentSchemaVersion,
                Identification = new IdentificationLayer
                {
                    IssueId = report.IssueId,
                    ReportedAt = report.CreatedAt.ToString("o"),
                    Reporter = report.Author,
                    Source = "issue-reporter",
                    ReporterAppVersion = report.ReporterVersion,
                },
                Environment = new EnvironmentLayer
                {
                    PlatformVersion = env.GetAurora("platform_version"),
                    MachineSwVersion = env.GetAurora("machine_sw_version"),
                    ApiLevel = env.GetAurora("api_level"),
                    BuildConfig = env.GetAurora("build_config"),
                    Architecture = env.GetAurora("architecture") ?? env.ProcessArchitecture,
                    ModuleVersions = new Dictionary<string, string>(env.ModuleVersions),
                    Site = report.Site,
                    Line = string.IsNullOrWhiteSpace(report.Line) ? env.GetAurora("line") : report.Line,
                    MachineId = report.EquipmentId,
                    EquipmentModel = env.GetAurora("equipment_model"),
                    Hostname = env.MachineName,
                    OsVersion = env.OsVersion,
                    OsArchitecture = env.OsArchitecture,
                    ScreenResolution = env.ScreenResolution,
                    DataPath = env.GetAurora("data_path"),
                    ConfigHash = env.ConfigHash,
                    SequenceId = env.GetAurora("sequence_id"),
                    SequenceRevision = env.GetAurora("sequence_revision"),
                    RecipeId = env.GetAurora("recipe_id"),
                    RecipeRevision = env.GetAurora("recipe_revision"),
                    AuroraEnvironmentFile = new AuroraFileInfo
                    {
                        Path = env.AuroraFilePath,
                        Found = env.AuroraFileFound,
                        WrittenAt = env.AuroraFileWrittenAt.HasValue ? env.AuroraFileWrittenAt.Value.ToString("o") : null,
                    },
                    Extra = env.Aurora
                        .Where(kv => !EnvironmentLayer.KnownAuroraKeys.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                        .ToDictionary(kv => kv.Key, kv => kv.Value),
                },
                Symptom = new SymptomLayer
                {
                    Title = report.Title,
                    SymptomType = report.SymptomType,
                    Expected = report.Expected,
                    Actual = report.Actual,
                    ReproSteps = report.ReproSteps,
                    Frequency = ToValue(report.Frequency),
                    Reproducible = ToValue(report.Reproducible),
                    OccurredAt = report.OccurredAt,
                    Severity = report.Severity.ToString().ToLowerInvariant(),
                    ActionsTaken = report.ActionsTaken,
                },
                Location = new LocationLayer
                {
                    OperatingMode = report.OperatingMode == OperatingMode.Unknown ? null : report.OperatingMode.ToString(),
                    LifecyclePhase = report.LifecyclePhase == LifecyclePhase.Unknown ? null : report.LifecyclePhase.ToString(),
                },
                Evidence = new EvidenceLayer
                {
                    AlarmCodes = (report.AlarmCodes ?? new List<string>()).ToList(),
                    LogBundle = new List<string> { "aurora_logs/", "aurora_syserror/" },
                    Screenshot = new List<string> { "screenshots/" },
                    ConfigSnapshot = "aurora_config/",
                    NewestLogTime = report.NewestLogTime.HasValue ? report.NewestLogTime.Value.ToString("o") : null,
                    LogGapWarningAcknowledged = report.LogGapWarningAcknowledged,
                    Collections = (report.Collections ?? new List<CollectionSummary>())
                        .Select(c => new ManifestCollection { Name = c.Name, Status = c.Status.ToString().ToLowerInvariant(), Detail = c.Detail })
                        .ToList(),
                },
            };
        }

        public void WriteTo(string path)
        {
            var serializer = new DataContractJsonSerializer(
                typeof(ReportManifest),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });

            using (var stream = File.Create(path))
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true))
            {
                serializer.WriteObject(writer, this);
            }
        }

        private static string ToValue(Frequency value)
        {
            return value == Frequency.Unknown ? null : value.ToString().ToLowerInvariant();
        }

        private static string ToValue(Reproducible value)
        {
            return value == Reproducible.Unknown ? null : value.ToString().ToLowerInvariant();
        }
    }

    [DataContract]
    public sealed class IdentificationLayer
    {
        [DataMember(Name = "issue_id", Order = 0)]
        public string IssueId { get; set; }

        [DataMember(Name = "reported_at", Order = 1)]
        public string ReportedAt { get; set; }

        [DataMember(Name = "reporter", Order = 2)]
        public string Reporter { get; set; }

        [DataMember(Name = "source", Order = 3)]
        public string Source { get; set; }

        [DataMember(Name = "reporter_app_version", Order = 4)]
        public string ReporterAppVersion { get; set; }
    }

    [DataContract]
    public sealed class EnvironmentLayer
    {
        /// <summary>Aurora environment file keys mapped to named fields; anything else goes to "extra".</summary>
        public static readonly string[] KnownAuroraKeys =
        {
            "platform_version", "machine_sw_version", "api_level", "build_config", "architecture", "line",
            "equipment_model", "data_path", "sequence_id", "sequence_revision", "recipe_id", "recipe_revision",
        };

        [DataMember(Name = "platform_version", Order = 0)]
        public string PlatformVersion { get; set; }

        [DataMember(Name = "machine_sw_version", Order = 1)]
        public string MachineSwVersion { get; set; }

        [DataMember(Name = "api_level", Order = 2)]
        public string ApiLevel { get; set; }

        [DataMember(Name = "build_config", Order = 3)]
        public string BuildConfig { get; set; }

        [DataMember(Name = "architecture", Order = 4)]
        public string Architecture { get; set; }

        [DataMember(Name = "module_versions", Order = 5)]
        public Dictionary<string, string> ModuleVersions { get; set; }

        [DataMember(Name = "site", Order = 6)]
        public string Site { get; set; }

        [DataMember(Name = "line", Order = 7)]
        public string Line { get; set; }

        [DataMember(Name = "machine_id", Order = 8)]
        public string MachineId { get; set; }

        [DataMember(Name = "equipment_model", Order = 9)]
        public string EquipmentModel { get; set; }

        [DataMember(Name = "hostname", Order = 10)]
        public string Hostname { get; set; }

        [DataMember(Name = "os_version", Order = 11)]
        public string OsVersion { get; set; }

        [DataMember(Name = "os_architecture", Order = 12)]
        public string OsArchitecture { get; set; }

        [DataMember(Name = "screen_resolution", Order = 13)]
        public string ScreenResolution { get; set; }

        [DataMember(Name = "data_path", Order = 14)]
        public string DataPath { get; set; }

        [DataMember(Name = "config_hash", Order = 15)]
        public string ConfigHash { get; set; }

        [DataMember(Name = "sequence_id", Order = 16)]
        public string SequenceId { get; set; }

        [DataMember(Name = "sequence_revision", Order = 17)]
        public string SequenceRevision { get; set; }

        [DataMember(Name = "recipe_id", Order = 18)]
        public string RecipeId { get; set; }

        [DataMember(Name = "recipe_revision", Order = 19)]
        public string RecipeRevision { get; set; }

        [DataMember(Name = "aurora_environment_file", Order = 20)]
        public AuroraFileInfo AuroraEnvironmentFile { get; set; }

        /// <summary>Aurora keys not in the catalog yet, kept so nothing Aurora sends is lost.</summary>
        [DataMember(Name = "extra", Order = 21)]
        public Dictionary<string, string> Extra { get; set; }
    }

    [DataContract]
    public sealed class AuroraFileInfo
    {
        [DataMember(Name = "path", Order = 0)]
        public string Path { get; set; }

        [DataMember(Name = "found", Order = 1)]
        public bool Found { get; set; }

        [DataMember(Name = "written_at", Order = 2)]
        public string WrittenAt { get; set; }
    }

    [DataContract]
    public sealed class SymptomLayer
    {
        [DataMember(Name = "title", Order = 0)]
        public string Title { get; set; }

        [DataMember(Name = "symptom_type", Order = 1)]
        public string SymptomType { get; set; }

        [DataMember(Name = "expected", Order = 2)]
        public string Expected { get; set; }

        [DataMember(Name = "actual", Order = 3)]
        public string Actual { get; set; }

        [DataMember(Name = "repro_steps", Order = 4)]
        public string ReproSteps { get; set; }

        [DataMember(Name = "frequency", Order = 5)]
        public string Frequency { get; set; }

        [DataMember(Name = "reproducible", Order = 6)]
        public string Reproducible { get; set; }

        [DataMember(Name = "occurred_at", Order = 7)]
        public string OccurredAt { get; set; }

        [DataMember(Name = "severity", Order = 8)]
        public string Severity { get; set; }

        [DataMember(Name = "actions_taken", Order = 9)]
        public string ActionsTaken { get; set; }
    }

    /// <summary>Where in Aurora's model the issue happened. Station and below are reserved for Aurora runtime.</summary>
    [DataContract]
    public sealed class LocationLayer
    {
        [DataMember(Name = "operating_mode", Order = 0)]
        public string OperatingMode { get; set; }

        [DataMember(Name = "lifecycle_phase", Order = 1)]
        public string LifecyclePhase { get; set; }

        [DataMember(Name = "station", Order = 2)]
        public string Station { get; set; }

        [DataMember(Name = "substation", Order = 3)]
        public string Substation { get; set; }

        [DataMember(Name = "motion_device", Order = 4)]
        public string MotionDevice { get; set; }

        [DataMember(Name = "axis", Order = 5)]
        public string Axis { get; set; }

        [DataMember(Name = "step_id", Order = 6)]
        public string StepId { get; set; }

        [DataMember(Name = "transition_id", Order = 7)]
        public string TransitionId { get; set; }
    }

    /// <summary>Attachments and machine evidence. Exception fields are reserved for Aurora runtime.</summary>
    [DataContract]
    public sealed class EvidenceLayer
    {
        [DataMember(Name = "alarm_codes", Order = 0)]
        public List<string> AlarmCodes { get; set; }

        [DataMember(Name = "exception_type", Order = 1)]
        public string ExceptionType { get; set; }

        [DataMember(Name = "message", Order = 2)]
        public string Message { get; set; }

        [DataMember(Name = "stack_hash", Order = 3)]
        public string StackHash { get; set; }

        [DataMember(Name = "log_bundle", Order = 4)]
        public List<string> LogBundle { get; set; }

        [DataMember(Name = "screenshot", Order = 5)]
        public List<string> Screenshot { get; set; }

        [DataMember(Name = "config_snapshot", Order = 6)]
        public string ConfigSnapshot { get; set; }

        [DataMember(Name = "newest_log_time", Order = 7)]
        public string NewestLogTime { get; set; }

        [DataMember(Name = "log_gap_warning_acknowledged", Order = 8)]
        public bool LogGapWarningAcknowledged { get; set; }

        [DataMember(Name = "collections", Order = 9)]
        public List<ManifestCollection> Collections { get; set; }
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
