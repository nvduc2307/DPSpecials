using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using ColorConverter = System.Windows.Media.ColorConverter;
using Color = System.Windows.Media.Color;
using FlowDirection = System.Windows.FlowDirection;
using FontFamily = System.Windows.Media.FontFamily;
using FormattedText = System.Windows.Media.FormattedText;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.action
{
    // Turns the simple SVG shapes of the 縦切図 (path / line / rect / text on an 80 x 28 canvas) into a WPF drawing,
    // so the same SVG is shown in the grid and printed in the PDF. The SVG files live in imgs\ and are embedded
    // in the assembly (looked up by file name without extension).
    public static class SvgDrawing
    {
        public const double Width = 80, Height = 28;

        // Text of the embedded imgs\<name>.svg, or null when there is no such image.
        public static string LoadEmbedded(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var assembly = typeof(SvgDrawing).Assembly;
            var suffix = ".imgs." + name + ".svg";
            var resource = assembly.GetManifestResourceNames()
                .FirstOrDefault(x => x.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (resource == null) return null;
            using var stream = assembly.GetManifestResourceStream(resource);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static DrawingImage ToImage(string svg)
        {
            var group = new DrawingGroup();
            XDocument doc;
            try { doc = XDocument.Parse(svg); }
            catch (Exception) { return Freeze(group); }

            foreach (var e in doc.Root.Elements())
            {
                switch (e.Name.LocalName)
                {
                    case "path":
                        Add(group, Geometry.Parse((string)e.Attribute("d") ?? string.Empty), e);
                        break;
                    case "line":
                        Add(group, new LineGeometry(new Point(Num(e, "x1"), Num(e, "y1")), new Point(Num(e, "x2"), Num(e, "y2"))), e, defaultFill: "none");
                        break;
                    case "rect":
                        Add(group, new RectangleGeometry(new Rect(Num(e, "x"), Num(e, "y"), Num(e, "width"), Num(e, "height"))), e);
                        break;
                    case "text":
                        AddText(group, e);
                        break;
                }
            }

            // Fixes the drawing's bounds to the canvas so every image is scaled alike.
            group.Children.Insert(0, new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, Width, Height))));
            return Freeze(group);
        }

        private static DrawingImage Freeze(DrawingGroup group)
        {
            var image = new DrawingImage(group);
            image.Freeze();
            return image;
        }

        private static void Add(DrawingGroup group, Geometry geometry, XElement e, string defaultFill = "#000000")
        {
            var fill = ParseBrush((string)e.Attribute("fill") ?? defaultFill);
            var stroke = ParseBrush((string)e.Attribute("stroke"));
            Pen pen = null;
            if (stroke != null)
                pen = new Pen(stroke, e.Attribute("stroke-width") != null ? Num(e, "stroke-width") : 1);
            group.Children.Add(new GeometryDrawing(fill, pen, geometry));
        }

        private static void AddText(DrawingGroup group, XElement e)
        {
            var size = e.Attribute("font-size") != null ? Num(e, "font-size") : 8;
            var family = new FontFamily((string)e.Attribute("font-family") ?? "Meiryo UI");
            var fill = ParseBrush((string)e.Attribute("fill") ?? "#000000") ?? Brushes.Black;
            var ft = new FormattedText(e.Value, CultureInfo.GetCultureInfo("ja-JP"), FlowDirection.LeftToRight,
                new Typeface(family, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), size, fill, 1.0);
            // SVG y is the baseline; FormattedText is positioned by its top-left corner.
            var geometry = ft.BuildGeometry(new Point(Num(e, "x"), Num(e, "y") - ft.Baseline));
            group.Children.Add(new GeometryDrawing(fill, null, geometry));
        }

        private static double Num(XElement e, string attribute) =>
            double.TryParse((string)e.Attribute(attribute), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

        private static Brush ParseBrush(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "none") return null;
            try
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
                brush.Freeze();
                return brush;
            }
            catch (Exception) { return null; }
        }
    }
}
