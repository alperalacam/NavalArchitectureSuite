using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    public partial class ShipBuilderView : UserControl
    {
        // Matches the isometric camera declared on HullViewport in XAML.
        private static readonly Point3D DefaultCameraPosition = new(30, -220, 140);
        private static readonly Vector3D DefaultCameraLookDirection = new(-30, 220, -140);
        private static readonly Vector3D DefaultUpDirection = new(0, 0, 1);

        public ShipBuilderView()
        {
            InitializeComponent();
            DataContext = new ShipBuilderViewModel();
            Loaded += (_, _) => ZoomToHullBounds(0);
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
            // Hull is centered on midship (X=0): stern at -Lpp/2, bow at +Lpp/2 — look from beyond the bow toward the stern.
            SetViewAndFit(new Point3D(1000, 0, 0), new Vector3D(-1, 0, 0), new Vector3D(0, 0, 1));

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
                HullViewport.ZoomExtents(bounds, animationTime);
            }, DispatcherPriority.Loaded);
        }
    }
}
