using System.Windows;
using System.Windows.Controls;
using NavalArchitectureSuite.Models;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class StabilityView : UserControl
    {
        private StabilityViewModel ViewModel => (StabilityViewModel)DataContext;

        public StabilityView()
        {
            InitializeComponent();
            DataContext = new StabilityViewModel();
        }

        private void AddCondition_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddCondition();
        }

        private void RemoveCondition_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: LoadingCondition condition })
            {
                ViewModel.RemoveCondition(condition);
            }
        }
    }
}
