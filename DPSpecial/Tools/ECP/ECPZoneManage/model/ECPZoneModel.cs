namespace DPSpecial.Tools.ECP.ECPZoneManage.model
{
    public partial class ECPZoneModel : ObservableObject
    {
        public int Id { get; set; }

        [ObservableProperty]
        private string _orderNo = string.Empty;

        [ObservableProperty]
        private string _propertyRegNo = string.Empty;

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
                ChangeNameAction?.Invoke(this);
            }
        }

        [ObservableProperty]
        private string _color = "#6DC3BB";

        public Action<ECPZoneModel> ChangeNameAction { get; set; }
    }

    public class ECPZoneSaveModel
    {
        public int Id { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public string PropertyRegNo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }
}
