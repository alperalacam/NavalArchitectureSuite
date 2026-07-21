using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class ShipBuilderView : UserControl
    {
        public ShipBuilderView()
        {
            InitializeComponent();
            DataContext = ShipBuilderViewModel.Instance;

            Loaded += (_, _) => ZoomAfterDelay(150);

            IsVisibleChanged += (_, e) =>
            {
                if ((bool)e.NewValue)
                    ZoomAfterDelay(150);
            };
        }

        // ── View buttons ─────────────────────────────────────────────────────

        private void ResetViewButton_Click(object sender, RoutedEventArgs e)
        {
            double lpp = ShipBuilderViewModel.Instance.Lpp;
            double dist = lpp * 2.0;
            HullViewport.SetView(
                new Point3D(-dist * 0.5, -dist, dist * 0.5),
                new Vector3D(dist * 0.5, dist, -dist * 0.5),
                new Vector3D(0, 0, 1), 0);
            ZoomAfterDelay(50);
        }

        private void TopViewButton_Click(object sender, RoutedEventArgs e)
        {
            double lpp = ShipBuilderViewModel.Instance.Lpp;
            HullViewport.SetView(
                new Point3D(lpp / 2, 0, lpp * 2),
                new Vector3D(0, 0, -1),
                new Vector3D(1, 0, 0), 0);
            ZoomAfterDelay(50);
        }

        private void SideViewButton_Click(object sender, RoutedEventArgs e)
        {
            double lpp = ShipBuilderViewModel.Instance.Lpp;
            HullViewport.SetView(
                new Point3D(lpp / 2, -lpp * 2, 0),
                new Vector3D(0, 1, 0),
                new Vector3D(0, 0, 1), 0);
            ZoomAfterDelay(50);
        }

        private void FrontViewButton_Click(object sender, RoutedEventArgs e)
        {
            double breadth = ShipBuilderViewModel.Instance.Breadth;
            HullViewport.SetView(
                new Point3D(breadth * 20, 0, 0),
                new Vector3D(-1, 0, 0),
                new Vector3D(0, 0, 1), 0);
            ZoomAfterDelay(50);
        }

        private void FitToScreenButton_Click(object sender, RoutedEventArgs e) =>
            HullViewport.ZoomExtents(400);

        // ── Pop-out ───────────────────────────────────────────────────────────

        private HullViewportWindow? _popOutWindow;

        private void PopOutViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (_popOutWindow is not null && _popOutWindow.IsVisible)
            {
                _popOutWindow.Activate();
                return;
            }
            _popOutWindow = new HullViewportWindow();
            _popOutWindow.Owner = Window.GetWindow(this);
            _popOutWindow.Show();
        }

        private void UprightButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ShipBuilderViewModel vm)
                vm.HeelAngle = 0.0;
        }

        // ── Zoom helper ───────────────────────────────────────────────────────

        /// <summary>
        /// Fires ZoomExtents after a short delay so HelixToolkit's visual tree
        /// has finished updating after a SetView or layout change.
        /// </summary>
        private void ZoomAfterDelay(int milliseconds)
        {
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(milliseconds)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                HullViewport.ZoomExtents(0);
            };
            timer.Start();
        }
    }
}
