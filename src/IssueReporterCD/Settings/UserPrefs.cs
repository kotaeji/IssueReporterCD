using System;
using System.Collections.Generic;
using System.IO;
using IssueReporterCD.Infrastructure;

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

        public string Line { get; set; }

        public static UserPrefs Load()
        {
            var prefs = new UserPrefs();
            try
            {
                Dictionary<string, string> values = KeyValueFile.Read(FilePath);
                string value;
                if (values.TryGetValue("EquipmentId", out value)) prefs.EquipmentId = value;
                if (values.TryGetValue("Site", out value)) prefs.Site = value;
                if (values.TryGetValue("Author", out value)) prefs.Author = value;
                if (values.TryGetValue("Line", out value)) prefs.Line = value;
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
                KeyValueFile.Write(FilePath, new Dictionary<string, string>
                {
                    { "EquipmentId", EquipmentId },
                    { "Site", Site },
                    { "Author", Author },
                    { "Line", Line },
                });
            }
            catch (Exception)
            {
                // Not worth interrupting the report for.
            }
        }
    }
}
