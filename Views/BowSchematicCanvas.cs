using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NavalArchitectureSuite.ViewModels;

namespace NavalArchitectureSuite.Views
{
    /// <summary>
    /// A Canvas-based 2D bow profile schematic that reads live from BowDesignViewModel.
    /// Shows: hull profile, bulb geometry (B_B, L_B, z_B), waterline, boot-topping,
    /// collision bulkhead position, and dimension annotations.
    /// </summary>
    public class BowSchematicCanvas : Canvas
    {
        // ── Palette matching the Suite dark navy/gold theme ──────────────────
        private static readonly Brush BrushNavy       = new SolidColorBrush(Color.FromRgb(0x0A, 0x1F, 0x3D));
        private static readonly Brush BrushUnderwater = new SolidColorBrush(Color.FromRgb(0x1A, 0x3A, 0x5C));
        private static readonly Brush BrushBootTop    = new SolidColorBrush(Color.FromRgb(0x8B, 0x1A, 0x1A));
        private static readonly Brush BrushTopside    = new SolidColorBrush(Color.FromRgb(0xA8, 0xB4, 0xC0));
        private static readonly Brush BrushDeck       = new SolidColorBrush(Color.FromRgb(0xD4, 0xC8, 0xA8));
        private static readonly Brush BrushBulb       = new SolidColorBrush(Color.FromRgb(0x1A, 0x3A, 0x5C));
        private static readonly Brush BrushWaterline  = new SolidColorBrush(Color.FromArgb(180, 0xC8, 0x96, 0x0C));
        private static readonly Brush BrushGold       = new SolidColorBrush(Color.FromRgb(0xC8, 0x96, 0x0C));
        private static readonly Brush BrushGoldLight  = new SolidColorBrush(Color.FromRgb(0xF2, 0xC9, 0x4C));
        private static readonly Brush BrushGrid       = new SolidColorBrush(Color.FromArgb(40, 0x8F, 0xA6, 0xC9));
        private static readonly Brush BrushBulkhead   = new SolidColorBrush(Color.FromArgb(200, 0x3D, 0xDC, 0x84));
        private static readonly Brush BrushText       = new SolidColorBrush(Color.FromRgb(0xD7, 0xE1, 0xF0));
        private static readonly Brush BrushTextMuted  = new SolidColorBrush(Color.FromRgb(0x8F, 0xA6, 0xC9));
        private static readonly Brush BrushBackground = new SolidColorBrush(Color.FromRgb(0x06, 0x10, 0x1E));

        private BowDesignViewModel? _vm;

        public void SetViewModel(BowDesignViewModel vm)
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
            AddRect(0, 0, W, H, BrushBackground, null, 0);

            // ── Read ViewModel values ────────────────────────────────────────
            double T       = Math.Max(1, _vm.Draft);
            double depth   = Math.Max(T + 1, _vm.Depth);
            double lpp     = Math.Max(1, _vm.Lpp);
            double B       = Math.Max(1, _vm.Breadth);
            double bB      = _vm.BulbBreadth;
            double lB      = _vm.BulbLength;
            double zB      = _vm.ZB;
            double cbMin   = _vm.CollisionBulkheadMin;
            double cbRec   = _vm.CollisionBulkheadRecommended;

            // ── Layout constants ─────────────────────────────────────────────
            // We show only the forward 25% of the ship in profile (side view, looking to starboard)
            // X axis: FP at right, stern direction to the left
            // Z axis: keel at bottom, deck at top

            double padL   = 80;   // left margin (labels)
            double padR   = 100;  // right margin (annotations right of FP)
            double padT   = 40;   // top margin
            double padB   = 60;   // bottom margin

            double drawW  = W - padL - padR;
            double drawH  = H - padT - padB;

            // Scale: show forward 30% of ship length
            double showL  = lpp * 0.30;
            double scaleX = drawW / (showL + lB);   // lB protrudes forward of FP
            double scaleZ = drawH / (depth * 1.05);

            // Canvas coords: origin at keel/FP intersection
            // cx(x) converts ship coords (0=FP, positive=aft) to canvas X
            // cz(z) converts ship coords (0=keel, positive=up) to canvas Y (inverted)
            double fpCanvasX = padL + lB * scaleX;   // FP position on canvas
            double keelCanvasY = padT + drawH;         // keel on canvas

            double cx(double x) => fpCanvasX - x * scaleX;       // aft = more left
            double cz(double z) => keelCanvasY - z * scaleZ;      // up = more up

            // ── Grid lines ──────────────────────────────────────────────────
            // Horizontal at T (waterline), T*0.9 (boot top bottom), deck
            var gridPen = new Pen(BrushGrid, 0.5) { DashStyle = DashStyles.Dot };

            // Vertical grid every 20% Lpp shown
            for (int i = 0; i <= 3; i++)
            {
                double xShip = showL * i / 3.0;
                double xC    = cx(xShip);
                AddLine(xC, padT, xC, keelCanvasY, BrushGrid, 0.5, isDashed: true);
            }

            // ── Hull profile (side view, starboard side) ─────────────────────
            // We draw the hull as a filled polygon: keel line → bow curve → deck → aft cut
            // Using simplified parabolic/cosine curves matching Cb & Cwp

            double aftCut = showL;   // ship coordinate of the aft cut of our view

            // Waterplane half-breadth at any station x (0=FP): b(x) = B/2 * f(x)
            // For profile view we show draft curve: keel at 0, section shape at FP is V-shaped
            // In profile (looking at starboard side) we see the sheer line and the keel line

            // Sheer (deck) line: rises toward bow
            // Keel line: straight (simplification)
            // Bow profile: raked stem

            // For side profile, x=ship station, z=height
            // Stem profile: from keel at FP+lB*0 to stem head at deck level
            // Simplified as a straight rake or slight S-curve

            double stemRakeX = depth * 0.15;   // stem leans forward ~15% of depth

            // Key points (ship coordinates)
            // Keel line from aft to FP (z=0)
            // Then up the stem to the deck
            // Then deck line aft
            // Then down at aft cut

            int nStations = 40;
            var hullPoints = new PointCollection();

            // Aft bottom corner
            hullPoints.Add(new Point(cx(aftCut), cz(0)));

            // Keel line to FP
            for (int i = nStations; i >= 0; i--)
            {
                double xS = aftCut * i / nStations;
                hullPoints.Add(new Point(cx(xS), cz(0)));
            }

            // Stem curve from keel-FP up to deck (forward of FP by stemRakeX)
            // The stem head is at (x = -stemRakeX, z = depth)
            int stemPts = 20;
            for (int i = 0; i <= stemPts; i++)
            {
                double t   = (double)i / stemPts;
                double xS  = -stemRakeX * t;             // forward of FP (negative = forward)
                double zS  = depth * t;
                // Small S-curve: x shift is sinusoidal
                double xCurve = stemRakeX * Math.Sin(t * Math.PI) * 0.3;
                hullPoints.Add(new Point(cx(xS + xCurve), cz(zS)));
            }

            // Deck line from stem head back aft
            for (int i = 0; i <= nStations; i++)
            {
                double xS = stemRakeX + aftCut * i / nStations;
                // Small sheer: deck rises slightly toward bow
                double sheer = depth * 0.03 * (1.0 - (double)i / nStations);
                hullPoints.Add(new Point(cx(xS), cz(depth + sheer)));
            }

            // Aft deck corner
            hullPoints.Add(new Point(cx(aftCut), cz(depth)));

            // ── Draw hull zones as stacked polygons ──────────────────────────
            double bootTopBot = T - T * 0.04;
            double bootTopTop = T + T * 0.04;
            double deckThick  = depth * 0.025;

            DrawHullZone(cx, cz, aftCut, stemRakeX, nStations, depth, 0,         bootTopBot, T,          BrushUnderwater);
            DrawHullZone(cx, cz, aftCut, stemRakeX, nStations, depth, bootTopBot, bootTopTop, T,          BrushBootTop);
            DrawHullZone(cx, cz, aftCut, stemRakeX, nStations, depth, bootTopTop, depth-deckThick, T,     BrushTopside);
            DrawHullZone(cx, cz, aftCut, stemRakeX, nStations, depth, depth-deckThick, depth, T,         BrushDeck);

            // Hull outline
            var hullOutline = new Polygon
            {
                Points = hullPoints,
                Fill = Brushes.Transparent,
                Stroke = BrushTextMuted,
                StrokeThickness = 0.8
            };
            Children.Add(hullOutline);

            // ── Bulbous bow ──────────────────────────────────────────────────
            if (bB > 0.1 && lB > 0.1)
            {
                // Bulb in profile: ellipse centered at (x=-lB/2, z=zB) with
                // half-axes: horizontal=lB, vertical=bB/2
                double bulbCx = fpCanvasX + lB * scaleX * 0.5;
                double bulbCy = cz(zB);
                double bulbRx = lB * scaleX * 0.8;
                double bulbRy = (bB / 2.0) * scaleZ * 0.9;

                var bulbEllipse = new Ellipse
                {
                    Width  = bulbRx * 2,
                    Height = bulbRy * 2,
                    Fill   = BrushBulb,
                    Stroke = BrushTextMuted,
                    StrokeThickness = 0.8
                };
                SetLeft(bulbEllipse, bulbCx - bulbRx);
                SetTop(bulbEllipse, bulbCy - bulbRy);
                Children.Add(bulbEllipse);

                // ── Bulb dimension annotations ────────────────────────────────
                // B_B — vertical arrow at FP
                double fpX = fpCanvasX;
                DrawDimArrow(fpX + 18, cz(zB - bB / 2), fpX + 18, cz(zB + bB / 2),
                             $"B_B = {bB:F1} m", fpX + 22, cz(zB), vertical: true);

                // z_B — horizontal dashed from keel to bulb centre
                AddLine(padL, cz(zB), fpX, cz(zB), BrushGold, 0.5, isDashed: true);
                AddLabel($"z_B = {zB:F1} m", padL - 4, cz(zB) - 6, BrushGold, 10, right: true);

                // L_B — horizontal arrow above bulb
                double bulbTop = cz(zB + bB / 2) - 14;
                DrawDimArrow(fpX, bulbTop, fpX + lB * scaleX, bulbTop,
                             $"L_B = {lB:F1} m", fpX + lB * scaleX / 2, bulbTop - 8, vertical: false);
            }

            // ── Waterline ────────────────────────────────────────────────────
            double wlY = cz(T);
            AddLine(padL, wlY, W - padR + 20, wlY, BrushWaterline, 1.2);
            AddLabel("WL", W - padR + 22, wlY - 6, BrushWaterline, 10);
            AddLabel($"T = {T:F1} m", padL - 4, wlY - 6, BrushGold, 10, right: true);

            // ── Boot-topping band label ──────────────────────────────────────
            double btMidY = cz((bootTopBot + bootTopTop) / 2.0);
            AddLabel("boot top", padL - 4, btMidY - 5, new SolidColorBrush(Color.FromRgb(0xCC, 0x60, 0x60)), 9, right: true);

            // ── FP line ──────────────────────────────────────────────────────
            AddLine(fpCanvasX, padT, fpCanvasX, keelCanvasY + 20, BrushTextMuted, 0.6, isDashed: true);
            AddLabel("FP", fpCanvasX - 10, keelCanvasY + 24, BrushTextMuted, 10);

            // ── Collision bulkhead ───────────────────────────────────────────
            double cbX = cx(cbRec);
            if (cbX > padL && cbX < W - padR)
            {
                AddLine(cbX, cz(0), cbX, cz(depth), BrushBulkhead, 1.2, isDashed: false);
                AddLabel($"C/B\n{cbRec:F1} m", cbX + 4, cz(depth * 0.6), BrushBulkhead, 9);
            }

            // ── Depth dimension ──────────────────────────────────────────────
            double dimX = W - padR + 50;
            DrawDimArrow(dimX, cz(0), dimX, cz(depth),
                         $"D = {depth:F1} m", dimX + 4, cz(depth / 2), vertical: true);

            // ── Title ────────────────────────────────────────────────────────
            AddLabel("Bow profile — starboard side view", padL, padT - 8, BrushTextMuted, 10);

            // ── Legend ───────────────────────────────────────────────────────
            double legX = padL;
            double legY = keelCanvasY + 30;
            DrawLegendItem(legX,      legY, BrushUnderwater, "underwater body");
            DrawLegendItem(legX + 130, legY, BrushBootTop,   "boot topping");
            DrawLegendItem(legX + 255, legY, BrushTopside,   "topside");
            DrawLegendItem(legX + 360, legY, BrushDeck,      "deck");
            DrawLegendItem(legX + 445, legY, BrushBulkhead,  "collision bulkhead");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void DrawHullZone(
            Func<double, double> cx, Func<double, double> cz,
            double aftCut, double stemRakeX, int n,
            double depth, double zLow, double zHigh, double T,
            Brush fill)
        {
            // Side profile zone: bounded between zLow and zHigh
            // Left edge: aft cut; right/top: stem; bottom: keel or z=0
            var pts = new PointCollection();

            // Bottom-aft corner
            double zBot = Math.Max(0, zLow);
            pts.Add(new Point(cx(aftCut), cz(zBot)));

            // Bottom line along keel/zLow
            for (int i = n; i >= 0; i--)
            {
                double xS = aftCut * i / n;
                pts.Add(new Point(cx(xS), cz(zBot)));
            }

            // Up the stem at zLow
            if (zLow <= 0)
            {
                // Follow the stem from keel to zHigh
                int stemPts = 10;
                for (int i = 0; i <= stemPts; i++)
                {
                    double t = zHigh / depth * i / stemPts;
                    double xCurve = stemRakeX * Math.Sin(t * Math.PI / (zHigh / depth)) * 0.3;
                    pts.Add(new Point(cx(-stemRakeX * t + xCurve), cz(depth * t)));
                    if (depth * t >= zHigh) break;
                }
            }
            else
            {
                // Left edge at xStem(zLow)
                double tLow = zLow / depth;
                double xStemLow = -stemRakeX * tLow + stemRakeX * Math.Sin(tLow * Math.PI) * 0.3;
                pts.Add(new Point(cx(xStemLow), cz(zLow)));

                // Follow stem from zLow to zHigh
                int stemPts = 8;
                for (int i = 0; i <= stemPts; i++)
                {
                    double t = tLow + (zHigh / depth - tLow) * i / stemPts;
                    double xCurve = stemRakeX * Math.Sin(t * Math.PI) * 0.3;
                    pts.Add(new Point(cx(-stemRakeX * t + xCurve), cz(depth * t)));
                }
            }

            // Top line from stem back aft
            double zTop = Math.Min(depth, zHigh);
            int nTop = n;
            double tHigh = zTop / depth;
            double xStemHigh = -stemRakeX * tHigh + stemRakeX * Math.Sin(tHigh * Math.PI) * 0.3;

            for (int i = 0; i <= nTop; i++)
            {
                double frac = (double)i / nTop;
                double xS = xStemHigh + (aftCut - xStemHigh) * frac;
                double sheer = (zTop >= depth * 0.95) ? depth * 0.03 * (1.0 - frac) : 0;
                pts.Add(new Point(cx(xS), cz(zTop + sheer)));
            }

            // Close
            pts.Add(new Point(cx(aftCut), cz(zTop)));

            var polygon = new Polygon
            {
                Points = pts,
                Fill = fill,
                Stroke = Brushes.Transparent,
                StrokeThickness = 0
            };
            Children.Add(polygon);
        }

        private void DrawDimArrow(double x1, double y1, double x2, double y2,
                                   string label, double labelX, double labelY,
                                   bool vertical)
        {
            var pen = new Pen(BrushGoldLight, 0.8);
            // End arrows (ticks)
            double tick = 5;
            if (vertical)
            {
                AddLine(x1 - tick, y1, x1 + tick, y1, BrushGoldLight, 0.8);
                AddLine(x2 - tick, y2, x2 + tick, y2, BrushGoldLight, 0.8);
                AddLine(x1, y1, x2, y2, BrushGoldLight, 0.8);
            }
            else
            {
                AddLine(x1, y1 - tick, x1, y1 + tick, BrushGoldLight, 0.8);
                AddLine(x2, y2 - tick, x2, y2 + tick, BrushGoldLight, 0.8);
                AddLine(x1, y1, x2, y2, BrushGoldLight, 0.8);
            }
            AddLabel(label, labelX, labelY, BrushGoldLight, 9);
        }

        private void DrawLegendItem(double x, double y, Brush color, string label)
        {
            AddRect(x, y, 14, 9, color, null, 0);
            AddLabel(label, x + 17, y, BrushTextMuted, 9);
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

        private void AddRect(double x, double y, double w, double h,
                              Brush? fill, Brush? stroke, double strokeThickness)
        {
            var rect = new Rectangle
            {
                Width = w, Height = h,
                Fill = fill ?? Brushes.Transparent,
                Stroke = stroke ?? Brushes.Transparent,
                StrokeThickness = strokeThickness
            };
            SetLeft(rect, x); SetTop(rect, y);
            Children.Add(rect);
        }

        private void AddLabel(string text, double x, double y,
                               Brush color, double size, bool right = false)
        {
            var tb = new TextBlock
            {
                Text = text,
                Foreground = color,
                FontSize = size,
                FontFamily = new FontFamily("Arial"),
                TextWrapping = TextWrapping.NoWrap
            };
            if (right)
            {
                tb.Measure(new Size(200, 30));
                x -= tb.DesiredSize.Width;
            }
            SetLeft(tb, x); SetTop(tb, y);
            Children.Add(tb);
        }
    }
}
