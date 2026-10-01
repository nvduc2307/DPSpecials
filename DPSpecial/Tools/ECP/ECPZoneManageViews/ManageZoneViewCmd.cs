using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ManageZoneViews.action;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ManageZoneViews
{
    [Transaction(TransactionMode.Manual)]
    public class ManageZoneViewCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var result = Result.Succeeded;
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;
            using (var tsg = new TransactionGroup(document, "ManageZoneViewCmd"))
            {
                tsg.Start();
                try
                {
                    var action = new ManageZoneViewAction(uiDocument);
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
