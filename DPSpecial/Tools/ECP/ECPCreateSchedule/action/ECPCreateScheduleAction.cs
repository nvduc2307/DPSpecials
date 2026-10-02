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
    // JP: 画面の初期化・コマンド・サンプルデータを組み立てるクラス
    // VI: Lớp khởi tạo dữ liệu mẫu, gắn command và hiển thị màn hình
    // EN: Class that wires up sample data, commands, and shows the window
    public partial class ECPCreateScheduleAction
    {
        // JP: ECP基材の単位面積あたりの重量 (kg/m²)
        // VI: Khối lượng riêng của vật liệu nền ECP (kg/m²)
        // EN: Weight per unit area of ECP base material (kg/m²)
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
                // JP: 担当支店の選択肢（サンプル）
                // VI: Danh sách chi nhánh phụ trách (dữ liệu mẫu)
                // EN: Branch office options (sample data)
                BranchOffices = new List<string> { "大阪支店", "東京支店", "名古屋支店", "福岡支店" },

                // JP: オーダー票種類の選択肢（サンプル）
                // VI: Danh sách loại phiếu đặt hàng (dữ liệu mẫu)
                // EN: Order slip type options (sample data)
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
                // JP: ヘッダーの3状態チェックボックス（オーダー票印刷の全選択/全解除/不確定）
                // VI: Checkbox 3 trạng thái ở header (chọn hết/bỏ chọn hết/không xác định)
                // EN: 3-state header checkbox (select all / deselect all / indeterminate)
                IsAllCheckedAction = _IsAllCheckedAction,
            };

            // JP: 保存済みデータを読み込み、ゾーンとECP要素を組み合わせてオーダー一覧を生成する
            // VI: Đọc dữ liệu đã lưu, kết hợp zone + ECP elements để tạo danh sách order
            // EN: Load saved data, combine with zones + ECP elements to build order list
            LoadAndMergeOrders();

            // JP: Header.PropertyName が変わったら全行の PropertyName を同期する
            // VI: Khi Header.PropertyName thay đổi, đồng bộ xuống PropertyName của tất cả các dòng
            // EN: When Header.PropertyName changes, sync it to all order rows' PropertyName
            _viewModel.Header.PropertyChanged += Header_PropertyChanged;

            _view = new ECPCreateScheduleView { DataContext = _viewModel };
        }

        // JP: 「修正」で一覧画面を閉じた場合は、明細画面を開き、閉じたら一覧画面を作り直して再表示する
        //     ※ Hide/Show ではなく Close + 再作成にするのは、モーダルを維持したままだと Revit の API コンテキストが失われ、
        //       戻るボタンの Transaction.Start() が失敗するため
        // VI: Nếu danh sách bị đóng do bấm "修正" thì mở màn hình chi tiết, đóng xong thì tạo lại danh sách và mở lại.
        //     Dùng Close + tạo lại thay vì Hide/Show vì giữ dialog modal làm mất API context của Revit,
        //     khiến Transaction.Start() ở nút Back bị lỗi
        // EN: If the list was closed by "修正", open the detail window, then rebuild and re-show the list when it closes.
        //     Close + rebuild is used instead of Hide/Show because keeping the modal alive loses Revit's API context,
        //     which made Transaction.Start() in the Back button fail
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

        // JP: 保存済みデータをProjectInformationから読み込み、現在のゾーン定義とECP要素の実測値を
        //     組み合わせて、オーダー一覧を生成する。保存データが無い場合はゾーン/要素から新規作成する。
        // VI: Đọc dữ liệu đã lưu từ ProjectInformation, kết hợp với zone definitions và ECP elements
        //     thực tế để tạo danh sách order. Nếu chưa có dữ liệu lưu thì tạo mới từ zone/elements.
        // EN: Read saved data from ProjectInformation, merge with current zone definitions and actual
        //     ECP elements to build the order list. If no saved data exists, create fresh from zones/elements.
        private void LoadAndMergeOrders()
        {
            // JP: 保存済みデータを読み込む | VI: Đọc dữ liệu đã lưu | EN: Load saved data
            var savedData = LoadSavedData();

            // JP: ヘッダーを復元する（保存データがあればそれを使う、なければデフォルト値を設定）
            // VI: Khôi phục header (dùng dữ liệu đã lưu nếu có, không thì dùng giá trị mặc định)
            // EN: Restore header (use saved data if available, otherwise set defaults)
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
                // JP: 初回起動時のデフォルト値
                // VI: Giá trị mặc định lần chạy đầu tiên
                // EN: Default values for first run
                _viewModel.Header.PropertyName = string.Empty;
                _viewModel.Header.PropertyDetail = string.Empty;
                _viewModel.Header.DealerName = string.Empty;
                _viewModel.Header.DealerDivision = string.Empty;
                _viewModel.Header.BranchOffice = string.Empty;
                _viewModel.Header.BranchOfficeDetail = string.Empty;
                _viewModel.Header.OrderSlipType = _viewModel.OrderSlipTypes.FirstOrDefault();
            }

            // JP: ゾーン定義とECP要素から最新のオーダーデータを計算する
            // VI: Tính toán dữ liệu order mới nhất từ zone definitions và ECP elements
            // EN: Compute fresh order data from zone definitions and ECP elements
            var freshOrders = GetOrders();

            // JP: 保存済みの手入力データ（日付・ステータス等）を最新の計算結果にマージする
            // VI: Merge dữ liệu đã nhập thủ công (ngày, trạng thái, v.v.) vào kết quả tính mới nhất
            // EN: Merge saved manual-entry data (dates, status, etc.) into the freshly computed results
            var savedOrdersByOrderNo = savedData?.Orders?
                .Where(o => !string.IsNullOrEmpty(o.OrderNo))
                .ToDictionary(o => o.OrderNo, o => o)
                ?? new Dictionary<string, ECPOrderScheduleOrderSaveModel>();

            foreach (var order in freshOrders)
            {
                if (savedOrdersByOrderNo.TryGetValue(order.OrderNo, out var saved))
                {
                    // JP: 面積・重量・枚数は実測値（最新）を使い、手入力項目は保存値を復元する
                    // VI: Diện tích/khối lượng/số tấm dùng giá trị tính mới, các mục nhập tay thì khôi phục từ dữ liệu đã lưu
                    // EN: Use freshly computed area/weight/sheets, restore manually entered fields from saved data
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

                // JP: 各行のチェック変化を監視してヘッダーの3状態チェックボックスに反映する
                // VI: Theo dõi thay đổi checkbox từng dòng để đồng bộ vào checkbox 3 trạng thái ở header
                // EN: Watch each row's checkbox change to keep the 3-state header checkbox in sync
                order.PropertyChanged += Order_PropertyChanged;
                _viewModel.Orders.Add(order);
            }

            // JP: 読み込み後、全行のチェック状態からヘッダーの3状態チェックボックスを同期する
            // VI: Sau khi load xong, đồng bộ checkbox 3 trạng thái ở header dựa trên trạng thái các dòng
            // EN: After loading, sync the header's 3-state checkbox from the rows' checked states
            if (_viewModel.Orders.Any())
            {
                var checkedCount = _viewModel.Orders.Count(o => o.IsChecked);
                if (checkedCount == 0) _viewModel.SetIsAllCheckedFromRows(false);
                else if (checkedCount == _viewModel.Orders.Count) _viewModel.SetIsAllCheckedFromRows(true);
                else _viewModel.SetIsAllCheckedFromRows(null);
            }
        }

        // JP: ProjectInformation から保存済みデータを読み込む
        // VI: Đọc dữ liệu đã lưu từ ProjectInformation
        // EN: Read saved data from ProjectInformation
        private ECPOrderScheduleSaveModel LoadSavedData()
        {
            var content = _orderScheduleSchema.Read(_document.ProjectInformation);
            if (string.IsNullOrEmpty(content)) return null;
            return JsonConvert.DeserializeObject<ECPOrderScheduleSaveModel>(content);
        }

        // JP: 現在のヘッダーとオーダー一覧を ProjectInformation に保存する
        // VI: Lưu header và danh sách order hiện tại vào ProjectInformation
        // EN: Save current header and order list to ProjectInformation
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

        // JP: ヘッダーの共有フィールドが変わったら、全行を同じ値に同期する
        // VI: Khi các trường chung ở header thay đổi → đồng bộ xuống tất cả dòng trong Orders
        // EN: When shared header fields change → sync to all order rows
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

        // JP: ゾーン定義を読み取り、各ゾーンに属するECP要素から面積・重量を集計して1行のオーダーデータを作成する
        //     基材m² = WidthMax(固定寸法) × Height(高さ)  ← 元板寸法（カット前）
        //     働きm² = Width(幅) × Height(高さ)           ← 実際の施工寸法（カット後）
        //     重量   = 面積 × WEIGHT_PER_M2
        // VI: Đọc zone, thu thập các phần tử ECP thuộc mỗi zone, tính tổng diện tích & khối lượng
        //     基材m² = WidthMax(固定寸法) × Height(高さ)  ← kích thước nguyên tấm (trước cắt)
        //     働きm² = Width(幅) × Height(高さ)           ← kích thước thi công thực tế (sau cắt)
        //     Khối lượng = Diện tích × WEIGHT_PER_M2
        // EN: Read zones, collect ECP elements per zone, sum area & weight
        //     BaseArea = WidthMax × Height  ← full panel dimension (before cutting)
        //     WorkArea = Width × Height     ← actual working dimension (after cutting)
        //     Weight   = Area × WEIGHT_PER_M2
        private List<ECPOrderScheduleModel> GetOrders()
        {
            var result = new List<ECPOrderScheduleModel>();

            // JP: ゾーン定義を読み取る | VI: Đọc danh sách zone | EN: Read zone definitions
            var zoneSchema = new ECPZoneSchema(ECPZoneSchema.GUID, ECPZoneSchema.NAME);
            var content = zoneSchema.Read(_document.ProjectInformation);
            if (string.IsNullOrEmpty(content)) return result;

            var zones = JsonConvert.DeserializeObject<List<ECPZoneSaveModel>>(content) ?? new List<ECPZoneSaveModel>();
            if (!zones.Any()) return result;

            // JP: 全ECP要素を取得し、ゾーンIDでグループ化する
            // VI: Lấy tất cả phần tử ECP, nhóm theo Zone ID
            // EN: Collect all ECP elements and group by assigned zone Id
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

        // JP: ドキュメント内の全ECP FamilyInstanceを取得し、各要素に割り当てられたゾーンIDでグループ化する
        // VI: Lấy tất cả FamilyInstance ECP trong document, nhóm theo Zone ID đã gán cho từng phần tử
        // EN: Collect all ECP FamilyInstances in the document and group them by their assigned zone Id
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

        // JP: 1つのECP要素から基材面積と働き面積を計算する (単位: m²)
        //     基材面積 = WidthMax(固定寸法) × Height  ← カット前の元板サイズ
        //     働き面積 = Width(幅) × Height            ← カット後の実際の施工サイズ
        //     Revit内部の寸法は feet → ToMillimeters() で mm に変換後、m² に換算
        // VI: Tính diện tích vật liệu nền và diện tích thi công từ 1 phần tử ECP (đơn vị: m²)
        //     Diện tích nền = WidthMax(固定寸法) × Height  ← kích thước tấm gốc trước cắt
        //     Diện tích thi công = Width(幅) × Height      ← kích thước thực tế sau cắt
        //     Kích thước trong Revit là feet → ToMillimeters() chuyển sang mm, rồi quy đổi ra m²
        // EN: Compute base area and working area for one ECP element (unit: m²)
        //     BaseArea = WidthMax × Height  ← original panel size before cutting
        //     WorkArea = Width × Height     ← actual installed size after cutting
        //     Revit internal units are feet → ToMillimeters() converts to mm, then to m²
        private (double baseAreaM2, double workAreaM2) GetElementArea(FamilyInstance element)
        {
            // JP: WidthMax(固定寸法) = Type parameter, Width(幅) = Instance parameter
            // VI: WidthMax(固定寸法) = tham số Type, Width(幅) = tham số Instance
            // EN: WidthMax(固定寸法) = Type parameter, Width(幅) = Instance parameter
            var widthMaxParam = element.Symbol.LookupParameter(WallParam.WidthMax);
            var widthParam = element.LookupParameter(WallParam.Width);
            var heightParam = element.LookupParameter(WallParam.Length);

            if (widthMaxParam == null || widthParam == null || heightParam == null)
                return (0, 0);

            var widthMaxMm = widthMaxParam.AsDouble().ToMillimeters();
            var widthMm = widthParam.AsDouble().ToMillimeters();
            var heightMm = heightParam.AsDouble().ToMillimeters();

            // mm² → m²  (÷ 1,000,000)
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

        // JP: ヘッダーの3状態チェックボックスをユーザーがクリックした時の処理
        //     true→全行ON、false→全行OFF。null（不確定）はユーザーが直接選べる状態ではないため無視する
        // VI: Xử lý khi người dùng bấm checkbox 3 trạng thái ở header
        //     true→bật hết, false→tắt hết. null (không xác định) không phải trạng thái người dùng chọn trực tiếp nên bỏ qua
        // EN: Handles the user clicking the 3-state header checkbox
        //     true→check all rows, false→uncheck all rows. null (indeterminate) is never user-selectable directly, so it's ignored
        private void _IsAllCheckedAction()
        {
            if (_viewModel.IsAllChecked == true)
                _CheckAllOnCommand();
            else if (_viewModel.IsAllChecked == false)
                _CheckAllOffCommand();
        }

        // JP: 行のIsCheckedが変わるたびに呼ばれ、ヘッダーの状態を再計算する
        // VI: Được gọi mỗi khi IsChecked của 1 dòng thay đổi, tính lại trạng thái header
        // EN: Called whenever a row's IsChecked changes, recomputes the header's state
        private void Order_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ECPOrderScheduleModel.IsChecked)) return;

            var checkedCount = _viewModel.Orders.Count(o => o.IsChecked);
            bool? newState;
            if (checkedCount == 0) newState = false;
            else if (checkedCount == _viewModel.Orders.Count) newState = true;
            else newState = null; // JP:一部だけON = 不確定 VI:chỉ một phần ON = không xác định EN:some but not all on = indeterminate

            _viewModel.SetIsAllCheckedFromRows(newState);
            _viewModel.OrderSlipCommand?.NotifyCanExecuteChanged();
        }

        // JP: 以下のボタンは未実装のスタブ（クリックすると案内メッセージを表示するだけ）
        // VI: Các nút dưới đây chỉ là stub chưa triển khai (bấm vào chỉ hiện thông báo)
        // EN: The buttons below are unimplemented stubs (clicking just shows an info message)
        private void _PrintListCommand() => IO.ShowInfo("一覧印刷は未実装です"); // VI: Chưa triển khai In danh sách / EN: Print list not implemented
        private void _CsvCommand() => IO.ShowInfo("CSV出力は未実装です"); // VI: Chưa triển khai Xuất CSV / EN: Export CSV not implemented
        private void _ItemCountCommand() => IO.ShowInfo("品種計は未実装です"); // VI: Chưa triển khai Thống kê theo chủng loại / EN: Item-type totals not implemented
        private void _CopyOrderWithDetailCommand() => IO.ShowInfo("コピー受注(明細有)は未実装です"); // VI: Chưa triển khai Sao chép đơn hàng (có chi tiết) / EN: Copy order with detail not implemented
        private void _CopyOrderWithoutDetailCommand() => IO.ShowInfo("コピー受注(明細無)は未実装です"); // VI: Chưa triển khai Sao chép đơn hàng (không chi tiết) / EN: Copy order without detail not implemented
        private void _InquiryCommand() => IO.ShowInfo("照会は未実装です"); // VI: Chưa triển khai Tra cứu / EN: Inquiry not implemented
        private void _DeleteCommand() => IO.ShowInfo("削除は未実装です"); // VI: Chưa triển khai Xóa / EN: Delete not implemented

        // JP: オーダー票ボタン — チェックされたゾーンのオーダー票を1つのPDFにまとめて出力する（ゾーンごとに新しいページ）
        // VI: Nút オーダー票 — xuất phiếu đặt hàng của các zone được check gộp thành 1 PDF (mỗi zone bắt đầu từ trang mới)
        // EN: Order slip button — export the order slips of the checked zones into one PDF (each zone starts on a new page)
        private void _OrderSlipCommand()
        {
            var checkedOrders = _viewModel.Orders.Where(o => o.IsChecked).ToList();
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

        // JP: 修正ボタン — 一覧画面を隠して明細画面を開き、明細画面が閉じたら一覧画面を再表示する
        // VI: Nút Sửa — ẩn màn hình danh sách, mở màn hình chi tiết; khi chi tiết đóng thì hiện lại danh sách
        // EN: Modify button — hide the list window, open the detail window, re-show the list when the detail closes
        private void _ModifyCommand()
        {
            var order = _viewModel.SelectedOrder;
            if (order == null) return;

            // JP: 選択行のゾーンに属するECP要素だけを明細行に変換する
            // VI: Chỉ chuyển các phần tử ECP thuộc zone của dòng được chọn thành dòng chi tiết
            // EN: Convert only the ECP elements belonging to the selected row's zone into detail rows
            var rows = GetDetailRows(order.ZoneId);

            // JP: 一覧画面を閉じるだけにして、明細画面の表示と一覧の再表示は Execute() のループで行う
            // VI: Chỉ đóng màn hình danh sách; việc mở màn hình chi tiết và mở lại danh sách do vòng lặp trong Execute() đảm nhiệm
            // EN: Just close the list window; opening the detail window and re-opening the list is done by the loop in Execute()
            _pendingDetail = (order, rows);
            _view.Close();
        }

        // JP: 指定ゾーンのECP要素を (型番, 長さ) ごとにまとめ、数量付きの明細行を作る
        // VI: Gom các phần tử ECP của zone theo (mã loại, chiều dài) và tạo dòng chi tiết kèm số lượng
        // EN: Group the zone's ECP elements by (type, length) and build detail rows with quantities
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
                })
                .GroupBy(x => (x.Family, x.Type, x.Length))
                .OrderBy(g => g.Key.Type)
                .ThenByDescending(g => g.Key.Length);

            var no = 1;
            foreach (var g in groups)
            {
                // JP: 面積は m²、重量は 面積 × WEIGHT_PER_M2 (kg, 整数)。注文票PDFの 基材/働き 列に使う
                // VI: Diện tích m², khối lượng = diện tích × WEIGHT_PER_M2 (kg, số nguyên); dùng cho cột 基材/働き của PDF
                // EN: Area in m², weight = area × WEIGHT_PER_M2 (kg, integer); used by the 基材/働き columns of the PDF
                var baseArea = g.Sum(x => x.Area.baseAreaM2);
                var workArea = g.Sum(x => x.Area.workAreaM2);
                rows.Add(new ECPOrderDetailRowModel
                {
                    BaseArea = baseArea.ToString("F3"),
                    BaseWeight = Math.Round(baseArea * WEIGHT_PER_M2).ToString("0"),
                    WorkArea = workArea.ToString("F3"),
                    WorkWeight = Math.Round(workArea * WEIGHT_PER_M2).ToString("0"),
                    ProcessCode = "K",
                    No = no.ToString(),
                    ProductName = g.Key.Family,
                    PartNumber = g.Key.Type,
                    Length = g.Key.Length.ToString("0"),
                    Quantity = g.Count().ToString(),
                    IsAlternate = (no - 1) % 2 == 1,
                });
                no++;
            }
            return rows;
        }

        // JP: 保存ボタン — 画面を閉じずにデータだけ保存する
        // VI: Nút Save — chỉ lưu dữ liệu, không đóng màn hình
        // EN: Save button — save data and keep the window open
        private void _SaveCommand()
        {
            SaveData();
            IO.ShowInfo("保存しました");
        }

        // JP: 戻るボタン — データを保存してから画面を閉じる
        // VI: Nút Back — lưu dữ liệu rồi đóng màn hình
        // EN: Back button — save data then close the window
        private void _BackCommand()
        {
            SaveData();
            _view.Close();
        }
    }
}
