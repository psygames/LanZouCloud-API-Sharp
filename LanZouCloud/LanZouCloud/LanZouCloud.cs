using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LanZouAPI
{
    public partial class LanZouCloud
    {
        private int _chunk_size = 4096;            // 上传或下载是的块大小
        private int _timeout = 15;                  // 每个请求的超时(不包含下载响应体的用时)
        private int _max_size = 100;                // 单个文件大小上限 MB
        private string _host_url = "https://pan.lanzoui.com";
        private string _doupload_url = "https://pc.woozooo.com/doupload.php";
        private string _account_url = "https://pc.woozooo.com/account.php";
        private string _mydisk_url = "https://pc.woozooo.com/mydisk.php";
        private string _proxy = null;
        private Dictionary<string, string> _headers = new Dictionary<string, string>()
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.100 Safari/537.36" },
            { "Referer", "https://pc.woozooo.com/mydisk.php" },
            { "Accept-Language", "zh-CN,zh;q=0.9" },  // 提取直连必需设置这个，否则拿不到数据
        };

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
        /// 通过cookie登录
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public async Task<LanZouCode> login_by_cookie(string ylogin, string phpdisk_info)
        {
            _set_cookie(".woozooo.com", "ylogin", ylogin);
            _set_cookie(".woozooo.com", "phpdisk_info", phpdisk_info);
            var html = await _get_text(_account_url);
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
        public async Task<LanZouCode> logout()
        {
            var html = await _get_text($"{_account_url}?action=logout");
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
        public async Task<LanZouCode> delete(long fid, bool is_file)
        {
            var post_data = is_file ? _post_data("task", $"{6}", "file_id", $"{fid}")
                                    : _post_data("task", $"{3}", "folder_id", $"{fid}");
            var text = await _post_text(_doupload_url, post_data);
            return _get_rescode(text);
        }


        /// <summary>
        /// 获取文件列表
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        public async Task<CloudFileList> get_file_list(long folder_id = -1, int page_start = 1, int max_page_count = 999)
        {
            var max_retries = 10; // 最大重试次数
            var page = page_start;
            var page_counter = 0;
            var file_list = new List<CloudFile>();
            while (max_retries > 0 && page_counter < max_page_count)
            {
                var post_data = _post_data("task", $"{5}", "folder_id", $"{folder_id}", "pg", $"{page}");
                var text = await _post_text(_doupload_url, post_data);
                if (string.IsNullOrEmpty(text))     // 网络异常，重试
                {
                    max_retries -= 1;
                    continue;
                }
                var code = _get_rescode(text);
                if (code != LanZouCode.SUCCESS)
                    return new CloudFileList() { code = code, files = file_list };

                var json = JsonMapper.ToObject(text);

                if (int.Parse(json["info"].ToString()) == 0)         // 已经拿到了全部的文件信息
                    break;

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

                // 下一页
                page += 1;
                page_counter += 1;
            }

            if (max_retries <= 0)
            {
                return new CloudFileList() { code = LanZouCode.NETWORK_ERROR, files = file_list };
            }

            return new CloudFileList() { code = LanZouCode.SUCCESS, files = file_list };
        }

        /// <summary>
        /// 获取子文件夹列表
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        public async Task<CloudFolderList> get_folder_list(long folder_id = -1)
        {
            var post_data = _post_data("task", $"{47}", "folder_id", $"{folder_id}");
            var text = await _post_text(_doupload_url, post_data);
            var code = _get_rescode(text);
            if (code != LanZouCode.SUCCESS)
                return new CloudFolderList() { code = code };

            var json = JsonMapper.ToObject(text);
            var folder_list = new List<CloudFolder>();

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

            return new CloudFolderList() { code = LanZouCode.SUCCESS, folders = folder_list };
        }

        /// <summary>
        /// 获取文件各种信息(包括下载直链)
        /// </summary>
        /// <param name="share_url">文件分享链接</param>
        /// <param name="pwd">文件提取码(如果有的话)</param>
        /// <returns></returns>
        public async Task<CloudFileDetail> get_file_info_by_url(string share_url, string pwd = "")
        {
            if (!await is_file_url(share_url))  // 非文件链接返回错误
                return new CloudFileDetail(LanZouCode.URL_INVALID, pwd, share_url);

            var first_page = await _get_text(share_url);  // 文件分享页面(第一页)
            if (string.IsNullOrEmpty(first_page))
                return new CloudFileDetail(LanZouCode.NETWORK_ERROR, pwd, share_url);

            if (first_page.Contains("acw_sc__v2"))
            {
                // 在页面被过多访问或其他情况下，有时候会先返回一个加密的页面，其执行计算出一个acw_sc__v2后放入页面后再重新访问页面才能获得正常页面
                // 若该页面进行了js加密，则进行解密，计算acw_sc__v2，并加入cookie
                var acw_sc__v2 = calc_acw_sc__v2(first_page);
                _set_cookie(new Uri(share_url).Host, "acw_sc__v2", $"{acw_sc__v2}");
                LogInfo($"Set Cookie: acw_sc__v2={acw_sc__v2}");
                first_page = await _get_text(share_url);   // 文件分享页面(第一页)
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
                var post_data = _post_data("action", "downprocess", "sign", $"{sign}", "p", $"{pwd}");
                var link_info_str = await _post_text(_host_url + "/ajaxm.php", post_data);  // 保存了重定向前的链接信息和文件名
                var second_page = await _get_text(share_url);  // 再次请求文件分享页面，可以看见文件名，时间，大小等信息(第二页)
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

                first_page = await _get_text(_host_url + para);
                if (string.IsNullOrEmpty(first_page))
                    return new CloudFileDetail(LanZouCode.NETWORK_ERROR, f_name, f_time, f_size, f_desc, pwd, share_url);
                first_page = remove_notes(first_page);
                // 一般情况 sign 的值就在 data 里，有时放在变量后面
                var sign = Regex.Match(first_page, "'sign':(.+?),").Groups[1].Value;
                if (sign.Length < 20)  // 此时 sign 保存在变量里面, 变量名是 sign 匹配的字符
                    sign = Regex.Match(first_page, $"var {sign}\\s*=\\s*'(.+?)';").Groups[1].Value;
                var post_data = _post_data("action", "downprocess", "sign", $"{sign}", "ves", $"{1}");
                var link_info_str = await _post_text(_host_url + "/ajaxm.php", post_data);
                if (string.IsNullOrEmpty(link_info_str))
                    return new CloudFileDetail(LanZouCode.NETWORK_ERROR, f_name, f_time, f_size, f_desc, pwd, share_url);
                link_info = JsonMapper.ToObject(link_info_str);
            }

            // 这里开始获取文件直链
            if ((int)link_info["zt"] != 1)  //# 返回信息异常，无法获取直链
                return new CloudFileDetail(LanZouCode.FAILED, f_name, f_time, f_size, f_desc, pwd, share_url);

            var fake_url = link_info["dom"].ToString() + "/file/" + link_info["url"].ToString();  // 假直连，存在流量异常检测
            string download_page_html = null;
            string redirect_url = null;

            using (var download_page = await _get_resp(fake_url, null, 0, false))
            {
                if (download_page == null || download_page.StatusCode != HttpStatusCode.Found)
                    return new CloudFileDetail(LanZouCode.NETWORK_ERROR, f_name, f_time, f_size, f_desc, pwd, share_url);

                redirect_url = download_page.Headers.Location.AbsoluteUri;// 重定向后的真直链

                // download_page.encoding = 'utf-8'
                download_page_html = await download_page.Content.ReadAsStringAsync();
                download_page_html = remove_notes(download_page_html);
            }

            string direct_url;
            if (!download_page_html.Contains("网络异常"))  // 没有遇到验证码
            {
                direct_url = redirect_url;
            }
            else // 遇到验证码，验证后才能获取下载直链
            {
                LogWarning($"Get direct url need verify code, force sleep 2 seconds.");
                var file_token = Regex.Match(download_page_html, "'file':'(.+?)'").Value;
                var file_sign = Regex.Match(download_page_html, "'sign':'(.+?)'").Value;
                var check_api = "https://vip.d0.baidupan.com/file/ajax.php";
                var post_data = _post_data("file", $"{file_token}", "el", $"{2}", "sign", $"{file_sign}");
                await Task.Delay(2000);     // 这里必需等待2s, 否则直链返回 ?SignError
                var text = await _post_text(check_api, post_data);
                var json = JsonMapper.ToObject(text);
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
        public async Task<CloudFileDetail> get_file_info_by_id(long file_id)
        {
            var info = await get_share_info(file_id);
            if (info.code != LanZouCode.SUCCESS)
                return new CloudFileDetail(info.code);
            return await get_file_info_by_url(info.url, info.pwd);
        }

        /// <summary>
        /// 通过分享链接获取下载直链
        /// </summary>
        /// <param name="share_url"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public async Task<DirectUrlInfo> get_durl_by_url(string share_url, string pwd = "")
        {
            var file_info = await get_file_info_by_url(share_url, pwd);
            if (file_info.code != LanZouCode.SUCCESS)
                return new DirectUrlInfo(file_info.code);
            return new DirectUrlInfo(LanZouCode.SUCCESS, file_info.name, file_info.durl);
        }

        /// <summary>
        /// 登录用户通过id获取直链
        /// </summary>
        /// <param name="file_id"></param>
        /// <returns></returns>
        public async Task<DirectUrlInfo> get_durl_by_id(long file_id)
        {
            var info = await get_share_info(file_id);  // 能获取直链，一定是文件
            return await get_durl_by_url(info.url, info.pwd);
        }

        /// <summary>
        /// 获取文件(夹)提取码、分享链接
        /// </summary>
        /// <param name="fid"></param>
        /// <param name="is_file"></param>
        /// <returns></returns>
        public async Task<ShareInfo> get_share_info(long fid, bool is_file = true)
        {
            var post_data = is_file ? _post_data("task", $"{22}", "file_id", $"{fid}")
                                    : _post_data("task", $"{18}", "folder_id", $"{fid}");

            // 获取分享链接和密码用
            var f_info_str = await _post_text(_doupload_url, post_data);
            if (string.IsNullOrEmpty(f_info_str))
                return new ShareInfo(LanZouCode.NETWORK_ERROR);

            var f_info = JsonMapper.ToObject(f_info_str)["info"];

            // id 有效性校验
            if (f_info.ContainsKey("f_id") && f_info["f_id"].ToString() == "i"
                || f_info.ContainsKey("name") && string.IsNullOrEmpty(f_info["name"].ToString()))
                return new ShareInfo(LanZouCode.ID_ERROR);

            // onof=1 时，存在有效的提取码; onof=0 时不存在提取码，但是 pwd 字段还是有一个无效的随机密码
            var pwd = f_info["onof"].ToString() == "1" ? f_info["pwd"].ToString() : "";
            string url;
            string name;
            string desc;
            if (f_info.ContainsKey("f_id")) // 说明返回的是文件的信息
            {
                url = f_info["is_newd"] + "//" + f_info["f_id"];        // 文件的分享链接需要拼凑
                var post_data_1 = _post_data("task", $"{12}", "file_id", $"{fid}");
                var file_info_str = await _post_text(_doupload_url, post_data_1);   // 文件信息
                if (string.IsNullOrEmpty(file_info_str))
                    return new ShareInfo(LanZouCode.NETWORK_ERROR);
                var file_info = JsonMapper.ToObject(file_info_str);
                // 无后缀的文件名(获得后缀又要发送请求,没有就没有吧,尽可能减少请求数量)
                name = file_info["text"].ToString();
                desc = file_info["info"].ToString();
            }
            else
            {
                url = f_info["new_url"].ToString();     // 文件夹的分享链接可以直接拿到
                name = f_info["name"].ToString();       // 文件夹名
                desc = f_info["des"].ToString();        // 文件夹描述
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
        public async Task<LanZouCode> set_passwd(long fid, string pwd = "", bool is_file = true)
        {
            var pwd_status = string.IsNullOrEmpty(pwd) ? 0 : 1;  // 是否开启密码
            var post_data = is_file ? _post_data("task", $"{23}", "file_id", $"{fid}", "shows", $"{pwd_status}", "shownames", $"{pwd}")
                                    : _post_data("task", $"{16}", "folder_id", $"{fid}", "shows", $"{pwd_status}", "shownames", $"{pwd}");
            var result = await _post_text(_doupload_url, post_data);
            return _get_rescode(result);
        }

        /// <summary>
        /// 创建文件夹(同时设置描述)
        /// </summary>
        /// <param name="folder_name"></param>
        /// <param name="parent_id"></param>
        /// <param name="desc"></param>
        /// <returns></returns>
        public async Task<MakeDirInfo> mkdir(string folder_name, long parent_id = -1, string desc = "")
        {
            folder_name = folder_name.Replace(' ', '_');    // 文件夹名称不能包含空格
            folder_name = name_format(folder_name);         // 去除非法字符

            var folder_list = await get_folder_list(parent_id);
            if (folder_list.code != LanZouCode.SUCCESS)
                return new MakeDirInfo(folder_list.code);

            var exist_folder = folder_list.folders.Find(a => a.name == folder_name);
            if (exist_folder != null)                       // 如果文件夹已经存在，直接返回
                return new MakeDirInfo(LanZouCode.SUCCESS, exist_folder.id, exist_folder.name, exist_folder.desc);

            var raw_move_folder_list = await get_move_folders();
            if (raw_move_folder_list.code != LanZouCode.SUCCESS)
                return new MakeDirInfo(raw_move_folder_list.code);

            var post_data = _post_data("task", $"{2}", "parent_id", $"{parent_id}", "folder_name", $"{folder_name}", "folder_description", $"{desc}");
            var result = await _post_text(_doupload_url, post_data);  // 创建文件夹
            if (string.IsNullOrEmpty(result))
            {
                return new MakeDirInfo(LanZouCode.NETWORK_ERROR);
            }

            if (!result.Contains("zt\":1"))
            {
                LogWarning($"Mkdir {folder_name} error, parent_id={parent_id}");
                return new MakeDirInfo(LanZouCode.MKDIR_ERROR);
            }


            // 允许在不同路径创建同名文件夹, 移动时可通过 get_move_paths() 区分
            var now_move_folder_list = await get_move_folders();
            if (now_move_folder_list.code != LanZouCode.SUCCESS)
                return new MakeDirInfo(now_move_folder_list.code);

            foreach (var kv in now_move_folder_list.folders)
            {
                if (!raw_move_folder_list.folders.ContainsKey(kv.Key)) // 不在原始列表中，即新增文件夹
                {
                    // 创建文件夹成功
                    LogInfo($"Mkdir {folder_name} #{kv.Key} in parent_id:{parent_id}");
                    return new MakeDirInfo(LanZouCode.SUCCESS, kv.Key, kv.Value, desc);
                }
            }

            LogWarning($"Mkdir {folder_name} error, parent_id:{parent_id}");
            return new MakeDirInfo(LanZouCode.MKDIR_ERROR);
        }

        /// <summary>
        /// 重命名文件夹及其描述
        /// </summary>
        /// <param name="folder_id"></param>
        /// <param name="folder_name"></param>
        /// <param name="desc"></param>
        /// <returns></returns>
        private async Task<LanZouCode> _set_dir_info(long folder_id, string folder_name, string desc = "")
        {
            // 不能用于重命名文件，id 无效仍然返回成功
            folder_name = name_format(folder_name);
            var post_data = _post_data("task", $"{4}", "folder_id", $"{folder_id}", "folder_name", $"{folder_name}", "folder_description", $"{desc}");
            var result = await _post_text(_doupload_url, post_data);
            return _get_rescode(result);
        }

        /// <summary>
        /// 重命名文件夹
        /// </summary>
        /// <param name="folder_id"></param>
        /// <param name="folder_name"></param>
        /// <returns></returns>
        public async Task<LanZouCode> rename_dir(long folder_id, string folder_name)
        {
            // 重命名文件要开会员额
            var info = await get_share_info(folder_id, false);
            if (info.code != LanZouCode.SUCCESS)
                return info.code;
            return await _set_dir_info(folder_id, folder_name, info.desc);
        }

        /// <summary>
        /// 设置文件(夹)描述
        /// </summary>
        /// <param name="fid"></param>
        /// <param name="desc"></param>
        /// <param name="is_file"></param>
        /// <returns></returns>
        public async Task<LanZouCode> set_desc(long fid, string desc = "", bool is_file = true)
        {
            if (is_file)
            {
                // 文件描述一旦设置了值，就不能再设置为空
                var post_data = _post_data("task", $"{11}", "file_id", $"{fid}", "desc", $"{desc}");
                var result = await _post_text(_doupload_url, post_data);
                return _get_rescode(result);
            }
            else
            {
                // 文件夹描述可以置空
                var info = await get_share_info(fid, false);
                if (info.code != LanZouCode.SUCCESS)
                    return info.code;
                return await _set_dir_info(fid, info.name, desc);
            }
        }

        /// <summary>
        /// 允许会员重命名文件(无法修后缀名)
        /// </summary>
        /// <param name="file_id"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task<LanZouCode> rename_file(long file_id, string filename)
        {
            var post_data = _post_data("task", $"{46}", "file_id", $"{file_id}", "file_name", $"{name_format(filename)}", "type", $"{2}");
            var result = await _post_text(_doupload_url, post_data);
            return _get_rescode(result);
        }

        internal class _MoveFolderList
        {
            internal LanZouCode code;
            internal Dictionary<long, string> folders;
        }

        /// <summary>
        /// 获取全部文件夹 id-name 列表，用于移动文件至新的文件夹
        /// </summary>
        /// <returns></returns>
        private async Task<_MoveFolderList> get_move_folders()
        {
            // 这里 file_id 可以为任意值,不会对结果产生影响
            var folders = new Dictionary<long, string>();
            folders.Add(-1, "LanZouCloud");
            var post_data = _post_data("task", $"{19}", "file_id", $"{-1}");
            var text = await _post_text(_doupload_url, post_data);
            var code = _get_rescode(text);
            if (code != LanZouCode.SUCCESS) // 获取失败或者网络异常
                return new _MoveFolderList() { code = code, folders = folders };

            var json = JsonMapper.ToObject(text);
            if (!json.ContainsKey("info"))  // 新注册用户无数据, info=None
                return new _MoveFolderList() { code = LanZouCode.SUCCESS, folders = folders };

            var info = json["info"];
            foreach (var j_folder in info)
            {
                var folder = (JsonData)j_folder;
                var folder_id = long.Parse(folder["folder_id"].ToString());
                var folder_name = folder["folder_name"].ToString();
                folders.Add(folder_id, folder_name);
            }
            return new _MoveFolderList() { code = LanZouCode.SUCCESS, folders = folders };
        }

        /// <summary>
        /// 移动文件到指定文件夹
        /// </summary>
        /// <param name="file_id"></param>
        /// <param name="folder_id"></param>
        /// <returns></returns>
        public async Task<LanZouCode> move_file(long file_id, long folder_id = -1)
        {
            // 移动回收站文件也返回成功(实际上行不通) (+_+)?
            var post_data = _post_data("task", $"{20}", "file_id", $"{file_id}", "folder_id", $"{folder_id}");
            var result = await _post_text(_doupload_url, post_data);
            LogInfo($"Move file file_id:{file_id} to folder_id:{folder_id}");
            return _get_rescode(result);
        }

        /// <summary>
        /// 移动文件夹(官方并没有直接支持此功能)
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <returns></returns>
        public async Task<LanZouCode> move_folder(long folder_id, long parent_folder_id = -1)
        {
            // 禁止移动文件夹到自身，禁止移动到 -2 这样的文件夹(文件还在,但是从此不可见)
            if (folder_id == parent_folder_id || parent_folder_id < -1)
                return LanZouCode.FAILED;

            var move_folder_list = await get_move_folders();
            if (move_folder_list.code != LanZouCode.SUCCESS)
                return move_folder_list.code;

            if (!move_folder_list.folders.TryGetValue(folder_id, out var folder_name))
            {
                LogWarning($"Not found folder id: {folder_id}");
                return LanZouCode.FAILED;
            }

            // 存在子文件夹，禁止移动
            var sub_folder_list = await get_folder_list(folder_id);
            if (sub_folder_list.code != LanZouCode.SUCCESS)
                return sub_folder_list.code;

            // 递归操作可能会产生大量请求,这里只允许移动单层文件夹
            if (sub_folder_list.folders.Count > 0)
            {
                LogWarning($"Found subdirectory in folder id: {folder_id}");
                return LanZouCode.FAILED;
            }

            // 在目标文件夹下创建同名文件夹
            var info = await get_share_info(folder_id, false);
            var mkdir_info = await mkdir(folder_name, parent_folder_id, info.desc);

            if (mkdir_info.code != LanZouCode.SUCCESS)
                return LanZouCode.FAILED;

            else if (mkdir_info.id == folder_id)            // 移动文件夹到同一目录
                return LanZouCode.FAILED;

            var code = await set_passwd(mkdir_info.id, info.pwd, false);     // 保持密码相同
            if (code != LanZouCode.SUCCESS)
                return code;

            // 移动子文件至新目录下
            var file_list = await get_file_list(folder_id);
            if (file_list.code != LanZouCode.SUCCESS)
                return code;

            foreach (var file in file_list.files)
            {
                code = await move_file(file.id, mkdir_info.id);
                if (code != LanZouCode.SUCCESS)
                {
                    LogWarning($"Move file Failed id：{file.id}");
                    return LanZouCode.FAILED;
                }
            }

            // 全部移动完成后删除原文件夹
            code = await delete(folder_id, false);
            if (code != LanZouCode.SUCCESS)
                return code;

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
        public async Task<DownloadInfo> down_file_by_url(string share_url, string save_dir,
            string pwd = "", bool overwrite = false, Action<DownloadProgressInfo> progress = null)
        {
            var down_info = new DownloadProgressInfo() { state = DownloadProgressInfo.State.Start, share_url = share_url };
            progress?.Invoke(down_info);

            if (!await is_file_url(share_url))
                return new DownloadInfo(LanZouCode.URL_INVALID, share_url);

            if (!Directory.Exists(save_dir))
                Directory.CreateDirectory(save_dir);

            var info = await get_durl_by_url(share_url, pwd);
            if (info.code != LanZouCode.SUCCESS)
                return new DownloadInfo(info.code, share_url);

            long? content_length;

            // 只请求头
            using (var resp_1 = await _get_resp(info.durl, null, 0, false, null, true))
            {
                if (resp_1 == null)
                    return new DownloadInfo(LanZouCode.FAILED, share_url, info.name);

                content_length = resp_1.Content.Headers.ContentLength;
            }


            // 对于 txt 文件, 可能出现没有 Content-Length 的情况
            // 此时文件需要下载一次才会出现 Content-Length
            // 这时候我们先读取一点数据, 再尝试获取一次, 通常只需读取 1 字节数据
            if (content_length == null)
            {
                // 请求内容
                using (var _stream = await _get_stream(info.durl))
                {
                    var _buffer = new byte[1];
                    var max_retries = 5;  // 5 次拿不到就算了

                    while (content_length == null && max_retries > 0)
                    {
                        max_retries -= 1;
                        LogWarning("Not found Content-Length in response headers");
                        LogInfo("Read 1 byte from stream...");
                        await _stream.ReadAsync(_buffer, 0, 1);

                        // 再请求一次试试，只请求头
                        using (var resp_3 = await _get_resp(info.durl, null, 0, false, null, true))
                        {
                            if (resp_3 == null)
                                return new DownloadInfo(LanZouCode.FAILED, share_url, info.name);

                            content_length = resp_3.Content.Headers.ContentLength;
                        }
                        LogInfo($"Content-Length: {content_length}");
                    }
                }
            }

            // 应该不会出现这种情况
            if (content_length == null)
                return new DownloadInfo(LanZouCode.FAILED, share_url, info.name);

            // 如果本地存在同名文件且设置了 overwrite, 则覆盖原文件
            // 否则修改下载文件路径, 自动在文件名后加序号
            var file_path = Path.Combine(save_dir, info.name);
            file_path = Path.GetFullPath(file_path);
            file_path = file_path.Replace("\\", "/");

            if (File.Exists(file_path))
            {
                if (overwrite)
                {
                    LogInfo($"Overwrite file {file_path}");
                    File.Delete(file_path); // 删除旧文件
                }
                else    // 自动重命名文件
                {
                    file_path = _auto_rename(file_path);
                    LogInfo($"File has already exists, auto rename to {file_path}");
                }
            }

            var tmp_file_path = file_path + ".download";  // 正在下载中的文件名
            LogInfo($"Save file to tmp path: {tmp_file_path}");

            // 支持断点续传下载
            long now_size = 0;
            bool is_continue = false;
            if (File.Exists(tmp_file_path))
            {
                now_size = new FileInfo(tmp_file_path).Length;  // 本地已经下载的文件大小
                is_continue = true;
            }

            var filename = Path.GetFileName(file_path);
            down_info.state = DownloadProgressInfo.State.Ready;
            down_info.filename = filename;
            down_info.is_continue = is_continue;
            progress?.Invoke(down_info);

            var headers = new Dictionary<string, string>(_headers);
            headers.Add("Range", $"bytes={now_size}-");
            HttpStatusCode statusCode;
            using (var resp = await _get_resp(info.durl, headers, 0, true, null, true))
            {
                if (resp == null)   // 网络异常
                    return new DownloadInfo(LanZouCode.FAILED, share_url, info.name);

                statusCode = resp.StatusCode;
            }

            if (statusCode == HttpStatusCode.RequestedRangeNotSatisfiable) // 已经下载完成
            {
                LogInfo($"File has already downloaded, just change filename to {file_path}");
            }
            else
            {
                using (var netStream = await _get_stream(info.durl, headers))
                {
                    int chunk_size = _chunk_size;
                    var chunk = new byte[chunk_size];
                    using (var fileStream = new FileStream(tmp_file_path, FileMode.Append,
                        FileAccess.Write, FileShare.Read, chunk_size))
                    {
                        down_info.state = DownloadProgressInfo.State.Downloading;

                        while (true)
                        {
                            var readLength = netStream.Read(chunk, 0, chunk_size);
                            if (readLength == 0)
                                break;

                            await fileStream.WriteAsync(chunk, 0, readLength);
                            now_size += readLength;

                            down_info.current = now_size;
                            down_info.total = (long)content_length;
                            progress?.Invoke(down_info);
                        }
                    }
                }
            }

            // 下载完成，改回正常文件名
            LogInfo($"move file to real path: {file_path}");
            File.Move(tmp_file_path, file_path);

            down_info.state = DownloadProgressInfo.State.Finish;
            progress?.Invoke(down_info);

            return new DownloadInfo(LanZouCode.SUCCESS, share_url, filename, file_path, is_continue);
        }

        /// <summary>
        /// 登录用户通过id下载文件(无需提取码)
        /// </summary>
        /// <param name="file_id"></param>
        /// <param name="save_dir"></param>
        /// <param name="overwrite"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task<DownloadInfo> down_file_by_id(long file_id, string save_dir,
            bool overwrite = false, Action<DownloadProgressInfo> progress = null)
        {
            var info = await get_share_info(file_id, true);
            if (info.code != LanZouCode.SUCCESS)
                return new DownloadInfo(info.code);

            return await down_file_by_url(info.url, save_dir, info.pwd, overwrite, progress);
        }


        /// <summary>
        /// 上传不超过 max_size 的文件
        /// </summary>
        /// <param name="file_path"></param>
        /// <param name="folder_id"></param>
        /// <param name="overwrite"></param>
        /// <param name="progress"></param>
        public async Task<UploadInfo> upload_file(string file_path, long folder_id = -1, bool overwrite = false,
            Action<UploadProgressInfo> progress = null)
        {
            file_path = Path.GetFullPath(file_path);
            file_path = file_path.Replace("\\", "/");

            var filename = name_format(Path.GetFileName(file_path));

            var up_info = new UploadProgressInfo() { state = UploadProgressInfo.State.Start, filename = filename };
            progress?.Invoke(up_info);

            if (!File.Exists(file_path))
                return new UploadInfo(LanZouCode.PATH_ERROR, filename, file_path);

            var file_size = new FileInfo(file_path).Length;

            if (file_size > _max_size * 1024 * 1024)
                return new UploadInfo(LanZouCode.OFFICIAL_LIMITED, filename, file_path);

            // 不允许上传的格式
            if (!is_name_valid(file_path))
                return new UploadInfo(LanZouCode.OFFICIAL_LIMITED, filename, file_path);

            // 文件已经存在同名文件就删除
            if (overwrite)
            {
                var file_list = await get_file_list(folder_id);
                if (file_list.code != LanZouCode.SUCCESS)
                    return new UploadInfo(file_list.code, filename, file_path);

                var same_files = file_list.files.FindAll(a => a.name == filename);
                foreach (var file in same_files)
                {
                    LogWarning($"Upload file {filename}, overwrite same name file id: {file.id}");
                    await delete(file.id, true);
                }
            }

            up_info.total = file_size;
            up_info.state = UploadProgressInfo.State.Ready;
            progress?.Invoke(up_info);

            LogInfo($"Upload file_path:{file_path} to folder_id:{folder_id}");

            var post_data = _post_data("task", $"{1}", "folder_id", $"{folder_id}", "id", "WU_FILE_0", "name", $"{filename}");

            string result;

            up_info.state = UploadProgressInfo.State.Uploading;
            progress?.Invoke(up_info);

            using (var fileStream = new FileStream(file_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var _content = new MultipartFormDataContent();
                foreach (var item in post_data)
                {
                    _content.Add(new StringContent(item.Value), item.Key);
                }
                _content.Add(new StreamContent(fileStream), "upload_file", filename);

                HttpContent content;
                if (progress != null)
                {
                    content = new ProgressableStreamContent(_content, _chunk_size, (current, total) =>
                    {
                        up_info.current = current;
                        up_info.total = total;
                        progress?.Invoke(up_info);
                    });
                }
                else
                {
                    content = _content;
                }

                result = await _post_text("https://pc.woozooo.com/fileup.php", content);
            }

            var code = _get_rescode(result);
            if (code != LanZouCode.SUCCESS)
                return new UploadInfo(code, filename, file_path);

            up_info.state = UploadProgressInfo.State.Finish;
            progress?.Invoke(up_info);

            var json = JsonMapper.ToObject(result);
            var file_id = long.Parse(json["text"][0]["id"].ToString());
            var f_id = json["text"][0]["f_id"].ToString();
            var is_newd = json["text"][0]["is_newd"].ToString();
            var share_url = is_newd + "/" + f_id;

            return new UploadInfo(LanZouCode.SUCCESS, filename, file_path, file_id, share_url);
        }

    }
}
