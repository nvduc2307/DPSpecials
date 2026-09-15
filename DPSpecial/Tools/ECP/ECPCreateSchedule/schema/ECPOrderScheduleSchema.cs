using DPSpecial.Cores;

namespace DPSpecial.Tools.ECP.ECPCreateSchedule.schema
{
    // JP: オーダー票一覧（ヘッダー＋明細行）を ProjectInformation に保存するためのスキーマ
    // VI: Schema lưu danh sách order (header + các dòng chi tiết) vào ProjectInformation
    // EN: Schema for persisting the order schedule list (header + detail rows) to ProjectInformation
    public class ECPOrderScheduleSchema : SchemaEntityBase
    {
        public const string GUID = "c7f3e8a1-5b2d-4e9f-a1c6-3d8e7f2b9a04";
        public const string NAME = "ECPOrderScheduleSchema";

        public ECPOrderScheduleSchema(string guid, string name) : base(guid, name)
        {
        }
    }
}
