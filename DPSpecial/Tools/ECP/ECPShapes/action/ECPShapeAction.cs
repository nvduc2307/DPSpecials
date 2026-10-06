using Autodesk.Revit.UI;
using DPSpecial.MVVM.Models;
using DPSpecial.Tools.ECP.ECPShapes.model;
using DPSpecial.Tools.ECP.ECPShapes.schema;
using DPSpecial.Utils;
using Nice3point.Revit.Extensions.Runtime;
using System.Windows;

namespace DPSpecial.Tools.ECP.ECPShapes.action
{
    public class ECPShapeAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private ECPShapeSchema _eCPShapeSchemal;
        // Quá thời gian này thì dừng xử lý, commit phần đã làm và kết thúc.
        private const int TimeoutSeconds = 120;
        // Template chỉ mở 1 lần cho cả lần chạy, mỗi hình chỉ nạp 1 lần rồi dùng lại cho mọi tường.
        private Document _templateDoc;
        private readonly List<ElementId> _loadedShapeIds = new List<ElementId>();
        private readonly HashSet<string> _missingShapes = new HashSet<string>();
        // Nhóm hình gốc đã tìm/nạp theo tên, tránh quét lại toàn bộ Group trong project cho từng tường.
        private readonly Dictionary<string, Group> _shapeCache = new Dictionary<string, Group>();
        public ECPShapeAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _eCPShapeSchemal = new ECPShapeSchema(ECPShapeSchema.GUID, ECPShapeSchema.NAME);
        }
        public static XYZ GetCenter(FamilyInstance wall)
        {
            XYZ result = null;
            var paraLength = wall.LookupParameter(WallParameterName.Length);
            var paraWidth = wall.LookupParameter(WallParameterName.Width);
            if (paraLength == null) return result;
            if (paraWidth == null) return result;
            var length = Math.Round(paraLength.AsDouble().ToMillimeters(), 0);
            var width = Math.Round(paraWidth.AsDouble().ToMillimeters(), 0);
            var trans = wall.GetTransform();
            result = trans.Origin + trans.BasisX * width / 2 + trans.BasisZ * length / 2;
            return result;
        }
        public void Execute()
        {
            try
            {
                ExecuteCore();
            }
            finally
            {
                var closeWatch = System.Diagnostics.Stopwatch.StartNew();
                CloseTemplate();
                PerfLog.Write($"ECPShape: đóng template {closeWatch.ElapsedMilliseconds} ms");
            }
        }
        private void ExecuteCore()
        {
            ValidateView();
            var walls = GetWallECPs();
            if (!walls.Any()) return;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            PerfLog.Write($"ECPShape: bắt đầu, {walls.Count} tường");
            using (var ts = new Transaction(_document, "new transaction"))
            {
                ts.SkipAllWarnings();
                ts.Start();
                foreach (var wall in walls)
                {
                    try
                    {
                        var trans = wall.GetTransform();
                        var eCPShapeSchemalInfo = _eCPShapeSchemal.Read(wall);
                        var shapeName = GetECPShapeName(wall);
                        var shape = GetGroupECPShape(shapeName);
                        if (shape == null) continue;
                        if (shape.Location == null) continue;
                        var center = wall.GetSolid().Select(x=>x.GetCenter()).ToList().GetCenter() ?? GetCenter(wall);
                        var vtMove = center - (shape.Location as LocationPoint)?.Point;
                        var shapeIds = ElementTransformUtils.CopyElement(_document, shape.Id, vtMove);
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
                            // Không Regenerate ở đây: mỗi lần buộc Revit dựng lại cả view Elevation,
                            // Revit tự cập nhật một lần khi commit.
                            _document.Delete(idShapeOld);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                PerfLog.Write($"ECPShape: xong vòng lặp tường sau {stopwatch.ElapsedMilliseconds} ms");
                DeleteLoadedShapes();
                _shapeCache.Clear();
                PerfLog.Write($"ECPShape: trước Commit, {stopwatch.ElapsedMilliseconds} ms");
                ts.Commit();
                PerfLog.Write($"ECPShape: Commit xong, {stopwatch.ElapsedMilliseconds} ms");
            }
        }
        // Xoá các nhóm hình vừa nạp từ template (đã copy xong cho từng tường).
        private void DeleteLoadedShapes()
        {
            foreach (var id in _loadedShapeIds)
            {
                try
                {
                    if (_document.GetElement(id) != null) _document.Delete(id);
                }
                catch (Exception)
                {
                }
            }
            _loadedShapeIds.Clear();
        }
        private void CloseTemplate()
        {
            try
            {
                if (_templateDoc is { IsValidObject: true })
                    _templateDoc.Close(false);
            }
            catch (Exception)
            {
            }
            _templateDoc = null;
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
            var walls_total = new FilteredElementCollector(_document, _document.ActiveView.Id)
                .WhereElementIsNotElementType()
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .ToList();
            if(!walls_total.Any()) return walls;
            var walls_ver = walls_total
                .Where(x => ECPFamilyName.ECPVerticalFamilyName.Any(f=>f == x.Symbol.FamilyName))
                .Where(x =>
                {
                    var trans = x.GetTransform();
                    var vty = trans.BasisY;
                    var vtx = trans.BasisX;
                    var vtz = trans.BasisZ;
                    var vtView = _document.ActiveView.ViewDirection;
                    var rest = vtx.IsParallel(vtView);
                    return rest;
                })
                .ToList();
            var walls_hor = walls_total
                .Where(x => x.Symbol.FamilyName.ToUpper().Contains("ECP"))
                .Where(x => x.GetTransform().BasisY.IsParallel(_document.ActiveView.ViewDirection))
                .ToList();
            if (walls_hor.Any()) walls.AddRange(walls_hor);
            if (walls_ver.Any()) walls.AddRange(walls_ver);
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
            var isVerticalWall = ECPFamilyName.ECPVerticalFamilyName.Any(x => x == wall.Symbol.FamilyName);
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
            if (isVerticalWall)
            {
                result = width < widthMax ? ECPShapeName.EH3 : ECPShapeName.EH0;
            }
            return result;
        }
        private Group GetGroupECPShape(string shapeECPName)
        {
            if (string.IsNullOrEmpty(shapeECPName)) return null;
            if (_shapeCache.TryGetValue(shapeECPName, out var cached) && cached.IsValidObject) return cached;
            // Hình đã nạp ở tường trước vẫn còn trong project nên được tìm thấy ở đây,
            // không phải mở lại template cho từng tường.
            var group = FindDetailGroupInstance(_document, shapeECPName);
            if (group != null)
            {
                _shapeCache[shapeECPName] = group;
                return group;
            }
            if (_missingShapes.Contains(shapeECPName)) return null;
            try
            {
                group = LoadDetailGroupInstanceFromTemplate(_document, shapeECPName, _document.ActiveView);
            }
            catch (Exception)
            {
                _missingShapes.Add(shapeECPName);
                throw;
            }
            if (group == null)
            {
                _missingShapes.Add(shapeECPName);
                return null;
            }
            _loadedShapeIds.Add(group.Id);
            _shapeCache[shapeECPName] = group;
            return group;
        }
        private Group LoadDetailGroupInstanceFromTemplate(Document targetDoc, string nameShape, Autodesk.Revit.DB.View targetView)
        {
            var app = targetDoc.Application;
            try
            {
                if (_templateDoc is not { IsValidObject: true })
                {
                    var path = $"{PathHelper.Templates}\\ShapeWallECP_Template.rte";
                    _templateDoc = app.OpenDocumentFile(path);
                }
                var sourceDoc = _templateDoc;
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
