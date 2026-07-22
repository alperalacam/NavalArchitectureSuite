using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  Shared geometry helpers used by all three Lines Plan canvases
    // ═══════════════════════════════════════════════════════════════════════════
    internal static class LinesGeometry
    {
        internal const int NStations   = 21;   // 0=AP … 20=FP
        internal const int NWaterlines =  6;   // BL + WL1..WL5
        internal const int NButtocks   =  3;   // B1 B2 B3

        // Palette
        internal static readonly Brush BrushBackground = new SolidColorBrush(Color.FromRgb(0x06, 0x10, 0x1E));
        internal static readonly Brush BrushStation    = new SolidColorBrush(Color.FromArgb(200, 0x8F, 0xA6, 0xC9));
        internal static readonly Brush BrushWaterline  = new SolidColorBrush(Color.FromRgb(0xC8, 0x96, 0x0C));
        internal static readonly Brush BrushButtock    = new SolidColorBrush(Color.FromArgb(160, 0x8F, 0xA6, 0xC9));
        internal static readonly Brush BrushGrid       = new SolidColorBrush(Color.FromArgb(35,  0x8F, 0xA6, 0xC9));
        internal static readonly Brush BrushLabel      = new SolidColorBrush(Color.FromRgb(0x8F, 0xA6, 0xC9));
        internal static readonly Brush BrushGold       = new SolidColorBrush(Color.FromRgb(0xC8, 0x96, 0x0C));
        internal static readonly Brush BrushDraftLine  = new SolidColorBrush(Color.FromArgb(220, 0xC8, 0x96, 0x0C));
        internal static readonly Brush BrushCentreLine = new SolidColorBrush(Color.FromArgb(160, 0x8F, 0xA6, 0xC9));

        // Hull form coefficients (updated per Redraw)
        internal static double ParallelStart, ParallelEnd, Exponent;
        internal const double SternBeamRatio  = 0.15;
        internal const double BowBeamRatio    = 0.02;
        internal const double SternSheerRatio = 1.08;
        internal const double BowSheerRatio   = 1.18;

        /// <summary>
        /// Label scale factor: keeps text proportionally sized regardless of
        /// canvas pixel width.  A "reference" screen width of 900 px maps to
        /// scale = 1.0.  At larger widths (e.g. 1600 px for A0 PDF render)
        /// the factor drops so labels stay compact relative to the drawing.
        /// Clamped to [0.45 … 1.0] so labels never become unreadably small.
        /// </summary>
        internal static double LabelScale(double canvasWidth)
            => Math.Clamp(900.0 / Math.Max(canvasWidth, 1.0), 0.45, 1.0);

        internal static void UpdateCoefficients(ShipBuilderViewModel vm)
        {
            double cm  = Math.Clamp(vm.Cm,  0.5,  0.995);
            double cwp = Math.Clamp(vm.Cwp, 0.5,  0.98);
            Exponent = 2.0 + 6.0 * (1.0 - cm);
            double pbf   = Math.Clamp(2.0 * cwp - 1.0, 0.0, 0.7);
            double taper = (1.0 - pbf) / 2.0;
            ParallelStart = taper;
            ParallelEnd   = 1.0 - taper;
        }

        internal static double BeamEnvelope(double xn, double sternR, double bowR)
        {
            if (xn <= ParallelStart)
            {
                double t = ParallelStart > 0 ? xn / ParallelStart : 1.0;
                return sternR + (1.0 - sternR) * 0.5 * (1.0 - Math.Cos(Math.PI * t));
            }
            if (xn >= ParallelEnd)
            {
                double t = ParallelEnd < 1 ? (xn - ParallelEnd) / (1.0 - ParallelEnd) : 0.0;
                return 1.0 - (1.0 - bowR) * 0.5 * (1.0 - Math.Cos(Math.PI * t));
            }
            return 1.0;
        }

        internal static double HalfBreadthAt(double z, double hb, double draft)
        {
            if (z >= draft) return hb;
            double zr = Math.Max(z, 0.0) / draft;
            return hb * Math.Pow(zr, 1.0 / Exponent);
        }

        internal static double StationFraction(int i) => (double)i / (NStations - 1);

        // ── Drawing primitives ────────────────────────────────────────────────

        internal static void AddLine(Canvas c, double x1, double y1, double x2, double y2,
                                     Brush stroke, double thickness = 0.7, bool dashed = false)
        {
            var ln = new Line { X1=x1, Y1=y1, X2=x2, Y2=y2, Stroke=stroke, StrokeThickness=thickness };
            if (dashed) ln.StrokeDashArray = new DoubleCollection { 4, 3 };
            c.Children.Add(ln);
        }

        internal static void AddPolyline(Canvas c, IList<Point> pts, Brush stroke, double thickness = 0.8)
        {
            if (pts.Count < 2) return;
            var pl = new Polyline { Stroke=stroke, StrokeThickness=thickness };
            foreach (var p in pts) pl.Points.Add(p);
            c.Children.Add(pl);
        }

        internal static void AddRect(Canvas c, double x, double y, double w, double h, Brush fill)
        {
            var r = new Rectangle { Width=w, Height=h, Fill=fill };
            Canvas.SetLeft(r, x); Canvas.SetTop(r, y);
            c.Children.Add(r);
        }

        /// <summary>
        /// Add a text label. Pass the base size in screen points; the actual
        /// rendered size is base * ls where ls = LabelScale(canvasWidth).
        /// </summary>
        internal static void AddLabel(Canvas c, string text, double x, double y,
                                      Brush color, double size = 8, double ls = 1.0)
        {
            var tb = new TextBlock
            {
                Text       = text,
                Foreground = color,
                FontSize   = Math.Max(size * ls, 4.0),   // never below 4 pt
                FontFamily = new FontFamily("Arial")
            };
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
            c.Children.Add(tb);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  BODY PLAN — 21 stations, lines only
    //  Forward stations (≥ midship) on right, aft on left.
    //  Gold = waterline traces.  Light blue = station outlines.
    // ═══════════════════════════════════════════════════════════════════════════
    public class BodyPlanCanvas : Canvas
    {
        private ShipBuilderViewModel? _vm;

        public void SetViewModel(ShipBuilderViewModel vm)
        {
            _vm = vm;
            vm.PropertyChanged += (_, _) => Dispatcher.Invoke(Redraw);
            Redraw();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo info) { base.OnRenderSizeChanged(info); Redraw(); }

        private void Redraw()
        {
            Children.Clear();
            if (_vm is null || ActualWidth < 50 || ActualHeight < 50) return;

            LinesGeometry.UpdateCoefficients(_vm);

            double W  = ActualWidth, H = ActualHeight;
            double ls = LinesGeometry.LabelScale(W);   // label scale for this render size

            const double padL = 48, padR = 12, padT = 24, padB = 36;
            double drawW = W - padL - padR, drawH = H - padT - padB;

            double beam  = Math.Max(1, _vm.Breadth);
            double depth = Math.Max(1, _vm.Depth);
            double draft = Math.Clamp(_vm.Draft, 0.1, depth);

            double scaleY = (drawW / 2.0) / (beam / 2.0);
            double scaleZ = drawH / depth;
            double clX    = padL + drawW / 2.0;
            double keelY  = padT + drawH;

            double cy(double y) => clX + y * scaleY;
            double cz(double z) => keelY - z * scaleZ;

            LinesGeometry.AddRect(this, 0, 0, W, H, LinesGeometry.BrushBackground);
            LinesGeometry.AddLabel(this, "BODY PLAN",  padL,      4, LinesGeometry.BrushGold,  10, ls);
            LinesGeometry.AddLabel(this, "FWD →",      clX + 20,  4, LinesGeometry.BrushLabel,  8, ls);
            LinesGeometry.AddLabel(this, "← AFT",      clX - 55,  4, LinesGeometry.BrushLabel,  8, ls);

            // Waterline grid
            for (int w = 0; w <= LinesGeometry.NWaterlines; w++)
            {
                double z = depth * w / LinesGeometry.NWaterlines;
                LinesGeometry.AddLine(this, padL, cz(z), W - padR, cz(z), LinesGeometry.BrushGrid, 0.4, true);
                if (w > 0) LinesGeometry.AddLabel(this, $"WL{w}", 2, cz(z) - 6, LinesGeometry.BrushLabel, 7, ls);
            }

            // Design draft line
            LinesGeometry.AddLine(this, padL, cz(draft), W - padR, cz(draft), LinesGeometry.BrushDraftLine, 1.0);
            LinesGeometry.AddLabel(this, $"T={draft:F1}m", 2, cz(draft) - 6, LinesGeometry.BrushGold, 7, ls);

            // Baseline & CL
            LinesGeometry.AddLine(this, padL, keelY, W - padR, keelY, LinesGeometry.BrushCentreLine, 0.8);
            LinesGeometry.AddLabel(this, "BL", 2, keelY - 6, LinesGeometry.BrushLabel, 7, ls);
            LinesGeometry.AddLine(this, clX, padT, clX, keelY, LinesGeometry.BrushCentreLine, 0.5, true);

            const int nPts = 40;
            int mid = LinesGeometry.NStations / 2;

            // Station section curves
            for (int si = 0; si < LinesGeometry.NStations; si++)
            {
                double xn    = LinesGeometry.StationFraction(si);
                double hb    = (beam / 2.0) * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternBeamRatio, LinesGeometry.BowBeamRatio);
                double deckZ = depth * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternSheerRatio, LinesGeometry.BowSheerRatio);
                bool   fwd   = si >= mid;
                double sgn   = fwd ? 1.0 : -1.0;

                var pts = new List<Point> { new(clX, keelY) };
                for (int k = 0; k <= nPts; k++)
                {
                    double z = deckZ * k / nPts;
                    pts.Add(new Point(cy(LinesGeometry.HalfBreadthAt(z, hb, draft) * sgn), cz(z)));
                }
                pts.Add(new Point(cy(hb * sgn), cz(deckZ)));
                LinesGeometry.AddPolyline(this, pts, LinesGeometry.BrushStation, 0.8);

                string lbl = si == 0 ? "AP" : si == LinesGeometry.NStations - 1 ? "FP" : $"{si}";
                LinesGeometry.AddLabel(this, lbl, cy(hb * sgn * 0.5) - 5, keelY + 6, LinesGeometry.BrushGold, 7, ls);
            }

            // Gold waterline traces
            for (int w = 1; w <= LinesGeometry.NWaterlines; w++)
            {
                double z = depth * w / LinesGeometry.NWaterlines;
                // Forward (right)
                var fPts = new List<Point> { new(clX, cz(z)) };
                for (int si = mid; si < LinesGeometry.NStations; si++)
                {
                    double xn = LinesGeometry.StationFraction(si);
                    double hb = (beam / 2.0) * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternBeamRatio, LinesGeometry.BowBeamRatio);
                    double dk = depth * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternSheerRatio, LinesGeometry.BowSheerRatio);
                    if (z > dk) continue;
                    fPts.Add(new Point(cy(LinesGeometry.HalfBreadthAt(z, hb, draft)), cz(z)));
                }
                LinesGeometry.AddPolyline(this, fPts, LinesGeometry.BrushWaterline, 0.7);

                // Aft (left)
                var aPts = new List<Point> { new(clX, cz(z)) };
                for (int si = 0; si <= mid; si++)
                {
                    double xn = LinesGeometry.StationFraction(si);
                    double hb = (beam / 2.0) * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternBeamRatio, LinesGeometry.BowBeamRatio);
                    double dk = depth * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternSheerRatio, LinesGeometry.BowSheerRatio);
                    if (z > dk) continue;
                    aPts.Add(new Point(cy(-LinesGeometry.HalfBreadthAt(z, hb, draft)), cz(z)));
                }
                LinesGeometry.AddPolyline(this, aPts, LinesGeometry.BrushWaterline, 0.7);
            }

            // Deck sheer line
            var dkFwd = new List<Point>();
            var dkAft = new List<Point>();
            for (int si = 0; si < LinesGeometry.NStations; si++)
            {
                double xn = LinesGeometry.StationFraction(si);
                double hb = (beam / 2.0) * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternBeamRatio, LinesGeometry.BowBeamRatio);
                double dk = depth * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternSheerRatio, LinesGeometry.BowSheerRatio);
                if (si >= mid) dkFwd.Add(new Point(cy(hb),  cz(dk)));
                else           dkAft.Add(new Point(cy(-hb), cz(dk)));
            }
            LinesGeometry.AddPolyline(this, dkFwd, LinesGeometry.BrushGold, 1.0);
            LinesGeometry.AddPolyline(this, dkAft, LinesGeometry.BrushGold, 1.0);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  SHEER PLAN — profile view
    //  X = ship length (AP left → FP right).  Z = height.
    //  Gold = sheer line & waterlines.  Light blue = stations & buttocks.
    // ═══════════════════════════════════════════════════════════════════════════
    public class SheerPlanCanvas : Canvas
    {
        private ShipBuilderViewModel? _vm;

        public void SetViewModel(ShipBuilderViewModel vm)
        {
            _vm = vm;
            vm.PropertyChanged += (_, _) => Dispatcher.Invoke(Redraw);
            Redraw();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo info) { base.OnRenderSizeChanged(info); Redraw(); }

        private void Redraw()
        {
            Children.Clear();
            if (_vm is null || ActualWidth < 50 || ActualHeight < 50) return;

            LinesGeometry.UpdateCoefficients(_vm);

            double W  = ActualWidth, H = ActualHeight;
            double ls = LinesGeometry.LabelScale(W);

            const double padL = 48, padR = 24, padT = 24, padB = 36;
            double drawW = W - padL - padR, drawH = H - padT - padB;

            double lpp   = Math.Max(1, _vm.Lpp);
            double beam  = Math.Max(1, _vm.Breadth);
            double depth = Math.Max(1, _vm.Depth);
            double draft = Math.Clamp(_vm.Draft, 0.1, depth);

            double scaleX = drawW / lpp;
            double scaleZ = drawH / depth;
            double apX    = padL;
            double fpX    = padL + drawW;
            double keelY  = padT + drawH;

            double cx(double x) => apX + x * scaleX;
            double cz(double z) => keelY - z * scaleZ;

            LinesGeometry.AddRect(this, 0, 0, W, H, LinesGeometry.BrushBackground);
            LinesGeometry.AddLabel(this, "SHEER PLAN (PROFILE VIEW)", padL, 4, LinesGeometry.BrushGold, 10, ls);

            // WL grid
            for (int w = 0; w <= LinesGeometry.NWaterlines; w++)
            {
                double z   = depth * w / LinesGeometry.NWaterlines;
                string lbl = w == 0 ? "BL" : $"WL{w}";
                LinesGeometry.AddLine(this, apX, cz(z), fpX, cz(z), LinesGeometry.BrushGrid, 0.4, true);
                LinesGeometry.AddLabel(this, lbl, 2, cz(z) - 6, LinesGeometry.BrushLabel, 7, ls);
            }

            // Design draft line
            LinesGeometry.AddLine(this, apX, cz(draft), fpX, cz(draft), LinesGeometry.BrushDraftLine, 1.0);
            LinesGeometry.AddLabel(this, $"T={draft:F1}m", 2, cz(draft) - 6, LinesGeometry.BrushGold, 7, ls);

            // Baseline & AP/FP verticals
            LinesGeometry.AddLine(this, apX, keelY, fpX, keelY, LinesGeometry.BrushCentreLine, 0.8);
            LinesGeometry.AddLine(this, apX, padT,  apX, keelY, LinesGeometry.BrushStation, 0.8);
            LinesGeometry.AddLine(this, fpX, padT,  fpX, keelY, LinesGeometry.BrushStation, 0.8);
            LinesGeometry.AddLabel(this, "AP", apX - 4, keelY + 6, LinesGeometry.BrushGold, 7, ls);
            LinesGeometry.AddLabel(this, "FP", fpX - 6, keelY + 6, LinesGeometry.BrushGold, 7, ls);

            // Station verticals
            for (int si = 1; si < LinesGeometry.NStations - 1; si++)
            {
                double xn = LinesGeometry.StationFraction(si);
                double dk = depth * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternSheerRatio, LinesGeometry.BowSheerRatio);
                LinesGeometry.AddLine(this, cx(xn * lpp), cz(dk), cx(xn * lpp), keelY, LinesGeometry.BrushStation, 0.5);
                if (si % 5 == 0)
                    LinesGeometry.AddLabel(this, $"St{si}", cx(xn * lpp) - 8, keelY + 6, LinesGeometry.BrushLabel, 7, ls);
            }

            // Sheer (deck) line — gold
            var sheer = new List<Point>();
            for (int si = 0; si < LinesGeometry.NStations; si++)
            {
                double xn = LinesGeometry.StationFraction(si);
                double dk = depth * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternSheerRatio, LinesGeometry.BowSheerRatio);
                sheer.Add(new Point(cx(xn * lpp), cz(dk)));
            }
            LinesGeometry.AddPolyline(this, sheer, LinesGeometry.BrushGold, 1.2);
            LinesGeometry.AddLabel(this, "SHEER", fpX + 2, sheer[sheer.Count - 1].Y - 4, LinesGeometry.BrushGold, 7, ls);

            // Keel
            LinesGeometry.AddLine(this, apX, keelY, fpX, keelY, LinesGeometry.BrushStation, 1.0);

            // Waterlines (gold, flat)
            for (int w = 1; w <= LinesGeometry.NWaterlines; w++)
            {
                double z = depth * w / LinesGeometry.NWaterlines;
                var wlPts = new List<Point>();
                for (int si = 0; si < LinesGeometry.NStations; si++)
                {
                    double xn = LinesGeometry.StationFraction(si);
                    double dk = depth * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternSheerRatio, LinesGeometry.BowSheerRatio);
                    if (z > dk) continue;
                    wlPts.Add(new Point(cx(xn * lpp), cz(z)));
                }
                LinesGeometry.AddPolyline(this, wlPts, LinesGeometry.BrushWaterline, 0.7);
            }

            // Buttock lines
            for (int b = 1; b <= LinesGeometry.NButtocks; b++)
            {
                double yB   = (beam / 2.0) * b / LinesGeometry.NButtocks;
                var bPts    = new List<Point>();
                for (int si = 0; si < LinesGeometry.NStations; si++)
                {
                    double xn = LinesGeometry.StationFraction(si);
                    double hb = (beam / 2.0) * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternBeamRatio, LinesGeometry.BowBeamRatio);
                    double dk = depth * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternSheerRatio, LinesGeometry.BowSheerRatio);
                    if (yB > hb) continue;
                    double zAt = Math.Min(draft * Math.Pow(yB / hb, LinesGeometry.Exponent), dk);
                    bPts.Add(new Point(cx(xn * lpp), cz(zAt)));
                }
                LinesGeometry.AddPolyline(this, bPts, LinesGeometry.BrushButtock, 0.7);
                if (bPts.Count > 0)
                    LinesGeometry.AddLabel(this, $"B{b}", bPts[bPts.Count - 1].X + 2, bPts[bPts.Count - 1].Y - 4, LinesGeometry.BrushLabel, 7, ls);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  HALF-BREADTH PLAN — top view
    //  X = ship length.  Y = breadth (starboard up, port down).
    //  Gold = waterlines & deck plan.  Light blue = buttock lines & stations.
    // ═══════════════════════════════════════════════════════════════════════════
    public class HalfBreadthCanvas : Canvas
    {
        private ShipBuilderViewModel? _vm;

        public void SetViewModel(ShipBuilderViewModel vm)
        {
            _vm = vm;
            vm.PropertyChanged += (_, _) => Dispatcher.Invoke(Redraw);
            Redraw();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo info) { base.OnRenderSizeChanged(info); Redraw(); }

        private void Redraw()
        {
            Children.Clear();
            if (_vm is null || ActualWidth < 50 || ActualHeight < 50) return;

            LinesGeometry.UpdateCoefficients(_vm);

            double W  = ActualWidth, H = ActualHeight;
            double ls = LinesGeometry.LabelScale(W);

            const double padL = 48, padR = 24, padT = 24, padB = 36;
            double drawW = W - padL - padR, drawH = H - padT - padB;

            double lpp   = Math.Max(1, _vm.Lpp);
            double beam  = Math.Max(1, _vm.Breadth);
            double depth = Math.Max(1, _vm.Depth);
            double draft = Math.Clamp(_vm.Draft, 0.1, depth);

            double scaleX = drawW / lpp;
            double scaleY = (drawH / 2.0) / (beam / 2.0);
            double apX    = padL;
            double fpX    = padL + drawW;
            double clY    = padT + drawH / 2.0;

            double cx(double x) => apX + x * scaleX;
            double cy(double y) => clY - y * scaleY;

            LinesGeometry.AddRect(this, 0, 0, W, H, LinesGeometry.BrushBackground);
            LinesGeometry.AddLabel(this, "HALF-BREADTH PLAN (TOP VIEW)", padL, 4, LinesGeometry.BrushGold, 10, ls);
            LinesGeometry.AddLabel(this, "STBD ↑", W - padR - 36, clY - drawH / 2.0 + 2, LinesGeometry.BrushLabel, 7, ls);
            LinesGeometry.AddLabel(this, "PORT ↓", W - padR - 36, clY + 4,               LinesGeometry.BrushLabel, 7, ls);

            // Centreline
            LinesGeometry.AddLine(this, apX, clY, fpX, clY, LinesGeometry.BrushCentreLine, 0.6, true);
            LinesGeometry.AddLabel(this, "CL", 2, clY - 6, LinesGeometry.BrushLabel, 7, ls);

            // AP / FP verticals
            LinesGeometry.AddLine(this, apX, padT, apX, padT + drawH, LinesGeometry.BrushStation, 0.8);
            LinesGeometry.AddLine(this, fpX, padT, fpX, padT + drawH, LinesGeometry.BrushStation, 0.8);
            LinesGeometry.AddLabel(this, "AP", apX - 4, padT + drawH + 4, LinesGeometry.BrushGold, 7, ls);
            LinesGeometry.AddLabel(this, "FP", fpX - 6, padT + drawH + 4, LinesGeometry.BrushGold, 7, ls);

            // Station verticals (faint)
            for (int si = 1; si < LinesGeometry.NStations - 1; si++)
            {
                double xn = LinesGeometry.StationFraction(si);
                double x  = cx(xn * lpp);
                LinesGeometry.AddLine(this, x, padT, x, padT + drawH, LinesGeometry.BrushGrid, 0.3, true);
                if (si % 5 == 0)
                    LinesGeometry.AddLabel(this, $"{si}", x - 4, padT + drawH + 4, LinesGeometry.BrushLabel, 7, ls);
            }

            // Waterlines as plan curves (gold)
            for (int w = 0; w <= LinesGeometry.NWaterlines; w++)
            {
                double z       = depth * w / LinesGeometry.NWaterlines;
                bool isDraft   = Math.Abs(z - draft) < depth * 0.015;
                var stbd       = new List<Point>();
                var port       = new List<Point>();

                for (int si = 0; si < LinesGeometry.NStations; si++)
                {
                    double xn = LinesGeometry.StationFraction(si);
                    double hb = (beam / 2.0) * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternBeamRatio, LinesGeometry.BowBeamRatio);
                    double dk = depth * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternSheerRatio, LinesGeometry.BowSheerRatio);
                    if (z > dk) continue;
                    double y = w == 0 ? 0 : LinesGeometry.HalfBreadthAt(z, hb, draft);
                    stbd.Add(new Point(cx(xn * lpp), cy(y)));
                    if (w > 0) port.Add(new Point(cx(xn * lpp), cy(-y)));
                }

                double th = isDraft ? 1.1 : 0.8;
                LinesGeometry.AddPolyline(this, stbd, LinesGeometry.BrushWaterline, th);
                LinesGeometry.AddPolyline(this, port, LinesGeometry.BrushWaterline, th);

                if (stbd.Count > 0)
                {
                    string lbl = w == 0 ? "BL" : isDraft ? $"T={draft:F1}m" : $"WL{w}";
                    LinesGeometry.AddLabel(this, lbl,
                        stbd[stbd.Count - 1].X + 2, stbd[stbd.Count - 1].Y - 5,
                        LinesGeometry.BrushLabel, 7, ls);
                }
            }

            // Deck plan outline (gold)
            var dkStbd = new List<Point>();
            var dkPort = new List<Point>();
            for (int si = 0; si < LinesGeometry.NStations; si++)
            {
                double xn = LinesGeometry.StationFraction(si);
                double hb = (beam / 2.0) * LinesGeometry.BeamEnvelope(xn, LinesGeometry.SternBeamRatio, LinesGeometry.BowBeamRatio);
                dkStbd.Add(new Point(cx(xn * lpp), cy(hb)));
                dkPort.Add(new Point(cx(xn * lpp), cy(-hb)));
            }
            LinesGeometry.AddPolyline(this, dkStbd, LinesGeometry.BrushGold, 1.1);
            LinesGeometry.AddPolyline(this, dkPort, LinesGeometry.BrushGold, 1.1);
            LinesGeometry.AddLabel(this, "DECK",
                fpX + 2, dkStbd[dkStbd.Count - 1].Y - 4,
                LinesGeometry.BrushGold, 7, ls);

            // Buttock lines (constant y offsets — light blue dashed)
            for (int b = 1; b <= LinesGeometry.NButtocks; b++)
            {
                double yB = (beam / 2.0) * b / LinesGeometry.NButtocks;
                LinesGeometry.AddLine(this, apX, cy(yB),  fpX, cy(yB),  LinesGeometry.BrushButtock, 0.6, true);
                LinesGeometry.AddLine(this, apX, cy(-yB), fpX, cy(-yB), LinesGeometry.BrushButtock, 0.6, true);
                LinesGeometry.AddLabel(this, $"B{b}", apX - 22, cy(yB) - 5, LinesGeometry.BrushLabel, 7, ls);
            }
        }
    }
}
