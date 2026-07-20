using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace NavalArchitectureSuite.Models
{
    /// <summary>
    /// Generates a simplified parametric hull shell for the Ship Builder 3D preview.
    /// Stern is at X=0, bow at X=Lpp, centerline at Y=0, keel at Z=0, deck at Z=Depth
    /// (rising toward the ends per the sheer envelope). Beam and depth (sheer) both vary
    /// along the length through a shared Lackenby-style envelope: a flat parallel middle
    /// body (sized from Cwp) flanked by cosine-eased entrance and run curves, narrowing to
    /// a near-point bow and a transom stern for beam, and rising toward both ends for sheer.
    /// Each station's underwater cross-section is a power-law bilge curve from keel to the
    /// design waterline (wall-sided above it up to the local deck height), with the exponent
    /// driven by Cm (fuller Cm -&gt; boxier section). Cb is not modelled directly.
    /// </summary>
    public static class HullMeshBuilder
    {
        private const int Stations = 41;
        private const int HalfPoints = 14; // sample points from deck edge to keel, per side

        // Beam envelope: half-beam at the stern/bow as a fraction of the parallel-body half-beam.
        private const double SternBeamRatio = 0.15;
        private const double BowBeamRatio = 0.02;

        // Sheer envelope: deck height at the stern/bow as a fraction of the molded depth amidships.
        private const double SternSheerRatio = 1.08;
        private const double BowSheerRatio = 1.18;

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

            // Parallel middle body sized from Cwp; the remainder splits evenly into an eased
            // entrance (forward) and run (aft).
            double parallelBodyFrac = Math.Clamp(2.0 * cwp - 1.0, 0.0, 0.7);
            double taperFrac = (1.0 - parallelBodyFrac) / 2.0;
            double parallelStart = taperFrac;
            double parallelEnd = 1.0 - taperFrac;

            // Lackenby-style envelope: 1.0 across the parallel middle body, cosine-eased down
            // (or up) to the given end ratios over the aft run and forward entrance.
            double LackenbyEnvelope(double xn, double sternRatio, double bowRatio)
            {
                if (xn <= parallelStart)
                {
                    double t = parallelStart > 0.0 ? xn / parallelStart : 1.0;
                    double eased = 0.5 * (1.0 - Math.Cos(Math.PI * t));
                    return sternRatio + (1.0 - sternRatio) * eased;
                }
                if (xn >= parallelEnd)
                {
                    double t = parallelEnd < 1.0 ? (xn - parallelEnd) / (1.0 - parallelEnd) : 0.0;
                    double eased = 0.5 * (1.0 - Math.Cos(Math.PI * t));
                    return 1.0 - (1.0 - bowRatio) * eased;
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
            for (int i = 0; i < Stations; i++)
            {
                double xn = (double)i / (Stations - 1);
                double x = xn * lpp;
                double halfBeamStation = halfBeamMax * LackenbyEnvelope(xn, SternBeamRatio, BowBeamRatio);
                double deckZ = depth * LackenbyEnvelope(xn, SternSheerRatio, BowSheerRatio);

                var pts = new Point3D[pointsPerStation];
                for (int k = 0; k <= HalfPoints; k++)
                {
                    double u = (double)k / HalfPoints;
                    double z = deckZ * (1.0 - u);
                    double y = HalfBreadthAt(z, halfBeamStation);
                    pts[k] = new Point3D(x, -y, z);
                }
                for (int k = 1; k <= HalfPoints; k++)
                {
                    double u = (double)k / HalfPoints;
                    double z = deckZ * u;
                    double y = HalfBreadthAt(z, halfBeamStation);
                    pts[HalfPoints + k] = new Point3D(x, y, z);
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

            // Translucent gold reference plane at the design waterline, spanning the full hull length.
            var wl = new MeshGeometry3D();
            double margin = 0.03 * lpp;
            double wy = halfBeamMax * 1.2;
            wl.Positions.Add(new Point3D(-margin, -wy, draft));
            wl.Positions.Add(new Point3D(lpp + margin, -wy, draft));
            wl.Positions.Add(new Point3D(lpp + margin, wy, draft));
            wl.Positions.Add(new Point3D(-margin, wy, draft));
            wl.TriangleIndices.Add(0); wl.TriangleIndices.Add(1); wl.TriangleIndices.Add(2);
            wl.TriangleIndices.Add(0); wl.TriangleIndices.Add(2); wl.TriangleIndices.Add(3);
            var wlMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(60, 0xC8, 0x96, 0x0C)));
            var wlModel = new GeometryModel3D(wl, wlMaterial) { BackMaterial = wlMaterial };
            group.Children.Add(wlModel);

            return group;
        }
    }
}
