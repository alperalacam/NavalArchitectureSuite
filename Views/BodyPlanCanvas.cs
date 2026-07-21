using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    /// <summary>
    /// Live 2D body plan canvas for the Ship Builder module.
    /// Slices the parametric hull at 11 stations (AP, 1/10 ... 9/10, FP)
    /// using the same geometry as HullMeshBuilder, and renders:
    ///   • Forward sections (right half of canvas)
    ///   • Aft sections (left half of canvas)
    ///   • Centreline, baseline, waterlines, deck line
    ///   • Colour zones matching the 3D hull
    /// Updates live whenever any ShipBuilder dimension changes.
    /// </summary>
    public class BodyPlanCanvas : Canvas
    {
        // ── Palette matching the 3D hull zones ──────────────────────────────
        private static readonly Brush BrushUnderwater = new SolidColorBrush(Color.FromRgb(0x1A, 0x3A, 0x5C));
        private static readonly Brush BrushBootTop    = new SolidColorBrush(Color.FromRgb(0x8B, 0x1A, 0x1A));
        private static readonly Brush BrushTopside    = new SolidColorBrush(Color.FromRgb(0xA8, 0xB4, 0xC0));
        private static readonly Brush BrushDeck       = new SolidColorBrush(Color.FromRgb(0xD4, 0xC8, 0xA8));
        private static readonly Brush BrushWaterline  = new SolidColorBrush(Color.FromArgb(200, 0xC8, 0x96, 0x0C));
        private static readonly Brush BrushGrid       = new SolidColorBrush(Color.FromArgb(50, 0x8F, 0xA6, 0xC9));
        private static readonly Brush BrushCentreLine = new SolidColorBrush(Color.FromArgb(180, 0x8F, 0xA6, 0xC9));
        private static readonly Brush BrushLabel      = new SolidColorBrush(Color.FromRgb(0x8F, 0xA6, 0xC9));
        private static readonly Brush BrushGold       = new SolidColorBrush(Color.FromRgb(0xC8, 0x96, 0x0C));
        private static readonly Brush BrushBackground = new SolidColorBrush(Color.FromRgb(0x06, 0x10, 0x1E));

        // Stations to show: 0=AP, 5=midship, 10=FP (11 stations total, in tenths)
        private static readonly double[] StationFractions =
            { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 };

        private static readonly string[] StationLabels =
            { "AP", "1", "2", "3", "4", "5", "4", "3", "2", "1", "FP" };

        private ShipBuilderViewModel? _vm;

        public void SetViewModel(ShipBuilderViewModel vm)
        {
            _vm = vm;
            vm.PropertyChanged += (_, _) => Dispatcher.Invoke(Redraw);
            Redraw();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo info)
        {
            base.OnRenderSizeChanged(info);
            Redraw();
        }

        private void Redraw()
        {
            Children.Clear();
            if (_vm is null || ActualWidth < 50 || ActualHeight < 50) return;

            double W = ActualWidth;
            double H = ActualHeight;

            // ── Background ──────────────────────────────────────────────────
            AddRect(0, 0, W, H, BrushBackground);

            // ── Read dimensions ──────────────────────────────────────────────
            double lpp   = Math.Max(1, _vm.Lpp);
            double beam  = Math.Max(1, _vm.Breadth);
            double depth = Math.Max(1, _vm.Depth);
            double draft = Math.Min(Math.Max(0.1, _vm.Draft), depth);
            double cb    = Math.Clamp(_vm.Cb,  0.3, 0.99);
            double cm    = Math.Clamp(_vm.Cm,  0.5, 0.995);
            double cwp   = Math.Clamp(_vm.Cwp, 0.5, 0.98);

            // ── Layout ──────────────────────────────────────────────────────
            double padL  = 52;   // left margin (station labels)
            double padR  = 16;
            double padT  = 28;
            double padB  = 40;   // bottom margin (baseline label)

            double drawW = W - padL - padR;
            double drawH = H - padT - padB;

            // Scale: half-beam maps to half the draw width; depth maps to draw height
            double halfBeam   = beam / 2.0;
            double scaleY     = (drawW / 2.0) / halfBeam;   // pixels per metre (transverse)
            double scaleZ     = drawH / depth;               // pixels per metre (vertical)

            // Canvas coordinate helpers
            // Body plan: CL at canvas centre-X, keel at bottom
            double clX      = padL + drawW / 2.0;           // centreline X
            double keelY    = padT + drawH;                  // keel canvas Y

            double cy(double y)  => clX + y * scaleY;       // breadth → canvas X (starboard right, port left)
            double cz(double z)  => keelY - z * scaleZ;     // height  → canvas Y

            // ── Hull geometry helpers (identical to HullMeshBuilder) ─────────
            double exponent         = 2.0 + 6.0 * (1.0 - cm);
            double parallelBodyFrac = Math.Clamp(2.0 * cwp - 1.0, 0.0, 0.7);
            double taperFrac        = (1.0 - parallelBodyFrac) / 2.0;
            double parallelStart    = taperFrac;
            double parallelEnd      = 1.0 - taperFrac;

            const double SternBeamRatio  = 0.15;
            const double BowBeamRatio    = 0.02;
            const double SternSheerRatio = 1.08;
            const double BowSheerRatio   = 1.18;
            const double BootToppingFrac = 0.04;

            double bootTop = draft + BootToppingFrac * draft;
            double bootBot = draft - BootToppingFrac * draft;
            double deckThickZ = 0.025 * depth;

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

            double HalfBreadthAt(double z, double hbStation)
            {
                if (z >= draft) return hbStation;
                double zr = Math.Max(z, 0.0) / draft;
                return hbStation * Math.Pow(zr, 1.0 / exponent);
            }

            // ── Grid — waterlines ────────────────────────────────────────────
            int nWL = 5;
            for (int w = 0; w <= nWL; w++)
            {
                double z    = depth * w / nWL;
                double canY = cz(z);
                AddLine(padL, canY, W - padR, canY, BrushGrid, 0.4, isDashed: true);
                if (w > 0)
                    AddLabel($"WL{w}", 2, canY - 6, BrushLabel, 8);
            }

            // ── Waterline (design draft) ─────────────────────────────────────
            AddLine(padL, cz(draft), W - padR, cz(draft), BrushWaterline, 1.0);
            AddLabel($"T={draft:F1}m", 2, cz(draft) - 6, BrushGold, 8);

            // ── Baseline ────────────────────────────────────────────────────
            AddLine(padL, keelY, W - padR, keelY, BrushCentreLine, 0.8);
            AddLabel("BL", 2, keelY - 6, BrushLabel, 8);
            AddLabel("Baseline", padL, keelY + 6, BrushLabel, 8);

            // ── Centreline ──────────────────────────────────────────────────
            AddLine(clX, padT, clX, keelY, BrushCentreLine, 0.6, isDashed: true);
            AddLabel("CL", clX + 3, padT, BrushLabel, 8);

            // ── Title ────────────────────────────────────────────────────────
            AddLabel("BODY PLAN", padL, 6, BrushGold, 10);
            AddLabel("FWD →", clX + 20, 6, BrushLabel, 8);
            AddLabel("← AFT", clX - 60, 6, BrushLabel, 8);

            // ── Draw each station ────────────────────────────────────────────
            int nPoints = 30;   // vertical sample points per section

            for (int si = 0; si < StationFractions.Length; si++)
            {
                double xn     = StationFractions[si];
                double hb     = halfBeam * LackenbyEnvelope(xn, SternBeamRatio, BowBeamRatio);
                double deckZ  = depth   * LackenbyEnvelope(xn, SternSheerRatio, BowSheerRatio);

                // Convention: forward stations (xn >= 0.5) drawn on RIGHT (starboard side = positive Y)
                //             aft stations    (xn <  0.5) drawn on LEFT  (port side = negative Y)
                bool isForward = xn >= 0.5;
                double sign    = isForward ? 1.0 : -1.0;

                // Build section points from keel to deck
                var pts = new List<Point>();
                for (int k = 0; k <= nPoints; k++)
                {
                    double z  = deckZ * k / nPoints;
                    double y  = HalfBreadthAt(z, hb) * sign;
                    pts.Add(new Point(cy(y), cz(z)));
                }

                // Add deck corner and close to centreline
                pts.Add(new Point(cy(hb * sign), cz(deckZ)));
                pts.Add(new Point(clX, cz(deckZ)));
                pts.Add(new Point(clX, keelY));

                // Draw section as coloured polygon strips (by zone)
                // Zone boundaries
                double[] zBounds = { 0, bootBot, bootTop, deckZ - deckThickZ, deckZ };
                Brush[] zoneBrush = { BrushUnderwater, BrushBootTop, BrushTopside, BrushDeck };

                for (int z2 = 0; z2 < zoneBrush.Length; z2++)
                {
                    double zLo = zBounds[z2];
                    double zHi = Math.Min(zBounds[z2 + 1], deckZ);
                    if (zHi <= zLo) continue;

                    // Sample section points in this zone
                    var zonePts = new PointCollection();
                    zonePts.Add(new Point(clX, cz(zLo)));                    // CL bottom
                    int nZ = 16;
                    for (int k = 0; k <= nZ; k++)
                    {
                        double z3 = zLo + (zHi - zLo) * k / nZ;
                        double y  = HalfBreadthAt(z3, hb) * sign;
                        zonePts.Add(new Point(cy(y), cz(z3)));
                    }
                    zonePts.Add(new Point(clX, cz(zHi)));                    // CL top

                    var poly = new Polygon
                    {
                        Points          = zonePts,
                        Fill            = zoneBrush[z2],
                        Stroke          = Brushes.Transparent,
                        StrokeThickness = 0,
                        Opacity         = 0.75
                    };
                    Children.Add(poly);
                }

                // Section outline
                var outline = new Polyline
                {
                    StrokeThickness = 1.0,
                    Stroke          = new SolidColorBrush(Color.FromArgb(200, 0xD7, 0xE1, 0xF0))
                };
                foreach (var p in pts) outline.Points.Add(p);
                Children.Add(outline);

                // Station label at baseline
                double labelX = cy(hb * sign * 0.5) - 6;
                AddLabel(StationLabels[si], labelX, keelY + 8, BrushGold, 8);
            }

            // ── Deck line ────────────────────────────────────────────────────
            // Connect sheer points of each station
            var deckPtsFwd = new PointCollection();
            var deckPtsAft = new PointCollection();

            for (int si = 0; si < StationFractions.Length; si++)
            {
                double xn    = StationFractions[si];
                double hb    = halfBeam * LackenbyEnvelope(xn, SternBeamRatio, BowBeamRatio);
                double deckZ = depth   * LackenbyEnvelope(xn, SternSheerRatio, BowSheerRatio);
                bool isForward = xn >= 0.5;
                double sign  = isForward ? 1.0 : -1.0;
                var pt       = new Point(cy(hb * sign), cz(deckZ));
                if (isForward) deckPtsFwd.Add(pt);
                else           deckPtsAft.Add(pt);
            }

            if (deckPtsFwd.Count > 1)
            {
                var pl = new Polyline { Stroke = BrushGold, StrokeThickness = 0.8 };
                foreach (var p in deckPtsFwd) pl.Points.Add(p);
                Children.Add(pl);
            }
            if (deckPtsAft.Count > 1)
            {
                var pl = new Polyline { Stroke = BrushGold, StrokeThickness = 0.8 };
                foreach (var p in deckPtsAft) pl.Points.Add(p);
                Children.Add(pl);
            }

            // ── Half-breadth scale bar ───────────────────────────────────────
            double scaleBarY  = keelY + 28;
            double scaleBarM  = Math.Round(halfBeam / 3.0, 0);   // ~1/3 of half-beam
            if (scaleBarM < 1) scaleBarM = 1;
            double scaleBarPx = scaleBarM * scaleY;

            AddLine(clX, scaleBarY, clX + scaleBarPx, scaleBarY, BrushGold, 1.0);
            AddLine(clX, scaleBarY - 3, clX, scaleBarY + 3, BrushGold, 0.8);
            AddLine(clX + scaleBarPx, scaleBarY - 3, clX + scaleBarPx, scaleBarY + 3, BrushGold, 0.8);
            AddLabel($"{scaleBarM:F0} m", clX + scaleBarPx / 2 - 8, scaleBarY + 4, BrushGold, 8);

            // ── Legend ───────────────────────────────────────────────────────
            double legX = padL;
            double legY = padT + 2;
            DrawLegendItem(legX,       legY, BrushUnderwater, "below WL");
            DrawLegendItem(legX + 75,  legY, BrushBootTop,   "boot top");
            DrawLegendItem(legX + 145, legY, BrushTopside,   "topside");
            DrawLegendItem(legX + 205, legY, BrushDeck,      "deck");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void DrawLegendItem(double x, double y, Brush color, string label)
        {
            AddRect(x, y, 12, 8, color);
            AddLabel(label, x + 15, y, BrushLabel, 8);
        }

        private void AddLine(double x1, double y1, double x2, double y2,
                              Brush stroke, double thickness, bool isDashed = false)
        {
            var line = new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = stroke,
                StrokeThickness = thickness
            };
            if (isDashed)
                line.StrokeDashArray = new DoubleCollection { 4, 3 };
            Children.Add(line);
        }

        private void AddRect(double x, double y, double w, double h, Brush fill)
        {
            var rect = new Rectangle { Width = w, Height = h, Fill = fill };
            SetLeft(rect, x); SetTop(rect, y);
            Children.Add(rect);
        }

        private void AddLabel(string text, double x, double y, Brush color, double size)
        {
            var tb = new TextBlock
            {
                Text        = text,
                Foreground  = color,
                FontSize    = size,
                FontFamily  = new FontFamily("Arial")
            };
            SetLeft(tb, x); SetTop(tb, y);
            Children.Add(tb);
        }
    }
}
