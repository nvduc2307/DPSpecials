using DPSpecial.Tools.Login.Licensing;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPZoneManageViews.action;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPZoneManageViews
{
    [Transaction(TransactionMode.Manual)]
    public class ECPZoneManageViewsCmd : IExternalCommand
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
            using (var tsg = new TransactionGroup(document, "ECPZoneManageViewsCmd"))
            {
                tsg.Start();
                try
                {
                    var action = new ECPZoneManageViewsAction(uiDocument);
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
