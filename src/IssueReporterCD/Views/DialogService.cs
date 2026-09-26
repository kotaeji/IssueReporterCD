using System;
using System.IO;
using System.Windows;
using IssueReporterCD.Settings;
using IssueReporterCD.ViewModels;
using WinForms = System.Windows.Forms;

namespace IssueReporterCD.Views
{
    public sealed class DialogService : IDialogService
    {
        private const string Caption = "Aurora Issue Reporter";
        private readonly SettingsService _settingsService;

        public DialogService(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public ReporterSettings ShowSettings(ReporterSettings current)
        {
            var window = new SettingsWindow(new SettingsViewModel(current, PickFolder)) { Owner = Application.Current.MainWindow };
            if (window.ShowDialog() != true)
            {
                return null;
            }

            try
            {
                return _settingsService.Save(window.Edits);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "설정을 저장하지 못했습니다.\n" + current.UserSettingsFile + "\n\n" + ex.Message,
                    Caption, MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public LogGapChoice AskLogGap(string message)
        {
            var dialog = new LogGapDialog(message) { Owner = Application.Current.MainWindow };
            dialog.ShowDialog();
            return dialog.Choice;
        }

        public bool Confirm(string message)
        {
            return MessageBox.Show(message, Caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        private static string PickFolder(string initialPath)
        {
            using (var dialog = new WinForms.FolderBrowserDialog())
            {
                dialog.ShowNewFolderButton = true;
                if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                {
                    dialog.SelectedPath = initialPath;
                }
                return dialog.ShowDialog() == WinForms.DialogResult.OK ? dialog.SelectedPath : null;
            }
        }
    }
}
