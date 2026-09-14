using System.ComponentModel;
using DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.model;
using DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.view;
using DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.viewModel;
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

        private readonly Document _document;
        private ECPCreateScheduleVM _viewModel;
        private ECPCreateScheduleView _view;

        public ECPCreateScheduleAction(Document document)
        {
            _document = document;
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
                OrderSlipCommand = new RelayCommand(_OrderSlipCommand),
                CsvCommand = new RelayCommand(_CsvCommand),
                ItemCountCommand = new RelayCommand(_ItemCountCommand),
                CopyOrderWithDetailCommand = new RelayCommand(_CopyOrderWithDetailCommand),
                CopyOrderWithoutDetailCommand = new RelayCommand(_CopyOrderWithoutDetailCommand),
                InquiryCommand = new RelayCommand(_InquiryCommand),
                ModifyCommand = new RelayCommand(_ModifyCommand),
                DeleteCommand = new RelayCommand(_DeleteCommand),
                BackCommand = new RelayCommand(_BackCommand),
                // JP: ヘッダーの3状態チェックボックス（オーダー票印刷の全選択/全解除/不確定）
                // VI: Checkbox 3 trạng thái ở header (chọn hết/bỏ chọn hết/không xác định)
                // EN: 3-state header checkbox (select all / deselect all / indeterminate)
                IsAllCheckedAction = _IsAllCheckedAction,
            };

            // JP: 検索欄の初期値（サンプル）
            // VI: Giá trị mặc định cho ô tìm kiếm (dữ liệu mẫu)
            // EN: Default values for the search fields (sample data)
            _viewModel.Header.PropertyName = "株式会社コパルコンパスティクス 加古川地区新事務所建設工事"; // JP:物件名称 VI:Tên công trình EN:Property name
            _viewModel.Header.PropertyDetail = "1F○工区-コーナー"; // JP:物件詳細 VI:Chi tiết công trình EN:Property detail
            _viewModel.Header.DealerName = "住友林業株式会社"; // JP:販売店名称(1行目) VI:Tên đại lý (dòng 1) EN:Dealer name (line 1)
            _viewModel.Header.DealerDivision = "木材建材事業本部大阪営業部 ツリューションクループ"; // JP:販売店名称(2行目/部署名) VI:Tên đại lý (dòng 2/bộ phận) EN:Dealer name (line 2/division)
            _viewModel.Header.BranchOffice = "大阪支店"; // JP:担当支店(1行目) VI:Chi nhánh (dòng 1) EN:Branch office (line 1)
            _viewModel.Header.BranchOfficeDetail = "大阪支店"; // JP:担当支店(2行目) VI:Chi nhánh (dòng 2) EN:Branch office (line 2)
            _viewModel.Header.OrderSlipType = _viewModel.OrderSlipTypes.FirstOrDefault();

            foreach (var order in GetOrders())
            {
                // JP: 各行のチェック変化を監視してヘッダーの3状態チェックボックスに反映する
                // VI: Theo dõi thay đổi checkbox từng dòng để đồng bộ vào checkbox 3 trạng thái ở header
                // EN: Watch each row's checkbox change to keep the 3-state header checkbox in sync
                order.PropertyChanged += Order_PropertyChanged;
                _viewModel.Orders.Add(order);
            }

            // JP: Header.PropertyName が変わったら全行の PropertyName を同期する
            // VI: Khi Header.PropertyName thay đổi, đồng bộ xuống PropertyName của tất cả các dòng
            // EN: When Header.PropertyName changes, sync it to all order rows' PropertyName
            _viewModel.Header.PropertyChanged += Header_PropertyChanged;

            _view = new ECPCreateScheduleView { DataContext = _viewModel };
        }

        public void Execute()
        {
            _view.ShowDialog();
        }

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
        }

        // JP: 以下のボタンは未実装のスタブ（クリックすると案内メッセージを表示するだけ）
        // VI: Các nút dưới đây chỉ là stub chưa triển khai (bấm vào chỉ hiện thông báo)
        // EN: The buttons below are unimplemented stubs (clicking just shows an info message)
        private void _PrintListCommand() => IO.ShowInfo("一覧印刷は未実装です"); // VI: Chưa triển khai In danh sách / EN: Print list not implemented
        private void _OrderSlipCommand() => IO.ShowInfo("オーダー票発行は未実装です"); // VI: Chưa triển khai Xuất phiếu đặt hàng / EN: Issue order slip not implemented
        private void _CsvCommand() => IO.ShowInfo("CSV出力は未実装です"); // VI: Chưa triển khai Xuất CSV / EN: Export CSV not implemented
        private void _ItemCountCommand() => IO.ShowInfo("品種計は未実装です"); // VI: Chưa triển khai Thống kê theo chủng loại / EN: Item-type totals not implemented
        private void _CopyOrderWithDetailCommand() => IO.ShowInfo("コピー受注(明細有)は未実装です"); // VI: Chưa triển khai Sao chép đơn hàng (có chi tiết) / EN: Copy order with detail not implemented
        private void _CopyOrderWithoutDetailCommand() => IO.ShowInfo("コピー受注(明細無)は未実装です"); // VI: Chưa triển khai Sao chép đơn hàng (không chi tiết) / EN: Copy order without detail not implemented
        private void _InquiryCommand() => IO.ShowInfo("照会は未実装です"); // VI: Chưa triển khai Tra cứu / EN: Inquiry not implemented
        private void _ModifyCommand() => IO.ShowInfo("修正は未実装です"); // VI: Chưa triển khai Sửa / EN: Modify not implemented
        private void _DeleteCommand() => IO.ShowInfo("削除は未実装です"); // VI: Chưa triển khai Xóa / EN: Delete not implemented

        private void _BackCommand()
        {
            _view.Close();
        }
    }
}
