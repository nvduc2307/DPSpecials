using Autodesk.Revit.UI;
using DPSpecial.MVVM.Models;
using DPSpecial.Tools.ECP.ECPSchedule.ECPShapes.model;
using DPSpecial.Tools.ECP.ECPSchedule.ECPShapes.schema;
using DPSpecial.Utils;
using Nice3point.Revit.Extensions.Runtime;
using System.Windows;

namespace DPSpecial.Tools.ECP.ECPSchedule.ECPShapes.action
{
    public class ECPShapeAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private ECPShapeSchema _eCPShapeSchemal;
        public ECPShapeAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _eCPShapeSchemal = new ECPShapeSchema(ECPShapeSchema.GUID, ECPShapeSchema.NAME);
        }
        public void Execute()
        {
            ValidateView();
            var walls = GetWallECPs();
            if (!walls.Any()) return;
            using (var ts = new Transaction(_document, "new transaction"))
            {
                ts.SkipAllWarnings();
                ts.Start();
                foreach (var wall in walls)
                {
                    try
                    {
                        var eCPShapeSchemalInfo = _eCPShapeSchemal.Read(wall);
                        var shapeName = GetECPShapeName(wall);
                        var shape = GetGroupECPShape(shapeName, out bool isDeleteGr);
                        if (shape == null) continue;
                        if (shape.Location == null) continue;
                        var center = wall.GetSolid().Select(x=>x.GetCenter()).ToList().GetCenter();
                        var vtMove = center - (shape.Location as LocationPoint)?.Point;
                        var shapeIds = ElementTransformUtils.CopyElement(_document, shape.Id, vtMove);
                        if (isDeleteGr)
                        {
                            _document.Delete(shape.Id);
                            _document.Regenerate();
                        }
                        _eCPShapeSchemal.Write(wall, shapeIds.First().ToString());
                        if (string.IsNullOrEmpty(eCPShapeSchemalInfo)) continue;
#if REVIT2022 || REVIT2023
                        var idShapeOld = new ElementId(int.Parse(eCPShapeSchemalInfo));
#else
                        var idShapeOld = new ElementId(long.Parse(eCPShapeSchemalInfo));
#endif
                        if (idShapeOld == null) continue;
                        try
                        {
                            _document.Delete(idShapeOld);
                            _document.Regenerate();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                ts.Commit();
            }
        }
        private void ValidateView()
        {
            var view = _document.ActiveView;
            if (view.ViewType != ViewType.Elevation)
                throw new Exception("View is not a Elevation View");
        }
        private List<FamilyInstance> GetWallECPs()
        {
            var walls = new List<FamilyInstance>();
            walls = new FilteredElementCollector(_document, _document.ActiveView.Id)
                .WhereElementIsNotElementType()
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .ToList();
            if(!walls.Any()) return walls;
            walls = walls.Where(x => x.Symbol.FamilyName.ToUpper().Contains("ECP")).ToList();
            return walls;
        }
        private string GetECPShapeName(FamilyInstance wall)
        {
            var result = string.Empty;
            if (wall == null) return result;
            var document = wall.Document;
            var view = document.ActiveView;
            var tranfs = wall.GetTransform();
            var isWallHasArrow = ECPFamilyName.ECPFamilyNameNormal.Any(x => x == wall.Symbol.FamilyName);
            var isWallHasNotArrow = ECPFamilyName.ECPFamilyNameNotArrow.Any(x => x == wall.Symbol.FamilyName);
            var paraWidthMax = wall.Symbol.LookupParameter(WallParameterName.WidthMax);
            var paraWidth = wall.LookupParameter(WallParameterName.Width);
            if (paraWidthMax == null) return result;
            if (paraWidth == null) return result;
            var widthMax = Math.Round(paraWidthMax.AsDouble().ToMillimeters(), 0);
            var width = Math.Round(paraWidth.AsDouble().ToMillimeters(), 0);
            if (isWallHasArrow)
            {
                if(tranfs.BasisX.DotProduct(view.RightDirection) > 0)
                    result = width < widthMax ? ECPShapeName.EL3 : ECPShapeName.EL0;
                else
                    result = width < widthMax ? ECPShapeName.ER3 : ECPShapeName.ER0;
            }
            if (isWallHasNotArrow)
            {
                if (tranfs.BasisX.DotProduct(view.RightDirection) > 0)
                    result = width < widthMax ? ECPShapeName.EL5 : ECPShapeName.EL1;
                else
                    result = width < widthMax ? ECPShapeName.ER5 : ECPShapeName.ER1;
            }
            return result;
        }
        private Group GetGroupECPShape(string shapeECPName, out bool isDeleteGr)
        {
            Group group = null;
            isDeleteGr = false;
            if (string.IsNullOrEmpty(shapeECPName)) return group;
            group = FindDetailGroupInstance(_document, shapeECPName);
            if(group != null) return group;
            group = LoadDetailGroupInstanceFromTemplate(_document, shapeECPName, _document.ActiveView);
            isDeleteGr = true;
            return group;
        }
        private Group LoadDetailGroupInstanceFromTemplate(Document targetDoc, string nameShape, Autodesk.Revit.DB.View targetView)
        {
            var app = targetDoc.Application;
            Document sourceDoc = null;
            try
            {
                var path = $"{PathHelper.Templates}\\ShapeWallECP_Template.rte";
                sourceDoc = app.OpenDocumentFile(path);
                var group = FindDetailGroupInstance(sourceDoc, nameShape);
                if (group == null) return null;

                var srcView = sourceDoc.GetElement(group.OwnerViewId) as Autodesk.Revit.DB.View;
                if (srcView is null) return null;

                var copyOpts = new CopyPasteOptions();
                copyOpts.SetDuplicateTypeNamesHandler(new UseDestinationTypesDuplicateHandler());

                var newIds = ElementTransformUtils.CopyElements(
                    srcView,
                    new List<ElementId> { group.Id },
                    targetView,
                    Transform.Identity,
                    copyOpts);

                targetDoc.Regenerate();
                Group newGroup = newIds
                    .Select(targetDoc.GetElement)
                    .OfType<Group>()
                    .FirstOrDefault();

                return newGroup;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (sourceDoc is { IsValidObject: true })
                    sourceDoc.Close(false);
            }
        }
        private static Group FindDetailGroupInstance(Document doc, string nameGroup, Autodesk.Revit.DB.View view = null)
        {
            FilteredElementCollector col = (view == null)
                ? new FilteredElementCollector(doc)
                : new FilteredElementCollector(doc, view.Id);

            return col.OfClass(typeof(Group))
                    .Cast<Group>()
                    .FirstOrDefault(g =>
                        g?.GroupType?.Category != null
                        && g.GroupType.Category.Id.ToString() == ((int)(BuiltInCategory.OST_IOSDetailGroups)).ToString()
                        && g.GroupType != null
                        && g.GroupType.Name == nameGroup
                        && (view == null || g.OwnerViewId == view.Id)
                        && (view != null || g.OwnerViewId != ElementId.InvalidElementId)
                    );
        }
    }
    class UseDestinationTypesDuplicateHandler : IDuplicateTypeNamesHandler
    {
        public DuplicateTypeAction OnDuplicateTypeNamesFound(DuplicateTypeNamesHandlerArgs args)
            => DuplicateTypeAction.UseDestinationTypes;
    }
}
