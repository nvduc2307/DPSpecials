using View = Autodesk.Revit.DB.View;

namespace DPSpecial.Tools.ECP.ECPZoneManageViewsSetting.viewModel
{
    public partial class ECPZoneManageViewsSettingVM : ObservableObject
    {
        public List<View> Views { get; set; } = new();

        [ObservableProperty]
        private View _selectedView;

        public RelayCommand ShowViewCommand { get; set; }
        public RelayCommand BackCommand { get; set; }
    }
}
