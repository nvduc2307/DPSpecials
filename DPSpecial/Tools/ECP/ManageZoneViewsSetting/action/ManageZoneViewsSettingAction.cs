using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ManageZoneViewsSetting.view;
using DPSpecial.Tools.ECP.ManageZoneViewsSetting.viewModel;
using DPSpecial.Utils;
using View = Autodesk.Revit.DB.View;

namespace DPSpecial.Tools.ECP.ManageZoneViewsSetting.action
{
    public class ManageZoneViewsSettingAction
    {
        public static string _nameViewSettingZone = "_settingZone";
        private readonly UIDocument _uidocument;
        private readonly Document _document;
        private readonly ManageZoneViewsSettingVM _viewModel;
        private readonly ManageZoneViewsSettingView _view;

        public ManageZoneViewsSettingAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;

            _viewModel = new ManageZoneViewsSettingVM
            {
                Views = GetViewElevationsSettingZone(),
                ShowViewCommand = new RelayCommand(_ShowView),
                BackCommand = new RelayCommand(_Back),
            };
            _view = new ManageZoneViewsSettingView { DataContext = _viewModel };
        }

        public void Execute()
        {
            _view.ShowDialog();
        }

        private void _ShowView()
        {
            var view = _viewModel.SelectedView;
            _view.Close();
            if (view != null)
                ShowView(_viewModel.Views, view);
        }

        private void _Back()
        {
            _view.Close();
        }

        public void ShowView(List<View> views, View view)
        {
            View viewTarget = null;
            if (!view.Name.Contains(_nameViewSettingZone))
            {
                var nameView = $"{view.Name}{_nameViewSettingZone}";
                viewTarget = views.FirstOrDefault(x => x.Name == nameView);
                if (viewTarget == null)
                {
                    using (var ts = new Transaction(_document, "new transaction"))
                    {
                        ts.SkipAllWarnings();
                        ts.Start();
                        var idView = view.Duplicate(ViewDuplicateOption.WithDetailing);
                        viewTarget = _document.GetElement(idView) as View;
                        viewTarget.Name = nameView;
                        viewTarget.ViewTemplateId = new ElementId(-1);
                        _document.Regenerate();
                        viewTarget.DetailLevel = ViewDetailLevel.Fine;
                        _document.Regenerate();
                        ts.Commit();
                    }
                }
            }
            else
                viewTarget = view;
            _uidocument.RequestViewChange(viewTarget);
            _uidocument.ActiveView = viewTarget;
        }

        public List<View> GetViewElevationsSettingZone()
        {
            var views = new FilteredElementCollector(_document)
                .WhereElementIsNotElementType()
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(x => !x.IsTemplate)
                .Where(x => x.ViewType == ViewType.Elevation)
                .Where(x => x.Name.Contains(_nameViewSettingZone))
                .OrderBy(x => x.Name)
                .ToList();
            return views;
        }
    }
}
