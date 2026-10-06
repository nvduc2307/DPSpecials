namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model
{
    // One row of the detail grid.
    public class ECPOrderDetailRowModel
    {
        public string No { get; set; } = string.Empty;
        // 施工図No
        public string WorkNo { get; set; } = string.Empty;
        // 商品名 / 製品番号
        public string ProductName { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        // 長さ[mm]
        public string Length { get; set; } = string.Empty;
        // 数量[枚] (blue code printed after the quantity)
        public string Quantity { get; set; } = string.Empty;
        public string QuantityCode { get; set; } = string.Empty;
        // Name of the wall image (imgs<name>.svg) from WallImage.GetImageWall; empty = draw from ShapeKind / Profile.
        public string ImageName { get; set; } = string.Empty;

        private System.Windows.Media.DrawingImage _imageSource;
        // The 縦切図 as a WPF image (the grid and the PDF both draw this).
        public System.Windows.Media.DrawingImage ImageSource => _imageSource ??= action.SvgDrawing.ToImage(action.ECPOrderDetailSvgExporter.GetSvg(this));

        // 縦切図: Normal / FlatCut / RightBlock
        public string ShapeKind { get; set; } = "Normal";
        // 縦切図 end profile, derived from the type name: a type starting with "MNY" is a corner panel (slanted left end,
        // groove on the right end), a type ending in "T" (凸) has a tongue on both ends,
        // every other type has a tongue on the left end and a groove on the right end.
        public string Profile => (PartNumber ?? string.Empty).Trim().StartsWith("MNY", StringComparison.OrdinalIgnoreCase)
            ? "Corner"
            : (PartNumber ?? string.Empty).Trim().EndsWith("T", StringComparison.OrdinalIgnoreCase)
            ? "DoubleTongue"
            : "TongueGroove";

        // Identifies the same wall group between sessions (used to match saved rows).
        // Fixed when the row is built (before any editing), so edits to Length etc. do not break the match.
        private string _key;
        public string Key { get => _key ??= $"{ProductName}|{PartNumber}|{Length}|{ImageName}"; set => _key = value; }

        // 寸法[mm]
        public string Dimension { get; set; } = string.Empty;
        // リブ / 角度 / 加工コード (printed on the order slip PDF only)
        public string Rib { get; set; } = string.Empty;
        public string Angle { get; set; } = string.Empty;
        public string ProcessCode { get; set; } = string.Empty;
        // 基材 面積[m²] / 重量[kg], 働き 面積[m²] / 重量[kg], 役物 長さ[m] / 重量[kg] (order slip PDF only)
        public string BaseArea { get; set; } = string.Empty;
        public string BaseWeight { get; set; } = string.Empty;
        public string WorkArea { get; set; } = string.Empty;
        public string WorkWeight { get; set; } = string.Empty;
        public string AccessoryLength { get; set; } = string.Empty;
        public string AccessoryWeight { get; set; } = string.Empty;
        // Every other row is tinted cyan.
        public bool IsAlternate { get; set; }
    }
}
