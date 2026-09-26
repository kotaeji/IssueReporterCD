using System.Globalization;

namespace IssueReporterCD.Infrastructure
{
    public static class SizeFormatter
    {
        public static string Format(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return size.ToString(unit == 0 ? "0" : "0.#", CultureInfo.InvariantCulture) + " " + units[unit];
        }
    }
}
