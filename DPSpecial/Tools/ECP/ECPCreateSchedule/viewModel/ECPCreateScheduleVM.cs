using System.Collections.ObjectModel;
using DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.model;

namespace DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.viewModel
{
    public partial class ECPCreateScheduleVM : ObservableObject
    {
        // JP: 物件名称 (検索欄)
        // VI: Tên công trình (ô tìm kiếm)
        // EN: Property/site name (search field)
        [ObservableProperty]
        private string _propertyName;

        // JP: 物件詳細 (検索欄)
        // VI: Chi tiết công trình (ô tìm kiếm)
        // EN: Property/site detail (search field)
        [ObservableProperty]
        private string _propertyDetail;

        // JP: 販売店名称 (検索欄・1行目)
        // VI: Tên đại lý bán hàng (ô tìm kiếm, dòng 1)
        // EN: Dealer name (search field, line 1)
        [ObservableProperty]
        private string _dealerName;

        // JP: 販売店名称の2行目（部署名）
        // VI: Dòng 2 của tên đại lý (tên bộ phận/phòng ban)
        // EN: Second line of dealer name (division/department name)
        [ObservableProperty]
        private string _dealerDivision;

        // JP: 担当支店
        // VI: Chi nhánh phụ trách
        // EN: Branch office in charge
        [ObservableProperty]
        private string _branchOffice;

        // JP: オーダー票 種類 (右上のコンボボックス、例: "1:オーダー票")
        // VI: Loại phiếu đặt hàng (combobox góc trên phải, vd "1:オーダー票")
        // EN: Order slip type (top-right combobox, e.g. "1:オーダー票")
        [ObservableProperty]
        private string _orderSlipType;

        // JP: 検索結果一覧（グリッドの行データ）
        // VI: Danh sách kết quả tìm kiếm (dữ liệu từng dòng trong lưới)
        // EN: Search result list (grid row data)
        public ObservableCollection<ECPOrderScheduleModel> Orders { get; set; } = new();

        private bool? _isAllChecked = false;
        // JP: 列見出しの3状態チェックボックス（オーダー票印刷）
        //     true=全行ON / false=全行OFF / null=一部の行だけON（不確定状態）
        //     ユーザーがクリックした時だけ IsAllCheckedAction を呼ぶ。行側の変化からの同期は
        //     SetIsAllCheckedFromRows で行い、無限ループを避ける。
        // VI: Checkbox 3 trạng thái ở header cột (オーダー票印刷)
        //     true=chọn hết / false=bỏ chọn hết / null=chỉ một phần dòng được chọn (trạng thái không xác định)
        //     Chỉ gọi IsAllCheckedAction khi người dùng bấm. Đồng bộ từ các dòng dùng
        //     SetIsAllCheckedFromRows để tránh lặp vô hạn.
        // EN: 3-state header checkbox (オーダー票印刷 column)
        //     true=all rows on / false=all rows off / null=only some rows on (indeterminate)
        //     IsAllCheckedAction fires only for user-driven clicks. Syncing from row changes goes
        //     through SetIsAllCheckedFromRows instead, to avoid an infinite loop.
        public bool? IsAllChecked
        {
            get => _isAllChecked;
            set
            {
                _isAllChecked = value;
                OnPropertyChanged();
                IsAllCheckedAction?.Invoke();
            }
        }
        public Action IsAllCheckedAction { get; set; }

        // JP: 行のチェック状態が変わった時に呼ぶ。ヘッダーのAction を発火させずに値だけ更新する
        // VI: Gọi khi trạng thái check của 1 dòng thay đổi. Chỉ cập nhật giá trị, không kích hoạt IsAllCheckedAction
        // EN: Call when a row's checked state changes. Updates the value only, without firing IsAllCheckedAction
        public void SetIsAllCheckedFromRows(bool? value)
        {
            _isAllChecked = value;
            OnPropertyChanged(nameof(IsAllChecked));
        }

        // JP: 担当支店の選択肢
        // VI: Danh sách chi nhánh phụ trách để chọn
        // EN: List of selectable branch offices
        public List<string> BranchOffices { get; set; } = new();

        // JP: オーダー票種類の選択肢
        // VI: Danh sách loại phiếu đặt hàng để chọn
        // EN: List of selectable order slip types
        public List<string> OrderSlipTypes { get; set; } = new();

        // JP: 検索ボタン
        // VI: Nút tìm kiếm
        // EN: Search button command
        public RelayCommand SearchCommand { get; set; }

        // JP: 全チェックON（全行のチェックボックスをON）
        // VI: Nút chọn tất cả (bật hết checkbox các dòng)
        // EN: Check-all-on button (turns every row checkbox on)
        public RelayCommand CheckAllOnCommand { get; set; }

        // JP: 全チェックOFF（全行のチェックボックスをOFF）
        // VI: Nút bỏ chọn tất cả (tắt hết checkbox các dòng)
        // EN: Check-all-off button (turns every row checkbox off)
        public RelayCommand CheckAllOffCommand { get; set; }

        // JP: 一覧印刷
        // VI: In danh sách
        // EN: Print list
        public RelayCommand PrintListCommand { get; set; }

        // JP: オーダー票（発行）
        // VI: Xuất phiếu đặt hàng
        // EN: Issue order slip
        public RelayCommand OrderSlipCommand { get; set; }

        // JP: CSV出力
        // VI: Xuất file CSV
        // EN: Export CSV
        public RelayCommand CsvCommand { get; set; }

        // JP: 品種計（品種ごとの集計）
        // VI: Thống kê theo chủng loại
        // EN: Item-type totals
        public RelayCommand ItemCountCommand { get; set; }

        // JP: コピー受注(明細有)
        // VI: Sao chép đơn hàng (có chi tiết)
        // EN: Copy order (with detail)
        public RelayCommand CopyOrderWithDetailCommand { get; set; }

        // JP: コピー受注(明細無)
        // VI: Sao chép đơn hàng (không chi tiết)
        // EN: Copy order (without detail)
        public RelayCommand CopyOrderWithoutDetailCommand { get; set; }

        // JP: 照会
        // VI: Tra cứu / xem chi tiết
        // EN: Inquiry / view detail
        public RelayCommand InquiryCommand { get; set; }

        // JP: 修正
        // VI: Sửa
        // EN: Modify
        public RelayCommand ModifyCommand { get; set; }

        // JP: 削除
        // VI: Xóa
        // EN: Delete
        public RelayCommand DeleteCommand { get; set; }

        // JP: 戻る（画面を閉じる）
        // VI: Quay lại (đóng màn hình)
        // EN: Back (close the window)
        public RelayCommand BackCommand { get; set; }
    }
}
