using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LanZouAPI
{
    public class Http
    {
        private CookieContainer cookieContainer;
        private Dictionary<string, string> defaultHeaders;
        private float defaultTimeout;

        public Http()
        {
            cookieContainer = new CookieContainer();
        }

        private HttpClient GetClient(Dictionary<string, string> headers,
            float timeout, bool allowRedirect, string proxy)
        {
            var handler = new HttpClientHandler();
            handler.UseCookies = true;
            handler.AllowAutoRedirect = allowRedirect;
            handler.CookieContainer = cookieContainer;

            if (proxy != null)
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(proxy);
            }

            var client = new HttpClient(handler, true);

            timeout = timeout > 0 ? timeout : defaultTimeout;
            if (timeout > 0)
            {
                client.Timeout = new TimeSpan((long)(timeout * 10000000L));
            }

            headers = headers ?? defaultHeaders;
            if (headers != null)
            {
                foreach (var item in headers)
                {
                    client.DefaultRequestHeaders.Add(item.Key, item.Value);
                }
            }

            return client;
        }

        public void SetDefaultTimeout(float timeout)
        {
            defaultTimeout = timeout;
        }

        public void SetDefaultHeaders(Dictionary<string, string> headers)
        {
            defaultHeaders = headers;
        }

        public void SetCookie(string domain, string name, string value)
        {
            cookieContainer.Add(new Cookie(name, value, null, domain));
        }

        public HttpResponseMessage Get(string url, Dictionary<string, string> headers = null,
            float timeout = 0, bool allowRedirect = true, string proxy = null, bool headersOnly = false)
        {
            return GetClient(headers, timeout, allowRedirect, proxy).GetAsync(url,
                headersOnly ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead).Result;
        }

        public HttpContentHeaders GetHeaders(string url, Dictionary<string, string> headers = null,
            float timeout = 0, bool allowRedirect = true, string proxy = null)
        {
            return GetClient(headers, timeout, allowRedirect, proxy).GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result.Content.Headers;
        }

        public Stream GetStream(string url, Dictionary<string, string> headers = null,
            float timeout = 0, bool allowRedirect = true, string proxy = null)
        {
            return GetClient(headers, timeout, allowRedirect, proxy).GetStreamAsync(url).Result;
        }

        public string GetString(string url, Dictionary<string, string> headers = null,
            float timeout = 0, bool allowRedirect = true, string proxy = null)
        {
            return GetClient(headers, timeout, allowRedirect, proxy).GetStringAsync(url).Result;
        }

        public string PostString(string url, Dictionary<string, string> data,
            Dictionary<string, string> headers = null, float timeout = 0, bool allowRedirect = true, string proxy = null)
        {
            var content = new FormUrlEncodedContent(data);
            return GetClient(headers, timeout, allowRedirect, proxy).
                PostAsync(url, content).Result.Content.ReadAsStringAsync().Result;
        }

        public string PostUpload(string url, Dictionary<string, string> data, Stream stream, string fileName, string filetag = "file", Action<long, long> progress = null,
            Dictionary<string, string> headers = null, float timeout = 0, bool allowRedirect = true, string proxy = null)
        {
            var _content = new MultipartFormDataContent();
            foreach (var item in data)
            {
                _content.Add(new StringContent(item.Value), item.Key);
            }
            _content.Add(new StreamContent(stream), filetag, fileName);

            HttpContent content;
            if (progress != null)
            {
                content = new ProgressableStreamContent(_content, stream, progress);
            }
            else
            {
                content = _content;
            }

            return GetClient(headers, timeout, allowRedirect, proxy).
                PostAsync(url, content).Result.Content.ReadAsStringAsync().Result;
        }

        public void Download(string url, string path, IProgress<long[]> progress = null, Dictionary<string, string> headers = null,
            float timeout = 0, bool allowRedirect = true, string proxy = null)
        {
        }
    }

    internal class ProgressableStreamContent : HttpContent
    {
        private const int defaultBufferSize = 4096;

        private HttpContent content;
        private int bufferSize;
        private Action<long, long> progress;

        public ProgressableStreamContent(HttpContent content, Stream stream, Action<long, long> progress)
            : this(content, stream, defaultBufferSize, progress) { }

        public ProgressableStreamContent(HttpContent content, Stream stream, int bufferSize, Action<long, long> progress)
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
}
