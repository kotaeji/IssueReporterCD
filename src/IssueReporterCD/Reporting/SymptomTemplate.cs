using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IssueReporterCD.Reporting
{
    /// <summary>
    /// A symptom type with prompts the engineer fills in. Loaded from SymptomTemplates\*.txt next to the exe,
    /// so templates can be tuned on site without rebuilding. "NN_" file name prefixes set the order.
    /// </summary>
    public sealed class SymptomTemplate
    {
        public const string FreeFormName = "직접 입력";

        public SymptomTemplate(string name, string body)
        {
            Name = name;
            Body = body;
        }

        public string Name { get; }

        public string Body { get; }

        public override string ToString()
        {
            return Name;
        }

        public static IReadOnlyList<SymptomTemplate> LoadAll(string folder)
        {
            var templates = new List<SymptomTemplate> { new SymptomTemplate(FreeFormName, string.Empty) };
            try
            {
                if (Directory.Exists(folder))
                {
                    templates.AddRange(Directory.GetFiles(folder, "*.txt")
                        .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                        .Select(f => new SymptomTemplate(
                            Regex.Replace(Path.GetFileNameWithoutExtension(f), @"^\d+_", string.Empty),
                            File.ReadAllText(f, Encoding.UTF8).TrimEnd())));
                }
            }
            catch (Exception)
            {
                // Without templates the engineer can still type freely.
            }
            return templates;
        }
    }
}
