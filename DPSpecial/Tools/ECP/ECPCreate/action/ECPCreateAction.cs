using Autodesk.Revit.UI;
using DPSpecial.MVVM.Models;
using DPSpecial.Tools.ECP.ECPCreate.view;
using DPSpecial.Tools.ECP.ECPCreate.viewModel;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPCreate.action
{
    public partial class ECPCreateAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private ECPCreateVM _viewModel;
        private ECPCreateView _view;
        private CurveVisualizationServer _curveShost;
        private List<XYZ> _listP;
        public ECPCreateAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _viewModel = new ECPCreateVM()
            {
                ECPFamilies = GetECPFamilies(),
                ECPWidthMax = 0,
                ECPWidth = 0,
                ECPThickness = 0,
                ECPGap = 10,
                ECPFamilyAction = _ECPFamilyAction,
                ECPWidthAction = _ECPWidthAction,
                ECPGapAction = _ECPGapAction,
                OkCommand = new RelayCommand(_OkCommand),
                CancelCommand = new RelayCommand(_CancelCommand)
            };
            _viewModel.ECPFamily = _viewModel.ECPFamilies.FirstOrDefault();
            _view = new ECPCreateView() { DataContext = _viewModel};
        }
        public void Execute()
        {
            CheckView();
            CheckParameter();
            _view.ShowDialog();
        }
        private void SelectCurveWall(out XYZ sp, out XYZ ep)
        {
            sp = null;
            ep = null;
            var isDo = true;
            var limit = 100;
            var eventMouse = new EventMouseHook();
            eventMouse.OnMouseMove += Em_OnMouseMove;
            eventMouse.Start(MouseButtons.None);
            _curveShost = null;
            _listP = new List<XYZ>();
            do
            {
                try
                {
                    var p = _uidocument.Selection.PickPoint($"P{_listP.Count + 1}");
                    _listP.Add(p);
                    if (_listP.Count == 2) throw new Exception();
                }
                catch (Exception)
                {
                    isDo = false;
                    eventMouse.OnMouseMove -= Em_OnMouseMove;
                    if (_curveShost != null)
                        _curveShost.UnRegister();
                    if (_listP.Count == 2)
                    {
                        var p1 = _listP.First();
                        var p2 = _listP.Last();
                        if (p1.DistanceTo(p2).ToMillimeters() < limit) return;
                        sp = p1; ep = p2;
                    }
                }
            } while (isDo);
        }
        private void CheckView()
        {
            var view = _document.ActiveView;
            if (view.ViewType != ViewType.FloorPlan)
                throw new Exception("View is not a FloorPlan");
        }
        private void CheckParameter()
        {
            if (!ParameterHelper.HasParameter(_document, WallParameterName.DPWALL_HOST))
                throw new Exception($"Param:{WallParameterName.DPWALL_HOST} is not found");
        }
        private List<model.ECPFamilyModel> GetECPFamilies()
        {
            var result = new List<model.ECPFamilyModel>();
            var sbs = new FilteredElementCollector(_document)
                .WhereElementIsElementType()
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(x=>x.FamilyName.ToUpper().Contains("ECP"))
                .ToList();
            if(!sbs.Any()) return result;
            var sbsGR = sbs
                .GroupBy(x=>x.FamilyName)
                .OrderBy(x=>x.FirstOrDefault().FamilyName)
                .ToList();
            foreach (var item in sbsGR)
            {
                var f = new model.ECPFamilyModel
                {
                    Symbol = item.FirstOrDefault()
                };
                f.Name = f.Symbol.FamilyName;
                result.Add(f);
            }
            return result;
        }
        private void CreateWall(XYZ sp, XYZ ep)
        {
            if (sp == null) return;
            if (ep == null) return;
            if (_viewModel.ECPFamily == null) return;
            var dir = (ep - sp).Normalize();
            var angle = dir.Y >=0 
                ? dir.AngleTo(XYZ.BasisX)
                : -dir.AngleTo(XYZ.BasisX);
            using (var ts = new Transaction(_document, "new transaction"))
            {
                //ts.SkipAllWarnings();
                ts.Start();
                if (!_viewModel.ECPFamily.Symbol.IsActive)
                    _viewModel.ECPFamily.Symbol.Activate();
                var isDo = true;
                var basePoint = sp;
                var walls = new List<FamilyInstance>();
                var hostId = Guid.NewGuid().ToString();
                do
                {
                    try
                    {
                        var width = _viewModel.ECPWidth;
                        var distance = Math.Round(basePoint.DistanceTo(ep).ToMillimeters(), 0);
                        if (distance == 0) throw new Exception();
                        if (dir.DotProduct((ep - basePoint).Normalize()) < 0) throw new Exception();
                        if (distance <= _viewModel.ECPWidth)
                            width = distance;
                        var w = _document.Create.NewFamilyInstance(
                            basePoint,
                            _viewModel.ECPFamily.Symbol,
                            Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        _document.Regenerate();
                        ElementTransformUtils.RotateElement(_document, w.Id, Line.CreateUnbound(basePoint, XYZ.BasisZ), angle);
                        basePoint = basePoint + dir * (width + _viewModel.ECPGap).FromMillimeters();
                        w.LookupParameter(WallParameterName.Width).Set(width.FromMillimeters());
                        w.LookupParameter(WallParameterName.DPWALL_HOST).Set(hostId);
                        walls.Add(w);
                    }
                    catch (Exception)
                    {
                        isDo = false;
                    }
                } while (isDo);
                ECPDimensionAction.Create(walls);
                ts.Commit();
            }

        }
        private void Em_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_listP.Any()) return;
            var p1 = _listP.First();
            var f = Plane.CreateByNormalAndOrigin(_document.ActiveView.ViewDirection, p1);
            var p2 = _uidocument
                .GetModelCoordinatesAtCursor()
                .RayIntersectPlane(f.Normal, f);
            var dir = (p2 - p1).Normalize();
            var nor = dir.CrossProduct(XYZ.BasisZ);
            var r1 = p1 - nor * _viewModel.ECPThickness.FromMillimeters() / 2;
            var r2 = p1 + nor * _viewModel.ECPThickness.FromMillimeters() / 2;
            var r3 = p2 + nor * _viewModel.ECPThickness.FromMillimeters() / 2;
            var r4 = p2 - nor * _viewModel.ECPThickness.FromMillimeters() / 2;

            Line l1 = null;
            Line l2 = null;
            Line l3 = null;
            Line l4 = null;

            if (r1.DistanceTo(r2).ToMillimeters() < 50) return;
            if (r2.DistanceTo(r3).ToMillimeters() < 50) return;
            if (r3.DistanceTo(r4).ToMillimeters() < 50) return;
            if (r4.DistanceTo(r1).ToMillimeters() < 50) return;

            l1 = Line.CreateBound(r1, r2);
            l2 = Line.CreateBound(r2, r3);
            l3 = Line.CreateBound(r3, r4);
            l4 = Line.CreateBound(r4, r1);
            var ls = new List<Line>()
            {
                l1,
                l2,
                l3,
                l4,
            };
            if (_curveShost != null)
                _curveShost.UnRegister();
            _curveShost = new CurveVisualizationServer(_uidocument, ls);
            _curveShost.Register();
        }
        private void _OkCommand()
        {
            if (_viewModel.ECPWidth == 0) return;
            _view.Close();
            SelectCurveWall(out XYZ sp, out XYZ ep);
            CreateWall(sp, ep);
        }
        private void _CancelCommand()
        {
        }
        private void _ECPGapAction()
        {
            if(_viewModel.ECPGap < 0) _viewModel.ECPGap = 0;
            if(_viewModel.ECPGap > 50) _viewModel.ECPGap = 50;
        }
        private void _ECPWidthAction()
        {
            if (_viewModel.ECPWidth > _viewModel.ECPWidthMax)
                _viewModel.ECPWidth = _viewModel.ECPWidthMax;
            if (_viewModel.ECPWidth < 100)
                _viewModel.ECPWidth = 100;
        }
        private void _ECPFamilyAction()
        {
            if (_viewModel.ECPFamily == null) return;
            _viewModel.ECPWidthMax =
                Math.Round(_viewModel.ECPFamily.Symbol.LookupParameter(WallParameterName.WidthMax).AsDouble().ToMillimeters(), 0);
            if (_viewModel.ECPWidth > _viewModel.ECPWidthMax || _viewModel.ECPWidth == 0)
                _viewModel.ECPWidth = _viewModel.ECPWidthMax;
            _viewModel.ECPThickness = Math.Round(_viewModel.ECPFamily.Symbol.LookupParameter(WallParameterName.Thickness).AsDouble().ToMillimeters(), 0);
        }
    }
}
