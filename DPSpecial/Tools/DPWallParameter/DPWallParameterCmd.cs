using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DPSpecial.Utils;

namespace DPSpecial.Tools.DPWallParameter
{
    [Transaction(TransactionMode.Manual)]
    public class DPWallParameterCmd : IExternalCommand
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
                    DPWallParameterAction.AddParameter(document);
                    IO.ShowInfo("Complete!");
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
