using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OxyPlot;
using OxyPlot.Wpf;

namespace NavalArchitectureSuite.Services
{
    /// <summary>
    /// Renders an OxyPlot PlotModel to a PNG byte array entirely in memory,
    /// with a white background suitable for PDF embedding.
    /// </summary>
    public static class ChartImageRenderer
    {
        /// <summary>
        /// Renders the given OxyPlot model to a PNG byte array.
        /// </summary>
        /// <param name="model">The OxyPlot PlotModel to render.</param>
        /// <param name="widthPx">Width in pixels (default 800).</param>
        /// <param name="heightPx">Height in pixels (default 400).</param>
        public static byte[] RenderToPng(PlotModel model, int widthPx = 800, int heightPx = 400)
        {
            // Clone background to white for print output.
            var originalBackground = model.Background;
            model.Background = OxyColors.White;

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
                // Restore the original dark background for the live UI.
                model.Background = originalBackground;
            }
        }
    }
}
