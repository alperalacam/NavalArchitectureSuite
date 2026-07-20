using System.Windows.Controls;
using NavalArchitectureSuite.Models;

namespace NavalArchitectureSuite.Views
{
    public partial class ModulePlaceholderView : UserControl
    {
        public ModulePlaceholderView(NavItem item)
        {
            InitializeComponent();
            DataContext = item;
        }
    }
}
