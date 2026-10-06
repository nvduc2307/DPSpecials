using System;
using System.IO;
using Newtonsoft.Json;

namespace DPSpecial.Tools.Login.Licensing
{
    internal sealed class LicenseServerConfiguration
    {
        private const string Placeholder = "REPLACE_WITH_DEPLOYMENT_ID";

        [JsonProperty("endpoint")]
        public string Endpoint { get; set; } = string.Empty;

        public bool IsConfigured =>
            Uri.TryCreate(Endpoint, UriKind.Absolute, out Uri? uri) &&
            uri.Scheme == Uri.UriSchemeHttps &&
            Endpoint.IndexOf(
                Placeholder,
                StringComparison.OrdinalIgnoreCase) < 0;

        public static LicenseServerConfiguration Load()
        {
            string environmentUrl =
                Environment.GetEnvironmentVariable(
                    "DPSPECIAL_LICENSE_API_URL") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(environmentUrl))
            {
                return new LicenseServerConfiguration
                {
                    Endpoint = environmentUrl.Trim()
                };
            }

            string assemblyDirectory =
                Path.GetDirectoryName(
                    typeof(LicenseServerConfiguration).Assembly.Location) ??
                string.Empty;
            string configPath = Path.Combine(
                assemblyDirectory,
                "Resources",
                "Settings",
                "ReleaseChannel.json");

            try
            {
                if (File.Exists(configPath))
                {
                    return JsonConvert.DeserializeObject<
                               LicenseServerConfiguration>(
                               File.ReadAllText(configPath)) ??
                           new LicenseServerConfiguration();
                }
            }
            catch
            {
                // Bên gọi báo lỗi cấu hình.
            }

            return new LicenseServerConfiguration();
        }
    }
}
