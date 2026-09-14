namespace DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.model
{
    // JP: 検索欄（ヘッダー部）のデータモデル
    // VI: Model dữ liệu cho phần header / ô tìm kiếm
    // EN: Data model for the search header section
    public partial class ECPOrderScheduleHeaderModel : ObservableObject
    {
        // JP: 物件名称
        // VI: Tên công trình
        // EN: Property/site name
        [ObservableProperty]
        private string _propertyName = string.Empty;

        // JP: 物件詳細
        // VI: Chi tiết công trình
        // EN: Property/site detail
        [ObservableProperty]
        private string _propertyDetail = string.Empty;

        // JP: 販売店名称
        // VI: Tên đại lý bán hàng
        // EN: Dealer name
        [ObservableProperty]
        private string _dealerName = string.Empty;

        // JP: 販売店名称の2行目（部署名）
        // VI: Dòng 2 của tên đại lý (tên bộ phận/phòng ban)
        // EN: Second line of dealer name (division/department name)
        [ObservableProperty]
        private string _dealerDivision = string.Empty;

        // JP: 担当支店（1行目）
        // VI: Chi nhánh phụ trách (dòng 1)
        // EN: Branch office in charge (line 1)
        [ObservableProperty]
        private string _branchOffice = string.Empty;

        // JP: 担当支店（2行目）
        // VI: Chi nhánh phụ trách (dòng 2)
        // EN: Branch office in charge (line 2)
        [ObservableProperty]
        private string _branchOfficeDetail = string.Empty;

        // JP: オーダー票種類
        // VI: Loại phiếu đặt hàng
        // EN: Order slip type
        [ObservableProperty]
        private string _orderSlipType = string.Empty;
    }
}
