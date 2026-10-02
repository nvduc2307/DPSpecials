using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.action;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail
{
    [Transaction(TransactionMode.Manual)]
    public class ECPCreateScheduleOrderDetailCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var result = Result.Succeeded;
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;
            using (var tsg = new TransactionGroup(document, "ECPCreateScheduleOrderDetailCmd"))
            {
                tsg.Start();
                try
                {
                    var action = new ECPCreateScheduleOrderDetailAction(uiDocument);
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
