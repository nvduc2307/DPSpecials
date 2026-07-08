using Autodesk.Revit.UI;
using DPSpecial.MVVM.Models;
using DPSpecial.Tools.ECP.ECPCreate.action;
using DPSpecial.Tools.ECP.ECPModifyWidth.view;
using DPSpecial.Tools.ECP.ECPModifyWidth.viewModel;
using DPSpecial.Utils;
using DPSpecial.Utils.ExternalEvent;

namespace DPSpecial.Tools.ECP.ECPModifyWidth.action
{
    public partial class ECPModifyWidthAction
    {
        private void _externalCommandSetWidthWallInvoke()
        {
            SetECPWith(_wallTarget, _viewModel.WCPWidth, _direction);
            //remove old dim
            ECPDimensionAction.Remove(_wallPick);
            //create dim
            var walls = GetGroupWall(_wallPick, _wallPick.GetTransform().BasisX);
            if (!walls.Any()) return;
            var dim = ECPDimensionAction.Create(walls);
            
            if (_wallTarget == null) return;
            if (_wallPick == null) return;
            _uidocument.Selection.SetElementIds(new List<ElementId>() { _wallTarget.Id });
            _viewModel.WCPWidthMax = GetECPWithMax(_wallTarget);
            if (_viewModel.WCPWidthMax == 0) return;
            _viewModel.WCPWidth = GetECPWith(_wallTarget);
        }
        private void _OkCommand(ECPModifyWidthView view)
        {
            if (view == null) return;
            if (_wallTarget == null) view.Close();
            _externalEventSetWidthWall.Raise();
        }
        private bool _ECPSelectFilter(Element element)
        {
            if (!(element is FamilyInstance fa)) return false;
            if (!fa.Symbol.FamilyName.ToUpper().Contains("ECP")) return false;
            return true;
        }
        private void _WCPWidthAction()
        {
            if (_viewModel.WCPWidth <= 0 || _viewModel.WCPWidth > _viewModel.WCPWidthMax)
                _viewModel.WCPWidth = _viewModel.WCPWidthMax;
        }
    }
}
