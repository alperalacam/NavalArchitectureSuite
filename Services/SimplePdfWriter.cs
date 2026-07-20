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
    public sealed class SimplePdfDocument
    {
        private const double PageWidth = 612;   // US Letter, points
        private const double PageHeight = 792;
        private const double MarginX = 54;
        private const double TopY = 738;
        private const double BottomY = 54;

        private const string RegularFont = "F1"; // Courier
        private const string BoldFont = "F2";    // Courier-Bold

        private readonly record struct Line(double Y, string Text, string Font, double Size);

        private readonly List<List<Line>> _pages = new();
        private List<Line> _currentPage = new();
        private double _y = TopY;

        public void AddLine(string text, double size = 10, bool bold = false)
        {
            EnsureRoom(size);
            _currentPage.Add(new Line(_y, text, bold ? BoldFont : RegularFont, size));
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

        private void EnsureRoom(double size)
        {
            if (_y - size < BottomY)
            {
                _pages.Add(_currentPage);
                _currentPage = new List<Line>();
                _y = TopY;
            }
        }

        public byte[] Build()
        {
            _pages.Add(_currentPage);

            const int fontCourier = 3;
            const int fontCourierBold = 4;
            const int firstPageObj = 5;
            int pageCount = _pages.Count;

            var objBytes = new Dictionary<int, byte[]>();

            objBytes[1] = Ascii("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            string kids = string.Join(" ", Enumerable.Range(0, pageCount).Select(i => $"{firstPageObj + 2 * i} 0 R"));
            objBytes[2] = Ascii($"2 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {pageCount} >>\nendobj\n");

            objBytes[fontCourier] = Ascii("3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>\nendobj\n");
            objBytes[fontCourierBold] = Ascii("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

            for (int i = 0; i < pageCount; i++)
            {
                int pageObjNum = firstPageObj + 2 * i;
                int contentObjNum = pageObjNum + 1;

                var content = new StringBuilder();
                content.Append("BT\n");
                foreach (var line in _pages[i])
                {
                    content.Append($"/{line.Font} {Fmt(line.Size)} Tf\n");
                    content.Append($"1 0 0 1 {Fmt(MarginX)} {Fmt(line.Y)} Tm\n");
                    content.Append('(').Append(EscapeAndMap(line.Text)).Append(") Tj\n");
                }
                content.Append("ET\n");

                byte[] contentBytes = LatinBytes(content.ToString());

                objBytes[pageObjNum] = Ascii(
                    $"{pageObjNum} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Fmt(PageWidth)} {Fmt(PageHeight)}] " +
                    $"/Resources << /Font << /F1 {fontCourier} 0 R /F2 {fontCourierBold} 0 R >> >> /Contents {contentObjNum} 0 R >>\nendobj\n");

                byte[] header = Ascii($"{contentObjNum} 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
                byte[] footer = Ascii("\nendstream\nendobj\n");
                var combined = new byte[header.Length + contentBytes.Length + footer.Length];
                Buffer.BlockCopy(header, 0, combined, 0, header.Length);
                Buffer.BlockCopy(contentBytes, 0, combined, header.Length, contentBytes.Length);
                Buffer.BlockCopy(footer, 0, combined, header.Length + contentBytes.Length, footer.Length);
                objBytes[contentObjNum] = combined;
            }

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
