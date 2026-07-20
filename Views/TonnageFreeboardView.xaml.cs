using System.Windows.Controls;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class TonnageFreeboardView : UserControl
    {
        public TonnageFreeboardView()
        {
            InitializeComponent();
            DataContext = new TonnageFreeboardViewModel();
        }
    }
}
