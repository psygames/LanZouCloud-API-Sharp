using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace LanZouAPI
{
    public class Http
    {
        private HttpClientHandler clientHandler;
        private HttpClient client;
        private const float DEFAULT_TIMEOUT = 10;

        public Http()
        {
            clientHandler = new HttpClientHandler();
            clientHandler.UseCookies = true;
            clientHandler.AllowAutoRedirect = true;
            clientHandler.CookieContainer = new CookieContainer();
            client = new HttpClient(clientHandler, true);
            SetTimeout(DEFAULT_TIMEOUT);
        }

        public void AllowRedirect(bool allowRedirect)
        {
            clientHandler.AllowAutoRedirect = allowRedirect;
        }

        public void SetProxy(string address)
        {
            clientHandler.UseProxy = true;
            clientHandler.Proxy = new WebProxy(address);
        }

        public void SetTimeout(float timeout)
        {
            client.Timeout = new TimeSpan((long)(timeout * 10000000L));
        }

        public void SetHeaders(Dictionary<string, string> headers)
        {
            foreach (var item in headers)
            {
                client.DefaultRequestHeaders.Add(item.Key, item.Value);
            }
        }

        public void SetCookies(string url, string header)
        {
            clientHandler.CookieContainer.SetCookies(new Uri(url), header);
        }

        public void AddCookie(string domain,string name,string value)
        {
            clientHandler.CookieContainer.Add(new Cookie(name, value, null, domain));
        }

        public string GetString(string url)
        {
            var res = client.GetStringAsync(url);
            res.Wait();
            return res.Result;
        }

        public HttpResponseMessage Get(string url)
        {
            var res = client.GetAsync(url);
            res.Wait();
            return res.Result;
        }

        public string PostString(string url, Dictionary<string, string> data)
        {
            var content = new FormUrlEncodedContent(data);
            var res = client.PostAsync(url, content);
            res.Wait();
            var text = res.Result.Content.ReadAsStringAsync();
            return text.Result;
        }

        public void Download(string url, string path, IProgress<long[]> progress = null)
        {

        }
    }
}
