using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace NavalArchitectureSuite.Models
{
    /// <summary>
    /// Generates a parametric hull shell split into four colour zones for the Ship Builder
    /// 3D preview:
    ///   • Below waterline (underwater body)  — deep navy blue
    ///   • Boot topping / waterline band      — red/oxide (anti-fouling / boot topping)
    ///   • Topside (freeboard)                — light gray
    ///   • Deck                               — warm off-white / cream
    /// Plus the translucent gold waterline reference plane.
    /// </summary>
    public static class HullMeshBuilder
    {
        private const int Stations   = 41;
        private const int HalfPoints = 14;

        private const double SternBeamRatio  = 0.15;
        private const double BowBeamRatio    = 0.02;
        private const double SternSheerRatio = 1.08;
        private const double BowSheerRatio   = 1.18;

        // Boot-topping band: fraction of draft above and below the waterline.
        private const double BootToppingFrac = 0.04; // 4 % of draft each side

        public static Model3DGroup BuildHull(
            double lpp, double beam, double depth, double draft,
            double cb, double cm, double cwp)
        {
            var group = new Model3DGroup();
            if (lpp <= 0 || beam <= 0 || depth <= 0 || draft <= 0)
                return group;

            cm    = Math.Clamp(cm,  0.5,  0.995);
            cwp   = Math.Clamp(cwp, 0.5,  0.98);
            draft = Math.Min(draft, depth);

            double halfBeamMax     = beam / 2.0;
            double exponent        = 2.0 + 6.0 * (1.0 - cm);
            double parallelBodyFrac = Math.Clamp(2.0 * cwp - 1.0, 0.0, 0.7);
            double taperFrac        = (1.0 - parallelBodyFrac) / 2.0;
            double parallelStart    = taperFrac;
            double parallelEnd      = 1.0 - taperFrac;

            double bootTop = draft + BootToppingFrac * draft;   // top of boot topping
            double bootBot = draft - BootToppingFrac * draft;   // bottom of boot topping

            // ── Envelope helpers ────────────────────────────────────────────────────
            double LackenbyEnvelope(double xn, double sternRatio, double bowRatio)
            {
                if (xn <= parallelStart)
                {
                    double t     = parallelStart > 0.0 ? xn / parallelStart : 1.0;
                    double eased = 0.5 * (1.0 - Math.Cos(Math.PI * t));
                    return sternRatio + (1.0 - sternRatio) * eased;
                }
                if (xn >= parallelEnd)
                {
                    double t     = parallelEnd < 1.0 ? (xn - parallelEnd) / (1.0 - parallelEnd) : 0.0;
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

            // ── Station geometry ────────────────────────────────────────────────────
            // We sample the hull at N vertical levels per station so we can assign each
            // strip to the correct colour zone.
            int pointsPerStation = 2 * HalfPoints + 1;

            var stationPts       = new Point3D[Stations][];
            var stationHalfBeam  = new double[Stations];
            var stationDeckZ     = new double[Stations];

            for (int i = 0; i < Stations; i++)
            {
                double xn             = (double)i / (Stations - 1);
                double x              = xn * lpp;
                double hb             = halfBeamMax * LackenbyEnvelope(xn, SternBeamRatio, BowBeamRatio);
                double dz             = depth * LackenbyEnvelope(xn, SternSheerRatio, BowSheerRatio);
                stationHalfBeam[i]    = hb;
                stationDeckZ[i]       = dz;

                var pts = new Point3D[pointsPerStation];
                for (int k = 0; k <= HalfPoints; k++)
                {
                    double u = (double)k / HalfPoints;
                    double z = dz * (1.0 - u);
                    double y = HalfBreadthAt(z, hb);
                    pts[k] = new Point3D(x, -y, z);         // starboard (−Y)
                }
                for (int k = 1; k <= HalfPoints; k++)
                {
                    double u = (double)k / HalfPoints;
                    double z = dz * u;
                    double y = HalfBreadthAt(z, hb);
                    pts[HalfPoints + k] = new Point3D(x, y, z);  // port (+Y)
                }
                stationPts[i] = pts;
            }

            // ── Zone classifier ─────────────────────────────────────────────────────
            // Returns which colour zone a vertex belongs to based on its Z height.
            //   0 = below waterline   1 = boot topping   2 = topside   3 = deck plate
            int Zone(double z, double deckZ)
            {
                double deckThickness = 0.025 * depth;   // deck plate = top 2.5 % of depth
                if (z >= deckZ - deckThickness)  return 3;   // deck
                if (z >= bootTop)                return 2;   // topside / freeboard
                if (z >= bootBot)                return 1;   // boot topping
                return 0;                                    // underwater body
            }

            // ── Four meshes, one per zone ────────────────────────────────────────────
            var meshes = new MeshGeometry3D[4];
            for (int z = 0; z < 4; z++) meshes[z] = new MeshGeometry3D();

            // We build each inter-station quad strip and route each triangle to the mesh
            // matching the zone of its centroid.
            for (int i = 0; i < Stations - 1; i++)
            {
                for (int k = 0; k < pointsPerStation - 1; k++)
                {
                    Point3D a = stationPts[i][k];
                    Point3D b = stationPts[i][k + 1];
                    Point3D c = stationPts[i + 1][k];
                    Point3D d = stationPts[i + 1][k + 1];

                    // Use the centroid Z of each triangle to pick the zone.
                    double refDeckZ = (stationDeckZ[i] + stationDeckZ[i + 1]) / 2.0;

                    AddTriangle(meshes[Zone((a.Z + c.Z + b.Z) / 3.0, refDeckZ)], a, c, b);
                    AddTriangle(meshes[Zone((b.Z + c.Z + d.Z) / 3.0, refDeckZ)], b, c, d);
                }
            }

            // Stern cap → underwater / boot topping zone
            CapStation(meshes, stationPts, stationDeckZ, 0,           flip: true,  bootBot, bootTop, depth);
            // Bow cap
            CapStation(meshes, stationPts, stationDeckZ, Stations - 1, flip: false, bootBot, bootTop, depth);

            // ── Materials ────────────────────────────────────────────────────────────
            // 0 below waterline : deep navy/dark blue-green (anti-fouling)
            // 1 boot topping    : dark oxide red
            // 2 topside         : medium gray
            // 3 deck            : warm off-white / cream
            Color[] diffuse = {
                Color.FromRgb(0x1A, 0x3A, 0x5C),   // 0  underwater  — deep navy
                Color.FromRgb(0x8B, 0x1A, 0x1A),   // 1  boot top    — dark red oxide
                Color.FromRgb(0xA8, 0xB4, 0xC0),   // 2  topside     — steel gray
                Color.FromRgb(0xD4, 0xC8, 0xA8),   // 3  deck        — warm cream
            };
            Color[] specular = {
                Color.FromRgb(0x20, 0x40, 0x60),
                Color.FromRgb(0x40, 0x10, 0x10),
                Color.FromRgb(0x60, 0x60, 0x70),
                Color.FromRgb(0x50, 0x48, 0x38),
            };

            for (int z = 0; z < 4; z++)
            {
                if (meshes[z].Positions.Count == 0) continue;
                var mat = new MaterialGroup();
                mat.Children.Add(new DiffuseMaterial(new SolidColorBrush(diffuse[z])));
                mat.Children.Add(new SpecularMaterial(new SolidColorBrush(specular[z]), 35));
                var gm = new GeometryModel3D(meshes[z], mat) { BackMaterial = mat };
                group.Children.Add(gm);
            }

            // ── Waterline reference plane (translucent gold) ─────────────────────────
            var wl     = new MeshGeometry3D();
            double margin = 0.03 * lpp;
            double wy     = halfBeamMax * 1.2;
            wl.Positions.Add(new Point3D(-margin,      -wy, draft));
            wl.Positions.Add(new Point3D(lpp + margin, -wy, draft));
            wl.Positions.Add(new Point3D(lpp + margin,  wy, draft));
            wl.Positions.Add(new Point3D(-margin,       wy, draft));
            wl.TriangleIndices.Add(0); wl.TriangleIndices.Add(1); wl.TriangleIndices.Add(2);
            wl.TriangleIndices.Add(0); wl.TriangleIndices.Add(2); wl.TriangleIndices.Add(3);
            var wlMat = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(70, 0xC8, 0x96, 0x0C)));
            group.Children.Add(new GeometryModel3D(wl, wlMat) { BackMaterial = wlMat });

            return group;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private static void AddTriangle(MeshGeometry3D mesh, Point3D a, Point3D b, Point3D c)
        {
            int start = mesh.Positions.Count;
            mesh.Positions.Add(a);
            mesh.Positions.Add(b);
            mesh.Positions.Add(c);
            mesh.TriangleIndices.Add(start);
            mesh.TriangleIndices.Add(start + 1);
            mesh.TriangleIndices.Add(start + 2);
        }

        private static void CapStation(
            MeshGeometry3D[] meshes,
            Point3D[][] stationPts,
            double[] stationDeckZ,
            int stationIndex,
            bool flip,
            double bootBot, double bootTop, double depth)
        {
            var pts     = stationPts[stationIndex];
            double dz   = stationDeckZ[stationIndex];
            int pps     = pts.Length;
            int keel    = pps / 2;   // keel vertex index (HalfPoints)

            for (int k = 0; k < pps - 1; k++)
            {
                if (k == keel) continue;
                Point3D a = pts[keel];
                Point3D b = pts[k];
                Point3D c = pts[k + 1];
                double centZ = (a.Z + b.Z + c.Z) / 3.0;

                int zone = centZ >= dz - 0.025 * depth ? 3
                         : centZ >= bootTop            ? 2
                         : centZ >= bootBot            ? 1
                         : 0;

                if (flip) AddTriangle(meshes[zone], a, c, b);
                else      AddTriangle(meshes[zone], a, b, c);
            }
        }
    }
}
