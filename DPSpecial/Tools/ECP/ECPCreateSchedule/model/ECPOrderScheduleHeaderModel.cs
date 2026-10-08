namespace DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.model
{
    public partial class ECPOrderScheduleHeaderModel : ObservableObject
    {
        [ObservableProperty]
        private string _propertyName = string.Empty;

        [ObservableProperty]
        private string _propertyDetail = string.Empty;

        [ObservableProperty]
        private string _dealerName = string.Empty;

        [ObservableProperty]
        private string _dealerDivision = string.Empty;

        [ObservableProperty]
        private string _branchOffice = string.Empty;

        [ObservableProperty]
        private string _branchOfficeDetail = string.Empty;

        [ObservableProperty]
        private string _orderSlipType = string.Empty;
    }
}
