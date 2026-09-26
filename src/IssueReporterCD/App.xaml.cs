using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using IssueReporterCD.Collectors;
using IssueReporterCD.Reporting;
using IssueReporterCD.Settings;
using IssueReporterCD.ViewModels;
using IssueReporterCD.Views;

namespace IssueReporterCD
{
    /// <summary>
    /// Runs as its own process, independent of Aurora, so it keeps working when Aurora has crashed or hung.
    /// It only reads Aurora's files; nothing here loads Aurora code or talks to the Aurora process.
    /// </summary>
    public partial class App : Application
    {
        private ReportSession _session;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            _session = ReportSession.Create();

            // Capture the screen before our window appears so it shows the equipment state as-is.
            string screenshotError = null;
            try
            {
                ScreenCapture.CaptureVirtualScreen(Path.Combine(_session.ScreenshotDir, "screen_at_launch.png"));
            }
            catch (Exception ex)
            {
                screenshotError = ex.Message;
            }

            var settingsService = new SettingsService();
            string templateDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SymptomTemplates");

            var viewModel = new MainViewModel(
                _session,
                settingsService.Load(),
                s => CollectorFactory.Create(s, _session.ScreenshotDir, screenshotError),
                SymptomTemplate.LoadAll(templateDir),
                new DialogService(settingsService),
                UserPrefs.Load());

            var window = new MainWindow { DataContext = viewModel };
            MainWindow = window;
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_session != null)
            {
                _session.CleanupIfReported();
            }
            base.OnExit(e);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                "예기치 않은 오류가 발생했습니다.\n\n" + e.Exception.Message,
                "Aurora Issue Reporter",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
