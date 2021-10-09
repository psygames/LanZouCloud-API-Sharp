using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace LanZouAPI
{
    public class Http
    {
        private CookieContainer cookieContainer;
        private HttpClientHandler clientHandler;
        private HttpClientHandler clientHandler_BanRedirect;
        private HttpClient client;
        private HttpClient client_BanRedirect;
        private const float DEFAULT_TIMEOUT = 10;

        public Http()
        {
            cookieContainer = new CookieContainer();

            clientHandler = new HttpClientHandler();
            clientHandler.UseCookies = true;
            clientHandler.AllowAutoRedirect = true;
            clientHandler.CookieContainer = cookieContainer;

            clientHandler_BanRedirect = new HttpClientHandler();
            clientHandler_BanRedirect.UseCookies = true;
            clientHandler_BanRedirect.AllowAutoRedirect = false;
            clientHandler_BanRedirect.CookieContainer = cookieContainer;

            client = new HttpClient(clientHandler, true);
            client_BanRedirect = new HttpClient(clientHandler_BanRedirect, true);
            SetTimeout(DEFAULT_TIMEOUT);
        }

        public void SetProxy(string address)
        {
            clientHandler.UseProxy = true;
            clientHandler.Proxy = new WebProxy(address);

            clientHandler_BanRedirect.UseProxy = true;
            clientHandler_BanRedirect.Proxy = new WebProxy(address);
        }

        public void SetTimeout(float timeout)
        {
            client.Timeout = new TimeSpan((long)(timeout * 10000000L));
            client_BanRedirect.Timeout = new TimeSpan((long)(timeout * 10000000L));
        }

        public void SetHeaders(Dictionary<string, string> headers)
        {
            foreach (var item in headers)
            {
                client.DefaultRequestHeaders.Add(item.Key, item.Value);
                client_BanRedirect.DefaultRequestHeaders.Add(item.Key, item.Value);
            }
        }

        public void SetCookies(string url, string header)
        {
            cookieContainer.SetCookies(new Uri(url), header);
        }

        public void AddCookie(string domain, string name, string value)
        {
            cookieContainer.Add(new Cookie(name, value, null, domain));
        }

        private HttpClient GetClient(bool allowRedirect)
        {
            return allowRedirect ? client : client_BanRedirect;
        }

        public string GetString(string url, bool allowRedirect = true)
        {
            var res = GetClient(allowRedirect).GetStringAsync(url);
            res.Wait();
            return res.Result;
        }

        public HttpResponseMessage Get(string url, bool allowRedirect = true)
        {
            var res = GetClient(allowRedirect).GetAsync(url);
            res.Wait();
            return res.Result;
        }

        public string PostString(string url, Dictionary<string, string> data, bool allowRedirect = true)
        {
            var content = new FormUrlEncodedContent(data);
            var res = GetClient(allowRedirect).PostAsync(url, content);
            res.Wait();
            var text = res.Result.Content.ReadAsStringAsync();
            return text.Result;
        }

        public void Download(string url, string path, IProgress<long[]> progress = null)
        {

        }
    }
}
