using DPSpecial.Tools.DPWallParameter;
using DPSpecial.Tools.ECP.ECPCreate;
using DPSpecial.Tools.ECP.ECPCreateSchedule;
using DPSpecial.Tools.ECP.ECPFamilyImport;
using DPSpecial.Tools.ECP.ECPModifyWidth;
using DPSpecial.Tools.ECP.ECPShapes;
using DPSpecial.Tools.ECP.ECPZoneInstall;
using DPSpecial.Tools.ECP.ECPZoneManage;
using DPSpecial.Tools.ECP.ECPZoneUpdate;
using Nice3point.Revit.Extensions.UI;
using Nice3point.Revit.Toolkit.External;

namespace DPSpecial
{
    /// <summary>
    ///     Application entry point
    /// </summary>
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            CreateRibbonGeneral();
            CreateRibbonECP();
            CreateRibbonECPSchedule();
        }
        private void CreateRibbonGeneral()
        {
            var panel = Application.CreatePanel("General", "DPSpecial");

            panel.AddPushButton<DPWallParameterCmd>("ECP Parameter")
                .SetImage("/DPSpecial;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/DPSpecial;component/Resources/Icons/RibbonIcon32.png");
        }
        private void CreateRibbonECP()
        {
            var panel = Application.CreatePanel("ECP", "DPSpecial");

            panel.AddPushButton<ECPFamilyImportCmd>("Load Family")
                .SetImage("/DPSpecial;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/DPSpecial;component/Resources/Icons/RibbonIcon32.png");

            panel.AddPushButton<ECPCreateCmd>("Create")
                .SetImage("/DPSpecial;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/DPSpecial;component/Resources/Icons/RibbonIcon32.png");

            panel.AddPushButton<ECPModifyWidthCmd>("Modify")
                .SetImage("/DPSpecial;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/DPSpecial;component/Resources/Icons/RibbonIcon32.png");
        }
        private void CreateRibbonECPSchedule()
        {
            var panel = Application.CreatePanel("ECP Schedule", "DPSpecial");

            panel.AddPushButton<ECPShapeCmd>("shape")
                .SetImage("/DPSpecial;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/DPSpecial;component/Resources/Icons/RibbonIcon32.png");

            panel.AddPushButton<ECPZoneManageCmd>("manage\nzone")
                .SetImage("/DPSpecial;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/DPSpecial;component/Resources/Icons/RibbonIcon32.png");

            panel.AddPushButton<ECPZoneInstallCmd>("install\nzone")
                .SetImage("/DPSpecial;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/DPSpecial;component/Resources/Icons/RibbonIcon32.png");

            panel.AddPushButton<ECPZoneUpdateCmd>("update\nzone")
                .SetImage("/DPSpecial;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/DPSpecial;component/Resources/Icons/RibbonIcon32.png");

            panel.AddPushButton<ECPCreateScheduleCmd>("create\nschedule")
                .SetImage("/DPSpecial;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/DPSpecial;component/Resources/Icons/RibbonIcon32.png");

        }
    }
}