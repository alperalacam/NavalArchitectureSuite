using System.Collections.Generic;
using System.IO;
using OxyPlot;
using OxyPlot.Wpf;

namespace NavalArchitectureSuite.Services
{
    /// <summary>
    /// Renders an OxyPlot PlotModel to a PNG byte array entirely in memory,
    /// with a white background and enlarged fonts suitable for PDF embedding.
    /// </summary>
    public static class ChartImageRenderer
    {
        public static byte[] RenderToPng(PlotModel model, int widthPx = 1100, int heightPx = 500)
        {
            // ── Save original values ─────────────────────────────────────────
            var originalBackground = model.Background;
            double origTitleSize   = model.TitleFontSize;
            double origSubSize     = model.SubtitleFontSize;
            double origLegendSize  = 12.0; // saved for reference only

            var savedAxis = new List<(OxyPlot.Axes.Axis axis,
                                      double fontSize,
                                      double titleSize)>();
            foreach (var axis in model.Axes)
                savedAxis.Add((axis, axis.FontSize, axis.TitleFontSize));

            var savedSeries = new List<(OxyPlot.Series.Series s, double fontSize)>();
            foreach (var s in model.Series)
                savedSeries.Add((s, s.FontSize));

            // ── Apply large print-friendly sizes ────────────────────────────
            model.Background       = OxyColors.White;
            model.TitleFontSize    = 18;
            model.SubtitleFontSize = 14;

            foreach (var axis in model.Axes)
            {
                axis.FontSize      = 16;   // tick number labels
                axis.TitleFontSize = 18;   // axis title: "GZ (m)", "Heel Angle (deg)" etc.
            }

            foreach (var s in model.Series)
                s.FontSize = 14;

            // ── Export ──────────────────────────────────────────────────────
            try
            {
                var exporter = new PngExporter
                {
                    Width  = widthPx,
                    Height = heightPx
                };

                using var ms = new MemoryStream();
                exporter.Export(model, ms);
                return ms.ToArray();
            }
            finally
            {
                // ── Restore for the live UI ──────────────────────────────────
                model.Background       = originalBackground;
                model.TitleFontSize    = origTitleSize;
                model.SubtitleFontSize = origSubSize;

                foreach (var (axis, fontSize, titleSize) in savedAxis)
                {
                    axis.FontSize      = fontSize;
                    axis.TitleFontSize = titleSize;
                }

                foreach (var (s, fontSize) in savedSeries)
                    s.FontSize = fontSize;
            }
        }
    }
}
