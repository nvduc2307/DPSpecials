using DPSpecial.Cores;

namespace DPSpecial.Tools.ECP.ECPZoneInstall.schema
{
    // Stores which zone (Id/Name/Color, as JSON) a given ECP element was tagged with.
    // Separate from ECPZoneManage.schema.ECPZoneSchema, which stores the project-wide zone list
    // on ProjectInformation; this one is written per-element.
    public class ECPZoneAssignSchema : SchemaEntityBase
    {
        public const string GUID = "34255941-b96a-4f2c-8784-a6f50dcc915b";
        public const string NAME = "ECPZoneAssignSchema";

        public ECPZoneAssignSchema(string guid, string name) : base(guid, name)
        {
        }
    }
}
