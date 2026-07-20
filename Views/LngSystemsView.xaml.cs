using System.Windows.Controls;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class LngSystemsView : UserControl
    {
        public LngSystemsView()
        {
            InitializeComponent();
            DataContext = new LngSystemsViewModel();
        }
    }
}
