using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LanZouAPI
{
    public partial class LanZouCloud
    {
        private async Task<string> _get_text(string url)
        {
            url = fix_url_domain(url);
            string text = null;
            using (var client = _get_client())
            {
                text = await client.GetStringAsync(url);
            }
            return text;
        }

        private async Task<string> _post_text(string url, Dictionary<string, string> data)
        {
            url = fix_url_domain(url);
            string text = null;
            using (var client = _get_client())
            {
                var content = new FormUrlEncodedContent(data);
                var resp = await client.PostAsync(url, content);
                text = await resp.Content.ReadAsStringAsync();
            }
            return text;
        }

        private async Task<HttpResponseMessage> _get_resp(string url, Dictionary<string, string> headers = null,
            float timeout = 0, bool allowRedirect = true, string proxy = null, bool req_header = false)
        {
            url = fix_url_domain(url);
            var req_op = req_header ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead;
            using (var client = _get_client(headers, timeout, allowRedirect, proxy))
            {
                return await client.GetAsync(url, req_op);
            }
        }


        internal class ProgressableStreamContent : HttpContent
        {
            private const int defaultBufferSize = 4096;

            private HttpContent content;
            private int bufferSize;
            private Action<long, long> progress;

            public ProgressableStreamContent(HttpContent content, Action<long, long> progress)
                : this(content, defaultBufferSize, progress) { }

            public ProgressableStreamContent(HttpContent content, int bufferSize, Action<long, long> progress)
            {
                this.content = content;
                this.bufferSize = bufferSize;
                this.progress = progress;

                foreach (var h in content.Headers)
                {
                    this.Headers.Add(h.Key, h.Value);
                }
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                return Task.Run(async () =>
                {
                    var buffer = new byte[bufferSize];
                    TryComputeLength(out var size);
                    var uploaded = 0;
                    using (var sinput = await content.ReadAsStreamAsync())
                    {
                        while (true)
                        {
                            var length = sinput.Read(buffer, 0, bufferSize);
                            if (length == 0) break;
                            stream.Write(buffer, 0, length);
                            uploaded += length;
                            progress?.Invoke(uploaded, size);
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

        private CookieContainer cookieContainer = new CookieContainer();
        private void _set_cookie(string domain, string name, string value)
        {
            cookieContainer.Add(new Cookie(name, value, null, domain));
        }

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

            if (timeout > 0)
            {
                client.Timeout = new TimeSpan((long)(timeout * 10000000L));
            }
            else
            {
                timeout = _timeout;
            }

            proxy = proxy ?? _proxy;
            if (proxy != null)
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(proxy);
            }

            return client;
        }


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
    }
}
