using System;
using System.IO;
using System.Text;

namespace IssueReporterCD.Settings
{
    /// <summary>
    /// Remembers the last-entered basic info so the engineer doesn't retype it each time.
    /// Stored as key=value lines in %LOCALAPPDATA%\IssueReporterCD\prefs.txt.
    /// </summary>
    public sealed class UserPrefs
    {
        private static string FilePath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "IssueReporterCD",
                    "prefs.txt");
            }
        }

        public string EquipmentId { get; set; }

        public string Site { get; set; }

        public string Author { get; set; }

        public static UserPrefs Load()
        {
            var prefs = new UserPrefs();
            try
            {
                if (!File.Exists(FilePath))
                {
                    return prefs;
                }

                foreach (string line in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    int separator = line.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, separator);
                    string value = line.Substring(separator + 1);
                    switch (key)
                    {
                        case "EquipmentId":
                            prefs.EquipmentId = value;
                            break;
                        case "Site":
                            prefs.Site = value;
                            break;
                        case "Author":
                            prefs.Author = value;
                            break;
                    }
                }
            }
            catch (Exception)
            {
                // Preferences are a convenience; start empty if they can't be read.
            }
            return prefs;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllLines(FilePath, new[]
                {
                    "EquipmentId=" + EquipmentId,
                    "Site=" + Site,
                    "Author=" + Author,
                }, Encoding.UTF8);
            }
            catch (Exception)
            {
                // Not worth interrupting the report for.
            }
        }
    }
}
