using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

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

        public string PostString(string url, Dictionary<string, string> data, Dictionary<string, string> headers = null,
            float timeout = 0, bool allowRedirect = true, string proxy = null)
        {
            var content = new FormUrlEncodedContent(data);
            var res = GetClient(headers, timeout, allowRedirect, proxy).PostAsync(url, content);
            res.Wait();
            var text = res.Result.Content.ReadAsStringAsync();
            return text.Result;
        }

        public void Download(string url, string path, IProgress<long[]> progress = null, Dictionary<string, string> headers = null,
            float timeout = 0, bool allowRedirect = true, string proxy = null)
        {
            var res = GetClient(headers, timeout, allowRedirect, proxy).GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        }
    }
}
