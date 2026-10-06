namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model
{
    // One saved header and its grid rows, referenced by the id of the zone they belong to.
    public class ECPOrderDetailHeaderSaveModel
    {
        public int ZoneId { get; set; }
        public ECPOrderDetailHeaderModel Header { get; set; } = new();
        public List<ECPOrderDetailRowSaveModel> Rows { get; set; } = new();
    }

    // The user-entered values of one grid row. The row is found again by Key (product, type, length, image);
    // everything derived from the walls (quantity, areas, weights...) is recomputed instead of being saved.
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
