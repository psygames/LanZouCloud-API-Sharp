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
            _session.SetDefaultHeaders(_headers);
            _session.SetDefaultTimeout(10);
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
        public LanZouCode set_max_size(int max_size = 100)
        {
            if (max_size < 100)
                return LanZouCode.FAILED;
            _max_size = max_size;
            return LanZouCode.SUCCESS;
        }

        /// <summary>
        /// 设置上传大文件数据块时，相邻两次上传之间的延时，减小被封号的可能
        /// </summary>
        /// <param name="range_begin"></param>
        /// <param name="range_end"></param>
        /// <returns></returns>
        public LanZouCode set_upload_delay(float delay)
        {
            if (delay < 0)
                return LanZouCode.FAILED;
            _upload_delay = delay;
            return LanZouCode.SUCCESS;
        }

        /// <summary>
        /// 通过cookie登录
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public LanZouCode login_by_cookie(string ylogin, string phpdisk_info)
        {
            _session.SetCookie(".woozooo.com", "ylogin", ylogin);
            _session.SetCookie(".woozooo.com", "phpdisk_info", phpdisk_info);
            var html = _get(_account_url);
            if (string.IsNullOrEmpty(html))
                return LanZouCode.NETWORK_ERROR;
            if (html.Contains("网盘用户登录"))
                return LanZouCode.FAILED;
            return LanZouCode.SUCCESS;
        }

        /// <summary>
        /// 注销
        /// </summary>
        /// <returns></returns>
        public LanZouCode logout()
        {
            var html = _get($"{_account_url}?action=logout");
            if (string.IsNullOrEmpty(html))
                return LanZouCode.NETWORK_ERROR;
            if (!html.Contains("退出系统成功"))
                return LanZouCode.FAILED;
            return LanZouCode.SUCCESS;
        }

        /// <summary>
        /// 把网盘的文件、无子文件夹的文件夹放到回收站
        /// </summary>
        /// <param name="fid"></param>
        /// <param name="is_file"></param>
        /// <returns></returns>
        public LanZouCode delete(long fid, bool is_file)
        {
            Dictionary<string, string> post_data;
            if (is_file)
                post_data = new Dictionary<string, string>() { { "task", $"{6}" }, { "file_id", $"{fid}" } };
            else
                post_data = new Dictionary<string, string>() { { "task", $"{3}" }, { "folder_id", $"{fid}" } };
            var text = _post(_doupload_url, post_data);
            return _get_rescode(text);
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
        /// 获取文件各种信息(包括下载直链)
        /// </summary>
        /// <param name="share_url">文件分享链接</param>
        /// <param name="pwd">文件提取码(如果有的话)</param>
        /// <returns></returns>
        public CloudFileDetail get_file_info_by_url(string share_url, string pwd = "")
        {
            if (!is_file_url(share_url))  // 非文件链接返回错误
                return new CloudFileDetail(LanZouCode.URL_INVALID, pwd, share_url);

            var first_page = _get(share_url);  // 文件分享页面(第一页)
            if (string.IsNullOrEmpty(first_page))
                return new CloudFileDetail(LanZouCode.NETWORK_ERROR, pwd, share_url);

            if (first_page.Contains("acw_sc__v2"))
            {
                // 在页面被过多访问或其他情况下，有时候会先返回一个加密的页面，其执行计算出一个acw_sc__v2后放入页面后再重新访问页面才能获得正常页面
                // 若该页面进行了js加密，则进行解密，计算acw_sc__v2，并加入cookie
                var acw_sc__v2 = calc_acw_sc__v2(first_page);
                _session.SetCookie(new Uri(share_url).Host, "acw_sc__v2", $"{acw_sc__v2}");
                Log.Info($"Set Cookie: acw_sc__v2={acw_sc__v2}");
                first_page = _get(share_url);   // 文件分享页面(第一页)
                if (string.IsNullOrEmpty(first_page))
                    return new CloudFileDetail(LanZouCode.NETWORK_ERROR, pwd, share_url);
            }

            first_page = remove_notes(first_page);  // 去除网页里的注释
            if (first_page.Contains("文件取消") || first_page.Contains("文件不存在"))
                return new CloudFileDetail(LanZouCode.FILE_CANCELLED, pwd, share_url);

            JsonData link_info;
            string f_name;
            string f_time;
            string f_size;
            string f_desc;

            // 这里获取下载直链 304 重定向前的链接
            if (first_page.Contains("id=\"pwdload\"") || first_page.Contains("id=\"passwddiv\""))   // 文件设置了提取码时
            {
                if (string.IsNullOrEmpty(pwd))
                    return new CloudFileDetail(LanZouCode.LACK_PASSWORD, pwd, share_url);  // 没给提取码直接退出
                var sign = Regex.Match(first_page, "sign=(\\w+?)&").Groups[1].Value;
                var post_data = new Dictionary<string, string>(){
                        { "action", "downprocess" },
                        { "sign", $"{sign}" },
                        { "p", $"{pwd}" },
                    };
                var link_info_str = _post(_host_url + "/ajaxm.php", post_data);  // 保存了重定向前的链接信息和文件名
                var second_page = _get(share_url);  // 再次请求文件分享页面，可以看见文件名，时间，大小等信息(第二页)
                if (string.IsNullOrEmpty(link_info_str) || string.IsNullOrEmpty(second_page))
                    return new CloudFileDetail(LanZouCode.NETWORK_ERROR, pwd, share_url);

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
                    return new CloudFileDetail(LanZouCode.NETWORK_ERROR, f_name, f_time, f_size, f_desc, pwd, share_url);
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
                    return new CloudFileDetail(LanZouCode.NETWORK_ERROR, f_name, f_time, f_size, f_desc, pwd, share_url);
                link_info = JsonMapper.ToObject(link_info_str);
            }

            // 这里开始获取文件直链
            if ((int)link_info["zt"] != 1)  //# 返回信息异常，无法获取直链
                return new CloudFileDetail(LanZouCode.FAILED, f_name, f_time, f_size, f_desc, pwd, share_url);

            var fake_url = link_info["dom"].ToString() + "/file/" + link_info["url"].ToString();  // 假直连，存在流量异常检测
            var download_page = _get_resp(fake_url, null, false);
            if (download_page == null || download_page.StatusCode != HttpStatusCode.Found)
                return new CloudFileDetail(LanZouCode.NETWORK_ERROR, f_name, f_time, f_size, f_desc, pwd, share_url);

            // download_page.encoding = 'utf-8'
            var task = download_page.Content.ReadAsStringAsync();
            task.Wait();
            var download_page_html = remove_notes(task.Result);
            string direct_url;
            if (!download_page_html.Contains("网络异常"))  // 没有遇到验证码
            {
                direct_url = download_page.Headers.Location.AbsoluteUri;  // 重定向后的真直链
            }
            else // 遇到验证码，验证后才能获取下载直链
            {
                var file_token = Regex.Match(download_page_html, "'file':'(.+?)'").Value;
                var file_sign = Regex.Match(download_page_html, "'sign':'(.+?)'").Value;
                var check_api = "https://vip.d0.baidupan.com/file/ajax.php";
                var post_data = new Dictionary<string, string>(){
                        { "file", $"{file_token}" },
                        { "el", $"{2}" },
                        { "sign", $"{file_sign}" },
                    };
                System.Threading.Thread.Sleep(2000);  // 这里必需等待2s, 否则直链返回 ?SignError
                var resp = _post(check_api, post_data);
                var json = JsonMapper.ToObject(resp);
                direct_url = json["url"].ToString();
                if (string.IsNullOrEmpty(direct_url))
                    return new CloudFileDetail(LanZouCode.CAPTCHA_ERROR, f_name, f_time, f_size, f_desc, pwd, share_url);
            }

            var f_type = Path.GetExtension(f_name).Substring(1);
            return new CloudFileDetail(LanZouCode.SUCCESS, f_name, f_time, f_size, f_desc, pwd, share_url, f_type, direct_url);
        }

        /// <summary>
        /// 通过 id 获取文件信息
        /// </summary>
        /// <param name="file_id"></param>
        /// <returns></returns>
        public CloudFileDetail get_file_info_by_id(long file_id)
        {
            var info = get_share_info(file_id);
            if (info.code != LanZouCode.SUCCESS)
                return new CloudFileDetail(info.code);
            return get_file_info_by_url(info.url, info.pwd);
        }

        /// <summary>
        /// 通过分享链接获取下载直链
        /// </summary>
        /// <param name="share_url"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public DirectUrlInfo get_durl_by_url(string share_url, string pwd = "")
        {
            var file_info = get_file_info_by_url(share_url, pwd);
            if (file_info.code != LanZouCode.SUCCESS)
                return new DirectUrlInfo(file_info.code);
            return new DirectUrlInfo(LanZouCode.SUCCESS, file_info.name, file_info.durl);
        }

        /// <summary>
        /// 登录用户通过id获取直链
        /// </summary>
        /// <param name="file_id"></param>
        /// <returns></returns>
        public DirectUrlInfo get_durl_by_id(long file_id)
        {
            var info = get_share_info(file_id);  // 能获取直链，一定是文件
            return get_durl_by_url(info.url, info.pwd);
        }

        /// <summary>
        /// 获取文件(夹)提取码、分享链接
        /// </summary>
        /// <param name="fid"></param>
        /// <param name="is_file"></param>
        /// <returns></returns>
        public ShareInfo get_share_info(long fid, bool is_file = true)
        {
            Dictionary<string, string> post_data;
            if (is_file)
                post_data = new Dictionary<string, string>() { { "task", $"{22}" }, { "file_id", $"{fid}" } };
            else
                post_data = new Dictionary<string, string>() { { "task", $"{18}" }, { "folder_id", $"{fid}" } };

            // 获取分享链接和密码用
            var f_info_str = _post(_doupload_url, post_data);
            if (string.IsNullOrEmpty(f_info_str))
                return new ShareInfo(LanZouCode.NETWORK_ERROR);
            var f_info = JsonMapper.ToObject(f_info_str)["info"];

            // id 有效性校验
            if (f_info.ContainsKey("f_id") && f_info["f_id"].ToString() == "i"
                || f_info.ContainsKey("name") && string.IsNullOrEmpty((string)f_info["name"]))
                return new ShareInfo(LanZouCode.ID_ERROR);

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
                    return new ShareInfo(LanZouCode.NETWORK_ERROR);
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
            return new ShareInfo(LanZouCode.SUCCESS, name, url, desc, pwd);
        }

        /// <summary>
        /// <para>设置网盘文件(夹)的提取码, 现在非会员用户不允许关闭提取码</para>
        /// <para>id 无效或者 id 类型不对应仍然返回成功 :(</para>
        /// <para>文件夹提取码长度 0 - 12 位 文件提取码 2 - 6 位</para>
        /// </summary>
        /// <param name="fid"></param>
        /// <param name="pwd"></param>
        /// <param name="is_file"></param>
        /// <returns></returns>
        public LanZouCode set_passwd(long fid, string pwd = "", bool is_file = true)
        {
            var pwd_status = string.IsNullOrEmpty(pwd) ? 0 : 1;  // 是否开启密码
            Dictionary<string, string> post_data;
            if (is_file)
                post_data = new Dictionary<string, string>() {
                    { "task", $"{23}" },
                    { "file_id", $"{fid}" },
                    { "shows", $"{pwd_status}" },
                    { "shownames", $"{pwd}" },
                };
            else
                post_data = new Dictionary<string, string>() {
                    { "task", $"{16}" },
                    { "folder_id", $"{fid}" },
                    { "shows", $"{pwd_status}" },
                    { "shownames", $"{pwd}" },
                };

            var result = _post(_doupload_url, post_data);
            return _get_rescode(result);
        }

        /// <summary>
        /// 创建文件夹(同时设置描述)
        /// </summary>
        /// <param name="folder_name"></param>
        /// <param name="parent_id"></param>
        /// <param name="desc"></param>
        /// <returns></returns>
        public MakeDirInfo mkdir(string folder_name, long parent_id = -1, string desc = "")
        {
            folder_name = folder_name.Replace(' ', '_');    // 文件夹名称不能包含空格
            folder_name = name_format(folder_name);         // 去除非法字符
            var folder_list = get_dir_list(parent_id);
            var exist_folder = folder_list.Find(a => a.name == folder_name);
            if (exist_folder != null)                       // 如果文件夹已经存在，直接返回
                return new MakeDirInfo(LanZouCode.SUCCESS, exist_folder.id, exist_folder.name, exist_folder.desc);

            var raw_folders = get_move_folders();
            var post_data = new Dictionary<string, string>() {
                    { "task", $"{2}" },
                    { "parent_id", $"{parent_id}" },
                    { "folder_name", $"{folder_name}" },
                    { "folder_description", $"{desc}" },
                };
            var result = _post(_doupload_url, post_data);  // 创建文件夹
            if (string.IsNullOrEmpty(result))
                return new MakeDirInfo(LanZouCode.NETWORK_ERROR);
            if (!result.Contains("zt\":1"))
                return new MakeDirInfo(LanZouCode.MKDIR_ERROR);
            Log.Warning($"Mkdir {folder_name} error, parent_id={parent_id}");

            // 允许在不同路径创建同名文件夹, 移动时可通过 get_move_paths() 区分
            foreach (var kv in get_move_folders())
            {
                if (!raw_folders.ContainsKey(kv.Key)) // 不在原始列表中，即新增文件夹
                {
                    Log.Info($"Mkdir {folder_name} #{kv.Key} in parent_id:{parent_id}");
                    return new MakeDirInfo(LanZouCode.SUCCESS, kv.Key, kv.Value, desc);
                }
            }
            Log.Warning($"Mkdir {folder_name} error, parent_id:{parent_id}");
            return new MakeDirInfo(LanZouCode.MKDIR_ERROR);
        }

        /// <summary>
        /// 重命名文件夹及其描述
        /// </summary>
        /// <param name="folder_id"></param>
        /// <param name="folder_name"></param>
        /// <param name="desc"></param>
        /// <returns></returns>
        private LanZouCode _set_dir_info(long folder_id, string folder_name, string desc = "")
        {
            // 不能用于重命名文件，id 无效仍然返回成功
            folder_name = name_format(folder_name);
            var post_data = new Dictionary<string, string>() {
                    { "task", $"{4}" },
                    { "folder_id", $"{folder_id}" },
                    { "folder_name", $"{folder_name}" },
                    { "folder_description", $"{desc}" },
                };
            var result = _post(_doupload_url, post_data);
            return _get_rescode(result);
        }

        /// <summary>
        /// 重命名文件夹
        /// </summary>
        /// <param name="folder_id"></param>
        /// <param name="folder_name"></param>
        /// <returns></returns>
        public LanZouCode rename_dir(long folder_id, string folder_name)
        {
            // 重命名文件要开会员额
            var info = get_share_info(folder_id, false);
            if (info.code != LanZouCode.SUCCESS)
                return info.code;
            return _set_dir_info(folder_id, folder_name, info.desc);
        }

        /// <summary>
        /// 设置文件(夹)描述
        /// </summary>
        /// <param name="fid"></param>
        /// <param name="desc"></param>
        /// <param name="is_file"></param>
        /// <returns></returns>
        public LanZouCode set_desc(long fid, string desc = "", bool is_file = true)
        {
            if (is_file)
            {
                // 文件描述一旦设置了值，就不能再设置为空
                var post_data = new Dictionary<string, string>() {
                    { "task", $"{11}" },
                    { "file_id", $"{fid}" },
                    { "desc", $"{desc}" },
                };
                var result = _post(_doupload_url, post_data);
                return _get_rescode(result);
            }
            else
            {
                // 文件夹描述可以置空
                var info = get_share_info(fid, false);
                if (info.code != LanZouCode.SUCCESS)
                    return info.code;
                return _set_dir_info(fid, info.name, desc);
            }
        }

        /// <summary>
        /// 允许会员重命名文件(无法修后缀名)
        /// </summary>
        /// <param name="file_id"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public LanZouCode rename_file(long file_id, string filename)
        {
            var post_data = new Dictionary<string, string>() {
                    { "task", $"{46}" },
                    { "file_id", $"{file_id}" },
                    { "file_name", $"{name_format(filename)}"},
                    { "type", $"{2}" },
                };
            var result = _post(_doupload_url, post_data);
            return _get_rescode(result);
        }

        /// <summary>
        /// 获取全部文件夹 id-name 列表，用于移动文件至新的文件夹
        /// </summary>
        /// <returns></returns>
        public Dictionary<long, string> get_move_folders()
        {
            // 这里 file_id 可以为任意值,不会对结果产生影响
            var result = new Dictionary<long, string>();
            result.Add(-1, "LanZouCloud");
            var post_data = new Dictionary<string, string>() { { "task", $"{19}" }, { "file_id", $"{-1}" } };
            var resp = _post(_doupload_url, post_data);
            if (string.IsNullOrEmpty(resp) || !resp.Contains("zt\":1")) // 获取失败或者网络异常
                return result;

            var json = JsonMapper.ToObject(resp);
            if (!json.ContainsKey("info"))  // 新注册用户无数据, info=None
                return result;

            var info = json["info"];
            foreach (var j_folder in info)
            {
                var folder = (JsonData)j_folder;
                var folder_id = long.Parse(folder["folder_id"].ToString());
                var folder_name = folder["folder_name"].ToString();
                result.Add(folder_id, folder_name);
            }
            return result;
        }


        /// <summary>
        /// 获取所有文件夹的绝对路径(耗时长)
        /// </summary>
        /// <returns></returns>
        public Dictionary<long, string> get_move_paths()
        {
            // 官方 bug, 可能会返回一些已经被删除的"幽灵文件夹"
            /*
            result = []
            root = FolderList()
            root.append(FolderId('LanZouCloud', -1))
            result.append(root)
            resp = self._post(self._doupload_url, data ={ "task": 19, "file_id": -1})
            if not resp or resp.json()['zt'] != 1:  # 获取失败或者网络异常
                return result

            ex = ThreadPoolExecutor()  # 线程数 min(32, os.cpu_count() + 4)
            id_list = [int(folder['folder_id']) for folder in resp.json()['info']]
            task_list = [ex.submit(self.get_full_path, fid) for fid in id_list]
            for task in as_completed(task_list) :
                result.append(task.result())
            return sorted(result)
            */
            return null;

        }

        /// <summary>
        /// 移动文件到指定文件夹
        /// </summary>
        /// <param name="file_id"></param>
        /// <param name="folder_id"></param>
        /// <returns></returns>
        public LanZouCode move_file(long file_id, long folder_id = -1)
        {
            // 移动回收站文件也返回成功(实际上行不通) (+_+)?
            var post_data = _post_data("task", $"{20}", "file_id", $"{file_id}", "folder_id", $"{folder_id}");
            var result = _post(_doupload_url, post_data);
            Log.Info($"Move file file_id:{file_id} to folder_id:{folder_id}");
            return _get_rescode(result);
        }

        /// <summary>
        /// 移动文件夹(官方并没有直接支持此功能)
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <returns></returns>
        public LanZouCode move_folder(long folder_id, long parent_folder_id = -1)
        {
            // 禁止移动文件夹到自身，禁止移动到 -2 这样的文件夹(文件还在,但是从此不可见)
            if (folder_id == parent_folder_id || parent_folder_id < -1)
                return LanZouCode.FAILED;

            if (!get_move_folders().TryGetValue(folder_id, out var folder_name))
            {
                Log.Warning($"Not found folder id: {folder_id}");
                return LanZouCode.FAILED;
            }

            // 存在子文件夹，禁止移动
            if (get_dir_list(folder_id).Count > 0)
            {
                // 递归操作可能会产生大量请求,这里只允许移动单层文件夹
                Log.Warning($"Found subdirectory in folder id: {folder_id}");
                return LanZouCode.FAILED;
            }

            // 在目标文件夹下创建同名文件夹
            var info = get_share_info(folder_id, false);
            var mkdir_info = mkdir(folder_name, parent_folder_id, info.desc);

            if (mkdir_info.code != LanZouCode.SUCCESS)
                return LanZouCode.FAILED;
            else if (mkdir_info.id == folder_id)            // 移动文件夹到同一目录
                return LanZouCode.FAILED;

            set_passwd(mkdir_info.id, info.pwd, false);     // 保持密码相同

            // 移动子文件至新目录下
            foreach (var file in get_file_list(folder_id))
            {
                var _code = move_file(file.id, mkdir_info.id);
                if (_code != LanZouCode.SUCCESS)
                {
                    Log.Warning($"Move file Failed id：{file.id}");
                    return LanZouCode.FAILED;
                }
            }

            // 全部移动完成后删除原文件夹
            delete(folder_id, false);
            //TODO: delete_rec(folder_id, false);

            return LanZouCode.SUCCESS;
        }


        /// <summary>
        /// 通过分享链接下载文件(需提取码)
        /// </summary>
        /// <param name="share_url"></param>
        /// <param name="save_dir"></param>
        /// <param name="pwd"></param>
        /// <param name="overwrite">文件已存在时是否强制覆盖</param>
        /// <param name="progress">用于显示下载进度</param>
        /// <returns></returns>
        public LanZouCode down_file_by_url(string share_url, string save_dir,
            string pwd = "", bool overwrite = false, IProgress<DownloadInfo> progress = null)
        {
            var down_info = new DownloadInfo();

            if (!is_file_url(share_url))
                return LanZouCode.URL_INVALID;
            if (!Directory.Exists(save_dir))
                Directory.CreateDirectory(save_dir);

            var info = get_durl_by_url(share_url, pwd);
            Log.Info($"File direct url info: {info}");
            if (info.code != LanZouCode.SUCCESS)
                return info.code;

            long? content_length;

            using (var resp_first = _get_resp(info.durl))
            {
                if (resp_first == null)
                    return LanZouCode.FAILED;

                content_length = resp_first.Content.Headers.ContentLength;

                // 对于 txt 文件, 可能出现没有 Content-Length 的情况
                // 此时文件需要下载一次才会出现 Content-Length
                // 这时候我们先读取一点数据, 再尝试获取一次, 通常只需读取 1 字节数据
                if (content_length == null)
                {
                    var _buffer = new byte[1];
                    var max_retries = 5;  // 5 次拿不到就算了

                    var task = resp_first.Content.ReadAsStreamAsync();
                    task.Wait();
                    var _stream = task.Result;
                    using (_stream)
                    {
                        while (content_length == null && max_retries > 0)
                        {
                            max_retries -= 1;
                            Log.Warning("Not found Content-Length in response headers");
                            Log.Info("Read 1 byte from stream...");
                            _stream.Read(_buffer, 0, 1);

                            // 再请求一次试试
                            using (var resp_ = _get_resp(info.durl, null, false, true))
                            {
                                if (resp_ == null)
                                {
                                    return LanZouCode.FAILED;
                                }
                                content_length = resp_.Content.Headers.ContentLength;
                                Log.Info($"Content-Length: {content_length}");
                            }
                        }
                    }
                }
            }

            if (content_length == null)
                return LanZouCode.FAILED;  // 应该不会出现这种情况

            // 如果本地存在同名文件且设置了 overwrite, 则覆盖原文件
            // 否则修改下载文件路径, 自动在文件名后加序号
            var file_path = Path.Combine(save_dir, info.name);
            if (File.Exists(file_path))
            {
                if (overwrite)
                {
                    Log.Info($"Overwrite file {file_path}");
                    File.Delete(file_path); // 删除旧文件
                }
                else    // 自动重命名文件
                {
                    file_path = _auto_rename(file_path);
                    Log.Info($"File has already exists, auto rename to {file_path}");
                }
            }

            var tmp_file_path = file_path + ".download";  // 正在下载中的文件名
            Log.Info($"Save file to {tmp_file_path}");

            // 支持断点续传下载
            long now_size = 0;
            if (File.Exists(tmp_file_path))
                now_size = new FileInfo(tmp_file_path).Length;  // 本地已经下载的文件大小
            var headers = new Dictionary<string, string>(_headers);
            headers.Add("Range", $"bytes={now_size}-");

            using (var resp = _get_resp(info.durl, headers))
            {
                if (resp == null)  // 网络异常
                    return LanZouCode.FAILED;
                if (resp.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)  // 已经下载完成
                    return LanZouCode.SUCCESS;

                int chunk_size = 4096;
                var chuck = new byte[chunk_size];
                var _task = resp.Content.ReadAsStreamAsync();
                _task.Wait();
                var netStream = _task.Result;
                var fileStream = new FileStream(tmp_file_path, FileMode.Append, FileAccess.Write, FileShare.Read, chunk_size);

                while (true)
                {
                    var readLength = netStream.Read(chuck, 0, chunk_size);
                    if (readLength == 0) break;
                    fileStream.Write(chuck, 0, readLength);
                    now_size += readLength;
                }

                // 下载完成
                netStream.Close();
                fileStream.Close();
            }

            // 下载完成，改回正常文件名
            File.Move(tmp_file_path, file_path);

            // TODO: 大文件切割问题
            // 文件下载完成后, 检查文件尾部 512 字节数据
            // 绕过官方限制上传时, API 会隐藏文件真实信息到文件尾部
            // 这里尝试提取隐藏信息, 并截断文件尾部数据
            /*
            if (new FileInfo(file_path).Length > 512)   // 文件大于 512 bytes 就检查一下
            {
                // file_info = None
                var _fs = new FileStream(file_path, FileMode.Open, FileAccess.Read, FileShare.Read);
                _fs.Seek(-512, SeekOrigin.End);
                var last_512_bytes = new byte[512];
                _fs.Read(last_512_bytes);
                _fs.Close();

                with open(file_path, 'rb') as f:
                    f.seek(-512, os.SEEK_END)
                    last_512_bytes = f.read()
                    file_info = un_serialize(last_512_bytes)
                // 大文件的记录文件也可以反序列化出 name,但是没有 padding 字段
                if file_info is not None and 'padding' in file_info:
                real_name = file_info['name']  // 解除伪装的真实文件名
                        logger.debug(f"Find meta info: real_name={real_name}")
                        real_path = save_dir + os.sep + real_name
                        // 如果存在同名文件且设置了 overwrite, 删掉原文件
                        if overwrite and os.path.exists(real_path):
                            os.remove(real_path)
                // 自动重命名, 文件存在就会加个序号
                        new_file_path = auto_rename(real_path)
                        os.rename(file_path, new_file_path)
                // 截断最后 512 字节隐藏信息, 还原文件
                        with open(new_file_path, 'rb+') as f:
                            f.seek(-512, os.SEEK_END)
                            f.truncate()
                        file_path = new_file_path  # 保存文件重命名后真实路径
            }
            */
            return LanZouCode.SUCCESS;
        }

        /// <summary>
        /// 登录用户通过id下载文件(无需提取码)
        /// </summary>
        /// <param name="file_id"></param>
        /// <param name="save_dir"></param>
        /// <param name="overwrite"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public LanZouCode down_file_by_id(long file_id, string save_dir,
            bool overwrite = false, IProgress<DownloadInfo> progress = null)
        {
            var info = get_share_info(file_id, true);
            if (info.code != LanZouCode.SUCCESS)
                return info.code;
            return down_file_by_url(info.url, save_dir, info.pwd, overwrite, progress);
        }


        /// <summary>
        /// 绕过格式限制上传不超过 max_size 的文件
        /// </summary>
        /// <param name="file_path"></param>
        /// <param name="folder_id"></param>
        /// <param name="overwrite"></param>
        /// <param name="progress"></param>
        public LanZouCode _upload_small_file(string file_path, long folder_id = -1, bool overwrite = true, IProgress<DownloadInfo> progress = null)
        {
            if (!File.Exists(file_path))
                return LanZouCode.PATH_ERROR;

            if (!is_name_valid(file_path))      // 不允许上传的格式
            {
                // if (_limit_mode)                // 不允许绕过官方限制
                return LanZouCode.OFFICIAL_LIMITED;

                // TODO: no limit
                // file_path = let_me_upload(file_path);  // 添加了报尾的新文件
                // need_delete = true;
            }

            // 文件已经存在同名文件就删除
            var filename = name_format(Path.GetFileName(file_path));
            if (overwrite)
            {
                var file_list = get_file_list(folder_id);
                var same_files = file_list.FindAll(a => a.name == filename);
                foreach (var file in same_files)
                {
                    delete(file.id, true);
                }
            }

            Log.Info($"Upload file_path:{file_path} to folder_id:{folder_id}");

            var post_data = _post_data("task", $"{1}", "folder_id", $"{folder_id}", "id", "WU_FILE_0", "name", $"{filename}");

            string result;
            using (var fileStream = new FileStream(file_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                result = _upload("https://pc.woozooo.com/fileup.php", post_data, fileStream, filename, "upload_file");
            }

            var code = _get_rescode(result);
            if (code != LanZouCode.SUCCESS)
                return code;

            var json = JsonMapper.ToObject(result);

            return LanZouCode.SUCCESS;
        }


        #endregion
    }
}
