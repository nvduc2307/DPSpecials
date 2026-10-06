using Autodesk.Revit.UI;
using DPSpecial.MVVM.Models;

namespace DPSpecial.Tools.ECP.ECPCreateScheduleOrderDetail.model
{
    public class WallImage
    {
        public const string WALL_PARAM_ARROW_HAS = "1 オスカット";
        public const string WALL_PARAM_ARROW_HAS_NOT = "4 オス側カット";
        public const string IMG_START_ARROW_END_ARROW = "START_ARROW_END_ARROW";
        public const string IMG_START_ARROW_END_NORMAL = "START_ARROW_END_NORMAL";
        public const string IMG_START_ARROW_END_CLOSE = "START_ARROW_END_CLOSE";
        public const string IMG_START_CLOSE_END_NORMAL = "START_CLOSE_END_NORMAL";
        public const string IMG_START_CLOSE_END_CLOSE = "START_CLOSE_END_CLOSE";
        public const string IMG_START_SLOPE_END_NORMAL = "START_SLOPE_END_NORMAL";
        public const string IMG_START_SLOPE_END_CLOSE = "START_SLOPE_END_CLOSE";

        public static string GetImageWall(FamilyInstance wall)
        {
            var result = string.Empty;
            if (wall == null) return result;
            var nameStype = wall.Symbol.FamilyName;
            var isEndWallClose = IsEndWallClose(wall);
            if (START_ARROW_END_NORMALS.Any(x => x == nameStype))
            {
                var wallParamArrowHas = wall.LookupParameter(WALL_PARAM_ARROW_HAS);
                var hasArrow = true;
                if(wallParamArrowHas != null)
                    hasArrow = wallParamArrowHas.AsInteger() == 0;
                return isEndWallClose 
                    ? hasArrow ? IMG_START_ARROW_END_NORMAL : IMG_START_CLOSE_END_NORMAL
                    : IMG_START_ARROW_END_CLOSE;
            }    
            if (START_2ARROW_END_NORMALS.Any(x => x == nameStype))
                return result;
            if (START_2ARROW_END_2ARROW.Any(x => x == nameStype))
                return result;
            if (START_CLOSE_END_ARROW.Any(x => x == nameStype))
                return result;
            if (START_ARROW_END_ARROW.Any(x => x == nameStype))
                return IMG_START_ARROW_END_ARROW;
            if (START_CLOSE_END_NORMALS.Any(x => x == nameStype))
            {
                var wallParamArrowHasNot = wall.LookupParameter(WALL_PARAM_ARROW_HAS_NOT);
                var hasNotArrow = true;
                if (wallParamArrowHasNot != null)
                    hasNotArrow = wallParamArrowHasNot.AsInteger() == 1;
                return isEndWallClose 
                    ? hasNotArrow ? IMG_START_CLOSE_END_NORMAL : IMG_START_ARROW_END_NORMAL
                    : IMG_START_CLOSE_END_CLOSE;
            }    
            if (START_CLOSE_END_CIRCLE.Any(x => x == nameStype))
                return result;
            if (START_SLOPE_END_NORMALS.Any(x => x == nameStype))
                return isEndWallClose ? IMG_START_SLOPE_END_NORMAL : IMG_START_SLOPE_END_CLOSE;
            if(START_ARROW_END_NORMALS_WALL_VERTICAL.Any(x => x == nameStype))
            {
                var wallParamArrowHas = wall.LookupParameter(WALL_PARAM_ARROW_HAS);
                var hasArrow = true;
                if (wallParamArrowHas != null)
                    hasArrow = wallParamArrowHas.AsInteger() == 0;
                return isEndWallClose
                    ? hasArrow ? IMG_START_ARROW_END_NORMAL : IMG_START_CLOSE_END_NORMAL
                    : IMG_START_ARROW_END_CLOSE;
            }
            return result;
        }
        private static bool IsEndWallClose(FamilyInstance wall)
        {
            var paraWidthMax = wall.Symbol.LookupParameter(WallParameterName.WidthMax);
            var paraWidth = wall.LookupParameter(WallParameterName.Width);
            if (paraWidthMax == null) return false;
            if (paraWidth == null) return false;
            var widthMax = Math.Round(paraWidthMax.AsDouble().ToMillimeters(), 0);
            var width = Math.Round(paraWidth.AsDouble().ToMillimeters(), 0);
            if(width < widthMax) return true;
            return false;
        }

        public static List<string> START_ARROW_END_NORMALS = new List<string>()
        {
            "ECP_MNH-5045A(メス側カット)",
            "ECP_MNH-5050A(メス側カット)",
            "ECP_MNH-5060A(メス側カット)",
            "ECP_MNH-60100A(メス側カット)",
            "ECP_MNH-60120A(オス側カット)",
            "ECP_MNH-60120A(メス側カット)",
            "ECP_MNH-6045A(メス側カット)",
            "ECP_MNH-6050A(メス側カット)",
            "ECP_MNH6060A(メス側カット)",
            "ECP_MNH-6080A(メス側カット)",
            "ECP_MNH-6090B1(メス側カット)",
            "ECP_MNH-7550A(メス側カット)",
            "ECP_MNH-7560A(メス側カット)",
            "ECP_MNH-7580A(メス側カット)",
            "ECP_MNH-7590B1(メス側カット)",
            "ECP_MNH-6040B1  (タイル接着剤張用）",
            "ECP_MNH-6050B1（タイル接着剤張用）",
            "ECP_MNH-6059B1（タイル接着剤張用）",
            "ECP_MNH-6060B1（タイル接着剤張用）",
            "ECP_MNH-6090B2  (タイル接着剤張用）",
            "ECP_MNH-7560B1（タイル接着剤張用）",
            "ECP_MNT-62121A",
            "ECP_MNT-6240B1",
            "ECP_MNT-6250B1",
            "ECP_MNT-6256A",
            "ECP_MNT-6259A",
            "ECP_MNT-6260B1",
            "ECP_MNT-6290B1",
            "ECP_MNT-6556A",
            "ECP_MNT-7760B1",
            "ECP_MNT-7790B1",
            "ECP_MNE-7560A",
            "ECP_MNG-6260A",
            "ECP_MNH-7560D",
            "ECP_MNP1-7560みなも",
            "ECP_MNP2-7560なごみ",
            "ECP_MNP3-10060やまなみ",
            "ECP_MNP4-7560なぎさ",
            "ECP_MNY50-8560",
            "ECP_MNY56-7560",
            "ECP_MNY58-9060",
            "ECP_MNY60-7060",
            "ECP_MNY60-7553",
            "ECP_MNY60-7560(ハイレーン)",
            "ECP_MNY60-7590",
            "ECP_MNY61-7560(リップル)",
            "ECP_MNY61-7590",
            "ECP_MNH-60100A",
            "ECP_MNH-60120A",
            "ECP_MNH6060A",
            "ECP_MNH-6090B1"
        };
        public static List<string> START_2ARROW_END_NORMALS = new List<string>()
        {
            "ECP_MNH-10060A(メス側カット)",
            "ECP_MNT-10260B1",
        };
        public static List<string> START_2ARROW_END_2ARROW = new List<string>()
        {
            "ECP_MNTW-10260B1",
        };
        public static List<string> START_CLOSE_END_ARROW = new List<string>()
        {
            "ECP_MNH-6060T(オス側カット)",
        };
        public static List<string> START_ARROW_END_ARROW = new List<string>()
        {
            "ECP_MNH-6060T(メス側カット)",
        };
        public static List<string> START_CLOSE_END_NORMALS = new List<string>()
        {
            "ECP_MNH-10060A(オス側カット)",
            "ECP_MNH-5045A(オス側カット)",
            "ECP_MNH-5050A(オス側カット)",
            "ECP_MNH-5060A(オス側カット)",
            "ECP_MNH-60100A(オス側カット)",
            "ECP_MNH-6045A(オス側カット)",
            "ECP_MNH-6050A(オス側カット)",
            "ECP_MNH6060A(オス側カット)",
            "ECP_MNH-6080A(オス側カット)",
            "ECP_MNH-6090B1(オス側カット)",
            "ECP_MNH-7550A(オス側カット)",
            "ECP_MNH-7560A(オス側カット)",
            "ECP_MNH-7580A(オス側カット)",
            "ECP_MNH-7590B1(オス側カット)",
        };

        public static List<string> START_CLOSE_END_CLOSE = new List<string>()
        {
            "ECP_MNFK-6230S3",
            "ECP_MNFK-6245S5",
            "ECP_MNLK-6030S3K",
            "ECP_MNLK-6045S5K",
            "ECP_MNLK-6050S6K",
            "ECP_MNLK-6060S7K",
            "ECP_MNLK-6250S6",
            "ECP_MNLK-6260S7",
            "ECP_MNLK-7545S5K",
            "ECP_MNLK-7550S6K",
            "ECP_MNLK-7560S7K",
            "ECP_MNLK-7745S5",
            "ECP_MNLK-7750S6",
            "ECP_MNLK-7760S7",
            "ECP_MNF-2550A7",
            "ECP_MNF-3530",
            "ECP_MNF-6030",
        };

        public static List<string> START_CLOSE_END_CIRCLE = new List<string>()
        {
            "ECP_MNFK-6030S3",
            "ECP_MNFK-6060S6",
            "ECP_MNFK-7560S6",
        };

        public static List<string> START_SLOPE_END_NORMALS = new List<string>()
        {
            "ECP_MNY45-10059(メス側カット)",
            "ECP_MNY45ー6058(メス側カット)",
            "ECP_MNY60-7560CN　ﾊｲﾚｰﾝ留め",
            "ECP_MNY61-7560CN　ﾘｯﾌﾟﾙ留め",
        };
        public static List<string> START_ARROW_END_NORMALS_WALL_VERTICAL = new List<string>()
        {
            "ECP_ヨコ貼り_左45度カット",
        };
    }
}
