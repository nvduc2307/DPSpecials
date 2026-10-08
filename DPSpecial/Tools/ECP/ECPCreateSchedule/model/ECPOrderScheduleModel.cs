namespace DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.model
{
    public partial class ECPOrderScheduleModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isChecked;

        public int ZoneId { get; set; }

        public string OrderNo { get; set; } = string.Empty;

        public string PropertyRegNo { get; set; } = string.Empty;

        [ObservableProperty]
        private string _propertyName = string.Empty;

        public string PropertyDetail { get; set; } = string.Empty;

        [ObservableProperty]
        private string _dealerName = string.Empty;

        [ObservableProperty]
        private string _dealerDivision = string.Empty;

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
