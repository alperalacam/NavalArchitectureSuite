using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using NavalArchitectureSuite.Services;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class HullViewportWindow : Window
    {
        public HullViewportWindow()
        {
            InitializeComponent();

            // Restore previous size and position if available.
            var saved = HullViewportSettingsService.Load();
            if (saved != null)
            {
                Width  = saved.Width;
                Height = saved.Height;
                Left   = saved.Left;
                Top    = saved.Top;
                WindowStartupLocation = WindowStartupLocation.Manual;
            }

            // Save size and position on close.
            Closing += (_, _) =>
            {
                HullViewportSettingsService.Save(new WindowSettings
                {
                    Width  = ActualWidth,
                    Height = ActualHeight,
                    Left   = Left,
                    Top    = Top
                });
            };

            var vm = ShipBuilderViewModel.Instance;
            DataContext = vm;

            UpdateVesselInfo(vm);
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(ShipBuilderViewModel.Lpp)
                                   or nameof(ShipBuilderViewModel.Breadth)
                                   or nameof(ShipBuilderViewModel.Displacement)
                                   or nameof(ShipBuilderViewModel.VesselType))
                    UpdateVesselInfo(vm);
            };

            Loaded += (_, _) =>
            {
                Viewport.SetView(
                    new Point3D(-120, -250, 120),
                    new Vector3D(120, 250, -120),
                    new Vector3D(0, 0, 1), 0);
                Dispatcher.BeginInvoke(() => Viewport.ZoomExtents(600), DispatcherPriority.Render);
            };
        }

        private void UpdateVesselInfo(ShipBuilderViewModel vm)
        {
            VesselInfoText.Text =
                $"{vm.VesselType}  •  Lpp {vm.Lpp:F0} m  •  B {vm.Breadth:F0} m  •  Δ {vm.Displacement:N0} t";
        }

        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            Viewport.SetView(
                new Point3D(-120, -250, 120),
                new Vector3D(120, 250, -120),
                new Vector3D(0, 0, 1), 400);
            Dispatcher.BeginInvoke(() => Viewport.ZoomExtents(0), DispatcherPriority.Render);
        }

        private void TopView_Click(object sender, RoutedEventArgs e)
        {
            Viewport.SetView(new Point3D(0, 0, 1000), new Vector3D(0, 0, -1), new Vector3D(1, 0, 0), 400);
            Dispatcher.BeginInvoke(() => Viewport.ZoomExtents(0), DispatcherPriority.Render);
        }

        private void SideView_Click(object sender, RoutedEventArgs e)
        {
            Viewport.SetView(new Point3D(0, -1000, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), 400);
            Dispatcher.BeginInvoke(() => Viewport.ZoomExtents(0), DispatcherPriority.Render);
        }

        private void FrontView_Click(object sender, RoutedEventArgs e)
        {
            Viewport.SetView(new Point3D(1000, 0, 0), new Vector3D(-1, 0, 0), new Vector3D(0, 0, 1), 400);
            Dispatcher.BeginInvoke(() => Viewport.ZoomExtents(0), DispatcherPriority.Render);
        }

        private void FitView_Click(object sender, RoutedEventArgs e) =>
            Viewport.ZoomExtents(400);

        private void Upright_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ShipBuilderViewModel vm)
                vm.HeelAngle = 0.0;
        }
    }
}
