using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace NavalArchitectureSuite.Services
{
    /// <summary>
    /// Hand-rolled, dependency-free single/multi-page PDF writer for simple text reports
    /// (title lines, body lines, two-column monospace tables). The project has no PDF
    /// library referenced, so this assembles the PDF object graph (objects, xref, trailer)
    /// directly rather than pulling in a new package. Content is set entirely in Courier
    /// so column alignment is exact without real font-metrics.
    /// </summary>
    /// <summary>Standard paper sizes supported by the PDF writer.</summary>
    public enum PdfPaperSize
    {
        A4,   // 210 x 297 mm  =  595 x 842 pt
        A3,   // 297 x 420 mm  =  842 x 1191 pt
        A1,   // 594 x 841 mm  =  1684 x 2384 pt
        A0,   // 841 x 1189 mm =  2384 x 3370 pt
    }

    public sealed class SimplePdfDocument
    {
        private readonly double PageWidth;
        private readonly double PageHeight;
        private const double MarginX  = 54;
        private double TopY    => PageHeight - 54;
        private const double BottomY  = 54;

        public PdfPaperSize PaperSize { get; }

        public SimplePdfDocument(PdfPaperSize size = PdfPaperSize.A4)
        {
            PaperSize = size;
            (PageWidth, PageHeight) = size switch
            {
                PdfPaperSize.A4 => (595.0,  842.0),
                PdfPaperSize.A3 => (842.0,  1191.0),
                PdfPaperSize.A1 => (1684.0, 2384.0),
                PdfPaperSize.A0 => (2384.0, 3370.0),
                _               => (595.0,  842.0),
            };
            _y = TopY;
        }

        private const string RegularFont = "F1"; // Courier
        private const string BoldFont    = "F2"; // Courier-Bold

        private readonly record struct Line(double Y, string Text, string Font, double Size);
        private readonly record struct ImageBlock(double Y, double Width, double Height, int ImageIndex);

        private readonly record struct PageContent(
            List<Line> Lines,
            List<ImageBlock> Images);

        private readonly List<PageContent> _pages = new();
        private List<Line>       _currentLines  = new();
        private List<ImageBlock> _currentImages = new();
        private double _y;

        // Accumulated PNG image bytes — each entry becomes a separate PDF XObject.
        private readonly List<byte[]> _images = new();

        public void AddLine(string text, double size = 10, bool bold = false)
        {
            EnsureRoom(size);
            _currentLines.Add(new Line(_y, text, bold ? BoldFont : RegularFont, size));
            _y -= size * 1.35;
        }

        public void AddWrapped(string text, int maxChars, double size = 10, bool bold = false)
        {
            foreach (var line in Wrap(text, maxChars))
                AddLine(line, size, bold);
        }

        /// <summary>Left/right justified line using padding — exact under Courier's fixed glyph width.</summary>
        public void AddTwoColumn(string left, string right, int totalChars, double size = 9, bool bold = false)
        {
            int pad = Math.Max(1, totalChars - left.Length - right.Length);
            AddLine(left + new string(' ', pad) + right, size, bold);
        }

        public void AddRule(int length = 88) => AddLine(new string('-', length), 9);

        public void AddSpacer(double amount = 8) => _y -= amount;

        /// <summary>
        /// Embeds a PNG image (supplied as raw bytes) into the current page.
        /// The image is scaled to fit the printable width and the cursor advances below it.
        /// </summary>
        public void AddImage(byte[] pngBytes, double maxWidthPts = 504, double maxHeightPts = 280)
        {
            if (pngBytes is null || pngBytes.Length == 0) return;

            // Read PNG dimensions from header (bytes 16-23: width then height, big-endian).
            int srcW = (pngBytes[16] << 24) | (pngBytes[17] << 16) | (pngBytes[18] << 8) | pngBytes[19];
            int srcH = (pngBytes[20] << 24) | (pngBytes[21] << 16) | (pngBytes[22] << 8) | pngBytes[23];

            double aspect = srcH > 0 ? (double)srcW / srcH : 2.0;
            double imgW = Math.Min(maxWidthPts, maxHeightPts * aspect);
            double imgH = imgW / aspect;
            if (imgH > maxHeightPts) { imgH = maxHeightPts; imgW = imgH * aspect; }

            // Start a new page if there is not enough room.
            if (_y - imgH < BottomY)
            {
                FlushPage();
            }

            int imageIndex = _images.Count;
            _images.Add(pngBytes);

            // PDF Y coordinate: bottom of image.
            double imgYBottom = _y - imgH;
            _currentImages.Add(new ImageBlock(imgYBottom, imgW, imgH, imageIndex));
            _y = imgYBottom - 8; // advance cursor below image
        }

        public static List<string> Wrap(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text)) return new List<string> { string.Empty };
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var lines = new List<string>();
            var sb = new StringBuilder();
            foreach (var word in words)
            {
                if (sb.Length > 0 && sb.Length + 1 + word.Length > maxChars)
                {
                    lines.Add(sb.ToString());
                    sb.Clear();
                }
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(word);
            }
            if (sb.Length > 0) lines.Add(sb.ToString());
            if (lines.Count == 0) lines.Add(string.Empty);
            return lines;
        }

        private void FlushPage()
        {
            _pages.Add(new PageContent(_currentLines, _currentImages));
            _currentLines  = new List<Line>();
            _currentImages = new List<ImageBlock>();
            _y = TopY;
        }

        private void EnsureRoom(double size)
        {
            if (_y - size < BottomY)
                FlushPage();
        }

        public byte[] Build()
        {
            FlushPage(); // commit the last in-progress page

            int imageCount  = _images.Count;
            int pageCount   = _pages.Count;

            // Object numbering:
            //  1          = Catalog
            //  2          = Pages
            //  3          = Courier font
            //  4          = Courier-Bold font
            //  5..4+N     = PNG image XObjects (N = imageCount)
            //  5+N..      = page pairs (Page dict + content stream), 2 objects each
            int firstImageObj = 5;
            int firstPageObj  = firstImageObj + imageCount;

            var objBytes = new Dictionary<int, byte[]>();

            objBytes[1] = Ascii("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            string kids = string.Join(" ", System.Linq.Enumerable.Range(0, pageCount)
                .Select(i => $"{firstPageObj + 2 * i} 0 R"));
            objBytes[2] = Ascii($"2 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {pageCount} >>\nendobj\n");

            objBytes[3] = Ascii("3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>\nendobj\n");
            objBytes[4] = Ascii("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

            // --- PNG image XObjects ---
            for (int idx = 0; idx < imageCount; idx++)
            {
                byte[] png  = _images[idx];
                int objNum  = firstImageObj + idx;

                // Read width/height from PNG header.
                int w = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
                int h = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
                int bitDepth   = png[24];
                int colorType  = png[25]; // 2=RGB, 6=RGBA

                // PDF expects raw (decoded) image data, but we can use FlateDecode
                // to pass the PNG IDAT chunk data directly.  However the simplest
                // universally-supported approach is to embed the whole PNG file
                // and use the /DCTDecode or /FlateDecode filter.  Since WPF's
                // PngExporter writes valid PNG files we embed them directly using
                // the /ASCIIHexDecode + /FlateDecode pipeline by writing the PNG
                // bytes as a raw stream with the /PNG filter — not all viewers
                // support that. The safest approach for a hand-rolled writer is to
                // convert to a flat raw RGB stream. We do that here by decoding the
                // PNG in managed code via System.Windows.Media.Imaging.
                byte[] rawRgb = DecodeToRgb24(png, out int imgW, out int imgH);

                byte[] header = Ascii(
                    $"{objNum} 0 obj\n" +
                    $"<< /Type /XObject /Subtype /Image " +
                    $"/Width {imgW} /Height {imgH} " +
                    $"/ColorSpace /DeviceRGB /BitsPerComponent 8 " +
                    $"/Length {rawRgb.Length} >>\n" +
                    $"stream\n");
                byte[] footer = Ascii("\nendstream\nendobj\n");

                var combined = new byte[header.Length + rawRgb.Length + footer.Length];
                Buffer.BlockCopy(header, 0, combined, 0, header.Length);
                Buffer.BlockCopy(rawRgb, 0, combined, header.Length, rawRgb.Length);
                Buffer.BlockCopy(footer, 0, combined, header.Length + rawRgb.Length, footer.Length);
                objBytes[objNum] = combined;
            }

            // --- Page pairs ---
            // Build XObject resource string once (same images on every page).
            string xObjRes = imageCount > 0
                ? "/XObject << " + string.Join(" ",
                    System.Linq.Enumerable.Range(0, imageCount)
                        .Select(i => $"/Img{i} {firstImageObj + i} 0 R")) + " >>"
                : string.Empty;

            for (int i = 0; i < pageCount; i++)
            {
                int pageObjNum    = firstPageObj + 2 * i;
                int contentObjNum = pageObjNum + 1;

                var page = _pages[i];
                var content = new StringBuilder();

                // Text
                if (page.Lines.Count > 0)
                {
                    content.Append("BT\n");
                    foreach (var line in page.Lines)
                    {
                        content.Append($"/{line.Font} {Fmt(line.Size)} Tf\n");
                        content.Append($"1 0 0 1 {Fmt(MarginX)} {Fmt(line.Y)} Tm\n");
                        content.Append('(').Append(EscapeAndMap(line.Text)).Append(") Tj\n");
                    }
                    content.Append("ET\n");
                }

                // Images
                foreach (var img in page.Images)
                {
                    content.Append($"q {Fmt(img.Width)} 0 0 {Fmt(img.Height)} {Fmt(MarginX)} {Fmt(img.Y)} cm /Img{img.ImageIndex} Do Q\n");
                }

                byte[] contentBytes = LatinBytes(content.ToString());

                string resources =
                    $"/Font << /F1 3 0 R /F2 4 0 R >>" +
                    (xObjRes.Length > 0 ? " " + xObjRes : string.Empty);

                objBytes[pageObjNum] = Ascii(
                    $"{pageObjNum} 0 obj\n" +
                    $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Fmt(PageWidth)} {Fmt(PageHeight)}] " +
                    $"/Resources << {resources} >> /Contents {contentObjNum} 0 R >>\nendobj\n");

                byte[] hdr = Ascii($"{contentObjNum} 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
                byte[] ftr = Ascii("\nendstream\nendobj\n");
                var combo = new byte[hdr.Length + contentBytes.Length + ftr.Length];
                Buffer.BlockCopy(hdr,   0, combo, 0,                                    hdr.Length);
                Buffer.BlockCopy(contentBytes, 0, combo, hdr.Length,                   contentBytes.Length);
                Buffer.BlockCopy(ftr,   0, combo, hdr.Length + contentBytes.Length,    ftr.Length);
                objBytes[contentObjNum] = combo;
            }

            // --- Assemble PDF ---
            int totalObjects = firstPageObj + 2 * pageCount - 1;

            using var ms = new MemoryStream();
            ms.Write(Ascii("%PDF-1.4\n"));

            var offsets = new long[totalObjects + 1];
            for (int n = 1; n <= totalObjects; n++)
            {
                offsets[n] = ms.Position;
                ms.Write(objBytes[n]);
            }

            long xrefOffset = ms.Position;
            var xref = new StringBuilder();
            xref.Append($"xref\n0 {totalObjects + 1}\n");
            xref.Append("0000000000 65535 f \n");
            for (int n = 1; n <= totalObjects; n++)
                xref.Append($"{offsets[n]:D10} 00000 n \n");
            ms.Write(Ascii(xref.ToString()));

            ms.Write(Ascii($"trailer\n<< /Size {totalObjects + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF"));

            return ms.ToArray();
        }

        /// <summary>
        /// Decodes a PNG byte array to a raw 24-bit RGB byte array (top-to-bottom,
        /// left-to-right) using WPF's built-in BitmapDecoder. Alpha is composited
        /// over white so the PDF image always has an opaque white background.
        /// </summary>
        private static byte[] DecodeToRgb24(byte[] png, out int width, out int height)
        {
            using var ms = new MemoryStream(png);
            var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(
                ms,
                System.Windows.Media.Imaging.BitmapCreateOptions.None,
                System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);

            var frame = decoder.Frames[0];

            // Convert to Bgr32 so we have a predictable 4-bytes-per-pixel layout.
            var converted = new System.Windows.Media.Imaging.FormatConvertedBitmap(
                frame,
                System.Windows.Media.PixelFormats.Bgr32,
                null, 0);

            width  = converted.PixelWidth;
            height = converted.PixelHeight;
            int stride = width * 4;
            byte[] bgr32 = new byte[height * stride];
            converted.CopyPixels(bgr32, stride, 0);

            // Convert Bgr32 -> RGB24 (PDF DeviceRGB, top-to-bottom).
            byte[] rgb24 = new byte[width * height * 3];
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    int src = row * stride + col * 4;
                    int dst = (row * width + col) * 3;
                    rgb24[dst]     = bgr32[src + 2]; // R
                    rgb24[dst + 1] = bgr32[src + 1]; // G
                    rgb24[dst + 2] = bgr32[src + 0]; // B
                }
            }
            return rgb24;
        }

        private static string Fmt(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        private static byte[] Ascii(string s) => LatinBytes(s);

        private static byte[] LatinBytes(string s)
        {
            var bytes = new byte[s.Length];
            for (int i = 0; i < s.Length; i++) bytes[i] = (byte)s[i];
            return bytes;
        }

        /// <summary>Maps a handful of common Unicode punctuation into WinAnsiEncoding byte
        /// positions and escapes PDF literal-string metacharacters. Anything else outside
        /// Latin-1 falls back to '?' rather than corrupting the content stream.</summary>
        private static string EscapeAndMap(string text)
        {
            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                char mapped = c switch
                {
                    '—' => (char)0x97, // em dash
                    '–' => (char)0x96, // en dash
                    '‘' or '’' => '\'',
                    '“' or '”' => '"',
                    '·' => (char)0xB7, // middot
                    _ => c
                };

                if (mapped == '\\' || mapped == '(' || mapped == ')')
                    sb.Append('\\').Append(mapped);
                else if (mapped <= 0xFF)
                    sb.Append(mapped);
                else
                    sb.Append('?');
            }
            return sb.ToString();
        }
    }
}
