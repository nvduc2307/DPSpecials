using DPSpecial.Tools.Login.Licensing;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ECPZoneManageViewsSetting.action;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ECPZoneManageViewsSetting
{
    [Transaction(TransactionMode.Manual)]
    public class ECPZoneManageViewsSettingCmd : IExternalCommand
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
            using (var tsg = new TransactionGroup(document, "ECPZoneManageViewsSettingCmd"))
            {
                tsg.Start();
                try
                {
                    var action = new ECPZoneManageViewsSettingAction(uiDocument);
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
