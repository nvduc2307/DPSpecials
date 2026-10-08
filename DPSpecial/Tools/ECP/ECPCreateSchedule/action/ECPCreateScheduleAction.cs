using System.ComponentModel;
using DPSpecial.Tools.ECP.ECPCreateSchedule.schema;
using DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.model;
using DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.view;
using DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.viewModel;
using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.action;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model;
using DPSpecial.Tools.ECP.ECPZoneInstall.schema;
using DPSpecial.Tools.ECP.ECPZoneManage.model;
using DPSpecial.Tools.ECP.ECPZoneManage.schema;
using DPSpecial.Utils;
using Newtonsoft.Json;
using WallParam = DPSpecial.MVVM.Models.WallParameterName;

namespace DPSpecial.Tools.ECP.ECPCreateSchedule.action
{
    public partial class ECPCreateScheduleAction
    {
        private const double WEIGHT_PER_M2 = 15.4;

        private readonly UIDocument _uidocument;
        private readonly Document _document;
        private readonly ECPOrderScheduleSchema _orderScheduleSchema;
        private ECPCreateScheduleVM _viewModel;
        private ECPCreateScheduleView _view;
        private (ECPOrderScheduleModel Order, List<ECPOrderDetailRowModel> Rows)? _pendingDetail;

        public ECPCreateScheduleAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = uidocument.Document;
            _orderScheduleSchema = new ECPOrderScheduleSchema(ECPOrderScheduleSchema.GUID, ECPOrderScheduleSchema.NAME);

            _viewModel = new ECPCreateScheduleVM
            {
                BranchOffices = new List<string> { "大阪支店", "東京支店", "名古屋支店", "福岡支店" },

                OrderSlipTypes = new List<string> { "1:オーダー票", "2:オーダー票(明細有)", "3:オーダー票(明細無)" },

                SearchCommand = new RelayCommand(_SearchCommand),
                CheckAllOnCommand = new RelayCommand(_CheckAllOnCommand),
                CheckAllOffCommand = new RelayCommand(_CheckAllOffCommand),
                PrintListCommand = new RelayCommand(_PrintListCommand),
                OrderSlipCommand = new RelayCommand(_OrderSlipCommand, () => _viewModel.Orders.Any(o => o.IsChecked)),
                CsvCommand = new RelayCommand(_CsvCommand),
                ItemCountCommand = new RelayCommand(_ItemCountCommand),
                CopyOrderWithDetailCommand = new RelayCommand(_CopyOrderWithDetailCommand),
                CopyOrderWithoutDetailCommand = new RelayCommand(_CopyOrderWithoutDetailCommand),
                InquiryCommand = new RelayCommand(_InquiryCommand),
                ModifyCommand = new RelayCommand(_ModifyCommand, () => _viewModel.SelectedOrder != null),
                DeleteCommand = new RelayCommand(_DeleteCommand),
                SaveCommand = new RelayCommand(_SaveCommand),
                BackCommand = new RelayCommand(_BackCommand),
                IsAllCheckedAction = _IsAllCheckedAction,
            };

            LoadAndMergeOrders();

            _viewModel.Header.PropertyChanged += Header_PropertyChanged;

            _view = new ECPCreateScheduleView { DataContext = _viewModel };
        }

        public void Execute()
        {
            while (true)
            {
                _view.ShowDialog();
                if (_pendingDetail == null) return;

                var (order, rows) = _pendingDetail.Value;
                _pendingDetail = null;
                new ECPCreateScheduleOrderDetailAction(_uidocument, order, rows, _viewModel.Header.BranchOffice, order.ZoneId).Execute();

                _view = new ECPCreateScheduleView { DataContext = _viewModel };
            }
        }

        #region Load / Merge / Save

        private void LoadAndMergeOrders()
        {
            var savedData = LoadSavedData();

            if (savedData != null)
            {
                _viewModel.Header.PropertyName = savedData.Header.PropertyName;
                _viewModel.Header.PropertyDetail = savedData.Header.PropertyDetail;
                _viewModel.Header.DealerName = savedData.Header.DealerName;
                _viewModel.Header.DealerDivision = savedData.Header.DealerDivision;
                _viewModel.Header.BranchOffice = savedData.Header.BranchOffice;
                _viewModel.Header.BranchOfficeDetail = savedData.Header.BranchOfficeDetail;
                _viewModel.Header.OrderSlipType = savedData.Header.OrderSlipType;
            }
            else
            {
                _viewModel.Header.PropertyName = string.Empty;
                _viewModel.Header.PropertyDetail = string.Empty;
                _viewModel.Header.DealerName = string.Empty;
                _viewModel.Header.DealerDivision = string.Empty;
                _viewModel.Header.BranchOffice = string.Empty;
                _viewModel.Header.BranchOfficeDetail = string.Empty;
                _viewModel.Header.OrderSlipType = _viewModel.OrderSlipTypes.FirstOrDefault();
            }

            var freshOrders = GetOrders();

            var savedOrdersByOrderNo = savedData?.Orders?
                .Where(o => !string.IsNullOrEmpty(o.OrderNo))
                .ToDictionary(o => o.OrderNo, o => o)
                ?? new Dictionary<string, ECPOrderScheduleOrderSaveModel>();

            foreach (var order in freshOrders)
            {
                if (savedOrdersByOrderNo.TryGetValue(order.OrderNo, out var saved))
                {
                    order.IsChecked = saved.IsChecked;
                    order.DesiredDeliveryDate = saved.DesiredDeliveryDate;
                    order.SiteArrivalDate = saved.SiteArrivalDate;
                    order.ProcessShipScheduledDate = saved.ProcessShipScheduledDate;
                    order.ProcessArrivalScheduledDate = saved.ProcessArrivalScheduledDate;
                    order.FactoryShipScheduledDate = saved.FactoryShipScheduledDate;
                    order.ChangeNotAllowed = saved.ChangeNotAllowed;
                    order.ProcessShipActualDate = saved.ProcessShipActualDate;
                    order.FactoryShipActualDate = saved.FactoryShipActualDate;
                    order.FinalShipDate = saved.FinalShipDate;
                    order.TemporaryStorage = saved.TemporaryStorage;
                    order.DeliveryChangeCount = saved.DeliveryChangeCount;
                    order.InternalDeform = saved.InternalDeform;
                    order.ManufacturingFactory = saved.ManufacturingFactory;
                    order.ProcessingFactory = saved.ProcessingFactory;
                    order.SalesRep = saved.SalesRep;
                    order.ProgressStatus = saved.ProgressStatus;
                }

                order.PropertyChanged += Order_PropertyChanged;
                _viewModel.Orders.Add(order);
            }

            if (_viewModel.Orders.Any())
            {
                var checkedCount = _viewModel.Orders.Count(o => o.IsChecked);
                if (checkedCount == 0) _viewModel.SetIsAllCheckedFromRows(false);
                else if (checkedCount == _viewModel.Orders.Count) _viewModel.SetIsAllCheckedFromRows(true);
                else _viewModel.SetIsAllCheckedFromRows(null);
            }
        }

        private ECPOrderScheduleSaveModel LoadSavedData()
        {
            var content = _orderScheduleSchema.Read(_document.ProjectInformation);
            if (string.IsNullOrEmpty(content)) return null;
            return JsonConvert.DeserializeObject<ECPOrderScheduleSaveModel>(content);
        }

        private void SaveData()
        {
            var saveModel = new ECPOrderScheduleSaveModel
            {
                Header = new ECPOrderScheduleHeaderSaveModel
                {
                    PropertyName = _viewModel.Header.PropertyName,
                    PropertyDetail = _viewModel.Header.PropertyDetail,
                    DealerName = _viewModel.Header.DealerName,
                    DealerDivision = _viewModel.Header.DealerDivision,
                    BranchOffice = _viewModel.Header.BranchOffice,
                    BranchOfficeDetail = _viewModel.Header.BranchOfficeDetail,
                    OrderSlipType = _viewModel.Header.OrderSlipType,
                },
                Orders = _viewModel.Orders.Select(o => new ECPOrderScheduleOrderSaveModel
                {
                    IsChecked = o.IsChecked,
                    OrderNo = o.OrderNo,
                    PropertyRegNo = o.PropertyRegNo,
                    PropertyName = o.PropertyName,
                    PropertyDetail = o.PropertyDetail,
                    DealerName = o.DealerName,
                    DealerDivision = o.DealerDivision,
                    BaseAreaM2 = o.BaseAreaM2,
                    BaseWeight = o.BaseWeight,
                    WorkAreaM2 = o.WorkAreaM2,
                    WorkWeight = o.WorkWeight,
                    AccessoryAreaM2 = o.AccessoryAreaM2,
                    ActualSheets = o.ActualSheets,
                    DesiredDeliveryDate = o.DesiredDeliveryDate,
                    SiteArrivalDate = o.SiteArrivalDate,
                    ProcessShipScheduledDate = o.ProcessShipScheduledDate,
                    ProcessArrivalScheduledDate = o.ProcessArrivalScheduledDate,
                    FactoryShipScheduledDate = o.FactoryShipScheduledDate,
                    ChangeNotAllowed = o.ChangeNotAllowed,
                    ProcessShipActualDate = o.ProcessShipActualDate,
                    FactoryShipActualDate = o.FactoryShipActualDate,
                    FinalShipDate = o.FinalShipDate,
                    TemporaryStorage = o.TemporaryStorage,
                    DeliveryChangeCount = o.DeliveryChangeCount,
                    InternalDeform = o.InternalDeform,
                    ManufacturingFactory = o.ManufacturingFactory,
                    ProcessingFactory = o.ProcessingFactory,
                    SalesRep = o.SalesRep,
                    ProgressStatus = o.ProgressStatus,
                }).ToList(),
            };

            var content = JsonConvert.SerializeObject(saveModel);

            using (var ts = new Transaction(_document, "Save ECP Order Schedule"))
            {
                ts.Start();
                _orderScheduleSchema.Write(_document.ProjectInformation, content);
                ts.Commit();
            }
        }

        #endregion

        private void Header_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ECPOrderScheduleHeaderModel.PropertyName):
                    foreach (var order in _viewModel.Orders)
                        order.PropertyName = _viewModel.Header.PropertyName;
                    break;
                case nameof(ECPOrderScheduleHeaderModel.DealerName):
                    foreach (var order in _viewModel.Orders)
                        order.DealerName = _viewModel.Header.DealerName;
                    break;
                case nameof(ECPOrderScheduleHeaderModel.DealerDivision):
                    foreach (var order in _viewModel.Orders)
                        order.DealerDivision = _viewModel.Header.DealerDivision;
                    break;
            }
        }

        private List<ECPOrderScheduleModel> GetOrders()
        {
            var result = new List<ECPOrderScheduleModel>();

            var zoneSchema = new ECPZoneSchema(ECPZoneSchema.GUID, ECPZoneSchema.NAME);
            var content = zoneSchema.Read(_document.ProjectInformation);
            if (string.IsNullOrEmpty(content)) return result;

            var zones = JsonConvert.DeserializeObject<List<ECPZoneSaveModel>>(content) ?? new List<ECPZoneSaveModel>();
            if (!zones.Any()) return result;

            var elementsByZone = GetECPElementsByZone();

            foreach (var zone in zones)
            {
                double totalBaseAreaM2 = 0;
                double totalWorkAreaM2 = 0;
                int sheetCount = 0;

                if (elementsByZone.TryGetValue(zone.Id, out var elements))
                {
                    foreach (var elem in elements)
                    {
                        var (baseArea, workArea) = GetElementArea(elem);
                        totalBaseAreaM2 += baseArea;
                        totalWorkAreaM2 += workArea;
                        sheetCount++;
                    }
                }
                if (totalBaseAreaM2 == 0) continue;
                if (totalWorkAreaM2 == 0) continue;

                var baseWeight = totalBaseAreaM2 * WEIGHT_PER_M2;
                var workWeight = totalWorkAreaM2 * WEIGHT_PER_M2;

                result.Add(new ECPOrderScheduleModel
                {
                    PropertyName = _viewModel.Header.PropertyName,
                    DealerName = _viewModel.Header.DealerName,
                    DealerDivision = _viewModel.Header.DealerDivision,
                    ZoneId = zone.Id,
                    OrderNo = zone.OrderNo,
                    PropertyRegNo = zone.PropertyRegNo,
                    PropertyDetail = zone.Name,
                    BaseAreaM2 = totalBaseAreaM2 > 0 ? totalBaseAreaM2.ToString("F3") : "",
                    BaseWeight = baseWeight > 0 ? Math.Round(baseWeight, 0).ToString("N0") : "",
                    WorkAreaM2 = totalWorkAreaM2 > 0 ? totalWorkAreaM2.ToString("F3") : "",
                    WorkWeight = workWeight > 0 ? Math.Round(workWeight, 0).ToString("N0") : "",
                    ActualSheets = sheetCount > 0 ? sheetCount.ToString() : "",
                    DesiredDeliveryDate = DateTime.Today.ToString("yyyy/MM/dd"),
                    SiteArrivalDate = DateTime.Today.ToString("yyyy/MM/dd"),
                    FactoryShipScheduledDate = DateTime.Today.ToString("yyyy/MM/dd"),
                    ChangeNotAllowed = "変更不可",
                });
            }
            return result;
        }

        private Dictionary<int, List<FamilyInstance>> GetECPElementsByZone()
        {
            var result = new Dictionary<int, List<FamilyInstance>>();
            var assignSchema = new ECPZoneAssignSchema(ECPZoneAssignSchema.GUID, ECPZoneAssignSchema.NAME);

            var ecpElements = new FilteredElementCollector(_document)
                .WhereElementIsNotElementType()
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(x => x.Symbol.FamilyName.ToUpper().Contains("ECP"));

            foreach (var elem in ecpElements)
            {
                var json = assignSchema.Read(elem);
                if (string.IsNullOrEmpty(json)) continue;

                var assignedZone = JsonConvert.DeserializeObject<ECPZoneSaveModel>(json);
                if (assignedZone == null) continue;

                if (!result.ContainsKey(assignedZone.Id))
                    result[assignedZone.Id] = new List<FamilyInstance>();
                result[assignedZone.Id].Add(elem);
            }
            return result;
        }

        private (double baseAreaM2, double workAreaM2) GetElementArea(FamilyInstance element)
        {
            var widthMaxParam = element.Symbol.LookupParameter(WallParam.WidthMax);
            var widthParam = element.LookupParameter(WallParam.Width);
            var heightParam = element.LookupParameter(WallParam.Length);

            if (widthMaxParam == null || widthParam == null || heightParam == null)
                return (0, 0);

            var widthMaxMm = widthMaxParam.AsDouble().ToMillimeters();
            var widthMm = widthParam.AsDouble().ToMillimeters();
            var heightMm = heightParam.AsDouble().ToMillimeters();

            var baseAreaM2 = (widthMaxMm * heightMm) / 1_000_000.0;
            var workAreaM2 = (widthMm * heightMm) / 1_000_000.0;

            return (Math.Round(baseAreaM2, 3), Math.Round(workAreaM2, 3));
        }

        private void _SearchCommand() { }

        private void _CheckAllOnCommand()
        {
            foreach (var order in _viewModel.Orders)
                order.IsChecked = true;
        }

        private void _CheckAllOffCommand()
        {
            foreach (var order in _viewModel.Orders)
                order.IsChecked = false;
        }

        private void _IsAllCheckedAction()
        {
            if (_viewModel.IsAllChecked == true)
                _CheckAllOnCommand();
            else if (_viewModel.IsAllChecked == false)
                _CheckAllOffCommand();
        }

        private void Order_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ECPOrderScheduleModel.IsChecked)) return;

            var checkedCount = _viewModel.Orders.Count(o => o.IsChecked);
            bool? newState;
            if (checkedCount == 0) newState = false;
            else if (checkedCount == _viewModel.Orders.Count) newState = true;
            else newState = null;

            _viewModel.SetIsAllCheckedFromRows(newState);
            _viewModel.OrderSlipCommand?.NotifyCanExecuteChanged();
        }

        private void _CsvCommand() => IO.ShowInfo("CSV出力は未実装です");
        private void _ItemCountCommand() => IO.ShowInfo("品種計は未実装です");
        private void _CopyOrderWithDetailCommand() => IO.ShowInfo("コピー受注(明細有)は未実装です");
        private void _CopyOrderWithoutDetailCommand() => IO.ShowInfo("コピー受注(明細無)は未実装です");
        private void _InquiryCommand() => IO.ShowInfo("照会は未実装です");
        private void _DeleteCommand() => IO.ShowInfo("削除は未実装です");

        private void _OrderSlipCommand() => ExportOrderSlips(_viewModel.Orders.Where(o => o.IsChecked).ToList());

        private void _PrintListCommand() => ExportOrderSlips(_viewModel.Orders.ToList());

        private void ExportOrderSlips(List<ECPOrderScheduleModel> checkedOrders)
        {
            if (!checkedOrders.Any()) return;

            using var dialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = "order",
            };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                var slips = checkedOrders.Select(o => new ECPOrderSlipPdfExporter.Slip
                {
                    Header = ECPCreateScheduleOrderDetailAction.BuildHeader(_document, o, _viewModel.Header.BranchOffice, o.ZoneId),
                    Rows = GetDetailRows(o.ZoneId),
                }).ToList();
                ECPOrderSlipPdfExporter.Export(slips, dialog.FileName);
                IO.ShowInfo($"Exported {checkedOrders.Count} order(s) to:\n{dialog.FileName}");
            }
            catch (Exception ex)
            {
                IO.ShowWarning(ex.Message);
            }
        }

        private void _ModifyCommand()
        {
            var order = _viewModel.SelectedOrder;
            if (order == null) return;

            var rows = GetDetailRows(order.ZoneId);

            _pendingDetail = (order, rows);
            _view.Close();
        }

        private List<ECPOrderDetailRowModel> GetDetailRows(int zoneId)
        {
            var rows = new List<ECPOrderDetailRowModel>();
            if (!GetECPElementsByZone().TryGetValue(zoneId, out var elements)) return rows;

            var groups = elements
                .Select(e => new
                {
                    Family = e.Symbol.FamilyName,
                    Type = e.Symbol.Name,
                    Length = Math.Round(e.LookupParameter(WallParam.Length)?.AsDouble().ToMillimeters() ?? 0),
                    Area = GetElementArea(e),
                    Image = WallImage.GetImageWall(e),
                })
                .GroupBy(x => (x.Family, x.Type, x.Length, x.Image))
                .OrderBy(g => g.Key.Type)
                .ThenByDescending(g => g.Key.Length);

            var no = 1;
            foreach (var g in groups)
            {
                var baseArea = g.Sum(x => x.Area.baseAreaM2);
                var workArea = g.Sum(x => x.Area.workAreaM2);
                rows.Add(new ECPOrderDetailRowModel
                {
                    BaseArea = baseArea.ToString("F3"),
                    BaseWeight = Math.Round(baseArea * WEIGHT_PER_M2).ToString("0"),
                    WorkArea = workArea.ToString("F3"),
                    WorkWeight = Math.Round(workArea * WEIGHT_PER_M2).ToString("0"),
                    ProcessCode = "K",
                    ImageName = g.Key.Image,
                    No = no.ToString(),
                    ProductName = g.Key.Family,
                    PartNumber = g.Key.Type,
                    Length = g.Key.Length.ToString("0"),
                    Quantity = g.Count().ToString(),
                    IsAlternate = (no - 1) % 2 == 1,
                });
                no++;
            }
            foreach (var r in rows) _ = r.Key;
            ECPOrderDetailHeaderStore.ApplySavedRows(_document, zoneId, rows);
            return rows;
        }

        private void _SaveCommand()
        {
            SaveData();
            IO.ShowInfo("保存しました");
        }

        private void _BackCommand()
        {
            SaveData();
            _view.Close();
        }
    }
}
