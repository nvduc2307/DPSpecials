using Autodesk.Revit.UI;
using DPSpecial.Contains;
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
            // 1. Read the current zone definitions from ProjectInformation (source of truth).
            var zones = GetZones();
            if (!zones.Any())
                throw new Exception("No zones have been defined yet. Run \"ECP Zone Manage\" first.");

            var zoneLookup = zones.ToDictionary(z => z.Id);

            // 2. Collect all ECP FamilyInstances in the document.
            var ecpElements = new FilteredElementCollector(_document)
                .WhereElementIsNotElementType()
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(x => x.Symbol.FamilyName.ToUpper().Contains("ECP"))
                .ToList();

            if (!ecpElements.Any())
                throw new Exception("No ECP elements found in the document.");

            // 3. Scan each element, compare assigned zone vs current zone definition.
            var updatedCount = 0;
            var removedCount = 0;
            var view = _document.ActiveView;

            using (var ts = new Transaction(_document, "Update ECP Zones"))
            {
                ts.Start();

                foreach (var element in ecpElements)
                {
                    var assignedJson = _assignSchema.Read(element);
                    if (string.IsNullOrEmpty(assignedJson))
                        continue; // Element has no zone assigned — skip.

                    var assignedZone = JsonConvert.DeserializeObject<ECPZoneSaveModel>(assignedJson);
                    if (assignedZone == null)
                        continue;

                    // Look up the current zone definition by Id.
                    if (!zoneLookup.TryGetValue(assignedZone.Id, out var currentZone))
                    {
                        // Zone was deleted from the zone list — remove assignment from element.
                        _assignSchema.Write(element, string.Empty);
                        ClearElementOverrides(view, element);
                        WriteParameterElement(element, string.Empty);
                        removedCount++;
                        continue;
                    }

                    // Compare each field: Name, OrderNo, PropertyRegNo, Color.
                    if (assignedZone.Name == currentZone.Name
                        && assignedZone.OrderNo == currentZone.OrderNo
                        && assignedZone.PropertyRegNo == currentZone.PropertyRegNo
                        && assignedZone.Color == currentZone.Color)
                    {
                        continue; // No changes — skip.
                    }

                    // Zone definition has changed — update the per-element entity.
                    var updatedJson = JsonConvert.SerializeObject(currentZone);
                    _assignSchema.Write(element, updatedJson);

                    // Refresh the color overlay to match the (possibly new) zone color.
                    var color = ParseColor(currentZone.Color);
                    TintElement(view, element, color);
                    WriteParameterElement(element, currentZone.Name);
                    updatedCount++;
                }

                ts.Commit();
            }

            // 4. Report results.
            var messages = new List<string>();
            if (updatedCount > 0)
                messages.Add($"Updated: {updatedCount} element(s).");
            if (removedCount > 0)
                messages.Add($"Removed zone (zone deleted): {removedCount} element(s).");
            if (updatedCount == 0 && removedCount == 0)
                messages.Add("All ECP zones are up to date. No changes needed.");

            IO.ShowInfo(string.Join("\n", messages));
        }

        /// <summary>
        /// Checks whether any ECP element in the model has a stale zone assignment.
        /// Returns true if at least one element's zone (Name, Number, Code, or Color)
        /// differs from the current zone definitions, or its zone was deleted.
        /// </summary>

        private List<ECPZoneSaveModel> GetZones()
        {
            var content = _zoneSchema.Read(_document.ProjectInformation);
            if (string.IsNullOrEmpty(content)) return new List<ECPZoneSaveModel>();
            return JsonConvert.DeserializeObject<List<ECPZoneSaveModel>>(content) ?? new List<ECPZoneSaveModel>();
        }

        private void TintElement(Autodesk.Revit.DB.View view, Element element, Color color)
        {
            var overrides = new OverrideGraphicSettings();
            overrides.SetSurfaceForegroundPatternColor(color);
            overrides.SetProjectionLineColor(color);
            overrides.SetSurfaceTransparency(30);
            view.SetElementOverrides(element.Id, overrides);
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

                // Zone was deleted → changed.
                if (!zoneLookup.TryGetValue(assignedZone.Id, out var currentZone))
                    return true;

                // Any field differs → changed.
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
