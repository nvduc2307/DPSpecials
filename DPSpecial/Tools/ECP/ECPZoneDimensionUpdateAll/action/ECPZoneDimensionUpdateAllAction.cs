using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPZoneInstall.action;
using DPSpecial.Tools.ECP.ECPZoneInstall.schema;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPZoneDimensionUpdateAll.action
{
    public class ECPZoneDimensionUpdateAllAction
    {
        private readonly UIDocument _uidocument;
        private readonly Document _document;
        private readonly ECPZoneDimensionHelper _helper;

        public ECPZoneDimensionUpdateAllAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            ECPZoneDimensionHelper.ValidateView(_document);
            _helper = new ECPZoneDimensionHelper(_document);
        }

        public void Execute()
        {
            var assignSchema = new ECPZoneAssignSchema(ECPZoneAssignSchema.GUID, ECPZoneAssignSchema.NAME);
            var walls = _helper.GetWallsInView()
                .Where(x => !string.IsNullOrEmpty(assignSchema.Read(x)))
                .ToList();
            if (!walls.Any())
            {
                IO.ShowInfo("No ECP walls with a zone were found in this view.");
                return;
            }

            var count = 0;
            using (var ts = new Transaction(_document, "Update All ECP Dimensions"))
            {
                ts.Start();
                _helper.InitTextTypes();
                foreach (var wall in walls)
                    if (_helper.Update(wall))
                        count++;
                ts.Commit();
            }
            _uidocument.RefreshActiveView();
            IO.ShowInfo($"Update completed.\nUpdated walls: {count}");
        }
    }
}
