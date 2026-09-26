using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using IssueReporterCD.Collectors;
using IssueReporterCD.Reporting;
using IssueReporterCD.Settings;
using IssueReporterCD.ViewModels;

namespace IssueReporterCD
{
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

            var settings = AppSettings.Load();
            var collectors = new List<ICollector>
            {
                new ScreenshotCollector(_session.ScreenshotDir, screenshotError),
                new SystemInfoCollector(),
                new DirectoryCollector("Aurora 로그", settings.AuroraLogDir, "aurora_logs", settings.LogMaxAgeDays),
                new DirectoryCollector("Aurora 설정", settings.AuroraConfigDir, "aurora_config", 0),
            };

            var viewModel = new MainViewModel(_session, collectors, settings, UserPrefs.Load());
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
