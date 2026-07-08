namespace DPSpecial.MVVM.ViewModel
{
    public partial class ProgressVM : ObservableObject
    {
        [ObservableProperty]
        private int _percenComplete;
        public Action<int> UpdateProgressAction { get; set; }
        public void Updateprogress(int value)
        {
            UpdateProgressAction?.Invoke(value);
        }
    }
}
