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
        private double GetECPWithMax(FamilyInstance wall)
        {
            if (wall == null) return 0;
            var paramMaxWidth = wall.Symbol.LookupParameter(WallParameterName.WidthMax);
            if (paramMaxWidth == null) return 0;
            return Math.Round(paramMaxWidth.AsDouble().ToMillimeters(), 0);
        }
        private double GetECPWith(FamilyInstance wall)
        {
            if (wall == null) return 0;
            var paramWidth = wall.LookupParameter(WallParameterName.Width);
            if (paramWidth == null) return 0;
            return Math.Round(paramWidth.AsDouble().ToMillimeters(), 0);
        }
        private List<FamilyInstance> GetGroupWall(FamilyInstance wall, XYZ direction)
        {
            var result = new List<FamilyInstance>();
            var paraHost = wall.LookupParameter(WallParameterName.DPWALL_HOST);
            if (paraHost == null) throw new Exception("ParaHost is not found");
            var host = paraHost.AsString();
            if (string.IsNullOrEmpty(host)) throw new Exception("Host is not found");
            var ecpWalls = new FilteredElementCollector(_document)
                .WhereElementIsNotElementType()
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(x => x.Symbol.FamilyName.ToUpper().Contains("ECP"))
                .ToList();
            if (!ecpWalls.Any()) return result;
            result = ecpWalls
                .Where(x => x.LookupParameter(WallParameterName.DPWALL_HOST).AsString() == host)
                .OrderBy(w => w.GetTransform().Origin.DotProduct(direction))
                .ToList();
            return result;
        }
        private void GetControlGWall(List<FamilyInstance> walls, XYZ vtx, out XYZ sp, out XYZ ep)
        {
            sp = null;
            ep = null;
            var ps = walls
                .Select(w =>
                {
                    var transf = w.GetTransform();
                    return new List<XYZ>()
                    {
                        transf.Origin, transf.Origin + transf.BasisX * w.LookupParameter(WallParameterName.Width).AsDouble(),
                    };
                })
                .Aggregate((ps1, ps2) => ps1.Concat(ps2).ToList())
                .Distinct(new ComparePoint())
                .ToList();
            if (ps.Count <= 1) return;
            ps = ps.OrderBy(x => x.DotProduct(vtx)).ToList();
            sp = ps.FirstOrDefault();
            ep = ps.LastOrDefault();
        }
        private void SetECPWith(FamilyInstance wall, double widthMm, XYZ direction)
        {
            try
            {
                if (wall == null) return;
                var transf = wall.GetTransform();
                var paramWidth = wall.LookupParameter(WallParameterName.Width);
                var paramWidthMax = wall.Symbol.LookupParameter(WallParameterName.WidthMax);
                if (paramWidth == null) return;
                if (paramWidthMax == null) return;
                var widthMax = Math.Round(paramWidthMax.AsDouble(), 0);
                var walls = GetGroupWall(wall, direction);
                if (!walls.Any()) return;
                GetControlGWall(walls, direction, out XYZ sp, out XYZ ep);
                if (sp == null || ep == null) return;
                var fStart = Plane.CreateByNormalAndOrigin(direction, sp);
                var fEnd = Plane.CreateByNormalAndOrigin(direction, ep);
                var indexTarget = walls.IndexOf(walls.FirstOrDefault(x => x.Id.ToString() == wall.Id.ToString()));
                if (indexTarget < 0) return;
                var currentWidth = Math.Round(paramWidth.AsDouble().ToMillimeters(), 0);
                var deltaWidth = Math.Abs(currentWidth - widthMm);
                if (deltaWidth < 50)
                {
                    _wallTarget = indexTarget + 1 <= walls.Count - 1
                    ? walls[indexTarget + 1]
                    : null;
                    if (_wallTarget == null) _view.Close();
                    return;
                }
                var trendMove = currentWidth <= widthMm ? 1 : -1;
                paramWidth.Set(widthMm.FromMillimeters());
                var elementsMove = transf.BasisX.DotProduct(direction) > 0
                    ? walls.Where((x, index) => index > indexTarget)
                    : walls.Where((x, index) => index >= indexTarget);
                if (elementsMove.Any())
                    ElementTransformUtils.MoveElements(_document, elementsMove.Select(x => x.Id).ToList(), trendMove * direction * deltaWidth.FromMillimeters());

                _document.Regenerate();
                GetControlGWall(walls, direction, out XYZ spNew, out XYZ epNew);
                var vtCheck1 = (epNew - sp).Normalize();
                var vtCheck2 = (epNew - ep).Normalize();
                if(vtCheck1.DotProduct(vtCheck2) < 0)
                {
                    //Điểm cuối của tường cuối bị ngắn đi sp ---- epNew ---- ep
                    var deltaWidthEnd = Math.Round(epNew.DistanceTo(ep).ToMillimeters(), 0) - 10.0;
                    var widthLast = Math.Round(walls.LastOrDefault().LookupParameter(WallParameterName.Width).AsDouble(), 0);
                    var wAddId = ElementTransformUtils.CopyElement(_document, walls.LastOrDefault().Id, new XYZ()).FirstOrDefault();
                    _document.Regenerate();
                    var wAdd = _document.GetElement(wAddId) as FamilyInstance;
                    wAdd.LookupParameter(WallParameterName.Width).Set(deltaWidthEnd.FromMillimeters());
                    if(direction.DotProduct(transf.BasisX) >= 0)
                        ElementTransformUtils.MoveElement(_document, wAdd.Id, direction * (wAdd.GetTransform().Origin.DistanceTo(epNew) + 10.0.FromMillimeters()));
                    else
                        ElementTransformUtils.MoveElement(_document, wAdd.Id, direction * (deltaWidthEnd.FromMillimeters() + 10.0.FromMillimeters()));
                }
                else
                {
                    //Điểm cuối của tường cuối bị dài hơn so với tường cũ sp ---- ep ---- epNew
                    var elementOutScope = new List<Element>();
                    foreach (var w in walls)
                    {
                        var tranfIndex = w.GetTransform();
                        var widthIndex = Math.Round(w.LookupParameter(WallParameterName.Width).AsDouble().ToMillimeters(), 0);
                        var spIndex = tranfIndex.BasisX.DotProduct(direction) > 0
                            ? tranfIndex.Origin
                            : tranfIndex.Origin + tranfIndex.BasisX * widthIndex.FromMillimeters();
                        var epIndex = tranfIndex.BasisX.DotProduct(direction) > 0
                            ? tranfIndex.Origin + tranfIndex.BasisX * widthIndex.FromMillimeters()
                            : tranfIndex.Origin;
                        var vtIndexCheck1 = (spIndex - ep).Normalize();
                        var vtIndexCheck2 = (epIndex - ep).Normalize();
                        if(vtIndexCheck1.DotProduct(vtIndexCheck2) >= 0)
                        {
                            if (vtIndexCheck1.DotProduct(direction) >= 0 && vtIndexCheck2.DotProduct(direction) >= 0)
                                _document.Delete(w.Id);
                        }
                        else
                        {
                            var distance = Math.Round(ep.Distance(epIndex).ToMillimeters(), 0);
                            var widthNew = widthIndex - distance;
                            if (widthNew <= 0) continue;
                            w.LookupParameter(WallParameterName.Width).Set(widthNew.FromMillimeters());
                            if(tranfIndex.BasisX.DotProduct(direction) < 0)
                                ElementTransformUtils.MoveElement(_document, w.Id, -direction * distance.FromMillimeters());
                        }
                    }
                }
                _document.Regenerate();
                _wallTarget = indexTarget + 1 <= walls.Count - 1
                    ? walls[indexTarget + 1]
                    : null;
                if (_wallTarget == null) _view.Close();
                //if(!_wallTarget.IsValidObject) _view.Close();
            }
            catch (Exception ex)
            {
                IO.ShowWarning(ex.Message);
                _wallTarget = null;
                _view.Close();
            }
        }
    }
}
