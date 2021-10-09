using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LanZouAPI
{
    public partial class LanZouCloud
    {
        private string _get_text(string url, bool allowRedirect = true)
        {
            foreach (var possible_url in _all_possible_urls(url))
            {
                try
                {
                    return _session.GetString(possible_url, allowRedirect);
                }
                catch
                {
                    Log.Error($"Get {possible_url} failed, try another domain");
                }
            }
            return null;
        }

        private HttpResponseMessage _get(string url, bool allowRedirect = true)
        {
            foreach (var possible_url in _all_possible_urls(url))
            {
                try
                {
                    return _session.Get(possible_url, allowRedirect);
                }
                catch
                {
                    Log.Error($"Get {possible_url} failed, try another domain");
                }
            }
            return null;
        }

        private string _post(string url, Dictionary<string, string> data, bool allowRedirect = true)
        {
            foreach (var possible_url in _all_possible_urls(url))
            {
                try
                {
                    return _session.PostString(possible_url, data, allowRedirect);
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
            if (url.Contains("lanzous.com"))
            {
                var possible_urls = new string[available_domains.Length];
                for (int i = 0; i < possible_urls.Length; i++)
                {
                    possible_urls[i] = url.Replace("lanzous.com", available_domains[i]);
                }
                return possible_urls;
            }
            return new string[] { url };
        }


        /// <summary>
        /// 删除网页的注释
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private string remove_notes(string html)
        {
            // 去掉 html 里面的 // 和 <!-- --> 注释，防止干扰正则匹配提取数据
            // 蓝奏云的前端程序员喜欢改完代码就把原来的代码注释掉,就直接推到生产环境了 =_=
            html = Regex.Replace(html, "<!--.+?-->|\\s+//\\s*.+", "");  // html 注释
            html = Regex.Replace(html, "(.+?[,;])\\s*//.+", "\\1");     // js 注释
            return html;
        }

        /// <summary>
        /// 判断是否为文件的分享链接
        /// </summary>
        /// <param name="share_url"></param>
        /// <returns></returns>
        private bool is_file_url(string share_url)
        {
            var base_pat = "https?://[a-zA-Z0-9-]*?\\.?lanzou[six].com/.+";  // 子域名可个性化设置或者不存在
            var user_pat = "https?://[a-zA-Z0-9-]*?\\.?lanzou[six].com/i[a-zA-Z0-9]{5,}/?";  // 普通用户 URL 规则
            if (!Regex.IsMatch(share_url, base_pat))
                return false;
            if (Regex.IsMatch(share_url, user_pat))
                return true;

            // VIP 用户的 URL 很随意
            var html = _get_text(share_url);
            if (string.IsNullOrEmpty(share_url))
                return false;

            html = remove_notes(html);
            if (Regex.Match(html, "class=\"fileinfo\"|id=\"file\"|文件描述").Success)
                return true;
            return false;

        }

        public string calc_acw_sc__v2(string html_text)
        {
            var arg1 = Regex.Match(html_text, "arg1='([0-9A-Z]+)'");
            var arg1str = "";
            if (arg1.Success)
                arg1str = arg1.Groups[1].Value;
            var acw_sc__v2 = hex_xor(unsbox(arg1str), "3000176000856006061501533003690027800375");
            return acw_sc__v2;
        }

        // 参考自 https://zhuanlan.zhihu.com/p/228507547
        public string unsbox(string str_arg)
        {
            int[] v1 = new int[]{15, 35, 29, 24, 33, 16, 1, 38, 10, 9, 19, 31, 40, 27, 22, 23,
                25, 13, 6, 11, 39, 18, 20, 8, 14, 21, 32, 26, 2, 30, 7, 4, 17, 5, 3, 28, 34, 37, 12, 36 };
            var v2 = new string[v1.Length];
            for (int idx = 0; idx < str_arg.Length; idx++)
            {
                var v3 = str_arg[idx];
                for (int idx2 = 0; idx2 < v1.Length; idx2++)
                {
                    if (v1[idx2] == idx + 1)
                        v2[idx2] = v3.ToString();
                }
            }
            var res = string.Join("", v2);
            return res;
        }

        public string hex_xor(string str_arg, string args)
        {
            var res = "";
            for (int idx = 0; idx < Math.Min(str_arg.Length, args.Length); idx += 2)
            {
                var v1 = Convert.ToInt32(str_arg.Substring(idx, 2), 16);
                var v2 = Convert.ToInt32(args.Substring(idx, 2), 16);
                var v3 = $"{v1 ^ v2:X2}";
                res += v3;
            }
            return res;
        }

        /// <summary>
        /// 输出格式化时间 DateTime
        /// </summary>
        /// <param name="time_str"></param>
        /// <returns></returns>
        public static string time_format(string time_str)
        {
            //TODO: code
            //if '秒前' in time_str or '分钟前' in time_str or '小时前' in time_str:
            //        return datetime.today().strftime('%Y-%m-%d')
            //elif '昨天' in time_str:
            //        return (datetime.today() - timedelta(days = 1)).strftime('%Y-%m-%d')
            //elif '前天' in time_str:
            //        return (datetime.today() - timedelta(days = 2)).strftime('%Y-%m-%d')
            //elif '天前' in time_str:
            //        days = time_str.replace(' 天前', '')
            //    return (datetime.today() - timedelta(days = int(days))).strftime('%Y-%m-%d')
            //else:
            //    return time_str
            return "todo time format";
        }
    }
}
