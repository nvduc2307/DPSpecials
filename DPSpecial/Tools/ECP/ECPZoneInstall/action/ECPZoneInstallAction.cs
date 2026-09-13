using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPZoneInstall.schema;
using DPSpecial.Tools.ECP.ECPZoneInstall.view;
using DPSpecial.Tools.ECP.ECPZoneInstall.viewModel;
using DPSpecial.Tools.ECP.ECPZoneManage.model;
using DPSpecial.Tools.ECP.ECPZoneManage.schema;
using DPSpecial.Utils;
using Newtonsoft.Json;
using Color = Autodesk.Revit.DB.Color;

namespace DPSpecial.Tools.ECP.ECPZoneInstall.action
{
    public class ECPZoneInstallAction
    {
        private readonly UIDocument _uidocument;
        private readonly Document _document;
        private readonly ECPZoneAssignSchema _assignSchema;
        private readonly ECPZoneInstallVM _viewModel;
        private readonly ECPZoneInstallView _view;

        public ECPZoneInstallAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            var zoneSchema = new ECPZoneSchema(ECPZoneSchema.GUID, ECPZoneSchema.NAME);
            _assignSchema = new ECPZoneAssignSchema(ECPZoneAssignSchema.GUID, ECPZoneAssignSchema.NAME);

            var zones = GetZones(zoneSchema);
            if (!zones.Any())
                throw new Exception("No zones have been defined yet. Run \"ECP Zone Manage\" first.");

            _viewModel = new ECPZoneInstallVM
            {
                Zones = zones,
                OkCommand = new RelayCommand(_Ok),
                CancelCommand = new RelayCommand(_Cancel),
            };
            _viewModel.Zone = _viewModel.Zones.FirstOrDefault();
            _view = new ECPZoneInstallView { DataContext = _viewModel };
        }

        public void Execute()
        {
            _view.ShowDialog();
        }

        // Zones themselves are defined/edited via ECPZoneManageCmd and persisted with the same schema;
        // this command only reads that list to let the user pick one to apply.
        private List<ECPZoneSaveModel> GetZones(ECPZoneSchema zoneSchema)
        {
            var content = zoneSchema.Read(_document.ProjectInformation);
            if (string.IsNullOrEmpty(content)) return new List<ECPZoneSaveModel>();
            return JsonConvert.DeserializeObject<List<ECPZoneSaveModel>>(content) ?? new List<ECPZoneSaveModel>();
        }

        // Keeps prompting for one ECP element at a time (like ECPCreateAction/ECPModifyWidthAction's
        // pick loops) until the user presses Esc, tagging each picked element with the chosen zone.
        private void _Ok()
        {
            var zone = _viewModel.Zone;
            if (zone == null)
            {
                _view.Close();
                return;
            }

            _view.Hide();
            var color = ParseColor(zone.Color);
            var view = _document.ActiveView;

            using (var ts = new Transaction(_document, "Install ECP Zone"))
            {
                ts.Start();
                var isDo = true;
                do
                {
                    try
                    {
                        var element = _uidocument.Selection.PickElement(
                            _document,
                            null,
                            _ECPSelectFilter,
                            $"Pick ECP element for zone \"{zone.Name}\" (Esc to stop)...");
                        if (element == null) continue;

                        _assignSchema.Write(element, JsonConvert.SerializeObject(zone));
                        TintElement(view, element, color);
                    }
                    catch (Exception)
                    {
                        isDo = false;
                    }
                } while (isDo);
                ts.Commit();
            }

            _view.ShowDialog();
        }

        private void _Cancel()
        {
            _view.Close();
        }

        private bool _ECPSelectFilter(Element element)
        {
            if (element is not FamilyInstance fa) return false;
            return fa.Symbol.FamilyName.ToUpper().Contains("ECP");
        }

        // Tints the element in the active view so the assigned zone is visible at a glance.
        private void TintElement(Autodesk.Revit.DB.View view, Element element, Color color)
        {
            var overrides = new OverrideGraphicSettings();
            overrides.SetSurfaceForegroundPatternColor(color);
            overrides.SetProjectionLineColor(color);
            overrides.SetSurfaceTransparency(30);
            view.SetElementOverrides(element.Id, overrides);
        }

        private Color ParseColor(string hex)
        {
            hex = (hex ?? string.Empty).TrimStart('#');
            if (hex.Length != 6) return new Color(109, 195, 187); // fallback: default zone color
            var r = Convert.ToByte(hex.Substring(0, 2), 16);
            var g = Convert.ToByte(hex.Substring(2, 2), 16);
            var b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new Color(r, g, b);
        }
    }
}
