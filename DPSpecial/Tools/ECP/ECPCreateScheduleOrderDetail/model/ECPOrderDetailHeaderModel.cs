using System.ComponentModel;
using Newtonsoft.Json;

namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model
{
    // 販売店受注修正 header data (everything above the detail grid and in the footer totals).
    public class ECPOrderDetailHeaderModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Each yes/no radio pair shares one bool; both radios bind two-way so either can be clicked.
        private void SetPair(ref bool field, bool value, string on, string off)
        {
            if (field == value) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(on));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(off));
        }

        // 物件登録No / 受注No / 営業担当者 / 進捗状況
        public string PropertyRegNo { get; set; } = string.Empty;
        public string OrderNo { get; set; } = string.Empty;
        public string SalesStaffCode { get; set; } = string.Empty;
        public string SalesStaffName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // お客様情報
        public string DealerCode { get; set; } = string.Empty;
        public string DealerName { get; set; } = string.Empty;
        public string DealerBranch { get; set; } = string.Empty;
        public string DealerContact { get; set; } = string.Empty;

        // 物件情報
        public string PropertyName { get; set; } = string.Empty;
        public string PropertyDetail { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string PrefectureCode { get; set; } = string.Empty;
        public string PrefectureName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string DesignCode { get; set; } = string.Empty;
        public string DesignName { get; set; } = string.Empty;
        public string ContractorCode { get; set; } = string.Empty;
        public string ContractorName { get; set; } = string.Empty;
        public string ConstructionShopCode { get; set; } = string.Empty;
        public string ConstructionShopName { get; set; } = string.Empty;
        public string Consignee { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;

        // 備考
        public string CustomerNote { get; set; } = string.Empty;

        // 基材情報
        public string UseCode { get; set; } = string.Empty;
        public string UseName { get; set; } = string.Empty;
        public string FactoryCode { get; set; } = string.Empty;
        public string FactoryName { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string FaceCode { get; set; } = string.Empty;
        public string FaceName { get; set; } = string.Empty;
        private bool _gasketOn;
        public bool GasketOn { get => _gasketOn; set => SetPair(ref _gasketOn, value, nameof(GasketOn), nameof(GasketOff)); }
        [JsonIgnore] public bool GasketOff { get => !_gasketOn; set => GasketOn = !value; }
        private bool _vinylOn;
        public bool VinylOn { get => _vinylOn; set => SetPair(ref _vinylOn, value, nameof(VinylOn), nameof(VinylOff)); }
        [JsonIgnore] public bool VinylOff { get => !_vinylOn; set => VinylOn = !value; }
        public string HardwareCode { get; set; } = string.Empty;
        public string HardwareName { get; set; } = string.Empty;

        // 仕上げ情報
        public string SpecCode { get; set; } = string.Empty;
        public string SpecName { get; set; } = string.Empty;
        public string PaintTypeCode { get; set; } = string.Empty;
        public string PaintTypeName { get; set; } = string.Empty;
        public string ColorNo { get; set; } = string.Empty;
        public string GlossCode { get; set; } = string.Empty;
        public string GlossName { get; set; } = string.Empty;
        public string SampleNo { get; set; } = string.Empty;

        // 希望納期
        public string DesiredDate { get; set; } = string.Empty;

        // Order slip PDF only: 営業担当 支店 / 現場到着予定日 / 工場出荷予定日 / 加工開始予定日 / 二次加工出荷・到着予定日 / フロア
        public string BranchOffice { get; set; } = string.Empty;
        public string SiteArrivalDate { get; set; } = string.Empty;
        public string FactoryShipDate { get; set; } = string.Empty;
        public string ProcessStartDate { get; set; } = string.Empty;
        public string SecondaryShipDate { get; set; } = string.Empty;
        public string SecondaryArrivalDate { get; set; } = string.Empty;
        public string Floor { get; set; } = string.Empty;

        // 配車情報
        public string DeliveryMethodCode { get; set; } = string.Empty;
        public string DeliveryMethodName { get; set; } = string.Empty;
        public string VehicleCode { get; set; } = string.Empty;
        public string VehicleName { get; set; } = string.Empty;
        public string VehicleTypeCode { get; set; } = string.Empty;
        public string VehicleTypeName { get; set; } = string.Empty;
        public string LoadWeight { get; set; } = string.Empty;
        public string LoadHeight { get; set; } = string.Empty;
        private bool _mapOn;
        public bool MapOn { get => _mapOn; set => SetPair(ref _mapOn, value, nameof(MapOn), nameof(MapOff)); }
        [JsonIgnore] public bool MapOff { get => !_mapOn; set => MapOn = !value; }

        // 断面図イメージ
        public string ConstructionDrawingNo { get; set; } = string.Empty;

        // Footer totals: 実枚 / 換枚 / 基材面積 / 基材重量 / 働き面積 / 働き重量 / 長さ
        public string ActualCount { get; set; } = string.Empty;
        public string ExchangeCount { get; set; } = string.Empty;
        public string BaseArea { get; set; } = string.Empty;
        public string BaseWeight { get; set; } = string.Empty;
        public string WorkArea { get; set; } = string.Empty;
        public string WorkWeight { get; set; } = string.Empty;
        public string TotalLength { get; set; } = string.Empty;
    }
}
