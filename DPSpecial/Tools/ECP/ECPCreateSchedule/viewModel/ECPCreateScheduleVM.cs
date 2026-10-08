using System.Collections.ObjectModel;
using DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.model;

namespace DPSpecial.Tools.ECP.ECPSchedule.ECPCreateSchedule.viewModel
{
    public partial class ECPCreateScheduleVM : ObservableObject
    {
        [ObservableProperty]
        private ECPOrderScheduleHeaderModel _header = new();

        public ObservableCollection<ECPOrderScheduleModel> Orders { get; set; } = new();

        [ObservableProperty]
        private ECPOrderScheduleModel _selectedOrder;

        partial void OnSelectedOrderChanged(ECPOrderScheduleModel value) => ModifyCommand?.NotifyCanExecuteChanged();

        private bool? _isAllChecked = false;
        public bool? IsAllChecked
        {
            get => _isAllChecked;
            set
            {
                _isAllChecked = value;
                OnPropertyChanged();
                IsAllCheckedAction?.Invoke();
            }
        }
        public Action IsAllCheckedAction { get; set; }

        public void SetIsAllCheckedFromRows(bool? value)
        {
            _isAllChecked = value;
            OnPropertyChanged(nameof(IsAllChecked));
        }

        public List<string> BranchOffices { get; set; } = new();

        public List<string> OrderSlipTypes { get; set; } = new();

        public RelayCommand SearchCommand { get; set; }

        public RelayCommand CheckAllOnCommand { get; set; }

        public RelayCommand CheckAllOffCommand { get; set; }

        public RelayCommand PrintListCommand { get; set; }

        public RelayCommand OrderSlipCommand { get; set; }

        public RelayCommand CsvCommand { get; set; }

        public RelayCommand ItemCountCommand { get; set; }

        public RelayCommand CopyOrderWithDetailCommand { get; set; }

        public RelayCommand CopyOrderWithoutDetailCommand { get; set; }

        public RelayCommand InquiryCommand { get; set; }

        public RelayCommand ModifyCommand { get; set; }

        public RelayCommand DeleteCommand { get; set; }

        public RelayCommand SaveCommand { get; set; }

        public RelayCommand BackCommand { get; set; }
    }
}
