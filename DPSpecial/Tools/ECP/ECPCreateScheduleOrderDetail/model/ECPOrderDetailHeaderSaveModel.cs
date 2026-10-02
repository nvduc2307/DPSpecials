namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model
{
    // One saved header, referenced by the id of the zone it belongs to.
    public class ECPOrderDetailHeaderSaveModel
    {
        public int ZoneId { get; set; }
        public ECPOrderDetailHeaderModel Header { get; set; } = new();
    }
}
