using System;
using System.Security.Cryptography;

namespace DPSpecial.Tools.Login.Licensing
{
    internal static class LeasePublicKey
    {
        // Public key RSA-2048 dùng xác thực chữ ký lease do license server cấp.
        // Private key chỉ nằm trên server (license-server/LicenseServer/keys/private.pem).
        // Xem lại bằng: dotnet run -- init (trong license-server/LicenseServer).
        private const string ModulusBase64 =
            "qcGx2HZwoZX6A7l5e2ClmPQzWS/nF10xe0QJKyXBP5YLI03FuF8b+YLdgGgdJcx8" +
            "WK9JbwEDVY+HRJxTet6upPL191BiIe7WqME52/NmOgx369uvfeJTO/slavU1dwLg" +
            "0RyXVlgZVSOdzsdpSBXte4HPrrouaRQfTDhnbczZwDb0x/3WI7UGUeJXLZs6SMIM" +
            "WxmVWHnv4n6p8MtAu8qg9MZ12ZKI7Qd5yrLR5oL8VnAYisw5MlUNvcKPYFiPwzgt" +
            "SOPd+28jmLt30s5z7Weqm+CEGl9fHmXWS7xRUv9YsJtQyvrbKrXiLe+Qfu4GojlX" +
            "3vko9+gPwtsLWbUE2C+eFQ==";
        private const string ExponentBase64 = "AQAB";

        public static RSAParameters CreateParameters()
        {
            return new RSAParameters
            {
                Modulus = Convert.FromBase64String(ModulusBase64),
                Exponent = Convert.FromBase64String(ExponentBase64)
            };
        }
    }
}
