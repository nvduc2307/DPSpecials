using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using FormattedText = System.Windows.Media.FormattedText;
using FlowDirection = System.Windows.FlowDirection;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model;

namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.action
{
    // Writes the オーダー票[メース一般] order slip as an A4 PDF (one or more pages).
    // Each page is drawn with WPF in a 1414 x 2000 design space (the layout of the reference PDF) and the recorded
    // drawing is then converted to PDF vector operators: lines and fills as paths, text as glyph outlines. That keeps
    // everything sharp at any zoom and needs no font embedding, so Japanese text looks the same on every machine.
    // A zone with more rows than fit on one page continues on the next page, repeating the header block and table
    // header; the 合計 row is printed after the last row only.
    public static class ECPOrderSlipPdfExporter
    {
        private const double PageW = 1414, PageH = 2000;
        private const double PdfW = 595.28, PdfH = 841.89;
        private const double FlattenTolerance = 0.02; // design units

        // Table geometry (design units)
        private const double TableTop = 667, HeaderH = 68, RowH = 47.4, TotalGap = 5, TableBottomLimit = 1915;
        private static readonly double[] ColX = { 68, 100, 161, 363, 417, 472, 586, 639, 681, 734, 808, 896, 963, 1051, 1118, 1206, 1273, 1307 };
        private static readonly int RowsPerPage = (int)((TableBottomLimit - (TableTop + HeaderH) - TotalGap - RowH) / RowH);

        private static readonly Brush Black = Brushes.Black;
        private static readonly Brush LabelFill = new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD4));
        private static readonly FontFamily Font = new("MS PGothic, MS Gothic, Meiryo, Yu Gothic, Segoe UI");
        private static readonly FontFamily NumFont = new("MS Gothic, Consolas, Courier New");

        // One order slip (header + its rows).
        public class Slip
        {
            public ECPOrderDetailHeaderModel Header { get; set; }
            public IReadOnlyList<ECPOrderDetailRowModel> Rows { get; set; }
        }

        public static void Export(ECPOrderDetailHeaderModel header, IReadOnlyList<ECPOrderDetailRowModel> rows, string path) =>
            Export(new[] { new Slip { Header = header, Rows = rows } }, path);

        // Several slips are written one after another into a single PDF; each starts on a new page.
        public static void Export(IReadOnlyList<Slip> slips, string path)
        {
            var stamp = DateTime.Now;
            var contents = new List<byte[]>();
            foreach (var slip in slips)
            {
                var pageCount = Math.Max(1, (int)Math.Ceiling(slip.Rows.Count / (double)RowsPerPage));
                for (var p = 0; p < pageCount; p++)
                {
                    var pageRows = slip.Rows.Skip(p * RowsPerPage).Take(RowsPerPage).ToList();
                    contents.Add(RenderPage(slip.Header, slip.Rows, pageRows, p == pageCount - 1, stamp));
                }
            }
            WritePdf(contents, path);
        }

        #region Page drawing

        // Returns the page's PDF content stream (vector operators).
        private static byte[] RenderPage(ECPOrderDetailHeaderModel h, IReadOnlyList<ECPOrderDetailRowModel> allRows,
            List<ECPOrderDetailRowModel> pageRows, bool isLast, DateTime stamp)
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                DrawTitle(dc, stamp);
                DrawHeaderBlock(dc, h);
                DrawTable(dc, pageRows, allRows, isLast);
                DrawFooter(dc);
            }

            var sb = new StringBuilder();
            EmitDrawing(visual.Drawing, Matrix.Identity, sb);
            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        #region Drawing -> PDF vector operators

        private static void EmitDrawing(Drawing drawing, Matrix parent, StringBuilder sb)
        {
            switch (drawing)
            {
                case DrawingGroup group:
                    var m = group.Transform != null ? group.Transform.Value * parent : parent;
                    foreach (var child in group.Children) EmitDrawing(child, m, sb);
                    break;
                case GeometryDrawing gd when gd.Geometry != null:
                    EmitGeometry(gd.Geometry, gd.Brush, gd.Pen, parent, sb);
                    break;
                case GlyphRunDrawing grd when grd.GlyphRun != null:
                    EmitGeometry(grd.GlyphRun.BuildGeometry(), grd.ForegroundBrush, null, parent, sb);
                    break;
            }
        }

        private static void EmitGeometry(Geometry geometry, Brush fill, Pen stroke, Matrix m, StringBuilder sb)
        {
            var fillColor = (fill as SolidColorBrush)?.Color;
            if (fillColor is { A: 0 }) fillColor = null; // fully transparent: nothing to paint
            var strokeColor = stroke != null && stroke.Thickness > 0 ? (stroke.Brush as SolidColorBrush)?.Color : null;
            if (fillColor == null && strokeColor == null) return;

            var path = geometry.GetFlattenedPathGeometry(FlattenTolerance, ToleranceType.Absolute);
            if (path.Figures.Count == 0) return;

            if (fillColor != null) sb.Append(Rgb(fillColor.Value)).Append(" rg\n");
            if (strokeColor != null)
            {
                var scale = Math.Sqrt(Math.Abs(m.Determinant)) * PdfW / PageW;
                sb.Append(Rgb(strokeColor.Value)).Append(" RG\n");
                sb.Append(Num(stroke.Thickness * scale)).Append(" w\n");
            }

            foreach (var fig in path.Figures)
            {
                sb.Append(Pt(m, fig.StartPoint)).Append(" m\n");
                foreach (var seg in fig.Segments)
                {
                    if (seg is PolyLineSegment poly)
                        foreach (var p in poly.Points) sb.Append(Pt(m, p)).Append(" l\n");
                    else if (seg is System.Windows.Media.LineSegment line)
                        sb.Append(Pt(m, line.Point)).Append(" l\n");
                }
                if (fig.IsClosed) sb.Append("h\n");
            }

            var evenOdd = path.FillRule == FillRule.EvenOdd;
            if (fillColor != null && strokeColor != null) sb.Append(evenOdd ? "B*\n" : "B\n");
            else if (fillColor != null) sb.Append(evenOdd ? "f*\n" : "f\n");
            else sb.Append("S\n");
        }

        private static string Rgb(Color c) => $"{Num(c.R / 255.0)} {Num(c.G / 255.0)} {Num(c.B / 255.0)}";

        // Design space (y down) -> PDF space (y up, points).
        private static string Pt(Matrix m, Point p)
        {
            var t = m.Transform(p);
            return Num(t.X * PdfW / PageW) + " " + Num(PdfH - t.Y * PdfH / PageH);
        }

        private static string Num(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        #endregion

        private static void DrawTitle(DrawingContext dc, DateTime stamp)
        {
            Text(dc, "オーダー票[メース一般]", 73, 33, 27, bold: true);
            Text(dc, "受注", 715, 31, 25, bold: true, align: TextAlignment.Center);
            Text(dc, "出力日", 1100, 42, 17);
            Text(dc, stamp.ToString("yyyy/MM/dd") + "   " + stamp.ToString("HH:mm:ss"), 1170, 45, 14.5, font: NumFont);
            dc.DrawLine(new Pen(Black, 2), new Point(68, 74), new Point(1374, 74));
        }

        private static void DrawHeaderBlock(DrawingContext dc, ECPOrderDetailHeaderModel h)
        {
            const double lx = 68, lw = 168;
            double Y(int r) => 89 + r * 27; // row top, 27 units per row

            // r0
            Label(dc, "■物件登録No.", lx, Y(0), lw); Value(dc, h.PropertyRegNo, 242, Y(0));
            Label(dc, "■受注No.", 344, Y(0), 100); Value(dc, h.OrderNo, 451, Y(0));
            Label(dc, "■進捗状況", 903, Y(0), 188); Value(dc, h.Status, 1098, Y(0));
            // r1
            Label(dc, "■営業担当", lx, Y(1), lw);
            Value(dc, h.BranchOffice, 242, Y(1)); Value(dc, h.BranchOffice, 445, Y(1));
            Value(dc, string.IsNullOrEmpty(h.SalesStaffName) ? "" : "・担当  " + h.SalesStaffName, 650, Y(1));
            // r2
            Label(dc, "■希望納期", lx, Y(2), lw); Value(dc, h.DesiredDate, 242, Y(2));
            Label(dc, "■現場到着予定日", 480, Y(2), 166); Value(dc, h.SiteArrivalDate, 653, Y(2));
            Label(dc, "■二次加工出荷予定日", 903, Y(2), 188); Value(dc, h.SecondaryShipDate, 1098, Y(2));
            // r3
            Label(dc, "■工場出荷予定日", lx, Y(3), lw); Value(dc, h.FactoryShipDate, 242, Y(3));
            Label(dc, "■加工開始予定日", 480, Y(3), 166); Value(dc, h.ProcessStartDate, 653, Y(3));
            Label(dc, "■二次加工到着予定日", 903, Y(3), 188); Value(dc, h.SecondaryArrivalDate, 1098, Y(3));
            // r4-r5
            Label(dc, "■物件名称", lx, Y(4), lw); Value(dc, h.PropertyName, 242, Y(4), 19.5);
            Label(dc, "■物件詳細", lx, Y(5), lw); Value(dc, h.PropertyDetail, 242, Y(5), 19.5);
            // r6
            Label(dc, "■住所", lx, Y(6), lw);
            Value(dc, string.IsNullOrEmpty(h.PostalCode) ? "" : "〒  " + h.PostalCode, 242, Y(6));
            Value(dc, h.PrefectureName + h.Address, 371, Y(6));
            Label(dc, "■フロア", 1220, Y(6), 80); Value(dc, h.Floor, 1306, Y(6));
            // r7
            Label(dc, "■施主", 424, Y(7), 88); Value(dc, h.OwnerName, 519, Y(7));
            Label(dc, "■設計", 829, Y(7), 80); Value(dc, h.DesignName, 917, Y(7));
            // r8-r9
            Label(dc, "■元請", lx, Y(8), lw); Value(dc, h.ContractorName, 242, Y(8));
            Label(dc, "■工事店", 829, Y(8), 80); Value(dc, h.ConstructionShopName, 917, Y(8));
            Label(dc, "■荷受人", lx, Y(9), lw); Value(dc, h.Consignee, 242, Y(9));
            Label(dc, "■連絡先", 829, Y(9), 80); Value(dc, h.ContactPhone, 917, Y(9));
            // r10
            Label(dc, "■用途", lx, Y(10), lw); Value(dc, h.UseName, 242, Y(10));
            Label(dc, "■出荷工場", 518, Y(10), 115); Value(dc, h.FactoryName, 640, Y(10));
            Label(dc, "■区分", 829, Y(10), 114); Value(dc, h.CategoryName, 951, Y(10));
            // r11
            Label(dc, "■表裏", lx, Y(11), lw); Value(dc, h.FaceName, 242, Y(11));
            Label(dc, "■ｶﾞｽｹｯﾄ貼り", 518, Y(11), 115); Value(dc, h.GasketOn ? "有" : "無", 642, Y(11));
            Label(dc, "■ﾋﾞﾆｰﾙ梱包", 681, Y(11), 115); Value(dc, h.VinylOn ? "有" : "無", 804, Y(11));
            // r12
            Label(dc, "■仕様", lx, Y(12), lw); Value(dc, h.SpecName, 242, Y(12));
            Label(dc, "■種類", 829, Y(12), 114); Value(dc, h.PaintTypeName, 951, Y(12));
            // r13
            Label(dc, "■色番号", lx, Y(13), lw); Value(dc, h.ColorNo, 242, Y(13));
            Label(dc, "■艶", 518, Y(13), 115); Value(dc, h.GlossName, 640, Y(13));
            Label(dc, "■ｻﾝﾌﾟﾙNo.", 829, Y(13), 114); Value(dc, h.SampleNo, 951, Y(13));
            // r14
            Label(dc, "■お客様備考", lx, Y(14), lw); Value(dc, h.CustomerNote, 242, Y(14));

            // lower block (gap above)
            double Y2(int r) => 519 + r * 27;
            Label(dc, "■受渡方法", lx, Y2(0), lw); Value(dc, h.DeliveryMethodName, 242, Y2(0));
            Label(dc, "■車両車種", 518, Y2(0), 115); Value(dc, h.VehicleName, 640, Y2(0)); Value(dc, h.VehicleTypeName, 735, Y2(0));
            Label(dc, "■金物製品", 836, Y2(0), 114); Value(dc, h.HardwareCode, 956, Y2(0)); Value(dc, h.HardwareName, 1007, Y2(0));
            Label(dc, "■梱包制限", lx, Y2(1), lw);
            Value(dc, $"重量   {h.LoadWeight} kg  高   {h.LoadHeight} 枚", 250, Y2(1));
            Label(dc, "■地図", 518, Y2(1), 115); Value(dc, h.MapOn ? "有" : "無", 642, Y2(1));
            Label(dc, "■販売店", lx, Y2(2), lw); Value(dc, (h.DealerName + " " + h.DealerBranch).Trim(), 242, Y2(2));
            Label(dc, "■担当者", lx, Y2(3), lw); Value(dc, h.DealerContact, 242, Y2(3));
        }

        private static void DrawTable(DrawingContext dc, List<ECPOrderDetailRowModel> pageRows,
            IReadOnlyList<ECPOrderDetailRowModel> allRows, bool isLast)
        {
            var pen = new Pen(Black, 1.4);
            var thick = new Pen(Black, 2.6);
            var bodyTop = TableTop + HeaderH;
            var bodyBottom = bodyTop + pageRows.Count * RowH;
            var right = ColX[ColX.Length - 1];

            // header cells
            dc.DrawRectangle(LabelFill, null, new Rect(ColX[0], TableTop, right - ColX[0], HeaderH));
            string[] heads = { "No.", "施工図", "製品番号", "長さ", "数量", "縦切図", "寸法", "ﾘﾌﾞ", "角度", "加工ｺｰﾄﾞ" };
            for (var i = 0; i < heads.Length; i++)
                Text(dc, heads[i], (ColX[i] + ColX[i + 1]) / 2, TableTop + 22, i == 0 || i >= 7 ? 13.5 : 15.5, align: TextAlignment.Center);
            string[] groups = { "基材", "働き", "役物" };
            for (var g = 0; g < 3; g++)
            {
                var x0 = ColX[10 + g * 2];
                var x1 = ColX[12 + g * 2];
                Text(dc, groups[g], (x0 + x1) / 2, TableTop + 6, 15.5, align: TextAlignment.Center);
                dc.DrawLine(pen, new Point(x0, TableTop + 34), new Point(x1, TableTop + 34));
            }
            string[] subs = { "面積", "重量", "面積", "重量", "長さ", "重量" };
            for (var i = 0; i < 6; i++)
                Text(dc, subs[i], (ColX[10 + i] + ColX[11 + i]) / 2, TableTop + 40, 15.5, align: TextAlignment.Center);

            // body rows
            for (var r = 0; r < pageRows.Count; r++)
            {
                var row = pageRows[r];
                var top = bodyTop + r * RowH;
                var mid = top + RowH / 2;
                Text(dc, row.No, (ColX[0] + ColX[1]) / 2, mid - 12, 19, align: TextAlignment.Center, font: NumFont);
                Text(dc, row.WorkNo, (ColX[1] + ColX[2]) / 2, mid - 12, 19, align: TextAlignment.Center, font: NumFont);
                Text(dc, row.PartNumber, ColX[2] + 6, mid - 12, 19.5, font: NumFont);
                Text(dc, row.Length, ColX[4] - 4, mid - 12, 19, align: TextAlignment.Right, font: NumFont);
                Text(dc, row.Quantity, ColX[5] - 6, mid - 12, 19, align: TextAlignment.Right, font: NumFont);
                DrawShape(dc, row, ColX[5], top);
                Text(dc, row.Dimension, ColX[7] - 6, mid - 12, 19, align: TextAlignment.Right, font: NumFont);
                Text(dc, row.Rib, (ColX[7] + ColX[8]) / 2, mid - 12, 19, align: TextAlignment.Center);
                Text(dc, row.Angle, (ColX[8] + ColX[9]) / 2, mid - 12, 19, align: TextAlignment.Center, font: NumFont);
                Text(dc, row.ProcessCode, ColX[9] + 8, mid - 12, 19, font: NumFont);
                var nums = new[] { row.BaseArea, row.BaseWeight, row.WorkArea, row.WorkWeight, row.AccessoryLength, row.AccessoryWeight };
                for (var i = 0; i < 6; i++)
                    Text(dc, nums[i], ColX[11 + i] - 8, mid - 9, 14.5, align: TextAlignment.Right, font: NumFont);
                dc.DrawLine(pen, new Point(ColX[0], top + RowH), new Point(right, top + RowH));
            }

            // vertical lines + outer frame
            for (var i = 0; i < ColX.Length; i++)
            {
                // the split between 面積 and 重量 only starts below the 基材/働き/役物 group header
                var isSubSplit = i is 11 or 13 or 15;
                dc.DrawLine(pen, new Point(ColX[i], isSubSplit ? TableTop + 34 : TableTop), new Point(ColX[i], bodyBottom));
            }
            dc.DrawLine(pen, new Point(ColX[0], bodyTop), new Point(right, bodyTop));
            dc.DrawRectangle(null, thick, new Rect(ColX[0], TableTop, right - ColX[0], bodyBottom - TableTop));

            // 合計 row (last page only)
            if (!isLast) return;
            var tTop = bodyBottom + TotalGap;
            dc.DrawRectangle(LabelFill, null, new Rect(ColX[0], tTop, ColX[4] - ColX[0], RowH));
            foreach (var x in new[] { ColX[0], ColX[4], ColX[5], ColX[10], ColX[11], ColX[12], ColX[13], ColX[14], ColX[15], ColX[16], ColX[17] })
                dc.DrawLine(pen, new Point(x, tTop), new Point(x, tTop + RowH));
            dc.DrawRectangle(null, thick, new Rect(ColX[0], tTop, right - ColX[0], RowH));
            Text(dc, "合計", (ColX[0] + ColX[4]) / 2, tTop + 11, 21, align: TextAlignment.Center);

            var qty = allRows.Sum(x => ParseInt(x.Quantity));
            Text(dc, qty.ToString(CultureInfo.InvariantCulture), ColX[5] - 6, tTop + 11, 19, align: TextAlignment.Right, font: NumFont);
            var totals = new[]
            {
                SumText(allRows.Select(x => x.BaseArea), 3), SumText(allRows.Select(x => x.BaseWeight), 0),
                SumText(allRows.Select(x => x.WorkArea), 3), SumText(allRows.Select(x => x.WorkWeight), 0),
                SumText(allRows.Select(x => x.AccessoryLength), 3), SumText(allRows.Select(x => x.AccessoryWeight), 0),
            };
            for (var i = 0; i < 6; i++)
                Text(dc, totals[i], ColX[11 + i] - 8, tTop + 14, 14.5, align: TextAlignment.Right, font: NumFont);
        }

        // 縦切図: the row's SVG drawing (80 x 28 canvas), scaled to fit the 114-unit-wide cell.
        private static void DrawShape(DrawingContext dc, ECPOrderDetailRowModel row, double cellX, double rowTop)
        {
            const double scale = 1.38;
            dc.PushTransform(new TranslateTransform(cellX + 6 - 3 * scale, rowTop + 7 - 2 * scale));
            dc.PushTransform(new ScaleTransform(scale, scale));
            dc.DrawDrawing(row.ImageSource.Drawing);
            dc.Pop();
            dc.Pop();
        }

        private static void DrawFooter(DrawingContext dc)
        {
            dc.DrawLine(new Pen(Black, 2), new Point(53, 1939), new Point(1367, 1939));
            Text(dc, "アイカテック建材株式会社", 710, 1956, 22, bold: true, align: TextAlignment.Center);
        }

        private static void Label(DrawingContext dc, string text, double x, double y, double w)
        {
            dc.DrawRectangle(LabelFill, null, new Rect(x, y, w, 27));
            Text(dc, text, x + 2, y + 3, 15.5);
        }

        private static void Value(DrawingContext dc, string text, double x, double y, double size = 15.5)
        {
            if (string.IsNullOrEmpty(text)) return;
            Text(dc, text, x, y + (size > 17 ? 0 : 3), size);
        }

        private static void Text(DrawingContext dc, string text, double x, double y, double size,
            bool bold = false, TextAlignment align = TextAlignment.Left, FontFamily font = null)
        {
            if (string.IsNullOrEmpty(text)) return;
            var ft = new FormattedText(text, CultureInfo.GetCultureInfo("ja-JP"), FlowDirection.LeftToRight,
                new Typeface(font ?? Font, FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
                size, Black, 1.0);
            var px = align switch
            {
                TextAlignment.Center => x - ft.Width / 2,
                TextAlignment.Right => x - ft.Width,
                _ => x,
            };
            dc.DrawText(ft, new Point(px, y));
        }

        private static int ParseInt(string s) =>
            int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;

        // Sum of the numeric strings (thousand separators ignored); empty when nothing was parsed.
        private static string SumText(IEnumerable<string> values, int decimals)
        {
            var any = false;
            double sum = 0;
            foreach (var s in values)
            {
                if (!double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) continue;
                any = true;
                sum += v;
            }
            return any ? sum.ToString("F" + decimals, CultureInfo.InvariantCulture) : "";
        }

        #endregion

        #region Minimal PDF writer (vector content stream per A4 page)

        private static void WritePdf(List<byte[]> contents, string path)
        {
            var inv = CultureInfo.InvariantCulture;
            var mediaBox = $"[0 0 {PdfW.ToString("0.##", inv)} {PdfH.ToString("0.##", inv)}]";

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            var offsets = new List<long>();
            void Write(string s) { var b = Encoding.ASCII.GetBytes(s); fs.Write(b, 0, b.Length); }
            void Begin(int id) { offsets.Add(fs.Position); Write($"{id} 0 obj\n"); }

            // object ids: 1 catalog, 2 pages, then per page: page, content
            Write("%PDF-1.4\n");
            Begin(1); Write("<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
            var kids = string.Join(" ", Enumerable.Range(0, contents.Count).Select(i => $"{3 + i * 2} 0 R"));
            Begin(2); Write($"<< /Type /Pages /Kids [{kids}] /Count {contents.Count} >>\nendobj\n");

            for (var i = 0; i < contents.Count; i++)
            {
                var pageId = 3 + i * 2;
                Begin(pageId);
                Write($"<< /Type /Page /Parent 2 0 R /MediaBox {mediaBox} /Resources << >> /Contents {pageId + 1} 0 R >>\nendobj\n");

                var packed = Deflate(contents[i]);
                Begin(pageId + 1);
                Write($"<< /Filter /FlateDecode /Length {packed.Length} >>\nstream\n");
                fs.Write(packed, 0, packed.Length);
                Write("\nendstream\nendobj\n");
            }

            var xref = fs.Position;
            Write($"xref\n0 {offsets.Count + 1}\n0000000000 65535 f \n");
            foreach (var o in offsets) Write(o.ToString("D10", inv) + " 00000 n \n");
            Write($"trailer\n<< /Size {offsets.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        }

        // zlib container (header + raw deflate + Adler-32), as required by PDF's FlateDecode.
        private static byte[] Deflate(byte[] data)
        {
            using var ms = new MemoryStream();
            ms.WriteByte(0x78); ms.WriteByte(0x9C);
            using (var ds = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Compress, true))
                ds.Write(data, 0, data.Length);

            uint a = 1, b = 0;
            foreach (var x in data) { a = (a + x) % 65521; b = (b + a) % 65521; }
            var adler = (b << 16) | a;
            ms.WriteByte((byte)(adler >> 24)); ms.WriteByte((byte)(adler >> 16));
            ms.WriteByte((byte)(adler >> 8)); ms.WriteByte((byte)adler);
            return ms.ToArray();
        }

        #endregion
    }
}
