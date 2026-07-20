using System.Windows.Controls;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class HydrostaticsView : UserControl
    {
        public HydrostaticsView()
        {
            InitializeComponent();
            DataContext = new HydrostaticsViewModel();
        }
    }
}
