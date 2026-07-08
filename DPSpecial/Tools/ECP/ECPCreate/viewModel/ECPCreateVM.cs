using DPSpecial.Tools.ECP.ECPCreate.model;

namespace DPSpecial.Tools.ECP.ECPCreate.viewModel
{
    public partial class ECPCreateVM : ObservableObject
    {
        private double _eCPGap;
        private double _eCPWidth;
        private ECPFamilyModel _eCPFamily;
        [ObservableProperty]
        private double _eCPThickness;
        [ObservableProperty]
        private double _eCPWidthMax;
        public ECPFamilyModel ECPFamily
        {
            get => _eCPFamily;
            set
            {
                _eCPFamily = value;
                OnPropertyChanged();
                ECPFamilyAction?.Invoke();
            }
        }
        public Action ECPFamilyAction { get; set; }
        public List<ECPFamilyModel> ECPFamilies { get; set; }
        public double ECPGap
        {
            get => _eCPGap;
            set
            {
                _eCPGap = value;
                OnPropertyChanged();
                ECPGapAction?.Invoke();
            }
        }
        public Action ECPGapAction { get; set; }
        public double ECPWidth
        {
            get => _eCPWidth;
            set
            {
                _eCPWidth = value;
                OnPropertyChanged();
                ECPWidthAction?.Invoke();
            }
        }
        public Action ECPWidthAction { get ; set; }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
    }
}
