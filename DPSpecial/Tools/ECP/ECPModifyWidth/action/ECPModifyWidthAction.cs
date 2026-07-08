using Autodesk.Revit.UI;
using DPSpecial.MVVM.Models;
using DPSpecial.Tools.ECP.ECPModifyWidth.view;
using DPSpecial.Tools.ECP.ECPModifyWidth.viewModel;
using DPSpecial.Utils;
using DPSpecial.Utils.ExternalEvent;

namespace DPSpecial.Tools.ECP.ECPModifyWidth.action
{
    public partial class ECPModifyWidthAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private ECPModifyWidthView _view;
        private ECPModifyWidthVM _viewModel;
        private FamilyInstance _wallTarget;
        private FamilyInstance _wallPick;
        private XYZ _direction;
        private CustomExternalCommand _externalCommandSetWidthWall;
        private ExternalEvent _externalEventSetWidthWall;

        public ECPModifyWidthAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _externalCommandSetWidthWall = new CustomExternalCommand("externalCommandSetWidthWall");
            _externalCommandSetWidthWall.Action = _externalCommandSetWidthWallInvoke;
            _externalEventSetWidthWall = ExternalEvent.Create(_externalCommandSetWidthWall);
            _viewModel = new ECPModifyWidthVM()
            {
                OkCommand = new RelayCommand<ECPModifyWidthView>(_OkCommand)
            };
            _view = new ECPModifyWidthView() { DataContext = _viewModel };
        }
        public void Execute()
        {
            var w = _uidocument.Selection.PickElement(_document, null, _ECPSelectFilter) as FamilyInstance;
            if (w == null) throw new Exception();
            var transf = w.GetTransform();
            var fAlong = Plane.CreateByNormalAndOrigin(transf.BasisY, transf.Origin);
            var p = _uidocument.Selection.PickPoint("Point").RayIntersectPlane(fAlong.Normal, fAlong);
            _direction = p.DistanceTo(transf.Origin).ToMillimeters() < 5 ? transf.BasisX : (p - transf.Origin).Normalize();
            _viewModel.WCPWidthMax = GetECPWithMax(w);
            if (_viewModel.WCPWidthMax == 0) return;
            _viewModel.WCPWidth = GetECPWith(w);
            _viewModel.WCPWidthAction = _WCPWidthAction;
            _wallTarget = w;
            _wallPick = w;
            _uidocument.Selection.SetElementIds(new List<ElementId>() { _wallTarget.Id });
            _view.Show();
        }
        
    }
}
