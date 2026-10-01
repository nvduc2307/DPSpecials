using View = Autodesk.Revit.DB.View;

namespace DPSpecial.Tools.ECP.ManageZoneViewsSetting.viewModel
{
    public partial class ManageZoneViewsSettingVM : ObservableObject
    {
        public List<View> Views { get; set; } = new();

        [ObservableProperty]
        private View _selectedView;

        public RelayCommand ShowViewCommand { get; set; }
        public RelayCommand BackCommand { get; set; }
    }
}
