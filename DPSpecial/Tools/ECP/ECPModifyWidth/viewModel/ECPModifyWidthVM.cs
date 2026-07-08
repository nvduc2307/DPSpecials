using DPSpecial.Tools.ECP.ECPModifyWidth.view;

namespace DPSpecial.Tools.ECP.ECPModifyWidth.viewModel
{
    public partial class ECPModifyWidthVM : ObservableObject
    {
        private double _wCPWidth;
        [ObservableProperty]
        private double _wCPWidthMax;
        public double WCPWidth
        {
            get => _wCPWidth;
            set
            {
                _wCPWidth = value;
                OnPropertyChanged();
                WCPWidthAction?.Invoke();
            }
        }
        public Action WCPWidthAction { get; set; }
        public RelayCommand<ECPModifyWidthView> OkCommand { get; set; }
    }
}
