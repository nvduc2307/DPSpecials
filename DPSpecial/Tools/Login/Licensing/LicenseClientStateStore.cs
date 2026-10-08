using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace DPSpecial.Tools.Login.Licensing
{
    internal static class LicenseClientStateStore
    {
        private static readonly byte[] Entropy =
            Encoding.UTF8.GetBytes("DPSpecial.RuntimeState.v1");

        public static string StateDirectory
        {
            get
            {
                string overrideDirectory =
                    Environment.GetEnvironmentVariable(
                        "DPSPECIAL_STATE_DIRECTORY") ?? string.Empty;
                return !string.IsNullOrWhiteSpace(overrideDirectory)
                    ? overrideDirectory.Trim()
                    : Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.LocalApplicationData),
                        "DPSpecial");
            }
        }

        public static string StateFilePath =>
            Path.Combine(StateDirectory, "runtime-state.dat");

        public static LicenseClientState? Load()
        {
            try
            {
                if (!File.Exists(StateFilePath))
                {
                    return null;
                }

                byte[] plainBytes = ProtectedData.Unprotect(
                    File.ReadAllBytes(StateFilePath),
                    Entropy,
                    DataProtectionScope.CurrentUser);
                return JsonConvert.DeserializeObject<LicenseClientState>(
                    Encoding.UTF8.GetString(plainBytes));
            }
            catch
            {
                return null;
            }
        }

        public static void Save(LicenseClientState state)
        {
            Directory.CreateDirectory(StateDirectory);
            string temporaryPath = StateFilePath + ".tmp";
            byte[] protectedBytes = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(state, Formatting.None)),
                Entropy,
                DataProtectionScope.CurrentUser);
            File.WriteAllBytes(temporaryPath, protectedBytes);

            if (File.Exists(StateFilePath))
            {
                File.Replace(temporaryPath, StateFilePath, null);
            }
            else
            {
                File.Move(temporaryPath, StateFilePath);
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(StateFilePath))
                {
                    File.Delete(StateFilePath);
                }
            }
            catch
            {
            }
        }
    }
}
