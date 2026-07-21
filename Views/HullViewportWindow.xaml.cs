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

            // On load: set isometric view with no animation, then zoom after render.
            Loaded += (_, _) => SetIsometric(animate: false);
        }

        private void UpdateVesselInfo(ShipBuilderViewModel vm)
        {
            VesselInfoText.Text =
                $"{vm.VesselType}  •  Lpp {vm.Lpp:F0} m  •  B {vm.Breadth:F0} m  •  Δ {vm.Displacement:N0} t";
        }

        // ── Shared helper: set a camera position and zoom after the frame renders ──
        private void SetViewAndZoom(Point3D position, Vector3D lookDir, Vector3D upDir, bool animate = true)
        {
            double animMs = animate ? 400 : 0;
            Viewport.SetView(position, lookDir, upDir, animMs);

            // Wait for the animation to finish before zooming.
            // Use a timer delay equal to animation duration + one frame.
            var delay = animate ? 450 : 50;
            var timer = new DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(delay) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Viewport.ZoomExtents(300);
            };
            timer.Start();
        }

        private void SetIsometric(bool animate = true)
        {
            // Isometric-style view: from starboard-stern-above quarter.
            double lpp = ShipBuilderViewModel.Instance.Lpp;
            double dist = lpp * 2.0;
            SetViewAndZoom(
                new Point3D(-dist * 0.5, -dist, dist * 0.5),
                new Vector3D(dist * 0.5, dist, -dist * 0.5),
                new Vector3D(0, 0, 1),
                animate);
        }

        private void ResetView_Click(object sender, RoutedEventArgs e) => SetIsometric();

        private void TopView_Click(object sender, RoutedEventArgs e)
        {
            double lpp = ShipBuilderViewModel.Instance.Lpp;
            SetViewAndZoom(
                new Point3D(lpp / 2, 0, lpp * 2),
                new Vector3D(0, 0, -1),
                new Vector3D(1, 0, 0));
        }

        private void SideView_Click(object sender, RoutedEventArgs e)
        {
            double lpp = ShipBuilderViewModel.Instance.Lpp;
            SetViewAndZoom(
                new Point3D(lpp / 2, -lpp * 2, 0),
                new Vector3D(0, 1, 0),
                new Vector3D(0, 0, 1));
        }

        private void FrontView_Click(object sender, RoutedEventArgs e)
        {
            double breadth = ShipBuilderViewModel.Instance.Breadth;
            SetViewAndZoom(
                new Point3D(breadth * 20, 0, 0),
                new Vector3D(-1, 0, 0),
                new Vector3D(0, 0, 1));
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
