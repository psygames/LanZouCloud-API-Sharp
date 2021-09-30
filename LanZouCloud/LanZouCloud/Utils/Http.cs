using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace LanZouCloud
{
    public class Http
    {
        public Task<string> GetText(string url, string proxy = null)
        {
            return InnerHttp.GetText(url, proxy);
        }

        public Task<byte[]> GetBytes(string url, string proxy = null)
        {
            return InnerHttp.GetBytes(url, proxy);
        }

        public Task<bool> Download(string url, string path, IProgress<long[]> progress = null, string proxy = null)
        {
            return InnerHttp.Download(url, path, progress, proxy);
        }

        static class InnerHttp
        {
            // Windows
            // private const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";

            // Android
            private const string userAgent = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.63 Mobile Safari/537.36";
            private const int DOWNLOAD_BUFFER_SIZE = 1024 * 8;
            private static Encoding encode = Encoding.UTF8;

            private static bool isProtocolFixed;
            private static void FixProtocol()
            {
                if (isProtocolFixed) return;
                ServicePointManager.DefaultConnectionLimit = 1000;                          // 连接数限制
                ServicePointManager.ServerCertificateValidationCallback =                   // 证书校验
                    new RemoteCertificateValidationCallback(delegate { return true; });
                try { ServicePointManager.SecurityProtocol = (SecurityProtocolType)4080; }  // SSL支持
                catch { }
                isProtocolFixed = true;
            }

            internal static async Task<bool> Download(string url, string path, IProgress<long[]> progress, string proxy)
            {
                try
                {
                    FixProtocol();

                    var request = WebRequest.Create(url) as HttpWebRequest;
                    request.UserAgent = userAgent;

                    if (!string.IsNullOrEmpty(proxy))
                    {
                        var proxyObject = new WebProxy(proxy);
                        request.Proxy = proxyObject;
                    }

                    var response = await request.GetResponseAsync() as HttpWebResponse;
                    var buffer = new byte[DOWNLOAD_BUFFER_SIZE];
                    var stream = response.GetResponseStream();
                    var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                    fileStream.SetLength(0);
                    var report = new long[2]; // 0-current, 1-total 
                    report[0] = 0;
                    report[1] = response.ContentLength;
                    progress?.Report(report);
                    int readLength = 0;
                    do
                    {
                        readLength = await stream.ReadAsync(buffer, 0, DOWNLOAD_BUFFER_SIZE);
                        fileStream.Write(buffer, 0, readLength);
                        report[0] += readLength;
                        progress?.Report(report);
                    } while (readLength > 0);
                    fileStream.Close();
                    stream.Close();
                    response.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                return false;
            }

            internal static async Task<byte[]> GetBytes(string url, string proxy)
            {
                try
                {
                    FixProtocol();

                    var request = WebRequest.Create(url) as HttpWebRequest;
                    request.UserAgent = userAgent;

                    if (!string.IsNullOrEmpty(proxy))
                    {
                        var proxyObject = new WebProxy(proxy);
                        request.Proxy = proxyObject;
                    }

                    var response = await request.GetResponseAsync() as HttpWebResponse;
                    var buffer = new byte[DOWNLOAD_BUFFER_SIZE];
                    var stream = response.GetResponseStream();
                    var memoryStream = new MemoryStream();
                    int readLength = 0;
                    do
                    {
                        readLength = await stream.ReadAsync(buffer, 0, DOWNLOAD_BUFFER_SIZE);
                        memoryStream.Write(buffer, 0, readLength);
                    } while (readLength > 0);
                    var bytes = memoryStream.ToArray();
                    memoryStream.Close();
                    stream.Close();
                    response.Close();
                    return bytes;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
                return null;
            }

            internal static async Task<string> GetText(string url, string proxy)
            {
                try
                {
                    FixProtocol();

                    var request = WebRequest.Create(url) as HttpWebRequest;
                    request.UserAgent = userAgent;

                    if (!string.IsNullOrEmpty(proxy))
                    {
                        var proxyObject = new WebProxy(proxy);
                        request.Proxy = proxyObject;
                    }

                    var response = await request.GetResponseAsync() as HttpWebResponse;
                    var stream = response.GetResponseStream();
                    var reader = new StreamReader(stream, encode);
                    var text = await reader.ReadToEndAsync();
                    reader.Close();
                    stream.Close();
                    response.Close();
                    return text;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
                return null;
            }
        }
    }
}
