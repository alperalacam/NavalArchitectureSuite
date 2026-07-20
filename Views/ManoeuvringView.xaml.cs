using System.Windows.Controls;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class ManoeuvringView : UserControl
    {
        public ManoeuvringView()
        {
            InitializeComponent();
            DataContext = new ManoeuvringViewModel();
        }
    }
}
