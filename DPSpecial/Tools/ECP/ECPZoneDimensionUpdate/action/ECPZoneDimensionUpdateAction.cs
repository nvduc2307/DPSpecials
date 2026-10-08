using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPZoneInstall.action;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPZoneDimensionUpdate.action
{
    public class ECPZoneDimensionUpdateAction
    {
        private readonly UIDocument _uidocument;
        private readonly Document _document;
        private readonly ECPZoneDimensionHelper _helper;

        public ECPZoneDimensionUpdateAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            ECPZoneDimensionHelper.ValidateView(_document);
            _helper = new ECPZoneDimensionHelper(_document);
        }

        public void Execute()
        {
            var view = _document.ActiveView;
            var count = 0;
            var isDo = true;
            do
            {
                IList<Element> elements;
                try
                {
                    elements = _uidocument.Selection.PickElements(
                        _document,
                        null,
                        x => ECPZoneDimensionHelper.IsInViewPlane(x, view),
                        "Pick ECP walls to update dimensions (Esc to stop)...");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    break;
                }
                if (elements == null || !elements.Any()) continue;

                using (var ts = new Transaction(_document, "Update ECP Dimensions"))
                {
                    ts.Start();
                    try
                    {
                        _helper.InitTextTypes();
                        foreach (var element in elements)
                            if (_helper.Update((FamilyInstance)element))
                                count++;
                        ts.Commit();
                    }
                    catch (Exception ex)
                    {
                        ts.RollBack();
                        IO.ShowWarning(ex.Message);
                        isDo = false;
                    }
                }
                _uidocument.RefreshActiveView();
            } while (isDo);

            if (count > 0)
                IO.ShowInfo($"Update completed.\nUpdated walls: {count}");
        }
    }
}
