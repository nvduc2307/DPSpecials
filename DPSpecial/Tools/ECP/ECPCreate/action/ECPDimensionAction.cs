using DPSpecial.MVVM.Models;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPCreate.action
{
    public class ECPDimensionAction
    {
        private const double _extent = 18;
        public static Dimension Create(List<FamilyInstance> walls)
        {
            Dimension result = null;
            try
            {
                if (!walls.Any()) return result;
                var document = walls.FirstOrDefault().Document;
                var scale = document.ActiveView.Scale;
                var transf = walls.FirstOrDefault().GetTransform();
                var vtx = transf.BasisX;
                var vty = transf.BasisY;
                var edges = new List<Edge>();
                var arr = new ReferenceArray();
                foreach (FamilyInstance f in walls)
                {
                    arr.Append(f.GetReferences(FamilyInstanceReferenceType.CenterLeftRight).ElementAt(0));
                    var refRight = f.GetReferences(FamilyInstanceReferenceType.Right).ElementAt(0);
                    if (refRight != null)
                        arr.Append(refRight);
                }
                var p = new XYZ(transf.Origin.X, transf.Origin.Y, document.ActiveView.GenLevel.Elevation) 
                    - transf.BasisY * scale * _extent.FromMillimeters();
                result = document.Create.NewDimension(document.ActiveView, Line.CreateUnbound(p, vtx), arr);
                //Alight Dim
                AlightTextDim(result, walls.FirstOrDefault().GetTransform().BasisX.CrossProduct(document.ActiveView.ViewDirection));
                CreateDimIntersect(walls);
            }
            catch (Exception)
            {
            }
            return result;
        }
        private static void CreateDimIntersect(List<FamilyInstance> walls)
        {
            if(!walls.Any()) return;
            var wall = walls.FirstOrDefault();
            var transf = wall.GetTransform();
            var document = wall.Document;
            document.Regenerate();
            var view = document.ActiveView;
            var scale = view.Scale;
            var grids = new FilteredElementCollector(document, document.ActiveView.Id)
                .WhereElementIsNotElementType()
                .OfClass(typeof(Grid))
                .Cast<Grid>()
                .ToList();
            if (!grids.Any()) return;
            var gridsTarget = grids
                .Where(g =>
                {
                    var gc = g.GetCurvesInView(DatumExtentType.ViewSpecific, document.ActiveView).ElementAt(0);
                    if (gc == null) return false;
                    if (!gc.Direction().IsParallel(transf.BasisY)) return false;
                    return true;
                });
            foreach (var w in walls)
            {
                var width = w.LookupParameter(WallParameterName.Width).AsDouble();
                var transTarget = w.GetTransform();
                var sp = transTarget.Origin;
                var ep = sp + transTarget.BasisX * width;
                sp.Add(new XYZ(0, 0, view.GenLevel.Elevation));
                ep.Add(new XYZ(0, 0, view.GenLevel.Elevation));
                var arr = new ReferenceArray();
                var l = Line.CreateBound(sp, ep);
                foreach (var gr in gridsTarget)
                {
                    var cg = gr.GetCurvesInView(DatumExtentType.ViewSpecific, document.ActiveView).ElementAt(0);
                    var mid = cg.Midpoint();
                    var f = Plane.CreateByNormalAndOrigin(transTarget.BasisX, mid);
                    var pInter = sp.RayIntersectPlane(f.Normal, f);
                    var vtsCheck = (sp - pInter).Normalize();
                    var vteCheck = (ep - pInter).Normalize();
                    var d1 = pInter.DistanceTo(sp).ToMillimeters();
                    var d2 = pInter.DistanceTo(ep).ToMillimeters();
                    if (d1 < 5) continue;
                    if (d2 < 5) continue;
                    if (vtsCheck.DotProduct(vteCheck) >= 0) continue;
                    arr.Append(new Reference(gr));
                    arr.Append(w.GetReferences(FamilyInstanceReferenceType.CenterLeftRight).ElementAt(0));
                    var refRight = w.GetReferences(FamilyInstanceReferenceType.Right).ElementAt(0);
                    if (refRight != null)
                        arr.Append(refRight);
                }
                if (arr.Size < 2) continue;
                var p = new XYZ(transf.Origin.X, transf.Origin.Y, document.ActiveView.GenLevel.Elevation)
                    - transf.BasisY * scale * _extent.FromMillimeters() * 2 / 3;
                document.Create.NewDimension(document.ActiveView, Line.CreateUnbound(p, transf.BasisX), arr);
            }
        }
        public static void Remove(FamilyInstance wall)
        {
            var document = wall.Document;
            var dims = new FilteredElementCollector(
                document, document.ActiveView.Id)
                .WhereElementIsNotElementType()
                .OfClass(typeof(Dimension))
                .Cast<Dimension>()
                .ToList();
            foreach (var dim in dims)
            {
                try
                {
                    foreach (var dimObj in dim.References)
                    {
                        if (!(dimObj is Reference dimRef)) continue;
                        if(dimRef.ElementId.ToString() == wall.Id.ToString())
                        {
                            document.Delete(dim.Id);
                            continue;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        public static void AlightTextDim(Dimension dim, XYZ directionMove)
        {
            var extent = 9.0.FromMillimeters();
            var document = dim.Document;
            var view = document.ActiveView;
            var scale = view.Scale;
            if (dim == null) return;
            foreach (var segItem in dim.Segments)
            {
                if (!(segItem is DimensionSegment dimensionSegment)) continue;
                if (dimensionSegment.Value < 50.0.FromMillimeters())
                    dimensionSegment.TextPosition += directionMove * scale * extent;
            }
        }
    }
}
