using System.IO;
using System.Text;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model;

namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.action
{
    // Writes the 縦切図 shapes of the detail grid as SVG files named after the type (製品番号).
    // The geometry mirrors the shapes drawn in ECPCreateScheduleOrderDetailView.xaml (80 x 28 canvas).
    public static class ECPOrderDetailSvgExporter
    {
        private const string OutlineDoubleTongue = "M3,7 L12,7 L12,2 L66,2 L66,7 L77,7 L77,21 L66,21 L66,26 L12,26 L12,21 L3,21 Z";
        private const string OutlineTongueGroove = "M3,7 L12,7 L12,2 L77,2 L77,8 L68,8 L68,20 L77,20 L77,26 L12,26 L12,21 L3,21 Z";
        private const string BlockDoubleTongue = "M54,3 L66,3 L66,8 L76,8 L76,20 L66,20 L66,25 L54,25 Z";
        private const string BlockTongueGroove = "M54,3 L76,3 L76,8 L69,8 L69,20 L76,20 L76,25 L54,25 Z";

        // Returns the files written. One file per distinct type + shape kind; when a type appears with
        // several shape kinds, the non-normal kinds get a "_<kind>" suffix so nothing is overwritten.
        public static List<string> Export(IEnumerable<ECPOrderDetailRowModel> rows, string folder)
        {
            Directory.CreateDirectory(folder);
            var written = new List<string>();
            var items = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.PartNumber))
                .GroupBy(x => (x.PartNumber.Trim(), kind: string.IsNullOrEmpty(x.ShapeKind) ? "Normal" : x.ShapeKind, x.Profile))
                .Select(g => g.Key)
                .ToList();

            foreach (var (type, kind, profile) in items)
            {
                var hasOtherKinds = items.Any(x => x.Item1 == type && x.kind != kind);
                var name = hasOtherKinds && kind != "Normal" ? $"{type}_{kind}" : type;
                var path = Path.Combine(folder, Sanitize(name) + ".svg");
                File.WriteAllText(path, BuildSvg(kind, profile), new UTF8Encoding(false));
                written.Add(path);
            }
            return written;
        }

        private static string BuildSvg(string kind, string profile)
        {
            var doubleTongue = profile == "DoubleTongue";
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"80\" height=\"28\" viewBox=\"0 0 80 28\">");
            sb.AppendLine($"  <path d=\"{(doubleTongue ? OutlineDoubleTongue : OutlineTongueGroove)}\" fill=\"#FFFFFF\" stroke=\"#333333\" stroke-width=\"1\"/>");
            if (kind == "FlatCut")
            {
                sb.AppendLine("  <rect x=\"3\" y=\"10\" width=\"5\" height=\"8\" fill=\"#000000\"/>");
                sb.AppendLine("  <text x=\"10\" y=\"17\" font-size=\"8\" font-family=\"MS UI Gothic, Meiryo UI, sans-serif\" fill=\"#000000\">←ﾌﾗｯﾄ切</text>");
            }
            else if (kind == "RightBlock")
            {
                sb.AppendLine($"  <path d=\"{(doubleTongue ? BlockDoubleTongue : BlockTongueGroove)}\" fill=\"#000000\"/>");
                sb.AppendLine("  <line x1=\"12\" y1=\"2\" x2=\"54\" y2=\"2\" stroke=\"#E03030\" stroke-width=\"1\"/>");
            }
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
