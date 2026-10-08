using DPSpecial.Tools.ECP.ECPZoneInstall.schema;
using DPSpecial.Utils;
using WallParam = DPSpecial.MVVM.Models.WallParameterName;

namespace DPSpecial.Tools.ECP.ECPZoneInstall.action
{
    public class ECPZoneDimensionHelper
    {
        public const string NameViewSettingZone = "_settingZone";
        private const double _textHeight = 1.8;
        private const string _textTypeName = "TextTypeDimmensionWall";
        private const double _shapeTextHeight = 1.2;
        private const string _shapeTextTypeName = "TextTypeShapeWall";

        private readonly Document _document;
        private readonly ECPZoneDimensionSchema _dimensionSchema;
        private TextNoteType _textType;
        private TextNoteType _shapeTextType;

        public ECPZoneDimensionHelper(Document document)
        {
            _document = document;
            _dimensionSchema = new ECPZoneDimensionSchema(ECPZoneDimensionSchema.GUID, ECPZoneDimensionSchema.NAME);
        }

        public static void ValidateView(Document document)
        {
            var activeView = document.ActiveView;
            if (activeView is not ViewSection vs || vs.ViewType != ViewType.Elevation)
                throw new Exception("Please switch to an elevation view before running this command.");
            if (!activeView.Name.Contains(NameViewSettingZone))
                throw new Exception($"The elevation view name must contain \"{NameViewSettingZone}\".\nCurrent view: \"{activeView.Name}\"");
        }

        public static bool IsECP(Element element)
        {
            if (element is not FamilyInstance fa) return false;
            return fa.Symbol.FamilyName.ToUpper().Contains("ECP");
        }

        public static bool IsInViewPlane(Element element, Autodesk.Revit.DB.View view)
        {
            if (!IsECP(element)) return false;
            var fa = (FamilyInstance)element;
            return fa.GetTransform().BasisX.IsParallel(view.RightDirection);
        }

        public List<FamilyInstance> GetWallsInView()
        {
            var view = _document.ActiveView;
            return new FilteredElementCollector(_document, view.Id)
                .WhereElementIsNotElementType()
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(x => IsInViewPlane(x, view))
                .ToList();
        }

        public void InitTextTypes()
        {
            var textTypes = new FilteredElementCollector(_document)
                .OfClass(typeof(TextNoteType))
                .Cast<TextNoteType>()
                .ToList();
            if (!textTypes.Any())
                throw new Exception("TextNoteType is not found");
            _textType = GetOrCreateTextType(textTypes, _textTypeName, _textHeight);
            _shapeTextType = GetOrCreateTextType(textTypes, _shapeTextTypeName, _shapeTextHeight);
        }

        public bool Update(FamilyInstance element)
        {
            RemoveDimensions(element);
            return ShowDimensions(element);
        }

        private TextNoteType GetOrCreateTextType(List<TextNoteType> textTypes, string name, double heightMm)
        {
            var textType = textTypes.FirstOrDefault(x => x.Name == name)
                ?? textTypes.First().Duplicate(name) as TextNoteType;
            var sizeParam = textType.get_Parameter(BuiltInParameter.TEXT_SIZE);
            var size = heightMm.FromMillimeters();
            if (Math.Abs(sizeParam.AsDouble() - size) > 1e-9)
                sizeParam.Set(size);
            return textType;
        }

        private string GetShapeName(FamilyInstance element)
        {
            var name = element.Symbol.Name ?? string.Empty;
            return name.StartsWith("ecp_", StringComparison.OrdinalIgnoreCase) ? name.Substring(4) : name;
        }

        private void RemoveDimensions(FamilyInstance element)
        {
            var textNotes = new FilteredElementCollector(_document, _document.ActiveView.Id)
                .OfClass(typeof(TextNote))
                .Cast<TextNote>()
                .Where(x => _dimensionSchema.Read(x) == element.UniqueId)
                .Select(x => x.Id)
                .ToList();
            if (textNotes.Any())
                _document.Delete(textNotes);
        }

        private bool ShowDimensions(FamilyInstance element)
        {
            if (_textType == null) return false;
            var widthParam = element.LookupParameter(WallParam.Width);
            var heightParam = element.LookupParameter(WallParam.Length);
            if (widthParam == null || heightParam == null) return false;

            var view = _document.ActiveView;
            var bb = element.get_BoundingBox(view);
            if (bb == null) return false;

            var corners = new List<XYZ>();
            foreach (var x in new[] { bb.Min.X, bb.Max.X })
                foreach (var y in new[] { bb.Min.Y, bb.Max.Y })
                    foreach (var z in new[] { bb.Min.Z, bb.Max.Z })
                        corners.Add(bb.Transform.OfPoint(new XYZ(x, y, z)));

            var right = view.RightDirection;
            var up = view.UpDirection;
            var rs = corners.Select(c => c.DotProduct(right)).ToList();
            var us = corners.Select(c => c.DotProduct(up)).ToList();
            var viewWidth = rs.Max() - rs.Min();
            var viewHeight = us.Max() - us.Min();
            var center = corners.Aggregate(XYZ.Zero, (a, c) => a + c) / corners.Count;

            var offset = _textHeight.FromMillimeters() * view.Scale;
            var width = Math.Round(widthParam.AsDouble().ToMillimeters(), 0);
            var height = Math.Round(heightParam.AsDouble().ToMillimeters(), 0);

            var pTextWidth = center - up * (viewHeight / 2 - offset);
            var textWidth = TextNote.Create(_document, view.Id, pTextWidth, $"{width}", _textType.Id);
            textWidth.VerticalAlignment = VerticalTextAlignment.Middle;
            textWidth.HorizontalAlignment = HorizontalTextAlignment.Center;

            var pTextHeight = center + right * (viewWidth / 2 - offset);
            var textHeight = TextNote.Create(_document, view.Id, pTextHeight, $"{height}", _textType.Id);
            textHeight.VerticalAlignment = VerticalTextAlignment.Middle;
            textHeight.HorizontalAlignment = HorizontalTextAlignment.Center;
            ElementTransformUtils.RotateElement(
                _document, textHeight.Id,
                Line.CreateUnbound(pTextHeight, view.ViewDirection),
                Math.PI / 2);

            var shape = GetShapeName(element);
            if (!string.IsNullOrEmpty(shape))
            {
                var pTextShape = center + up * (viewHeight / 2 - _shapeTextHeight.FromMillimeters() * view.Scale);
                var textShape = TextNote.Create(_document, view.Id, pTextShape, shape, _shapeTextType.Id);
                textShape.VerticalAlignment = VerticalTextAlignment.Middle;
                textShape.HorizontalAlignment = HorizontalTextAlignment.Center;
                _dimensionSchema.Write(textShape, element.UniqueId);
            }

            _dimensionSchema.Write(textWidth, element.UniqueId);
            _dimensionSchema.Write(textHeight, element.UniqueId);
            return true;
        }
    }
}
