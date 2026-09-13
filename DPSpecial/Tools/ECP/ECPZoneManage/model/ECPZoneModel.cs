namespace DPSpecial.Tools.ECP.ECPZoneManage.model
{
    public partial class ECPZoneModel : ObservableObject
    {
        public int Id { get; set; }

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

        // Invoked whenever Name changes so the caller can validate uniqueness against other zones.
        public Action<ECPZoneModel> ChangeNameAction { get; set; }
    }

    // Plain DTO persisted to the Revit document (Extensible Storage) as JSON.
    public class ECPZoneSaveModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }
}
