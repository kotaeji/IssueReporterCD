using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IssueReporterCD.Infrastructure
{
    /// <summary>
    /// Reads and writes simple "key=value" text files. Blank lines and lines starting with '#' are ignored.
    /// </summary>
    public static class KeyValueFile
    {
        public static Dictionary<string, string> Read(string path)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return values;
            }

            foreach (string rawLine in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                int separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                values[line.Substring(0, separator).Trim()] = line.Substring(separator + 1).Trim();
            }
            return values;
        }

        public static void Write(string path, IEnumerable<KeyValuePair<string, string>> values)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path, values.Select(v => v.Key + "=" + v.Value), new UTF8Encoding(true));
        }
    }
}
