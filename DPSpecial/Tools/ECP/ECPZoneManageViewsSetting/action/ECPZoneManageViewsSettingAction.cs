using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPZoneManageViewsSetting.view;
using DPSpecial.Tools.ECP.ECPZoneManageViewsSetting.viewModel;
using DPSpecial.Utils;
using View = Autodesk.Revit.DB.View;

namespace DPSpecial.Tools.ECP.ECPZoneManageViewsSetting.action
{
    public class ECPZoneManageViewsSettingAction
    {
        public static string _nameViewSettingZone = "_settingZone";
        private readonly UIDocument _uidocument;
        private readonly Document _document;
        private readonly ECPZoneManageViewsSettingVM _viewModel;
        private readonly ECPZoneManageViewsSettingView _view;

        public ECPZoneManageViewsSettingAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;

            _viewModel = new ECPZoneManageViewsSettingVM
            {
                Views = GetViewElevationsSettingZone(),
                ShowViewCommand = new RelayCommand(_ShowView),
                BackCommand = new RelayCommand(_Back),
            };
            _view = new ECPZoneManageViewsSettingView { DataContext = _viewModel };
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
