using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

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
            var sb = new StringBuilder();
            sb.AppendLine("==== Aurora Issue Report ====");
            sb.AppendLine("생성 시각 (Created)  : " + report.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss zzz"));
            sb.AppendLine("장비 ID (Equipment)  : " + report.EquipmentId);
            sb.AppendLine("사이트 (Site)        : " + report.Site);
            sb.AppendLine("작성자 (Author)      : " + report.Author);
            sb.AppendLine("발생 시각 (Occurred) : " + report.OccurredAt);
            sb.AppendLine("긴급도 (Severity)    : " + FormatSeverity(report.Severity));
            sb.AppendLine("증상 유형 (Type)     : " + report.SymptomType);
            sb.AppendLine("최근 로그 (Last log) : " + FormatLogCheck(report));
            sb.AppendLine();
            AppendSection(sb, "증상 (Symptom)", report.Symptom);
            AppendSection(sb, "재현 절차 (Repro steps)", report.ReproSteps);
            AppendSection(sb, "조치 내용 (Actions taken)", report.ActionsTaken);

            sb.AppendLine("■ 수집 데이터 (Collected data)");
            foreach (var item in report.Collections ?? Enumerable.Empty<CollectionSummary>())
            {
                sb.AppendLine(string.Format("- {0}: {1} ({2})", item.Name, item.Status, item.Detail));
            }
            return sb.ToString();
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

        private static string FormatSeverity(Severity severity)
        {
            switch (severity)
            {
                case Severity.High:
                    return "상 (High)";
                case Severity.Low:
                    return "하 (Low)";
                default:
                    return "중 (Medium)";
            }
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
