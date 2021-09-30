using System;
using System.Collections.Generic;

namespace LanZouCloud
{
    public class Client
    {
        private Http _session = new Http();
        private bool _limit_mode = true;         // 是否保持官方限制
        private int _timeout = 15;               // 每个请求的超时(不包含下载响应体的用时)
        private int _max_size = 100;             // 单个文件大小上限 MB
        private int[] _upload_delay = new int[] { 0, 0 };  // 文件上传延时
        private string _host_url = "https://pan.lanzoui.com";
        private string _doupload_url = "https://pc.woozooo.com/doupload.php";
        private string _account_url = "https://pc.woozooo.com/account.php";
        private string _mydisk_url = "https://pc.woozooo.com/mydisk.php";
        private Dictionary<string, string> _cookies = null;
        private Dictionary<string, string> _headers = new Dictionary<string, string>()
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.100 Safari/537.36" },
            { "Referer", "https://pc.woozooo.com/mydisk.php" },
            { "Accept-Language", "zh-CN,zh;q=0.9" },  // 提取直连必需设置这个，否则拿不到数据
        };

        public Client()
        {

        }

        private string _get(string url)
        {
            foreach (var possible_url in _all_possible_urls(url))
            {
                try
                {
                    // kwargs.setdefault('timeout', self._timeout)
                    // kwargs.setdefault('headers', self._headers)
                    // return _session.GetText(possible_url);
                }
                catch
                {
                    Log.Error($"Get {possible_url} failed, try another domain");
                }
            }
            return null;
        }

        private string _post(string url, Dictionary<string, string> data)
        {
            foreach (var possible_url in _all_possible_urls(url))
            {
                try
                {
                    // kwargs.setdefault('timeout', self._timeout)
                    // kwargs.setdefault('headers', self._headers)
                    // return _session.post(possible_url, data, verify = False, **kwargs)
                }
                catch
                {
                    Log.Error($"Post to {possible_url} ({data}) failed, try another domain");
                }
            }
            return null;
        }

        private static string[] available_domains = new string[]
        {
            "lanzoui.com",  // 鲁ICP备15001327号-6, 2020-06-09, SEO 排名最低
            "lanzoux.com",  // 鲁ICP备15001327号-5, 2020-06-09
            "lanzous.com",  // 主域名, 备案异常, 部分地区已经无法访问
        };

        /// <summary>
        /// 蓝奏云的主域名有时会挂掉, 此时尝试切换到备用域名
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string[] _all_possible_urls(string url)
        {
            var possible_urls = new string[available_domains.Length];
            for (int i = 0; i < possible_urls.Length; i++)
            {
                possible_urls[i] = url.Replace("lanzous.com", available_domains[i]);
            }
            return possible_urls;
        }

        /// <summary>
        /// 解除官方限制
        /// </summary>
        public void ignore_limits()
        {
            Log.Warning("*** You have enabled the big file upload and filename disguise features ***");
            Log.Warning("*** This means that you fully understand what may happen and still agree to take the risk ***");
            this._limit_mode = false;
        }
    }
}
