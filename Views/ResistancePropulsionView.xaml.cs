using System.Windows.Controls;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class ResistancePropulsionView : UserControl
    {
        public ResistancePropulsionView()
        {
            InitializeComponent();
            DataContext = new ResistancePropulsionViewModel();
        }
    }
}
