using View = Autodesk.Revit.DB.View;

namespace DPSpecial.Tools.ECP.ManageZoneViews.viewModel
{
    public partial class ManageZoneViewVM : ObservableObject
    {
        public List<View> Views { get; set; } = new();

        [ObservableProperty]
        private View _selectedView;

        public RelayCommand ShowViewCommand { get; set; }
        public RelayCommand BackCommand { get; set; }
    }
}
