namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model
{
    public class ECPOrderDetailHeaderSaveModel
    {
        public int ZoneId { get; set; }
        public ECPOrderDetailHeaderModel Header { get; set; } = new();
        public List<ECPOrderDetailRowSaveModel> Rows { get; set; } = new();
    }

    public class ECPOrderDetailRowSaveModel
    {
        public string Key { get; set; } = string.Empty;
        public string WorkNo { get; set; } = string.Empty;
        public string QuantityCode { get; set; } = string.Empty;
        public string Dimension { get; set; } = string.Empty;
        public string Rib { get; set; } = string.Empty;
        public string Angle { get; set; } = string.Empty;
        public string ProcessCode { get; set; } = string.Empty;
    }
}
