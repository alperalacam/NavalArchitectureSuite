using System.Windows;
using System.Windows.Controls;
using NavalArchitectureSuite.Models;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class DamageStabilityView : UserControl
    {
        private DamageStabilityViewModel ViewModel => (DamageStabilityViewModel)DataContext;

        public DamageStabilityView()
        {
            InitializeComponent();
            DataContext = new DamageStabilityViewModel();
        }

        private void AddCase_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddCase();
        }

        private void RemoveCase_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: DamageCase damageCase })
            {
                ViewModel.RemoveCase(damageCase);
            }
        }
    }
}
