using System.Windows;
using IssueReporterCD.ViewModels;

namespace IssueReporterCD.Views
{
    public partial class LogGapDialog : Window
    {
        public LogGapDialog(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        /// <summary>Closing the window counts as cancel.</summary>
        public LogGapChoice Choice { get; private set; } = LogGapChoice.Cancel;

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            Choice = LogGapChoice.OpenSettings;
            DialogResult = true;
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            Choice = LogGapChoice.Continue;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Choice = LogGapChoice.Cancel;
            DialogResult = false;
        }
    }
}
