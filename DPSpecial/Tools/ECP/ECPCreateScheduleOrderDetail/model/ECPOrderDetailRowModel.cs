namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model
{
    public class ECPOrderDetailRowModel
    {
        public string No { get; set; } = string.Empty;
        public string WorkNo { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string Length { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string QuantityCode { get; set; } = string.Empty;
        public string ImageName { get; set; } = string.Empty;

        private System.Windows.Media.DrawingImage _imageSource;
        public System.Windows.Media.DrawingImage ImageSource => _imageSource ??= action.SvgDrawing.ToImage(action.ECPOrderDetailSvgExporter.GetSvg(this));

        public string ShapeKind { get; set; } = "Normal";
        public string Profile => (PartNumber ?? string.Empty).Trim().StartsWith("MNY", StringComparison.OrdinalIgnoreCase)
            ? "Corner"
            : (PartNumber ?? string.Empty).Trim().EndsWith("T", StringComparison.OrdinalIgnoreCase)
            ? "DoubleTongue"
            : "TongueGroove";

        private string _key;
        public string Key { get => _key ??= $"{ProductName}|{PartNumber}|{Length}|{ImageName}"; set => _key = value; }

        public string Dimension { get; set; } = string.Empty;
        public string Rib { get; set; } = string.Empty;
        public string Angle { get; set; } = string.Empty;
        public string ProcessCode { get; set; } = string.Empty;
        public string BaseArea { get; set; } = string.Empty;
        public string BaseWeight { get; set; } = string.Empty;
        public string WorkArea { get; set; } = string.Empty;
        public string WorkWeight { get; set; } = string.Empty;
        public string AccessoryLength { get; set; } = string.Empty;
        public string AccessoryWeight { get; set; } = string.Empty;
        public bool IsAlternate { get; set; }
    }
}
