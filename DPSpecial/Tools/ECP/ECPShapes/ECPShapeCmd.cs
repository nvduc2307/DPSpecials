using Autodesk.Revit.Attributes;
using DPSpecial.Tools.Login.Licensing;
using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPShapes.action;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPShapes
{
    [Transaction(TransactionMode.Manual)]
    public class ECPShapeCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var total = System.Diagnostics.Stopwatch.StartNew();
            if (!LicenseGate.EnsureFeature(string.Empty))
            {
                return Result.Cancelled;
            }
            PerfLog.Write($"ECPShapeCmd: kiểm tra license {total.ElapsedMilliseconds} ms");

            var result = Result.Succeeded;
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;
            using (var tsg = new TransactionGroup(document, "Command"))
            {
                tsg.Start();
                try
                {
                    var action = new ECPShapeAction(uiDocument);
                    action.Execute();
                    PerfLog.Write($"ECPShapeCmd: Execute xong, {total.ElapsedMilliseconds} ms");
                    tsg.Assimilate();
                    PerfLog.Write($"ECPShapeCmd: Assimilate xong, {total.ElapsedMilliseconds} ms");
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
