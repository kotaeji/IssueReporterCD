using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using IssueReporterCD.Collectors;

namespace IssueReporterCD.Reporting
{
    public static class ReportPackager
    {
        public const string DescriptionFileName = "issue_report.txt";

        /// <summary>
        /// Writes issue_report.txt and report.json into the session folder and zips the whole folder.
        /// </summary>
        /// <returns>Full path of the created zip, named yyyyMMdd_HHmmss_{EquipmentId}.zip.</returns>
        public static string Package(ReportSession session, IssueReport report, string outputDir)
        {
            File.WriteAllText(
                Path.Combine(session.WorkDir, DescriptionFileName),
                FormatDescription(report),
                new UTF8Encoding(true));
            ReportManifest.From(report).WriteTo(Path.Combine(session.WorkDir, ReportManifest.FileName));

            Directory.CreateDirectory(outputDir);
            string baseName = report.CreatedAt.ToString("yyyyMMdd_HHmmss") + "_" + SanitizeFileName(report.EquipmentId);
            string zipPath = Path.Combine(outputDir, baseName + ".zip");
            for (int i = 2; File.Exists(zipPath); i++)
            {
                zipPath = Path.Combine(outputDir, baseName + "_" + i + ".zip");
            }

            // Optimal is the strongest level the built-in .zip (Deflate) supports on .NET Framework.
            ZipFile.CreateFromDirectory(session.WorkDir, zipPath, CompressionLevel.Optimal, false);
            return zipPath;
        }

        public static string FormatDescription(IssueReport report)
        {
            EnvironmentFingerprint env = report.Environment ?? new EnvironmentFingerprint();
            var sb = new StringBuilder();
            sb.AppendLine("==== Aurora Issue Report ====");
            sb.AppendLine("리포트 ID (Issue ID)    : " + report.IssueId);
            sb.AppendLine("생성 시각 (Reported at) : " + report.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss zzz"));
            sb.AppendLine("작성자 (Reporter)       : " + report.Author);
            sb.AppendLine();

            sb.AppendLine("■ 제목 (Title)");
            sb.AppendLine(Value(report.Title));
            sb.AppendLine();

            sb.AppendLine("■ 개요 (Summary)");
            sb.AppendLine("발생 시각 (Occurred at)   : " + report.OccurredAt);
            sb.AppendLine("긴급도 (Severity)         : " + FieldLabels.Of(report.Severity) + " (" + report.Severity + ")");
            sb.AppendLine("빈도 (Frequency)          : " + FieldLabels.Of(report.Frequency));
            sb.AppendLine("재현 (Reproducible)       : " + FieldLabels.Of(report.Reproducible));
            sb.AppendLine("운전 모드 (Operating mode): " + FieldLabels.Of(report.OperatingMode));
            sb.AppendLine("단계 (Lifecycle phase)    : " + FieldLabels.Of(report.LifecyclePhase));
            sb.AppendLine("증상 유형 (Symptom type)  : " + report.SymptomType);
            sb.AppendLine("알람 코드 (Alarm codes)   : " + (report.AlarmCodes != null && report.AlarmCodes.Count > 0 ? string.Join(", ", report.AlarmCodes) : "(없음)"));
            sb.AppendLine();

            AppendSection(sb, "실제 동작 / 증상 (Actual)", report.Actual);
            AppendSection(sb, "기대 동작 (Expected)", report.Expected);
            AppendSection(sb, "재현 절차 (Repro steps)", report.ReproSteps);
            AppendSection(sb, "조치 내용 (Actions taken)", report.ActionsTaken);

            sb.AppendLine("■ 환경 (Environment)");
            sb.AppendLine("장비 ID (Machine ID)      : " + report.EquipmentId);
            sb.AppendLine("사이트 / 라인 (Site/Line) : " + report.Site + " / " + Value(string.IsNullOrWhiteSpace(report.Line) ? env.GetAurora("line") : report.Line));
            sb.AppendLine("플랫폼 버전 (Platform)    : " + Value(env.GetAurora("platform_version")));
            sb.AppendLine("장비 SW 버전 (Machine SW) : " + Value(env.GetAurora("machine_sw_version")));
            sb.AppendLine("API Level                 : " + Value(env.GetAurora("api_level")));
            sb.AppendLine("빌드 구성 (Build config)  : " + Value(env.GetAurora("build_config")));
            sb.AppendLine("장비 모델 (Model)         : " + Value(env.GetAurora("equipment_model")));
            sb.AppendLine("시퀀스 (Sequence)         : " + Value(env.GetAurora("sequence_id")) + " rev " + Value(env.GetAurora("sequence_revision")));
            sb.AppendLine("레시피 (Recipe)           : " + Value(env.GetAurora("recipe_id")) + " rev " + Value(env.GetAurora("recipe_revision")));
            sb.AppendLine("DataPath                  : " + Value(env.GetAurora("data_path")));
            sb.AppendLine("설정 해시 (Config hash)   : " + Value(env.ConfigHash) + (env.ConfigHash != null ? " (" + env.ConfigFileCount + " files)" : string.Empty));
            sb.AppendLine("OS                        : " + env.OsVersion + " / " + env.OsArchitecture);
            sb.AppendLine("화면 (Screens)            : " + Value(env.ScreenResolution));
            foreach (var module in env.ModuleVersions.OrderBy(m => m.Key))
            {
                sb.AppendLine("모듈 (Module) " + module.Key + " : " + module.Value);
            }
            sb.AppendLine("Aurora 정보 파일          : " + (env.AuroraFileFound
                ? env.AuroraFilePath + " (" + (env.AuroraFileWrittenAt.HasValue ? env.AuroraFileWrittenAt.Value.ToString("yyyy-MM-dd HH:mm") : "?") + " 기록)"
                : "없음 (" + env.AuroraFilePath + ")"));
            sb.AppendLine();

            sb.AppendLine("■ 수집 데이터 (Collected data)");
            sb.AppendLine("최근 로그 (Last log): " + FormatLogCheck(report));
            foreach (var item in report.Collections ?? Enumerable.Empty<CollectionSummary>())
            {
                sb.AppendLine(string.Format("- {0}: {1} ({2})", item.Name, item.Status, item.Detail));
            }
            return sb.ToString();
        }

        private static string Value(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(없음)" : value.Trim();
        }

        private static string FormatLogCheck(IssueReport report)
        {
            string text = report.NewestLogTime.HasValue
                ? report.NewestLogTime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                : "(찾지 못함)";
            return report.LogGapWarningAcknowledged ? text + " ※ 시간 차이 경고 확인 후 진행" : text;
        }

        private static void AppendSection(StringBuilder sb, string title, string body)
        {
            sb.AppendLine("■ " + title);
            sb.AppendLine(string.IsNullOrWhiteSpace(body) ? "(없음)" : body.Trim());
            sb.AppendLine();
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "UNKNOWN";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (char c in value.Trim())
            {
                sb.Append(invalid.Contains(c) || char.IsWhiteSpace(c) ? '-' : c);
            }
            return sb.ToString();
        }
    }
}
