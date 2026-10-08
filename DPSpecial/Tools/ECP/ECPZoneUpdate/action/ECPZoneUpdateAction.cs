using Autodesk.Revit.UI;
using DPSpecial.Contains;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.action;
using DPSpecial.Tools.ECP.ECPZoneInstall.action;
using DPSpecial.Tools.ECP.ECPZoneInstall.schema;
using DPSpecial.Tools.ECP.ECPZoneManage.model;
using DPSpecial.Tools.ECP.ECPZoneManage.schema;
using DPSpecial.Utils;
using Newtonsoft.Json;
using Color = Autodesk.Revit.DB.Color;

namespace DPSpecial.Tools.ECP.ECPZoneUpdate.action
{
    public class ECPZoneUpdateAction
    {
        private readonly UIDocument _uidocument;
        private readonly Document _document;
        private readonly ECPZoneSchema _zoneSchema;
        private readonly ECPZoneAssignSchema _assignSchema;

        public ECPZoneUpdateAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _zoneSchema = new ECPZoneSchema(ECPZoneSchema.GUID, ECPZoneSchema.NAME);
            _assignSchema = new ECPZoneAssignSchema(ECPZoneAssignSchema.GUID, ECPZoneAssignSchema.NAME);
        }

        public void Execute()
        {
            var zones = GetZones();
            if (!zones.Any())
                throw new Exception("No zones have been defined yet. Run \"ECP Zone Manage\" first.");

            var zoneLookup = zones.ToDictionary(z => z.Id);

            var ecpElements = new FilteredElementCollector(_document)
                .WhereElementIsNotElementType()
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(x => x.Symbol.FamilyName.ToUpper().Contains("ECP"))
                .ToList();

            if (!ecpElements.Any())
                throw new Exception("No ECP elements found in the document.");

            var updatedCount = 0;
            var removedCount = 0;
            var headerCount = 0;
            var view = _document.ActiveView;
            var isSettingView = view.Name.Contains(ECPZoneDimensionHelper.NameViewSettingZone);
            var patternId = isSettingView ? GetDiagonalCrosshatchPatternId() : ElementId.InvalidElementId;

            using (var ts = new Transaction(_document, "Update ECP Zones"))
            {
                ts.Start();

                foreach (var element in ecpElements)
                {
                    var assignedJson = _assignSchema.Read(element);
                    if (string.IsNullOrEmpty(assignedJson))
                        continue;

                    var assignedZone = JsonConvert.DeserializeObject<ECPZoneSaveModel>(assignedJson);
                    if (assignedZone == null)
                        continue;

                    if (!zoneLookup.TryGetValue(assignedZone.Id, out var currentZone))
                    {
                        _assignSchema.Write(element, string.Empty);
                        if (isSettingView)
                            ClearElementOverrides(view, element);
                        WriteParameterElement(element, string.Empty);
                        removedCount++;
                        continue;
                    }

                    if (assignedZone.Name == currentZone.Name
                        && assignedZone.OrderNo == currentZone.OrderNo
                        && assignedZone.PropertyRegNo == currentZone.PropertyRegNo
                        && assignedZone.Color == currentZone.Color)
                    {
                        continue;
                    }

                    var updatedJson = JsonConvert.SerializeObject(currentZone);
                    _assignSchema.Write(element, updatedJson);

                    if (isSettingView)
                        TintElement(view, element, ParseColor(currentZone.Color), patternId);
                    WriteParameterElement(element, currentZone.Name);
                    updatedCount++;
                }

                headerCount = ECPOrderDetailHeaderStore.SyncWithZones(_document, zones);

                ts.Commit();
            }

            var messages = new List<string>();
            if (updatedCount > 0)
                messages.Add($"Updated: {updatedCount} element(s).");
            if (removedCount > 0)
                messages.Add($"Removed zone (zone deleted): {removedCount} element(s).");
            if (headerCount > 0)
                messages.Add($"Order headers updated: {headerCount}.");
            if (updatedCount == 0 && removedCount == 0 && headerCount == 0)
                messages.Add("All ECP zones are up to date. No changes needed.");

            IO.ShowInfo(string.Join("\n", messages));
        }


        private List<ECPZoneSaveModel> GetZones()
        {
            var content = _zoneSchema.Read(_document.ProjectInformation);
            if (string.IsNullOrEmpty(content)) return new List<ECPZoneSaveModel>();
            return JsonConvert.DeserializeObject<List<ECPZoneSaveModel>>(content) ?? new List<ECPZoneSaveModel>();
        }

        private void TintElement(Autodesk.Revit.DB.View view, Element element, Color color, ElementId patternId)
        {
            var overrides = new OverrideGraphicSettings();
            if (patternId != ElementId.InvalidElementId)
            {
                overrides.SetSurfaceForegroundPatternId(patternId);
                overrides.SetSurfaceForegroundPatternVisible(true);
            }
            overrides.SetSurfaceForegroundPatternColor(color);
            overrides.SetProjectionLineColor(color);
            overrides.SetSurfaceTransparency(30);
            view.SetElementOverrides(element.Id, overrides);
        }

        private ElementId GetDiagonalCrosshatchPatternId()
        {
            var patterns = new FilteredElementCollector(_document)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .ToList();
            var pattern = patterns.FirstOrDefault(x => string.Equals(x.Name, "Diagonal crosshatch", StringComparison.OrdinalIgnoreCase))
                ?? patterns.FirstOrDefault(x => x.Name.IndexOf("Diagonal crosshatch", StringComparison.OrdinalIgnoreCase) >= 0);
            return pattern?.Id ?? ElementId.InvalidElementId;
        }

        private void ClearElementOverrides(Autodesk.Revit.DB.View view, Element element)
        {
            view.SetElementOverrides(element.Id, new OverrideGraphicSettings());
        }

        private void WriteParameterElement(Element element, string zone)
        {
            try
            {
                element.LookupParameter(WallParameterName.ZONE).Set(zone);
            }
            catch (Exception)
            {
            }
        }

        private Color ParseColor(string hex)
        {
            hex = (hex ?? string.Empty).TrimStart('#');
            if (hex.Length != 6) return new Color(109, 195, 187);
            var r = Convert.ToByte(hex.Substring(0, 2), 16);
            var g = Convert.ToByte(hex.Substring(2, 2), 16);
            var b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new Color(r, g, b);
        }
        public bool HasZoneChanged()
        {
            var zones = GetZones();
            if (!zones.Any()) return false;

            var zoneLookup = zones.ToDictionary(z => z.Id);

            var ecpElements = new FilteredElementCollector(_document)
                .WhereElementIsNotElementType()
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(x => x.Symbol.FamilyName.ToUpper().Contains("ECP"));

            foreach (var element in ecpElements)
            {
                var assignedJson = _assignSchema.Read(element);
                if (string.IsNullOrEmpty(assignedJson))
                    continue;

                var assignedZone = JsonConvert.DeserializeObject<ECPZoneSaveModel>(assignedJson);
                if (assignedZone == null)
                    continue;

                if (!zoneLookup.TryGetValue(assignedZone.Id, out var currentZone))
                    return true;

                if (assignedZone.Name != currentZone.Name
                    || assignedZone.OrderNo != currentZone.OrderNo
                    || assignedZone.PropertyRegNo != currentZone.PropertyRegNo
                    || assignedZone.Color != currentZone.Color)
                    return true;
            }

            return false;
        }
    }
}
