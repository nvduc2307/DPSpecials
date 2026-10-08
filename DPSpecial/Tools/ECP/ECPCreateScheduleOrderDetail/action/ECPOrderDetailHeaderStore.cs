using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.schema;
using DPSpecial.Tools.ECP.ECPZoneManage.model;
using Newtonsoft.Json;

namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.action
{
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

        public static void Save(Document document, int zoneId, ECPOrderDetailHeaderModel header, IEnumerable<ECPOrderDetailRowModel> rows)
        {
            var all = Load(document);
            all.RemoveAll(x => x.ZoneId == zoneId);
            all.Add(new ECPOrderDetailHeaderSaveModel
            {
                ZoneId = zoneId,
                Header = header,
                Rows = rows.Select(r => new ECPOrderDetailRowSaveModel
                {
                    Key = r.Key,
                    WorkNo = r.WorkNo,
                    QuantityCode = r.QuantityCode,
                    Dimension = r.Dimension,
                    Rib = r.Rib,
                    Angle = r.Angle,
                    ProcessCode = r.ProcessCode,
                }).ToList(),
            });

            using var ts = new Transaction(document, "Save ECP Order Header");
            ts.Start();
            Write(document, all);
            ts.Commit();
        }

        public static void ApplySavedRows(Document document, int zoneId, IEnumerable<ECPOrderDetailRowModel> rows)
        {
            var saved = Load(document).FirstOrDefault(x => x.ZoneId == zoneId)?.Rows;
            if (saved == null || saved.Count == 0) return;

            var lookup = saved.GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.First());
            foreach (var row in rows)
            {
                if (!lookup.TryGetValue(row.Key, out var s)) continue;
                row.WorkNo = s.WorkNo;
                row.QuantityCode = s.QuantityCode;
                row.Dimension = s.Dimension;
                row.Rib = s.Rib;
                row.Angle = s.Angle;
                row.ProcessCode = s.ProcessCode;
            }
        }

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
