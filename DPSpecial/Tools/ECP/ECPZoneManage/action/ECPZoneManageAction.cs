using System.Collections.ObjectModel;
using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPZoneManage.model;
using DPSpecial.Tools.ECP.ECPZoneManage.schema;
using DPSpecial.Tools.ECP.ECPZoneManage.view;
using DPSpecial.Tools.ECP.ECPZoneManage.viewModel;
using DPSpecial.Utils;
using Newtonsoft.Json;

namespace DPSpecial.Tools.ECP.ECPZoneManage.action
{
    public class ECPZoneManageAction
    {
        private readonly UIDocument _uidocument;
        private readonly Document _document;
        private readonly ECPZoneSchema _schema;
        private readonly ECPZoneManageVM _viewModel;
        private readonly ECPZoneManageView _view;

        public ECPZoneManageAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _schema = new ECPZoneSchema(ECPZoneSchema.GUID, ECPZoneSchema.NAME);

            _viewModel = new ECPZoneManageVM
            {
                Zones = GetZones(),
                PickColorCommand = new RelayCommand<ECPZoneModel>(_PickColor),
                CreateZoneCommand = new RelayCommand(_CreateZone),
                DeleteZoneCommand = new RelayCommand(_DeleteZone),
                OkCommand = new RelayCommand(_Ok),
                CancelCommand = new RelayCommand(_Cancel),
            };
            InitZoneNameValidation();

            _view = new ECPZoneManageView { DataContext = _viewModel };
            _view.Loaded += (_, _) =>
                _viewModel.ZoneDataGrid = _view.FindName("ZoneDataGrid") as System.Windows.Controls.DataGrid;
        }

        public void Execute()
        {
            _view.ShowDialog();
        }

        // Zones are persisted as JSON on ProjectInformation via Extensible Storage (DPSpecial.Cores.SchemaEntityBase),
        // the same pattern ECPShapeAction uses for per-element data.
        private ObservableCollection<ECPZoneModel> GetZones()
        {
            var result = new ObservableCollection<ECPZoneModel>();
            var content = _schema.Read(_document.ProjectInformation);
            if (string.IsNullOrEmpty(content)) return result;

            var saved = JsonConvert.DeserializeObject<List<ECPZoneSaveModel>>(content) ?? new List<ECPZoneSaveModel>();
            foreach (var item in saved)
                result.Add(new ECPZoneModel { Id = item.Id, Name = item.Name, Color = item.Color });
            return result;
        }

        private void InitZoneNameValidation()
        {
            foreach (var zone in _viewModel.Zones)
                zone.ChangeNameAction = _ChangeNameAction;
        }

        // Blocks duplicate zone names: if the new name collides with another zone, warn and drop the last
        // typed character (mirrors DPTools.BricsR.BricsRZones.ManageZones' ManageZonesVM._changeNameAction).
        private void _ChangeNameAction(ECPZoneModel zone)
        {
            var duplicated = _viewModel.Zones.FirstOrDefault(x => x.Name == zone.Name && x.Id != zone.Id);
            if (duplicated == null) return;

            IO.ShowWarning("Zone name already exists.\nTên zone đã tồn tại.");

            // Temporarily detach the callback so the corrective assignment below doesn't re-enter this method.
            zone.ChangeNameAction = null;
            zone.Name = zone.Name.Length > 0 ? zone.Name.Substring(0, zone.Name.Length - 1) : zone.Name;
            zone.ChangeNameAction = _ChangeNameAction;
        }

        private void _PickColor(ECPZoneModel? zone)
        {
            if (zone == null) return;
            var dialog = new ColorDialog { FullOpen = true };
            if (dialog.ShowDialog() == DialogResult.OK)
                zone.Color = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        }

        private void _CreateZone()
        {
            var nextId = _viewModel.Zones.Any() ? _viewModel.Zones.Max(x => x.Id) + 1 : 1;
            var zone = new ECPZoneModel
            {
                Id = nextId,
                Name = $"Zone {nextId}",
                ChangeNameAction = _ChangeNameAction,
            };
            _viewModel.Zones.Add(zone);
        }

        private void _DeleteZone()
        {
            if (_viewModel.ZoneDataGrid?.SelectedItem is not ECPZoneModel zone) return;
            _viewModel.Zones.Remove(zone);
        }

        private void _Ok()
        {
            _view.Close();

            // Keep only one entry per name (oldest Id wins) in case duplicate-name validation was ever bypassed.
            var zoneSaves = _viewModel.Zones
                .GroupBy(x => x.Name)
                .Select(g => g.OrderBy(x => x.Id).First())
                .Select(x => new ECPZoneSaveModel { Id = x.Id, Name = x.Name, Color = x.Color })
                .ToList();
            var content = JsonConvert.SerializeObject(zoneSaves);

            using (var ts = new Transaction(_document, "Save ECP Zones"))
            {
                ts.Start();
                _schema.Write(_document.ProjectInformation, content);
                ts.Commit();
            }
        }

        private void _Cancel()
        {
            _view.Close();
        }
    }
}
