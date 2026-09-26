using System.Windows;
using IssueReporterCD.Settings;
using IssueReporterCD.ViewModels;

namespace IssueReporterCD.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow(SettingsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        public SettingsEdits Edits { get; private set; }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SettingsEdits edits;
            if (_viewModel.TryGetEdits(out edits))
            {
                Edits = edits;
                DialogResult = true;
            }
        }
    }
}
