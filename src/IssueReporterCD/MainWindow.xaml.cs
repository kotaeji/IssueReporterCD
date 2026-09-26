using System.Windows;
using IssueReporterCD.ViewModels;

namespace IssueReporterCD
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                viewModel.StartCollection();
            }
        }
    }
}
