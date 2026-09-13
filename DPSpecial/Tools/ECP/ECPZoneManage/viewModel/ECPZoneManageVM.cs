using System.Collections.ObjectModel;
using DPSpecial.Tools.ECP.ECPZoneManage.model;

namespace DPSpecial.Tools.ECP.ECPZoneManage.viewModel
{
    public partial class ECPZoneManageVM : ObservableObject
    {
        public ObservableCollection<ECPZoneModel> Zones { get; set; } = new();

        // Set from the view's Loaded event so DeleteZone can read the currently selected row.
        public System.Windows.Controls.DataGrid ZoneDataGrid { get; set; }

        public RelayCommand<ECPZoneModel> PickColorCommand { get; set; }
        public RelayCommand CreateZoneCommand { get; set; }
        public RelayCommand DeleteZoneCommand { get; set; }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
    }
}
