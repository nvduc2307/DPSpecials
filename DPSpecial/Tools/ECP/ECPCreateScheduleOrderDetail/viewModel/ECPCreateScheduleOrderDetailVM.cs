using System.Collections.ObjectModel;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model;

namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.viewModel
{
    public partial class ECPCreateScheduleOrderDetailVM : ObservableObject
    {
        public ECPOrderDetailHeaderModel Header { get; set; } = new();
        public ObservableCollection<ECPOrderDetailRowModel> Rows { get; set; } = new();

        public RelayCommand AttachCommand { get; set; }
        public RelayCommand SaveCommand { get; set; }
        public RelayCommand ExportSvgCommand { get; set; }
        public RelayCommand ConfirmPrintCommand { get; set; }
        public RelayCommand UpdateInfoCommand { get; set; }
        public RelayCommand ConfirmCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
    }
}
