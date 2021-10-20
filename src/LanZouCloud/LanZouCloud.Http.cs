using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LanZouCloudAPI
{
    public partial class LanZouCloud
    {
        private const int http_retries = 3;
        private CookieContainer cookieContainer = new CookieContainer();
        private void _set_cookie(string domain, string name, string value)
        {
            cookieContainer.Add(new Cookie(name, value, null, domain));
        }

        // 需要自己处理超时重试 ！！！
        private HttpClient _get_client(Dictionary<string, string> headers = null,
            float timeout = 0, bool allowRedirect = true, string proxy = null)
        {
            var handler = new HttpClientHandler();
            handler.UseCookies = true;
            handler.AllowAutoRedirect = allowRedirect;
            handler.CookieContainer = cookieContainer;

            var client = new HttpClient(handler, true);

            headers = headers ?? _headers;

            if (headers != null)
            {
                foreach (var item in headers)
                {
                    client.DefaultRequestHeaders.Add(item.Key, item.Value);
                }
            }

            timeout = timeout > 0 ? timeout : _timeout;
            client.Timeout = new TimeSpan((long)(timeout * 10000000L));

            proxy = proxy ?? _proxy;
            if (proxy != null)
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(proxy);
            }

            return client;
        }

        #region 网络超时，自动重试
        private async Task<string> _get_text(string url)
        {
            url = fix_url_domain(url);
            string text = null;
            for (int i = 0; i < http_retries; i++)
            {
                try
                {
                    using (var client = _get_client())
                    {
                        using (var resp = await client.GetAsync(url))
                        {
                            resp.EnsureSuccessStatusCode();
                            text = await resp.Content.ReadAsStringAsync();
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    Log($"Http Error: {ex.Message}", LogLevel.Error, nameof(_get_text));
                    if (i < http_retries) Log($"Retry({i + 1}): {url}", LogLevel.Info, nameof(_get_text));
                }
            }
            return text;
        }

        private async Task<string> _post_text(string url, Dictionary<string, string> data)
        {
            url = fix_url_domain(url);
            string text = null;
            for (int i = 0; i < http_retries; i++)
            {
                try
                {
                    using (var client = _get_client())
                    {
                        using (var content = new FormUrlEncodedContent(data))
                        {
                            using (var resp = await client.PostAsync(url, content))
                            {
                                resp.EnsureSuccessStatusCode();
                                text = await resp.Content.ReadAsStringAsync();
                            }
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    Log($"Http Error: {ex.Message}", LogLevel.Error, nameof(_post_text));
                    if (i < http_retries) Log($"Retry({i + 1}): {url}", LogLevel.Info, nameof(_post_text));
                }
            }
            return text;
        }

        private async Task<HttpContentHeaders> _get_headers(string url)
        {
            url = fix_url_domain(url);
            HttpContentHeaders content_headers = null;
            for (int i = 0; i < http_retries; i++)
            {
                try
                {
                    using (var client = _get_client(null, 0, false))
                    {
                        using (var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                        {
                            // dont ensure success code, casue code is 502 Bad Gateway
                            content_headers = resp.Content.Headers;
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    Log($"Http Error: {ex.Message}", LogLevel.Error, nameof(_get_headers));
                    if (i < http_retries) Log($"Retry({i + 1}): {url}", LogLevel.Info, nameof(_get_headers));
                }
            }
            return content_headers;
        }
        #endregion

        private string[] available_domains = new string[]
        {
            "lanzoui.com",  // 鲁ICP备15001327号-6, 2020-06-09, SEO 排名最低
            "lanzoux.com",  // 鲁ICP备15001327号-5, 2020-06-09
            "lanzous.com",  // 主域名, 备案异常, 部分地区已经无法访问
        };

        private string fix_url_domain(string url, int index = 0)
        {
            if (!url.Contains("lanzous.com")) return url;
            return url.Replace("lanzous.com", available_domains[index]);
        }



        #region 辅助类
        internal class UTF8EncodingStreamContent : StreamContent
        {
            string fileName;

            internal UTF8EncodingStreamContent(Stream content, string name, string fileName) : base(content)
            {
                this.fileName = fileName;
                var fn = new StringBuilder();
                foreach (var b in Encoding.UTF8.GetBytes(fileName))
                {
                    fn.Append((char)b);
                }
#if UNITY_5_3_OR_NEWER
                Headers.Add("Content-Type", "application/octet-stream");
                Headers.Add("Content-Disposition", $"form-data; name=\"{name}\"; filename=\"{fn}\"");
#else

                Headers.Add("Content-Type", "application/octet-stream");
                Headers.Add("Content-Disposition", $"form-data; name=\"{name}\"; filename=\"{fn}\"");
#endif
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
#if UNITY_5_3_OR_NEWER
                stream.Position = 0;
                var header = new StreamReader(stream).ReadToEnd();
                var h = header.IndexOf("filename=\"") + "filename=\"".Length;
                var t = header.IndexOf("\"", h);
                var newHeader = header.Substring(0, h) + fileName + header.Substring(t);
                var bytes = Encoding.UTF8.GetBytes(newHeader);
                stream.Position = 0;
                stream.Write(bytes, 0, bytes.Length);
#endif
                return base.SerializeToStreamAsync(stream, context);
            }

            protected override bool TryComputeLength(out long length)
            {
                return base.TryComputeLength(out length);
            }
        }

        internal class ProgressableStreamContent : HttpContent
        {
            private HttpContent content;
            private int bufferSize;
            private Action<long, long> progress;

            internal ProgressableStreamContent(HttpContent content, int bufferSize, Action<long, long> progress)
            {
                this.content = content;
                this.bufferSize = bufferSize;
                this.progress = progress;

                foreach (var h in content.Headers)
                {
                    Headers.Add(h.Key, h.Value);
                }
            }

            protected override Task SerializeToStreamAsync(Stream netStream, TransportContext context)
            {
                return Task.Run(async () =>
                {
                    var buffer = new byte[bufferSize];
                    TryComputeLength(out var total);
                    var current = 0;
                    using (var fileStream = await content.ReadAsStreamAsync())
                    {
                        while (true)
                        {
                            var length = await fileStream.ReadAsync(buffer, 0, bufferSize);
                            if (length == 0)
                                break;

                            await netStream.WriteAsync(buffer, 0, length);
                            current += length;

                            progress?.Invoke(current, total);
                        }
                    }
                });
            }

            protected override bool TryComputeLength(out long length)
            {
                length = content.Headers.ContentLength.GetValueOrDefault();
                return true;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    content.Dispose();
                }
                base.Dispose(disposing);
            }
        }
        #endregion
    }
}
