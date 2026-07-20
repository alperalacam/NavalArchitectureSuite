using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace NavalArchitectureSuite.Models
{
    /// <summary>
    /// Generates a simplified parametric hull shell for the Ship Builder 3D preview.
    /// This is a visualization aid, not a faired lines plan: the longitudinal plan-view
    /// shape is a cosine-eased entrance / parallel-body / run distribution sized from Cwp
    /// (fuller Cwp -&gt; longer parallel middle body), and each station's cross-section is a
    /// power-law "bilge" curve from keel to the design waterline (wall-sided above it),
    /// with the exponent driven by Cm (fuller Cm -&gt; boxier section, closer to the deck
    /// edge width all the way down). The bow tapers to near a point (stem); the stern
    /// keeps a small transom width. Cb is not modelled directly (it falls out loosely of
    /// the Cwp/Cm shaping) — this trades hydrostatic-offset accuracy for a fast, always-
    /// valid, live-updating preview mesh.
    /// </summary>
    public static class HullMeshBuilder
    {
        private const int Stations = 41;
        private const int HalfPoints = 14; // sample points from deck edge to keel, per side

        public static Model3DGroup BuildHull(double lpp, double beam, double depth, double draft, double cb, double cm, double cwp)
        {
            var group = new Model3DGroup();
            if (lpp <= 0 || beam <= 0 || depth <= 0 || draft <= 0)
            {
                return group;
            }

            cm = Math.Clamp(cm, 0.5, 0.995);
            cwp = Math.Clamp(cwp, 0.5, 0.98);
            _ = cb; // reserved for a future, more detailed section-area fit
            draft = Math.Min(draft, depth);

            int pointsPerStation = 2 * HalfPoints + 1;
            double halfBeamMax = beam / 2.0;
            double exponent = 2.0 + 6.0 * (1.0 - cm); // fuller Cm -> section closer to rectangular

            double parallelBodyFrac = Math.Clamp(2.0 * cwp - 1.0, 0.0, 0.7);
            double taperFrac = (1.0 - parallelBodyFrac) / 2.0;
            const double sternMinRatio = 0.15;
            const double bowMinRatio = 0.02;

            double EnvelopeRatio(double xn)
            {
                if (taperFrac <= 0.0) return 1.0;
                if (xn < taperFrac)
                {
                    double t = xn / taperFrac;
                    return sternMinRatio + (1.0 - sternMinRatio) * 0.5 * (1.0 - Math.Cos(Math.PI * t));
                }
                if (xn > 1.0 - taperFrac)
                {
                    double t = (xn - (1.0 - taperFrac)) / taperFrac;
                    return 1.0 - (1.0 - bowMinRatio) * 0.5 * (1.0 - Math.Cos(Math.PI * t));
                }
                return 1.0;
            }

            double HalfBreadthAt(double z, double halfBeamStation)
            {
                if (z >= draft) return halfBeamStation;
                double zr = Math.Max(z, 0.0) / draft;
                return halfBeamStation * Math.Pow(zr, 1.0 / exponent);
            }

            var stationPoints = new Point3D[Stations][];
            double halfLpp = lpp / 2.0;
            double halfDepth = depth / 2.0;
            for (int i = 0; i < Stations; i++)
            {
                double xn = (double)i / (Stations - 1);
                // Centered on midship (X=0): stern at -halfLpp, bow at +halfLpp. Keeps the
                // model's centroid near the world origin so camera framing (SetView/ZoomExtents
                // in ShipBuilderView) doesn't depend on Lpp.
                double x = xn * lpp - halfLpp;
                double halfBeamStation = halfBeamMax * EnvelopeRatio(xn);

                var pts = new Point3D[pointsPerStation];
                for (int k = 0; k <= HalfPoints; k++)
                {
                    double u = (double)k / HalfPoints;
                    double z = depth * (1.0 - u);
                    double y = HalfBreadthAt(z, halfBeamStation);
                    // z stays in the keel-based (0..depth) frame for HalfBreadthAt/draft math;
                    // only the stored point is shifted so the keel-to-deck midpoint sits at Z=0.
                    pts[k] = new Point3D(x, -y, z - halfDepth);
                }
                for (int k = 1; k <= HalfPoints; k++)
                {
                    double u = (double)k / HalfPoints;
                    double z = depth * u;
                    double y = HalfBreadthAt(z, halfBeamStation);
                    pts[HalfPoints + k] = new Point3D(x, y, z - halfDepth);
                }
                stationPoints[i] = pts;
            }

            var mesh = new MeshGeometry3D();
            var positions = mesh.Positions;
            var indices = mesh.TriangleIndices;

            var stationBaseIndex = new int[Stations][];
            for (int i = 0; i < Stations; i++)
            {
                stationBaseIndex[i] = new int[pointsPerStation];
                for (int k = 0; k < pointsPerStation; k++)
                {
                    stationBaseIndex[i][k] = positions.Count;
                    positions.Add(stationPoints[i][k]);
                }
            }

            for (int i = 0; i < Stations - 1; i++)
            {
                for (int k = 0; k < pointsPerStation - 1; k++)
                {
                    int a = stationBaseIndex[i][k];
                    int b = stationBaseIndex[i][k + 1];
                    int c = stationBaseIndex[i + 1][k];
                    int d = stationBaseIndex[i + 1][k + 1];

                    indices.Add(a); indices.Add(c); indices.Add(b);
                    indices.Add(b); indices.Add(c); indices.Add(d);
                }
            }

            void CapStation(int stationIndex, bool flip)
            {
                var basePts = stationBaseIndex[stationIndex];
                int keel = basePts[HalfPoints];
                for (int k = 0; k < pointsPerStation - 1; k++)
                {
                    if (k == HalfPoints) continue; // degenerate triangle at the keel vertex
                    int a = keel;
                    int b = basePts[k];
                    int c = basePts[k + 1];
                    if (flip) { indices.Add(a); indices.Add(c); indices.Add(b); }
                    else { indices.Add(a); indices.Add(b); indices.Add(c); }
                }
            }

            CapStation(0, flip: true);
            CapStation(Stations - 1, flip: false);

            var hullMaterial = new MaterialGroup();
            hullMaterial.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0x8F, 0xA6, 0xC9))));
            hullMaterial.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromRgb(0x50, 0x50, 0x50)), 40));

            var hullGeometryModel = new GeometryModel3D(mesh, hullMaterial) { BackMaterial = hullMaterial };
            group.Children.Add(hullGeometryModel);

            // Translucent gold reference plane at the design waterline.
            var wl = new MeshGeometry3D();
            double m = 0.03 * lpp;
            double wy = halfBeamMax * 1.2;
            double draftCentered = draft - halfDepth;
            wl.Positions.Add(new Point3D(-halfLpp - m, -wy, draftCentered));
            wl.Positions.Add(new Point3D(halfLpp + m, -wy, draftCentered));
            wl.Positions.Add(new Point3D(halfLpp + m, wy, draftCentered));
            wl.Positions.Add(new Point3D(-halfLpp - m, wy, draftCentered));
            wl.TriangleIndices.Add(0); wl.TriangleIndices.Add(1); wl.TriangleIndices.Add(2);
            wl.TriangleIndices.Add(0); wl.TriangleIndices.Add(2); wl.TriangleIndices.Add(3);
            var wlMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(60, 0xC8, 0x96, 0x0C)));
            var wlModel = new GeometryModel3D(wl, wlMaterial) { BackMaterial = wlMaterial };
            group.Children.Add(wlModel);

            return group;
        }
    }
}
