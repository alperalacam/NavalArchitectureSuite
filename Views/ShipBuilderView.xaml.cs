using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class ShipBuilderView : UserControl
    {
        // Matches the top-down plan camera declared on HullViewport in XAML:
        // looking straight down with +X (bow) mapped to the top of the screen, so
        // BOW is up, STERN is down, PORT (+Y) is left and STARBOARD (-Y) is right.
        private static readonly Point3D DefaultCameraPosition = new(0, 0, 1000);
        private static readonly Vector3D DefaultCameraLookDirection = new(0, 0, -1);
        private static readonly Vector3D DefaultUpDirection = new(1, 0, 0);

        public ShipBuilderView()
        {
            InitializeComponent();
            DataContext = ShipBuilderViewModel.Instance;  // Use the shared singleton

            // First attempt: when the control is laid out.
            Loaded += (_, _) => ZoomToHullBounds(0);

            // Second attempt: every time the view is navigated back to (IsVisibleChanged)
            // because the viewport may not have rendered on the very first Loaded call
            // when ShipBuilderView is kept alive as a singleton across nav clicks.
            IsVisibleChanged += (_, e) =>
            {
                if ((bool)e.NewValue)
                    ZoomToHullBounds(0);
            };
        }

        private void ResetViewButton_Click(object sender, RoutedEventArgs e) =>
            SetViewAndFit(DefaultCameraPosition, DefaultCameraLookDirection, DefaultUpDirection);

        private void TopViewButton_Click(object sender, RoutedEventArgs e) =>
            // Looking straight down (plan view); bow (+X) points up-screen.
            SetViewAndFit(new Point3D(0, 0, 1000), new Vector3D(0, 0, -1), new Vector3D(1, 0, 0));

        private void SideViewButton_Click(object sender, RoutedEventArgs e) =>
            // Looking from the +Y (starboard) side toward the hull.
            SetViewAndFit(new Point3D(0, 1000, 0), new Vector3D(0, -1, 0), new Vector3D(0, 0, 1));

        private void FrontViewButton_Click(object sender, RoutedEventArgs e) =>
            // Stern is at X=0, bow at X=Lpp (per HullMeshBuilder) — look from beyond the bow toward the stern.
            SetViewAndFit(new Point3D(1000, 0, 0), new Vector3D(-1, 0, 0), new Vector3D(0, 0, 1));

        private HullViewportWindow? _popOutWindow;

        private void PopOutViewButton_Click(object sender, RoutedEventArgs e)
        {
            // If already open, bring it to front instead of opening a second one.
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

        private void FitToScreenButton_Click(object sender, RoutedEventArgs e) =>
            ZoomToHullBounds(400);

        private void SetViewAndFit(Point3D position, Vector3D lookDirection, Vector3D upDirection)
        {
            HullViewport.SetView(position, lookDirection, upDirection, 0);
            ZoomToHullBounds(400);
        }

        /// <summary>
        /// Fits the camera to the hull's own geometry bounds instead of calling
        /// HullViewport.ZoomExtents() with no bounds, which asks HelixToolkit to walk the
        /// live visual tree and can silently no-op (empty bounds) if that walk runs before
        /// the ModelVisual3D's Content binding has been applied. Model3DGroup.Bounds is
        /// computed straight from the mesh data, so it is correct immediately regardless of
        /// layout/render timing; still deferred one dispatcher pass so it never races the
        /// binding update that follows a parameter change.
        /// </summary>
        private void ZoomToHullBounds(double animationTime)
        {
            if (DataContext is not ShipBuilderViewModel viewModel) return;

            Dispatcher.BeginInvoke(() =>
            {
                Rect3D bounds = viewModel.HullModel.Bounds;
                if (bounds.IsEmpty) return;

                // ZoomExtents with bounds can silently no-op when the camera is very
                // close to the geometry. Call ZoomExtents on the viewport directly with
                // a generous margin so HelixToolkit recalculates the full scene radius.
                HullViewport.ZoomExtents(animationTime);
            }, DispatcherPriority.Render);
        }
    }
}
