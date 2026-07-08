using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPCreate.action;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPCreate
{
    [Transaction(TransactionMode.Manual)]
    public class ECPCreateCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var result = Result.Succeeded;
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;
            using (var tsg = new TransactionGroup(document, "ECPCreateCmd"))
            {
                tsg.Start();
                try
                {
                    var action = new ECPCreateAction(uiDocument);
                    action.Execute();
                    //var fa = uiDocument.Selection.PickElement(document) as FamilyInstance;
                    //if (fa == null)
                    //    throw new Exception();
                    //var solid = fa.GetSolid();
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
