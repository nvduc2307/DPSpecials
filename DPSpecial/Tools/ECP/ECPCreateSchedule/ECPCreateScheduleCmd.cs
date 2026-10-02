using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPCreateSchedule.action;
using DPSpecial.Tools.ECP.ECPZoneUpdate.action;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPCreateSchedule
{
    [Transaction(TransactionMode.Manual)]
    public class ECPCreateScheduleCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            var result = Result.Succeeded;
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;
            using (var tsg = new TransactionGroup(document, "Command"))
            {
                tsg.Start();
                try
                {
                    var zoneUpdateCheck = new ECPZoneUpdateAction(uiDocument);
                    if (zoneUpdateCheck.HasZoneChanged())
                        throw new Exception("Zone definitions have been changed.\nPlease run \"Update Zone\" before creating schedule.");

                    var action = new ECPCreateScheduleAction(uiDocument);
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
