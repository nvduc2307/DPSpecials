namespace DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.model
{
    // JP: ProjectInformation に保存するための DTO（ヘッダー＋明細行をまとめたルートオブジェクト）
    // VI: DTO gốc chứa Header + danh sách Orders để lưu vào ProjectInformation
    // EN: Root DTO that bundles Header + Orders list for persistence to ProjectInformation
    public class ECPOrderScheduleSaveModel
    {
        public ECPOrderScheduleHeaderSaveModel Header { get; set; } = new();
        public List<ECPOrderScheduleOrderSaveModel> Orders { get; set; } = new();
    }

    // JP: ヘッダー部の保存用 DTO（ObservableProperty を持たないプレーンオブジェクト）
    // VI: DTO lưu phần header (plain object, không có ObservableProperty)
    // EN: Plain DTO for the header section (no ObservableProperty)
    public class ECPOrderScheduleHeaderSaveModel
    {
        public string PropertyName { get; set; } = string.Empty;
        public string PropertyDetail { get; set; } = string.Empty;
        public string DealerName { get; set; } = string.Empty;
        public string DealerDivision { get; set; } = string.Empty;
        public string BranchOffice { get; set; } = string.Empty;
        public string BranchOfficeDetail { get; set; } = string.Empty;
        public string OrderSlipType { get; set; } = string.Empty;
    }

    // JP: 明細行1行分の保存用 DTO（ObservableProperty を持たないプレーンオブジェクト）
    // VI: DTO lưu 1 dòng chi tiết (plain object, không có ObservableProperty)
    // EN: Plain DTO for a single order detail row (no ObservableProperty)
    public class ECPOrderScheduleOrderSaveModel
    {
        public bool IsChecked { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public string PropertyRegNo { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string PropertyDetail { get; set; } = string.Empty;
        public string DealerName { get; set; } = string.Empty;
        public string DealerDivision { get; set; } = string.Empty;
        public string BaseAreaM2 { get; set; } = string.Empty;
        public string BaseWeight { get; set; } = string.Empty;
        public string WorkAreaM2 { get; set; } = string.Empty;
        public string WorkWeight { get; set; } = string.Empty;
        public string AccessoryAreaM2 { get; set; } = string.Empty;
        public string ActualSheets { get; set; } = string.Empty;
        public string DesiredDeliveryDate { get; set; } = string.Empty;
        public string SiteArrivalDate { get; set; } = string.Empty;
        public string ProcessShipScheduledDate { get; set; } = string.Empty;
        public string ProcessArrivalScheduledDate { get; set; } = string.Empty;
        public string FactoryShipScheduledDate { get; set; } = string.Empty;
        public string ChangeNotAllowed { get; set; } = string.Empty;
        public string ProcessShipActualDate { get; set; } = string.Empty;
        public string FactoryShipActualDate { get; set; } = string.Empty;
        public string FinalShipDate { get; set; } = string.Empty;
        public string TemporaryStorage { get; set; } = string.Empty;
        public string DeliveryChangeCount { get; set; } = string.Empty;
        public string InternalDeform { get; set; } = string.Empty;
        public string ManufacturingFactory { get; set; } = string.Empty;
        public string ProcessingFactory { get; set; } = string.Empty;
        public string SalesRep { get; set; } = string.Empty;
        public string ProgressStatus { get; set; } = string.Empty;
    }
}
