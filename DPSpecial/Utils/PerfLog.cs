using System.IO;

namespace DPSpecial.Utils
{
    /// <summary>
    ///     Ghi mốc thời gian vào %LocalAppData%\DPSpecial\perf.log để tìm đoạn chậm.
    ///     Mọi lỗi ghi file đều bị bỏ qua, không ảnh hưởng lệnh.
    /// </summary>
    public static class PerfLog
    {
        private static readonly object Sync = new object();

        public static string FilePath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DPSpecial",
                "perf.log");

        public static void Write(string message)
        {
            try
            {
                lock (Sync)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                    File.AppendAllText(
                        FilePath,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + message + Environment.NewLine);
                }
            }
            catch
            {
            }
        }
    }
}
