using DPSpecial.Cores;

namespace DPSpecial.Tools.ECP.ECPZoneInstall.schema
{
    // Written on the width/height TextNotes created by Install Zone; Content holds the host
    // element's UniqueId so the notes can be found and replaced the next time the element is installed.
    public class ECPZoneDimensionSchema : SchemaEntityBase
    {
        public const string GUID = "8c1d6f0e-3b7a-4e52-9a14-5d2f7c9b6e31";
        public const string NAME = "ECPZoneDimensionSchema";

        public ECPZoneDimensionSchema(string guid, string name) : base(guid, name)
        {
        }
    }
}
