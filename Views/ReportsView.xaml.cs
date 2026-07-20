using System.Windows;
using System.Windows.Controls;
using NavalArchitectureSuite.Models;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class ReportsView : UserControl
    {
        private ReportsViewModel ViewModel => (ReportsViewModel)DataContext;

        public ReportsView()
        {
            InitializeComponent();
            DataContext = new ReportsViewModel();
        }

        private void AddEntry_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddEntry();
        }

        private void RemoveEntry_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: ReportEntry entry })
            {
                ViewModel.RemoveEntry(entry);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ExportMarkdown();
        }
    }
}
