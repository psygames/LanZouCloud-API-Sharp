using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
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
            float timeout = 0, bool allowRedirect = true, string proxy = null, bool getHeaders = false)
        {
            var res = GetClient(headers, timeout, allowRedirect, proxy).GetAsync(url,
                getHeaders ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead);
            res.Wait();
            return res.Result;
        }

        public string GetString(string url, Dictionary<string, string> headers = null,
            float timeout = 0, bool allowRedirect = true, string proxy = null)
        {
            var res = GetClient(headers, timeout, allowRedirect, proxy).GetStringAsync(url);
            res.Wait();
            return res.Result;
        }

        public string PostString(string url, Dictionary<string, string> data,
            Dictionary<string, string> headers = null, float timeout = 0, bool allowRedirect = true, string proxy = null)
        {
            var content = new FormUrlEncodedContent(data);
            var res = GetClient(headers, timeout, allowRedirect, proxy).PostAsync(url, content);
            res.Wait();
            var text = res.Result.Content.ReadAsStringAsync();
            return text.Result;
        }

        public string PostUpload(string url, Dictionary<string, string> data, Stream stream, string fileName, string filetag = "file",
            Dictionary<string, string> headers = null, float timeout = 0, bool allowRedirect = true, string proxy = null)
        {
            var content = new MultipartFormDataContent();
            foreach (var item in data)
            {
                content.Add(new StringContent(item.Value), item.Key);
            }
            content.Add(new StreamContent(stream), filetag, fileName);
            var res = GetClient(headers, timeout, allowRedirect, proxy).PostAsync(url, content);
            res.Wait();
            var text = res.Result.Content.ReadAsStringAsync();
            return text.Result;
        }

        public void Download(string url, string path, IProgress<long[]> progress = null, Dictionary<string, string> headers = null,
            float timeout = 0, bool allowRedirect = true, string proxy = null)
        {
        }
    }

    internal class ProgressableStreamContent : HttpContent
    {

        /// <summary> 
        /// Lets keep buffer of 20kb 
        /// </summary> 
        private const int defaultBufferSize = 5 * 4096;

        private HttpContent content;
        private int bufferSize;
        //private bool contentConsumed; 
        private Action<long, long> progress;

        public ProgressableStreamContent(HttpContent content, Action<long, long> progress) : this(content, defaultBufferSize, progress) { }

        public ProgressableStreamContent(HttpContent content, int bufferSize, Action<long, long> progress)
        {
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException("bufferSize");
            }

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
                var buffer = new Byte[this.bufferSize];
                long size;
                TryComputeLength(out size);
                var uploaded = 0;


                using (var sinput = await content.ReadAsStreamAsync())
                {
                    while (true)
                    {
                        var length = sinput.Read(buffer, 0, buffer.Length);
                        if (length <= 0) break;

                        //downloader.Uploaded = uploaded += length; 
                        uploaded += length;
                        progress?.Invoke(uploaded, size);

                        //System.Diagnostics.Debug.WriteLine($"Bytes sent {uploaded} of {size}"); 

                        stream.Write(buffer, 0, length);
                        stream.Flush();
                    }
                }
                stream.Flush();
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
