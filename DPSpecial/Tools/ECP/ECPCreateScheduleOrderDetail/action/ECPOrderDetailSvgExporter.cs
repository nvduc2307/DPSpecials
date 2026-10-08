using System.IO;
using System.Text;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model;

namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.action
{
    public static class ECPOrderDetailSvgExporter
    {
        public const string OutlineDoubleTongue = "M3,7 L12,7 L12,2 L66,2 L66,7 L77,7 L77,21 L66,21 L66,26 L12,26 L12,21 L3,21 Z";
        public const string OutlineTongueGroove = "M3,7 L12,7 L12,2 L77,2 L77,8 L68,8 L68,20 L77,20 L77,26 L12,26 L12,21 L3,21 Z";
        public const string BlockDoubleTongue = "M54,3 L66,3 L66,8 L76,8 L76,20 L66,20 L66,25 L54,25 Z";
        public const string BlockTongueGroove = "M54,3 L76,3 L76,8 L69,8 L69,20 L76,20 L76,25 L54,25 Z";
        public const string OutlineCorner = "M17,2 L77,2 L77,8 L68,8 L68,20 L77,20 L77,26 L3,26 Z";
        public const string BlockCorner = "M47,3 L76,3 L76,8 L69,8 L69,20 L76,20 L76,25 L47,25 Z";

        public static string OutlineOf(string profile) =>
            profile == "Corner" ? OutlineCorner : profile == "DoubleTongue" ? OutlineDoubleTongue : OutlineTongueGroove;

        public static string BlockOf(string profile) =>
            profile == "Corner" ? BlockCorner : profile == "DoubleTongue" ? BlockDoubleTongue : BlockTongueGroove;

        public static double RedLineStartX(string profile) => profile == "Corner" ? 17 : 12;
        public const double CornerBlockX = 47;

        public static double BlockLeftX(string profile) => profile == "Corner" ? CornerBlockX : 54;
        public static bool BlockHasDottedEdge(string profile) => profile != "DoubleTongue";
        public const double FlatCutEdgeX = 12;

        public static List<string> Export(IEnumerable<ECPOrderDetailRowModel> rows, string folder)
        {
            Directory.CreateDirectory(folder);
            var written = new List<string>();
            var items = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.PartNumber))
                .Select(x => (Type: x.PartNumber.Trim(),
                              Label: !string.IsNullOrEmpty(x.ImageName) ? x.ImageName : string.IsNullOrEmpty(x.ShapeKind) ? "Normal" : x.ShapeKind,
                              Svg: GetSvg(x)))
                .Distinct()
                .ToList();

            foreach (var (type, label, svg) in items)
            {
                var hasOthers = items.Any(x => x.Type == type && x.Label != label);
                var name = hasOthers && label != "Normal" ? $"{type}_{label}" : type;
                var path = Path.Combine(folder, Sanitize(name) + ".svg");
                File.WriteAllText(path, svg, new UTF8Encoding(false));
                written.Add(path);
            }
            return written;
        }

        public static string GetSvg(ECPOrderDetailRowModel row) =>
            SvgDrawing.LoadEmbedded(row.ImageName) ?? BuildSvg(string.IsNullOrEmpty(row.ShapeKind) ? "Normal" : row.ShapeKind, row.Profile);

        public static string BuildSvg(string kind, string profile)
        {

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"80\" height=\"28\" viewBox=\"0 0 80 28\">");
            sb.AppendLine($"  <path d=\"{OutlineOf(profile)}\" fill=\"#FFFFFF\" stroke=\"#333333\" stroke-width=\"1\"/>");
            if (kind == "FlatCut")
            {
                sb.AppendLine("  <rect x=\"3\" y=\"7\" width=\"9\" height=\"14\" fill=\"#000000\"/>");
                for (var y = 3; y <= 24; y += 3)
                    sb.AppendLine($"  <rect x=\"{FlatCutEdgeX - 0.5}\" y=\"{y}\" width=\"1\" height=\"1.5\" fill=\"#000000\"/>");
                sb.AppendLine("  <text x=\"14\" y=\"17\" font-size=\"8\" font-family=\"MS UI Gothic, Meiryo UI, sans-serif\" fill=\"#000000\">←ﾌﾗｯﾄ切</text>");
            }
            else if (kind == "RightBlock")
            {
                sb.AppendLine($"  <path d=\"{BlockOf(profile)}\" fill=\"#000000\"/>");
                sb.AppendLine($"  <line x1=\"{RedLineStartX(profile)}\" y1=\"2\" x2=\"{BlockLeftX(profile)}\" y2=\"2\" stroke=\"#E03030\" stroke-width=\"1\"/>");
                if (BlockHasDottedEdge(profile))
                    for (var y = 5; y <= 23; y += 4)
                        sb.AppendLine($"  <rect x=\"{BlockLeftX(profile) - 0.5}\" y=\"{y}\" width=\"1.5\" height=\"2\" fill=\"#FFFFFF\"/>");
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
