using System.ComponentModel;
using DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.model;
using DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.view;
using DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.viewModel;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPCreateSchedule.action
{
    // JP: 画面の初期化・コマンド・サンプルデータを組み立てるクラス
    // VI: Lớp khởi tạo dữ liệu mẫu, gắn command và hiển thị màn hình
    // EN: Class that wires up sample data, commands, and shows the window
    public partial class ECPCreateScheduleAction
    {
        private ECPCreateScheduleVM _viewModel;
        private ECPCreateScheduleView _view;

        public ECPCreateScheduleAction()
        {
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
            _viewModel.PropertyName = "株式会社コパルコンパスティクス 加古川地区新事務所建設工事"; // JP:物件名称 VI:Tên công trình EN:Property name
            _viewModel.DealerName = "住友林業株式会社"; // JP:販売店名称(1行目) VI:Tên đại lý (dòng 1) EN:Dealer name (line 1)
            _viewModel.DealerDivision = "木材建材事業本部大阪営業部 ツリューションクループ"; // JP:販売店名称(2行目/部署名) VI:Tên đại lý (dòng 2/bộ phận) EN:Dealer name (line 2/division)
            _viewModel.BranchOffice = _viewModel.BranchOffices.FirstOrDefault();
            _viewModel.OrderSlipType = _viewModel.OrderSlipTypes.FirstOrDefault();

            foreach (var order in GetSampleOrders())
            {
                // JP: 各行のチェック変化を監視してヘッダーの3状態チェックボックスに反映する
                // VI: Theo dõi thay đổi checkbox từng dòng để đồng bộ vào checkbox 3 trạng thái ở header
                // EN: Watch each row's checkbox change to keep the 3-state header checkbox in sync
                order.PropertyChanged += Order_PropertyChanged;
                _viewModel.Orders.Add(order);
            }

            _view = new ECPCreateScheduleView { DataContext = _viewModel };
        }

        public void Execute()
        {
            _view.ShowDialog();
        }

        // JP: グリッドに表示するサンプル行データを作成する
        // VI: Tạo dữ liệu mẫu cho các dòng hiển thị trong lưới
        // EN: Build sample rows to display in the grid
        private List<ECPOrderScheduleModel> GetSampleOrders()
        {
            const string propertyName = "株式会社コパルコンパスティクス 加古川地区新事務所建設工事"; // JP:物件名称 VI:Tên công trình EN:Property name
            const string dealerName = "住友林業株式会社"; // JP:販売店名称 VI:Tên đại lý EN:Dealer name
            const string desiredDeliveryDate = "2026/09/24"; // JP:希望納期 VI:Ngày giao hàng mong muốn EN:Desired delivery date
            const string siteArrivalDate = "2026/10/05"; // JP:現場到着予定日 VI:Ngày dự kiến hàng đến công trường EN:Scheduled site arrival date

            // JP: 工場出荷予定日 - 列「工場出荷予定日 / 変更期日(赤)」の1行目データ。2行目は変更不可（赤字）
            // VI: Ngày dự kiến xuất xưởng - dữ liệu dòng 1 của cột "工場出荷予定日 / 変更期日(đỏ)". Dòng 2 là 変更不可 (đỏ)
            // EN: Scheduled factory shipment date - line-1 data for the "工場出荷予定日 / 変更期日(red)" column. Line 2 is 変更不可 (red)
            const string factoryShipScheduledDate = "2026/10/02";

            const string manufacturingFactory = "市川工場"; // JP:製造工場 VI:Nhà máy sản xuất EN:Manufacturing factory
            const string salesRep = "田川 雅浩"; // JP:営業担当者 VI:Nhân viên kinh doanh phụ trách EN:Sales representative
            const string progressStatus = "納期回答済"; // JP:進捗状況 VI:Tình trạng tiến độ (đã phản hồi ngày giao) EN:Progress status (delivery date confirmed)

            // JP:受注No, 物件詳細, 基材m², 基材重量, 働きm², 働き重量, 役物m², 実枚, 内変形
            // VI:Số đơn hàng, Chi tiết công trình, Diện tích/Khối lượng vật liệu nền, Diện tích/Khối lượng thi công, Diện tích phụ kiện, Số tấm thực tế, Số biến dạng nội bộ
            // EN:Order No, Property detail, Base area/weight, Working area/weight, Accessory area, Actual sheets, Internal deform count
            (string orderNo, string propertyDetail, string baseArea, string baseWeight, string workArea, string workWeight, string accessoryArea, string sheets, string internalDeform)[] rows =
            {
                ("1262702924", "1F①工区-コーナー", "2.462", "151", "2.462", "151", "4.245", "1", ""),
                ("1262702925", "1F②工区-フラット", "27.398", "1,737", "27.250", "1,728", "", "13", ""),
                ("1262702926", "1F③工区-ワイド", "5.076", "336", "5.076", "336", "", "2", ""),
                ("1262702927", "1F④工区-コーナー", "2.462", "151", "2.462", "151", "4.245", "1", ""),
                ("1262702928", "1F⑤工区-フラット", "50.340", "3,194", "50.340", "3,194", "", "41", ""),
                ("1262702929", "1F⑥工区-フラット", "54.873", "3,485", "54.873", "3,485", "", "43", ""),
                ("1262702930", "1F⑦工区-コーナー", "6.974", "426", "6.256", "383", "12.025", "4", "2"),
                ("1262702931", "1F⑧工区-フラット", "44.258", "2,811", "39.958", "2,537", "", "28", "1"),
                ("1262702932", "1F⑨工区-コーナー", "2.462", "151", "2.462", "151", "4.245", "1", ""),
                ("1262702933", "1F⑩工区-フラット", "45.843", "2,906", "45.406", "2,878", "", "29", "1"),
                ("1262702934", "1F⑪工区-ワイド", "27.099", "1,801", "27.099", "1,801", "", "9", ""),
                ("1262702935", "1F⑫工区-フラット", "20.417", "1,299", "15.022", "955", "", "18", ""),
            };

            var result = new List<ECPOrderScheduleModel>();
            foreach (var row in rows)
            {
                result.Add(new ECPOrderScheduleModel
                {
                    OrderNo = row.orderNo,
                    PropertyRegNo = "B260000604",
                    PropertyName = propertyName,
                    PropertyDetail = row.propertyDetail,
                    DealerName = dealerName,
                    // JP: グリッド上は部署名を表示しない想定（検索欄にのみ表示）
                    // VI: Không hiển thị tên bộ phận trong lưới (chỉ hiển thị ở ô tìm kiếm)
                    // EN: Division name is not shown in the grid (search field only)
                    DealerDivision = string.Empty,
                    BaseAreaM2 = row.baseArea,
                    BaseWeight = row.baseWeight,
                    WorkAreaM2 = row.workArea,
                    WorkWeight = row.workWeight,
                    AccessoryAreaM2 = row.accessoryArea,
                    ActualSheets = row.sheets,
                    DesiredDeliveryDate = desiredDeliveryDate,
                    SiteArrivalDate = siteArrivalDate,
                    // JP: サンプルではこの2列は未加工のため空欄（ユーザー提供の画像に合わせる）
                    // VI: Trong dữ liệu mẫu, 2 cột này để trống vì đơn chưa gia công (khớp ảnh người dùng cung cấp)
                    // EN: Left blank in sample data since these orders are not yet processed (matches the user-provided screenshot)
                    ProcessShipScheduledDate = "",
                    ProcessArrivalScheduledDate = "",
                    FactoryShipScheduledDate = factoryShipScheduledDate,
                    ChangeNotAllowed = "変更不可", // JP:変更不可 VI:Không thể thay đổi EN:Cannot be changed
                    ProcessShipActualDate = "",
                    FactoryShipActualDate = "",
                    FinalShipDate = "",
                    TemporaryStorage = "",
                    DeliveryChangeCount = "",
                    InternalDeform = row.internalDeform,
                    ManufacturingFactory = manufacturingFactory,
                    ProcessingFactory = "",
                    SalesRep = salesRep,
                    ProgressStatus = progressStatus,
                });
            }
            return result;
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
