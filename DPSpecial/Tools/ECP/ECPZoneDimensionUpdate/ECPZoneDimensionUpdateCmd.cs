using DPSpecial.Tools.Login.Licensing;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPZoneDimensionUpdate.action;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPZoneDimensionUpdate
{
    [Transaction(TransactionMode.Manual)]
    public class ECPZoneDimensionUpdateCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!LicenseGate.EnsureFeature(string.Empty))
            {
                return Result.Cancelled;
            }

            var result = Result.Succeeded;
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;
            using (var tsg = new TransactionGroup(document, "ECPZoneDimensionUpdateCmd"))
            {
                tsg.Start();
                try
                {
                    var action = new ECPZoneDimensionUpdateAction(uiDocument);
                    action.Execute();
                    tsg.Assimilate();
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }
                catch (Exception ex)
                {
                    IO.ShowWarning(ex.Message);
                    tsg.RollBack();
                    result = Result.Failed;
                }
            }
            return result;
        }
    }
}
