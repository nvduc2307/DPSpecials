using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DPSpecial.Tools.Login.Licensing
{
    /// <summary>
    ///     Gọi license server bằng HttpWebRequest (System.dll). Không dùng System.Net.Http để tránh
    ///     xung đột phiên bản với bản đã được Revit nạp sẵn (R22-R24 chạy .NET Framework).
    /// </summary>
    internal sealed class LicenseServerClient
    {
        private const int RequestTimeoutMilliseconds = 15000;
        private const int MaximumAttempts = 3;
        private const int RetryDelayMilliseconds = 250;
        private const int MaximumRedirects = 5;
        private readonly string _apiUrl;

        static LicenseServerClient()
        {
#if NETFRAMEWORK
            // Google chỉ nhận TLS 1.2 trở lên; chỉ bổ sung, không bỏ giao thức nào.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#endif
        }

        public LicenseServerClient(string apiUrl)
        {
            _apiUrl = apiUrl;
        }

        public Task<LicenseServerCallResult> SendAsync(
            string action,
            string credential,
            string deviceHash)
        {
            return Task.Run(() => Send(action, credential, deviceHash));
        }

        private LicenseServerCallResult Send(
            string action,
            string credential,
            string deviceHash)
        {
            LicenseServerRequest request = new LicenseServerRequest
            {
                Action = action,
                Credential = credential,
                DeviceHash = deviceHash,
                Product = LeaseVerifier.ProductName,
                RequestId = Guid.NewGuid().ToString("N")
            };
            byte[] body = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(request));
            string lastError = "Không kết nối được dịch vụ xác nhận.";

            for (int attempt = 1; attempt <= MaximumAttempts; attempt++)
            {
                try
                {
                    int status;
                    string responseJson = PostWithRedirects(body, out status);

                    LicenseServerResponse? serverResponse = null;
                    try
                    {
                        serverResponse = JsonConvert.DeserializeObject<
                            LicenseServerResponse>(responseJson);
                    }
                    catch (JsonException)
                    {
                    }

                    // Server trả JSON cho cả lỗi nghiệp vụ; chỉ lỗi hạ tầng mới thử lại.
                    if (serverResponse != null &&
                        !string.IsNullOrEmpty(serverResponse.Code) &&
                        status != 500)
                    {
                        return LicenseServerCallResult.Reachable(
                            serverResponse);
                    }

                    lastError =
                        "Dịch vụ xác nhận trả về HTTP " + status + ".";
                    if (!ShouldRetry(status) || attempt == MaximumAttempts)
                    {
                        return LicenseServerCallResult.Unreachable(lastError);
                    }
                }
                catch (WebException exception)
                {
                    lastError = exception.Status == WebExceptionStatus.Timeout
                        ? "Kết nối dịch vụ xác nhận quá thời gian " +
                          (RequestTimeoutMilliseconds / 1000) + " giây."
                        : "Không kết nối được dịch vụ xác nhận: " +
                          exception.Message;
                }
                catch (IOException exception)
                {
                    lastError =
                        "Không kết nối được dịch vụ xác nhận: " +
                        exception.Message;
                }

                if (attempt < MaximumAttempts)
                {
                    System.Threading.Thread.Sleep(RetryDelayMilliseconds);
                }
            }

            return LicenseServerCallResult.Unreachable(lastError);
        }

        private string PostWithRedirects(byte[] body, out int statusCode)
        {
            Uri uri = new Uri(_apiUrl);
            bool isPost = true;

            for (int redirect = 0; redirect <= MaximumRedirects; redirect++)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.Timeout = RequestTimeoutMilliseconds;
                request.ReadWriteTimeout = RequestTimeoutMilliseconds;
                request.AllowAutoRedirect = false;
                request.UserAgent = "DPSpecial";
                if (isPost)
                {
                    request.Method = "POST";
                    request.ContentType = "application/json; charset=utf-8";
                    request.ContentLength = body.Length;
                    using (Stream stream = request.GetRequestStream())
                    {
                        stream.Write(body, 0, body.Length);
                    }
                }
                else
                {
                    request.Method = "GET";
                }

                HttpWebResponse response;
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException exception)
                    when (exception.Response is HttpWebResponse errorResponse)
                {
                    response = errorResponse;
                }

                using (response)
                {
                    statusCode = (int)response.StatusCode;
                    if (IsRedirect(statusCode))
                    {
                        string location = response.Headers["Location"];
                        Uri next;
                        if (string.IsNullOrEmpty(location) ||
                            !Uri.TryCreate(uri, location, out next) ||
                            !IsTrustedGoogleRedirect(next))
                        {
                            return ReadBody(response);
                        }

                        // Apps Script chuyển POST sang một URL GET chứa kết quả.
                        uri = next;
                        isPost = false;
                        continue;
                    }

                    return ReadBody(response);
                }
            }

            statusCode = 0;
            return string.Empty;
        }

        private static string ReadBody(HttpWebResponse response)
        {
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(
                       stream,
                       Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static bool IsRedirect(int value)
        {
            return value == 301 ||
                   value == 302 ||
                   value == 303 ||
                   value == 307 ||
                   value == 308;
        }

        private static bool ShouldRetry(int value)
        {
            return value == 0 ||
                   value == 408 ||
                   value == 429 ||
                   value == 500 ||
                   value == 502 ||
                   value == 503 ||
                   value == 504;
        }

        private static bool IsTrustedGoogleRedirect(Uri uri)
        {
            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            string host = uri.Host;
            return host.Equals(
                       "script.google.com",
                       StringComparison.OrdinalIgnoreCase) ||
                   host.EndsWith(
                       ".googleusercontent.com",
                       StringComparison.OrdinalIgnoreCase);
        }
    }

    internal sealed class LicenseServerCallResult
    {
        private LicenseServerCallResult(
            bool isReachable,
            string errorMessage,
            LicenseServerResponse? response)
        {
            IsReachable = isReachable;
            ErrorMessage = errorMessage;
            Response = response;
        }

        public bool IsReachable { get; }
        public string ErrorMessage { get; }
        public LicenseServerResponse? Response { get; }

        public static LicenseServerCallResult Reachable(
            LicenseServerResponse response)
        {
            return new LicenseServerCallResult(true, string.Empty, response);
        }

        public static LicenseServerCallResult Unreachable(string message)
        {
            return new LicenseServerCallResult(false, message, null);
        }
    }
}
