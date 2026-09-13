using DPSpecial.Tools.ECP.ECPZoneManage.model;

namespace DPSpecial.Tools.ECP.ECPZoneInstall.viewModel
{
    public partial class ECPZoneInstallVM : ObservableObject
    {
        // Zones are defined/edited via ECPZoneManageCmd; this screen only lets the user pick one to apply.
        public List<ECPZoneSaveModel> Zones { get; set; } = new();

        [ObservableProperty]
        private ECPZoneSaveModel _zone;

        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
    }
}
