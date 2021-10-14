using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LanZouAPI
{
    public partial class LanZouCloud
    {
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
        private async Task<bool> is_file_url(string share_url)
        {
            var base_pat = "https?://[a-zA-Z0-9-]*?\\.?lanzou[six].com/.+";  // 子域名可个性化设置或者不存在
            var user_pat = "https?://[a-zA-Z0-9-]*?\\.?lanzou[six].com/i[a-zA-Z0-9]{5,}/?";  // 普通用户 URL 规则
            if (!Regex.IsMatch(share_url, base_pat))
                return false;
            if (Regex.IsMatch(share_url, user_pat))
                return true;

            // VIP 用户的 URL 很随意
            var html = await _get_text(share_url);
            if (string.IsNullOrEmpty(share_url))
                return false;

            html = remove_notes(html);
            if (Regex.Match(html, "class=\"fileinfo\"|id=\"file\"|文件描述").Success)
                return true;
            return false;
        }

        private string calc_acw_sc__v2(string html_text)
        {
            var arg1 = Regex.Match(html_text, "arg1='([0-9A-Z]+)'");
            var arg1str = "";
            if (arg1.Success)
                arg1str = arg1.Groups[1].Value;
            var acw_sc__v2 = hex_xor(unsbox(arg1str), "3000176000856006061501533003690027800375");
            return acw_sc__v2;
        }

        // 参考自 https://zhuanlan.zhihu.com/p/228507547
        private string unsbox(string str_arg)
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

        private string hex_xor(string str_arg, string args)
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
        public string time_format(string time_str)
        {
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

        /// <summary>
        /// 去除非法字符
        /// </summary>
        /// <param name="name"></param>
        private string name_format(string name)
        {
            // 去除其它字符集的空白符,去除重复空白字符
            name = name.Replace("\xa0", " ").Replace("\u3000", " ").Replace("  ", " ");
            return Regex.Replace(name, "[$%^!*<>)(+=`'\"/:;,?]", "");
        }

        /// <summary>
        /// 从返回结果中获得 返回码
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private LanZouCode _get_rescode(string text)
        {
            if (string.IsNullOrEmpty(text))
                return LanZouCode.NETWORK_ERROR;
            if (text.Contains("info\":\"login not"))
                return LanZouCode.NOT_LOGIN;
            if (!text.Contains("zt\":1"))
                return LanZouCode.FAILED;
            return LanZouCode.SUCCESS;
        }

        /// <summary>
        /// 构建 Post 数据，格式：key, value, key, value, ...
        /// </summary>
        /// <param name="key_vals"></param>
        /// <returns></returns>
        private Dictionary<string, string> _post_data(params string[] key_vals)
        {
            if (key_vals == null || key_vals.Length % 2 != 0)
                throw new ArgumentException("参数数量不匹配!");
            var data = new Dictionary<string, string>();
            for (int i = 0; i < key_vals.Length; i += 2)
            {
                data.Add(key_vals[i], key_vals[i + 1]);
            }
            return data;
        }

        /// <summary>
        /// 如果文件存在，则给文件名添加序号
        /// </summary>
        /// <param name="file_path"></param>
        /// <returns></returns>
        private string _auto_rename(string file_path)
        {
            if (!File.Exists(file_path))
                return file_path;
            var fpath = Path.GetDirectoryName(file_path);
            var fname_no_ext = Path.GetFileNameWithoutExtension(file_path);
            var ext = Path.GetExtension(file_path);
            var count = 1;
            var fset = new HashSet<string>();
            foreach (var f in Directory.GetFiles(fpath))
            {
                fset.Add(Path.GetFileName(f));
            }
            while (count < 99999)
            {
                var current = $"{fname_no_ext}({count}){ext}";
                if (!fset.Contains(current))
                {
                    return Path.Combine(fpath, current).Replace("\\", "/");
                }
                count++;
            }
            throw new Exception("重复文件数量过多，或其他未知错误");
        }


        private static readonly HashSet<string> valid_suffix_list = new HashSet<string>()
        {
            "ppt", "xapk", "ke", "azw", "cpk", "gho", "dwg", "db", "docx", "deb", "e", "ttf", "xls", "bat",
            "crx", "rpm", "txf", "pdf", "apk", "ipa", "txt", "mobi", "osk", "dmg", "rp", "osz", "jar",
            "ttc", "z", "w3x", "xlsx", "cetrainer", "ct", "rar", "mp3", "pptx", "mobileconfig", "epub",
            "imazingapp", "doc", "iso", "img", "appimage", "7z", "rplib", "lolgezi", "exe", "azw3", "zip",
            "conf", "tar", "dll", "flac", "xpa", "lua", "cad", "hwt", "accdb", "ce",
            "xmind", "enc", "bds", "bdi", "ssf", "it", "gz"
        };

        /// <summary>
        /// 检查文件名是否允许上传
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private bool is_name_valid(string filename)
        {
            var ext = Path.GetExtension(filename).Substring(1);
            return valid_suffix_list.Contains(ext);
        }
    }
}
