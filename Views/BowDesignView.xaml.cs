using System.Windows.Controls;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class BowDesignView : UserControl
    {
        public BowDesignView()
        {
            InitializeComponent();
            DataContext = new BowDesignViewModel();
        }
    }
}
