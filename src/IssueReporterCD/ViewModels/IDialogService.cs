using IssueReporterCD.Settings;

namespace IssueReporterCD.ViewModels
{
    public enum LogGapChoice
    {
        OpenSettings,
        Continue,
        Cancel,
    }

    /// <summary>
    /// Windows and message boxes the view models need, kept behind an interface so the
    /// view models don't depend on WPF windows (and can be tested with a fake).
    /// </summary>
    public interface IDialogService
    {
        /// <returns>The saved settings, or null if the user cancelled.</returns>
        ReporterSettings ShowSettings(ReporterSettings current);

        LogGapChoice AskLogGap(string message);

        bool Confirm(string message);
    }
}
