using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPCreate.action
{
    public class ECPDimensionAction
    {
        private const double _extent = 9;
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
                var p = new XYZ(transf.Origin.X, transf.Origin.Y, document.ActiveView.GenLevel.Elevation) - transf.BasisY * scale * _extent.FromMillimeters() * 3;
                result = document.Create.NewDimension(document.ActiveView, Line.CreateUnbound(p, vtx), arr);
            }
            catch (Exception)
            {
            }
            return result;
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
