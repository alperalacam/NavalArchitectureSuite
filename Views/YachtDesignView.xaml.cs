using System.Windows.Controls;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class YachtDesignView : UserControl
    {
        private YachtDesignViewModel ViewModel => (YachtDesignViewModel)DataContext;

        public YachtDesignView()
        {
            InitializeComponent();
            DataContext = new YachtDesignViewModel();
        }
    }
}
