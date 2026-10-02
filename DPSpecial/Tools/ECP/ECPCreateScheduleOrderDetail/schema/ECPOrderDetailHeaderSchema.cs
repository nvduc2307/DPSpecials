using DPSpecial.Cores;

namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.schema
{
    // Persists the 販売店受注修正 header blocks (one per zone, as JSON) on ProjectInformation.
    public class ECPOrderDetailHeaderSchema : SchemaEntityBase
    {
        public const string GUID = "5e1b7c42-9a3d-4f68-b0d5-2c8a6e1f7d93";
        public const string NAME = "ECPOrderDetailHeaderSchema";

        public ECPOrderDetailHeaderSchema(string guid, string name) : base(guid, name)
        {
        }
    }
}
