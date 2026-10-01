using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DPSpecial.Tools.ECP.ManageZoneViewsSetting.action;
using DPSpecial.Utils;

namespace DPSpecial.Tools.ECP.ManageZoneViewsSetting
{
    [Transaction(TransactionMode.Manual)]
    public class ManageZoneViewsSettingCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var result = Result.Succeeded;
            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;
            using (var tsg = new TransactionGroup(document, "ManageZoneViewsSettingCmd"))
            {
                tsg.Start();
                try
                {
                    var action = new ManageZoneViewsSettingAction(uiDocument);
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
