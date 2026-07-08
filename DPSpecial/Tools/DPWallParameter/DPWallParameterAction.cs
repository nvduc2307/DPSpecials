using DPSpecial.MVVM.Models;
using DPSpecial.Utils;
using System.IO;

namespace DPSpecial.Tools.DPWallParameter
{
    public class DPWallParameterAction
    {
        public static void CheckParameter(Element element)
        {
            var hasPara = CheckHasParameter(element);
            if (!hasPara)
                AddParameter(element.Document);
        }
        public static void AddParameter(Document document)
        {
            var pathShareParameter = $"{PathHelper.Parameters}\\ShareParameterBricsWall.txt";
            if (!File.Exists(pathShareParameter)) return;
            using (var ts = new Transaction(document, "AddParameter"))
            {
                ts.SkipAllWarnings();
                ts.Start();
                ParameterHelper
                    .CreateSharedParameters(
                        document,
                        pathShareParameter,
                        BuiltInCategory.OST_GenericModel);
                ts.Commit();
            }
        }
        private static bool CheckHasParameter(Element element)
        {
            if (!ParameterHelper.HasParameter(element, WallParameterName.DPWALL_HOST)) return false;
            return true;
        }
    }
}
