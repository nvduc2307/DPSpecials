using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace DPSpecial.Tools.Login.Licensing
{
    internal static class BootstrapCredentialProvider
    {
        private const string ResourceName =
            "DPSpecial.Resources.Settings.ReleaseProfile.dat";

        public static string GetCredential()
        {
            string environmentCredential =
                Environment.GetEnvironmentVariable(
                    "DPSPECIAL_BOOTSTRAP_CREDENTIAL") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(environmentCredential))
            {
                return environmentCredential.Trim();
            }

            try
            {
                Assembly assembly =
                    typeof(BootstrapCredentialProvider).Assembly;
                using (Stream? stream =
                       assembly.GetManifestResourceStream(ResourceName))
                {
                    if (stream == null)
                    {
                        return string.Empty;
                    }

                    using (StreamReader reader = new StreamReader(
                               stream,
                               Encoding.UTF8,
                               true))
                    {
                        string encoded = reader.ReadToEnd().Trim();
                        return Encoding.UTF8.GetString(
                            Convert.FromBase64String(encoded));
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
