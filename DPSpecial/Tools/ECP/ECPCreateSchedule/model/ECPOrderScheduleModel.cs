namespace DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.model
{
    // JP: 検索結果一覧の1行分のデータ
    // VI: Dữ liệu của một dòng trong danh sách kết quả tìm kiếm
    // EN: Data for a single row in the search result list
    public partial class ECPOrderScheduleModel : ObservableObject
    {
        // JP: 印刷対象チェック（列見出し「オーダー票印刷」）
        // VI: Checkbox chọn dòng để in (cột "オーダー票印刷" - In phiếu đặt hàng)
        // EN: Row checkbox for printing (column "オーダー票印刷" - print order slip)
        [ObservableProperty]
        private bool _isChecked;

        // JP: 受注No
        // VI: Số đơn hàng
        // EN: Order No.
        public string OrderNo { get; set; } = string.Empty;

        // JP: 物件登録No
        // VI: Số đăng ký công trình
        // EN: Property registration No.
        public string PropertyRegNo { get; set; } = string.Empty;

        // JP: 物件名称
        // VI: Tên công trình
        // EN: Property/site name
        [ObservableProperty]
        private string _propertyName = string.Empty;

        // JP: 物件詳細
        // VI: Chi tiết công trình
        // EN: Property/site detail
        public string PropertyDetail { get; set; } = string.Empty;

        // JP: 販売店名称
        // VI: Tên đại lý bán hàng
        // EN: Dealer name
        [ObservableProperty]
        private string _dealerName = string.Empty;

        // JP: 販売店名称の2行目（部署名）- 検索欄のみで使用、グリッドには非表示の想定
        // VI: Dòng 2 của tên đại lý (bộ phận) - chỉ dùng ở ô tìm kiếm, không hiển thị trong lưới
        // EN: Second line of dealer name (division) - used in the search field only, not shown in the grid
        [ObservableProperty]
        private string _dealerDivision = string.Empty;

        // JP: 基材m²
        // VI: Diện tích vật liệu nền (m²)
        // EN: Base material area (m²)
        public string BaseAreaM2 { get; set; } = string.Empty;

        // JP: 基材重量
        // VI: Khối lượng vật liệu nền
        // EN: Base material weight
        public string BaseWeight { get; set; } = string.Empty;

        // JP: 働きm²
        // VI: Diện tích thi công/hữu ích (m²)
        // EN: Working area (m²)
        public string WorkAreaM2 { get; set; } = string.Empty;

        // JP: 働き重量
        // VI: Khối lượng thi công/hữu ích
        // EN: Working weight
        public string WorkWeight { get; set; } = string.Empty;

        // JP: 役物m²
        // VI: Diện tích phụ kiện/vật tư đặc biệt (m²)
        // EN: Accessory/special part area (m²)
        public string AccessoryAreaM2 { get; set; } = string.Empty;

        // JP: 実枚（実際の枚数）
        // VI: Số tấm thực tế
        // EN: Actual sheet count
        public string ActualSheets { get; set; } = string.Empty;

        // JP: 希望納期
        // VI: Ngày giao hàng mong muốn
        // EN: Desired delivery date
        public string DesiredDeliveryDate { get; set; } = string.Empty;

        // JP: 現場到着予定日
        // VI: Ngày dự kiến hàng đến công trường
        // EN: Scheduled site arrival date
        public string SiteArrivalDate { get; set; } = string.Empty;

        // JP: 加工出荷予定日 (列: 加工出荷予定日 / 加工到着予定日 の1行目)
        // VI: Ngày dự kiến xuất hàng gia công (dòng 1 của cột "加工出荷予定日 / 加工到着予定日")
        // EN: Scheduled processing shipment date (line 1 of the "加工出荷予定日 / 加工到着予定日" column)
        public string ProcessShipScheduledDate { get; set; } = string.Empty;

        // JP: 加工到着予定日 (同上の列の2行目)
        // VI: Ngày dự kiến hàng đến để gia công (dòng 2 của cột trên)
        // EN: Scheduled processing arrival date (line 2 of the column above)
        public string ProcessArrivalScheduledDate { get; set; } = string.Empty;

        // JP: 工場出荷予定日 (列: 工場出荷予定日 / 変更期日(赤) の1行目、黒字)
        // VI: Ngày dự kiến xuất xưởng (dòng 1 của cột "工場出荷予定日 / 変更期日(đỏ)", chữ đen)
        // EN: Scheduled factory shipment date (line 1 of the "工場出荷予定日 / 変更期日(red)" column, black text)
        public string FactoryShipScheduledDate { get; set; } = string.Empty;

        // JP: 変更不可（赤字表示） - 上記列の2行目データ。ヘッダー2行目「変更期日」(赤)に対応
        // VI: Không thể thay đổi (chữ đỏ) - dữ liệu dòng 2 của cột trên, ứng với header dòng 2 "変更期日" (đỏ)
        // EN: "Cannot be changed" note (red text) - line-2 data for the column above, matching its red line-2 header "変更期日"
        public string ChangeNotAllowed { get; set; } = string.Empty;

        // JP: 加工出荷実績日 (列: 加工出荷実績日 / 工場出荷実績日 の1行目)
        // VI: Ngày xuất hàng gia công thực tế (dòng 1 của cột "加工出荷実績日 / 工場出荷実績日")
        // EN: Actual processing shipment date (line 1 of the "加工出荷実績日 / 工場出荷実績日" column)
        public string ProcessShipActualDate { get; set; } = string.Empty;

        // JP: 工場出荷実績日 (同上の列の2行目)
        // VI: Ngày xuất xưởng thực tế (dòng 2 của cột trên)
        // EN: Actual factory shipment date (line 2 of the column above)
        public string FactoryShipActualDate { get; set; } = string.Empty;

        // JP: 最終出荷日
        // VI: Ngày xuất hàng cuối cùng
        // EN: Final shipment date
        public string FinalShipDate { get; set; } = string.Empty;

        // JP: 一時保管
        // VI: Lưu kho tạm thời
        // EN: Temporary storage
        public string TemporaryStorage { get; set; } = string.Empty;

        // JP: 納変数 (列: 納変数 / 内変数 の1行目)
        // VI: Số lần thay đổi giao hàng (dòng 1 của cột "納変数 / 内変数")
        // EN: Delivery change count (line 1 of the "納変数 / 内変数" column)
        public string DeliveryChangeCount { get; set; } = string.Empty;

        // JP: 内変数（内部変更数） (同上の列の2行目)
        // VI: Số lần thay đổi nội bộ (dòng 2 của cột trên)
        // EN: Internal change count (line 2 of the column above)
        public string InternalDeform { get; set; } = string.Empty;

        // JP: 製造工場 (列: 製造工場 / 加工工場 の1行目)
        // VI: Nhà máy sản xuất (dòng 1 của cột "製造工場 / 加工工場")
        // EN: Manufacturing factory (line 1 of the "製造工場 / 加工工場" column)
        public string ManufacturingFactory { get; set; } = string.Empty;

        // JP: 加工工場 (同上の列の2行目)
        // VI: Nhà máy gia công (dòng 2 của cột trên)
        // EN: Processing factory (line 2 of the column above)
        public string ProcessingFactory { get; set; } = string.Empty;

        // JP: 営業担当者
        // VI: Nhân viên kinh doanh phụ trách
        // EN: Sales representative
        public string SalesRep { get; set; } = string.Empty;

        // JP: 進捗状況
        // VI: Tình trạng tiến độ
        // EN: Progress status
        public string ProgressStatus { get; set; } = string.Empty;
    }
}
