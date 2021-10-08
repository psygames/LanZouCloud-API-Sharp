using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace LanZouAPI
{
    public partial class LanZouCloud
    {
        private Http _session = new Http();
        private bool _limit_mode = true;            // 是否保持官方限制
        private int _timeout = 15;                  // 每个请求的超时(不包含下载响应体的用时)
        private int _max_size = 100;                // 单个文件大小上限 MB
        private float _upload_delay = 1;            // 文件上传延时
        private string _host_url = "https://pan.lanzoui.com";
        private string _doupload_url = "https://pc.woozooo.com/doupload.php";
        private string _account_url = "https://pc.woozooo.com/account.php";
        private string _mydisk_url = "https://pc.woozooo.com/mydisk.php";
        private Dictionary<string, string> _headers = new Dictionary<string, string>()
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.100 Safari/537.36" },
            { "Referer", "https://pc.woozooo.com/mydisk.php" },
            { "Accept-Language", "zh-CN,zh;q=0.9" },  // 提取直连必需设置这个，否则拿不到数据
        };

        public LanZouCloud()
        {
            _session.SetTimeout(_timeout);
            _session.SetHeaders(_headers);
        }

        #region API
        /// <summary>
        /// 解除官方限制
        /// </summary>
        public void ignore_limits()
        {
            Log.Warning("*** You have enabled the big file upload and filename disguise features ***");
            Log.Warning("*** This means that you fully understand what may happen and still agree to take the risk ***");
            _limit_mode = false;
        }

        /// <summary>
        /// 设置单文件大小限制(会员用户可超过 100M)
        /// </summary>
        /// <param name="max_size"></param>
        /// <returns></returns>
        public ResultCode set_max_size(int max_size = 100)
        {
            if (max_size < 100)
                return ResultCode.FAILED;
            _max_size = max_size;
            return ResultCode.SUCCESS;
        }

        /// <summary>
        /// 设置上传大文件数据块时，相邻两次上传之间的延时，减小被封号的可能
        /// </summary>
        /// <param name="range_begin"></param>
        /// <param name="range_end"></param>
        /// <returns></returns>
        public ResultCode set_upload_delay(float delay)
        {
            if (delay < 0)
                return ResultCode.FAILED;
            _upload_delay = delay;
            return ResultCode.SUCCESS;
        }

        /// <summary>
        /// 通过cookie登录
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public ResultCode login_by_cookie(string ylogin, string phpdisk_info)
        {
            _session.SetCookies(new List<Cookie>() {
                new Cookie("ylogin", ylogin, null, "woozooo.com"),
                new Cookie("phpdisk_info", phpdisk_info, null, "pc.woozooo.com"),
            });
            var html = _get(_account_url);
            Log.Info(html);
            if (string.IsNullOrEmpty(html))
                return ResultCode.NETWORK_ERROR;
            if (html.Contains("网盘用户登录"))
                return ResultCode.FAILED;
            return ResultCode.SUCCESS;
        }

        /// <summary>
        /// 注销
        /// </summary>
        /// <returns></returns>
        public ResultCode logout()
        {
            var html = _get($"{_account_url}?action=logout");
            if (string.IsNullOrEmpty(html))
                return ResultCode.NETWORK_ERROR;
            if (html.Contains("退出系统成功"))
                return ResultCode.SUCCESS;
            return ResultCode.FAILED;
        }

        /// <summary>
        /// 把网盘的文件、无子文件夹的文件夹放到回收站
        /// </summary>
        /// <param name="fid"></param>
        /// <param name="is_file"></param>
        /// <returns></returns>
        public ResultCode delete(long fid, bool is_file)
        {
            Dictionary<string, string> post_data = null;
            if (is_file)
                post_data = new Dictionary<string, string>() { { "task", $"{6}" }, { "file_id", $"{fid}" } };
            else
                post_data = new Dictionary<string, string>() { { "task", $"{3}" }, { "folder_id", $"{fid}" } };
            var text = _post(_doupload_url, post_data);
            if (string.IsNullOrEmpty(text))
                return ResultCode.NETWORK_ERROR;
            if (text.Contains("zt\":1"))
                return ResultCode.SUCCESS;
            return ResultCode.FAILED;
        }


        /// <summary>
        /// 获取文件列表
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        public List<CloudFile> get_file_list(long folder_id = -1)
        {
            var page = 1;
            var file_list = new List<CloudFile>();
            while (true)
            {
                var post_data = new Dictionary<string, string>() {
                    { "task", $"{5}" },
                    { "folder_id", $"{folder_id}" },
                    { "pg", $"{page}" },
                };

                var resp = _post(_doupload_url, post_data);
                Log.Info(resp);
                if (string.IsNullOrEmpty(resp))     // 网络异常，重试
                    continue;
                var json = JsonMapper.ToObject(resp);
                if ((int)json["info"] == 0)
                    break;                          // 已经拿到了全部的文件信息
                page += 1;                          // 下一页

                foreach (var _json in json["text"])
                {
                    var f_json = (JsonData)_json;
                    file_list.Add(new CloudFile()
                    {
                        id = long.Parse(f_json["id"].ToString()),
                        name = f_json["name_all"].ToString().Replace("&amp;", "&"),
                        time = time_format((string)f_json["time"]),                     // 上传时间
                        size = f_json["size"].ToString().Replace(",", ""),              // 文件大小
                        type = Path.GetExtension(f_json["name_all"].ToString()).Substring(1),   // 文件类型
                        downs = int.Parse(f_json["downs"].ToString()),                  // 下载次数
                        has_pwd = int.Parse(f_json["onof"].ToString()) == 1,            // 是否存在提取码
                        has_des = int.Parse(f_json["is_des"].ToString()) == 1,          // 是否存在描述
                    });
                }
            }

            return file_list;
        }

        /// <summary>
        /// 获取子文件夹列表
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        public List<CloudFolder> get_dir_list(long folder_id = -1)
        {
            var folder_list = new List<CloudFolder>();
            var post_data = new Dictionary<string, string>() {
                    { "task", $"{47}" },
                    { "folder_id", $"{folder_id}" },
                };

            var resp = _post(_doupload_url, post_data);
            Log.Info(resp);
            if (string.IsNullOrEmpty(resp))     // 网络异常，重试
                return folder_list;
            var json = JsonMapper.ToObject(resp);

            foreach (var _json in json["text"])
            {
                var f_json = (JsonData)_json;
                folder_list.Add(new CloudFolder()
                {
                    id = long.Parse(f_json["fol_id"].ToString()),
                    name = f_json["name"].ToString(),
                    has_pwd = int.Parse(f_json["onof"].ToString()) == 1,
                    desc = f_json["folder_des"].ToString().Trim('[', ']'),
                });
            }

            return folder_list;
        }

        /// <summary>
        /// 获取文件(夹)提取码、分享链接
        /// </summary>
        /// <param name="fid"></param>
        /// <param name="is_file"></param>
        /// <returns></returns>
        public ShareInfo get_share_info(long fid, bool is_file = true)
        {
            Dictionary<string, string> post_data = null;
            if (is_file)
                post_data = new Dictionary<string, string>() { { "task", $"{22}" }, { "file_id", $"{fid}" } };
            else
                post_data = new Dictionary<string, string>() { { "task", $"{18}" }, { "folder_id", $"{fid}" } };

            // 获取分享链接和密码用
            var f_info_str = _post(_doupload_url, post_data);
            if (string.IsNullOrEmpty(f_info_str))
                return new ShareInfo(ResultCode.NETWORK_ERROR);
            var f_info = JsonMapper.ToObject(f_info_str)["info"];

            // id 有效性校验
            if (f_info.ContainsKey("f_id") && f_info["f_id"].ToString() == "i"
                || f_info.ContainsKey("name") && string.IsNullOrEmpty((string)f_info["name"]))
                return new ShareInfo(ResultCode.ID_ERROR);

            // onof=1 时，存在有效的提取码; onof=0 时不存在提取码，但是 pwd 字段还是有一个无效的随机密码
            var pwd = f_info["onof"].ToString() == "1" ? f_info["pwd"].ToString() : "";
            string url;
            string name;
            string desc;
            if (f_info.ContainsKey("f_id")) // 说明返回的是文件的信息
            {
                url = f_info["is_newd"] + "//" + f_info["f_id"];        // 文件的分享链接需要拼凑
                var _post_data = new Dictionary<string, string>() { { "task", $"{12}" }, { "file_id", $"{fid}" } };
                var file_info_str = _post(_doupload_url, _post_data);   // 文件信息
                if (string.IsNullOrEmpty(file_info_str))
                    return new ShareInfo(ResultCode.NETWORK_ERROR);
                var file_info = JsonMapper.ToObject(file_info_str);
                // 无后缀的文件名(获得后缀又要发送请求,没有就没有吧,尽可能减少请求数量)
                name = file_info["text"].ToString();
                desc = file_info["info"].ToString();
            }
            else
            {
                url = f_info["new_url"].ToString();  // 文件夹的分享链接可以直接拿到
                name = f_info["name"].ToString();  // 文件夹名
                desc = f_info["des"].ToString();  // 文件夹描述
            }
            return new ShareInfo(ResultCode.SUCCESS, name, url, desc, pwd);
        }

        /// <summary>
        /// 获取文件各种信息(包括下载直链)
        /// </summary>
        /// <param name="share_url">文件分享链接</param>
        /// <param name="pwd">文件提取码(如果有的话)</param>
        /// <returns></returns>

        public CloudFileDetail get_file_info_by_url(string share_url, string pwd = "")
        {
            if (!is_file_url(share_url))  // 非文件链接返回错误
                return new CloudFileDetail(ResultCode.URL_INVALID, pwd, share_url);

            var first_page = _get(share_url);  // 文件分享页面(第一页)
            if (string.IsNullOrEmpty(first_page))
                return new CloudFileDetail(ResultCode.NETWORK_ERROR, pwd, share_url);

            if (first_page.Contains("acw_sc__v2"))
            {
                // 在页面被过多访问或其他情况下，有时候会先返回一个加密的页面，其执行计算出一个acw_sc__v2后放入页面后再重新访问页面才能获得正常页面
                // 若该页面进行了js加密，则进行解密，计算acw_sc__v2，并加入cookie
                var acw_sc__v2 = calc_acw_sc__v2(first_page);
                _session.SetCookie("acw_sc__v2", acw_sc__v2);
                Log.Info($"Set Cookie: acw_sc__v2={acw_sc__v2}");
                first_page = _get(share_url);   // 文件分享页面(第一页)
                if (string.IsNullOrEmpty(first_page))
                    return new CloudFileDetail(ResultCode.NETWORK_ERROR, pwd, share_url);
            }

            first_page = remove_notes(first_page);  // 去除网页里的注释
            if (first_page.Contains("文件取消") || first_page.Contains("文件不存在"))
                return new CloudFileDetail(ResultCode.FILE_CANCELLED, pwd, share_url);

            JsonData link_info;
            string f_name;
            string f_time;
            string f_size;
            string f_desc;

            // 这里获取下载直链 304 重定向前的链接
            if (first_page.Contains("id=\"pwdload\"") || first_page.Contains("id=\"passwddiv\""))   // 文件设置了提取码时
            {
                if (string.IsNullOrEmpty(pwd))
                    return new CloudFileDetail(ResultCode.LACK_PASSWORD, pwd, share_url);  // 没给提取码直接退出
                var sign = Regex.Match(first_page, "sign=(\\w+?)&").Groups[1].Value;
                var post_data = new Dictionary<string, string>(){
                        { "action", "downprocess" },
                        { "sign", $"{sign}" },
                        { "p", $"{pwd}" },
                    };
                var link_info_str = _post(_host_url + "/ajaxm.php", post_data);  // 保存了重定向前的链接信息和文件名
                var second_page = _get(share_url);  // 再次请求文件分享页面，可以看见文件名，时间，大小等信息(第二页)
                if (string.IsNullOrEmpty(link_info_str) || string.IsNullOrEmpty(second_page))
                    return new CloudFileDetail(ResultCode.NETWORK_ERROR, pwd, share_url);

                link_info = JsonMapper.ToObject(link_info_str);
                second_page = remove_notes(second_page);
                // 提取文件信息
                f_name = link_info["inf"].ToString().Replace("*", "_");
                var f_size_match = Regex.Match(second_page, "大小.+?(\\d[\\d.,]+\\s?[BKM]?)<");
                f_size = f_size_match.Success ? f_size_match.Groups[1].Value.Replace(",", "") : "0 M";
                var f_time_match = Regex.Match(second_page, "class=\"n_file_infos\">(.+?)</span>");
                f_time = f_time_match.Success ? time_format(f_time_match.Groups[1].Value) : time_format("0 小时前");
                var f_desc_match = Regex.Match(second_page, "class=\"n_box_des\">(.*?)</div>");
                f_desc = f_desc_match.Success ? f_desc_match.Groups[1].Value : "";
            }
            else // 文件没有设置提取码时,文件信息都暴露在分享页面上
            {
                var para = Regex.Match(first_page, "<iframe.*?src=\"(.+?)\"").Groups[1].Value;  // 提取下载页面 URL 的参数

                // 文件名位置变化很多
                Match f_name_match;
                if ((f_name_match = Regex.Match(first_page, "<title>(.+?) - 蓝奏云</title>")).Success
                    || (f_name_match = Regex.Match(first_page, "<div class=\"filethetext\".+?>([^<>]+?)</div>")).Success
                    || (f_name_match = Regex.Match(first_page, "<div style=\"font-size.+?>([^<>].+?)</div>")).Success
                    || (f_name_match = Regex.Match(first_page, "var filename = '(.+?)';")).Success
                    || (f_name_match = Regex.Match(first_page, "id=\"filenajax\">(.+?)</div>")).Success
                    || (f_name_match = Regex.Match(first_page, "<div class=\"b\"><span>([^<>]+?)</span></div>")).Success)
                    f_name = f_name_match.Groups[1].Value.Replace("*", "_");
                else
                    f_name = "未匹配到文件名";

                // 匹配文件时间，文件没有时间信息就视为今天，统一表示为 2020-01-01 格式
                var f_time_match = Regex.Match(first_page, @">(\d+\s?[秒天分小][钟时]?前|[昨前]天\s?[\d:]+?|\d+\s?天前|\d{4}-\d\d-\d\d)<");
                f_time = f_time_match.Success ? time_format(f_time_match.Groups[1].Value) : time_format("0 小时前");

                // 匹配文件大小
                var f_size_match = Regex.Match(first_page, @"大小.+?(\d[\d.,]+\s?[BKM]?)<");
                f_size = f_size_match.Success ? f_size_match.Groups[1].Value.Replace(",", "") : "0 M";

                // 匹配文件描述
                var f_desc_match = Regex.Match(first_page, @"文件描述.+?<br>\n?\s*(.*?)\s*</td>");
                f_desc = f_desc_match.Success ? f_desc_match.Groups[1].Value : "";

                first_page = _get(_host_url + para);
                if (string.IsNullOrEmpty(first_page))
                    return new CloudFileDetail(ResultCode.NETWORK_ERROR, f_name, f_time, f_size, f_desc, pwd, share_url);
                first_page = remove_notes(first_page);
                // 一般情况 sign 的值就在 data 里，有时放在变量后面
                var sign = Regex.Match(first_page, "'sign':(.+?),").Groups[1].Value;
                if (sign.Length < 20)  // 此时 sign 保存在变量里面, 变量名是 sign 匹配的字符
                    sign = Regex.Match(first_page, $"var {sign}\\s*=\\s*'(.+?)';").Groups[1].Value;
                var post_data = new Dictionary<string, string>(){
                        { "action", "downprocess" },
                        { "sign", $"{sign}" },
                        { "ves", $"{1}" },
                    };
                var link_info_str = _post(_host_url + "/ajaxm.php", post_data);
                if (string.IsNullOrEmpty(link_info_str))
                    return new CloudFileDetail(ResultCode.NETWORK_ERROR, f_name, f_time, f_size, f_desc, pwd, share_url);
                link_info = JsonMapper.ToObject(link_info_str);
            }

            // 这里开始获取文件直链
            if ((int)link_info["zt"] != 1)  //# 返回信息异常，无法获取直链
                return new CloudFileDetail(ResultCode.FAILED, f_name, f_time, f_size, f_desc, pwd, share_url);

            var fake_url = link_info["dom"].ToJson() + "/file/" + link_info["url"].ToString();  // 假直连，存在流量异常检测
            var download_page = _get(fake_url);
            if (string.IsNullOrEmpty(download_page))
                return FileDetail(LanZouCloud.NETWORK_ERROR, name = f_name, time = f_time, size = f_size, desc = f_desc,
                                  pwd = pwd, url = share_url)
            download_page.encoding = 'utf-8'
            download_page_html = remove_notes(download_page.text)
            if '网络异常' not in download_page_html:  # 没有遇到验证码
                direct_url = download_page.headers['Location']  # 重定向后的真直链
            else:  # 遇到验证码，验证后才能获取下载直链
                file_token = re.findall("'file':'(.+?)'", download_page_html)[0]
                file_sign = re.findall("'sign':'(.+?)'", download_page_html)[0]
                check_api = 'https://vip.d0.baidupan.com/file/ajax.php'
                post_data = { 'file': file_token, 'el': 2, 'sign': file_sign}
            sleep(2)  # 这里必需等待2s, 否则直链返回 ?SignError
                resp = self._post(check_api, post_data)
                direct_url = resp.json()['url']
                if not direct_url:
                return FileDetail(LanZouCloud.CAPTCHA_ERROR, name = f_name, time = f_time, size = f_size, desc = f_desc,
                                  pwd = pwd, url = share_url)

            f_type = f_name.split('.')[-1]
            return FileDetail(LanZouCloud.SUCCESS,
                              name = f_name, size = f_size, type = f_type, time = f_time,
                              desc = f_desc, pwd = pwd, url = share_url, durl = direct_url)

                    */

                return null;
        }

        /// <summary>
        /// 通过 id 获取文件信息
        /// </summary>
        /// <param name="file_id"></param>
        /// <returns></returns>
        public CloudFileDetail get_file_info_by_id(long file_id)
        {
            var info = get_share_info(file_id);
            if (info.code != ResultCode.SUCCESS)
                return new CloudFileDetail(info.code);
            return get_file_info_by_url(info.url, info.pwd);
        }
        #endregion
    }
}
