using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.schema;
using DPSpecial.Tools.ECP.ECPZoneManage.model;
using Newtonsoft.Json;

namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.action
{
    // Reads/writes the per-zone headers stored on ProjectInformation (a JSON list keyed by zone id).
    public static class ECPOrderDetailHeaderStore
    {
        private static ECPOrderDetailHeaderSchema CreateSchema() =>
            new(ECPOrderDetailHeaderSchema.GUID, ECPOrderDetailHeaderSchema.NAME);

        public static List<ECPOrderDetailHeaderSaveModel> Load(Document document)
        {
            var content = CreateSchema().Read(document.ProjectInformation);
            if (string.IsNullOrEmpty(content)) return new List<ECPOrderDetailHeaderSaveModel>();
            return JsonConvert.DeserializeObject<List<ECPOrderDetailHeaderSaveModel>>(content)
                   ?? new List<ECPOrderDetailHeaderSaveModel>();
        }

        public static ECPOrderDetailHeaderModel Find(Document document, int zoneId) =>
            Load(document).FirstOrDefault(x => x.ZoneId == zoneId)?.Header;

        // Inserts or replaces the header of one zone (opens its own transaction).
        public static void Save(Document document, int zoneId, ECPOrderDetailHeaderModel header)
        {
            var all = Load(document);
            all.RemoveAll(x => x.ZoneId == zoneId);
            all.Add(new ECPOrderDetailHeaderSaveModel { ZoneId = zoneId, Header = header });

            using var ts = new Transaction(document, "Save ECP Order Header");
            ts.Start();
            Write(document, all);
            ts.Commit();
        }

        // Brings the saved headers in line with the current zone definitions: the zone-derived fields
        // (物件登録No / 受注No / 物件詳細) are refreshed, and headers of deleted zones are dropped.
        // Must be called inside an open transaction. Returns the number of headers changed or removed.
        public static int SyncWithZones(Document document, IReadOnlyCollection<ECPZoneSaveModel> zones)
        {
            var all = Load(document);
            if (all.Count == 0) return 0;

            var lookup = zones.ToDictionary(z => z.Id);
            var changed = 0;
            foreach (var item in all.ToList())
            {
                if (!lookup.TryGetValue(item.ZoneId, out var zone))
                {
                    all.Remove(item);
                    changed++;
                    continue;
                }

                var h = item.Header;
                if (h.PropertyRegNo == zone.PropertyRegNo && h.OrderNo == zone.OrderNo && h.PropertyDetail == zone.Name)
                    continue;
                h.PropertyRegNo = zone.PropertyRegNo;
                h.OrderNo = zone.OrderNo;
                h.PropertyDetail = zone.Name;
                changed++;
            }

            if (changed > 0) Write(document, all);
            return changed;
        }

        private static void Write(Document document, List<ECPOrderDetailHeaderSaveModel> all) =>
            CreateSchema().Write(document.ProjectInformation, JsonConvert.SerializeObject(all));
    }
}
