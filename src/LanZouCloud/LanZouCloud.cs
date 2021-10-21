using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LanZouCloudAPI
{
    public partial class LanZouCloud
    {
        private int _chunk_size = 4096;             // 上传或下载是的块大小
        private int _timeout = 15;                  // 每个请求的超时(不包含下载响应体的用时)
        private int _max_size = 100;                // 单个文件大小上限 MB
        private string _host_url = "https://pan.lanzoui.com";
        private string _doupload_url = "https://pc.woozooo.com/doupload.php";
        private string _account_url = "https://pc.woozooo.com/account.php";
        // private string _mydisk_url = "https://pc.woozooo.com/mydisk.php";
        private string _proxy = null;
        private Dictionary<string, string> _headers = new Dictionary<string, string>()
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.100 Safari/537.36" },
            { "Referer", "https://pc.woozooo.com/mydisk.php" },
            { "Accept-Language", "zh-CN,zh;q=0.9" },  // 提取直连必需设置这个，否则拿不到数据
        };


        #region Private APIs（内部使用）
        /// <summary>
        /// 获取全部文件夹 id-name 列表，用于移动文件至新的文件夹
        /// </summary>
        /// <returns></returns>
        private async Task<MoveFolderList> GetMoveFolders()
        {
            LogInfo("Get move folders", nameof(GetMoveFolders));
            MoveFolderList result;

            // 这里 file_id 可以为任意值,不会对结果产生影响
            var folders = new Dictionary<long, string>();
            folders.Add(-1, "LanZouCloud");
            var post_data = _post_data("task", $"{19}", "file_id", $"{-1}");
            var text = await _post_text(_doupload_url, post_data);
            var _res = _get_result(text);

            if (_res.code != LanZouCode.SUCCESS) // 获取失败或者网络异常
            {
                result = new MoveFolderList(_res.code, _res.message, folders);
            }
            else
            {
                var json = JsonMapper.ToObject(text);
                if (!json.ContainsKey("info"))  // 新注册用户无数据, info=None
                {
                    Log("New user has no move folders", LogLevel.Warning, nameof(GetMoveFolders));
                    result = new MoveFolderList(LanZouCode.SUCCESS, _success_msg, folders);
                }
                else
                {
                    var info = json["info"];
                    foreach (var j_folder in info)
                    {
                        var folder = (JsonData)j_folder;
                        var folder_id = long.Parse(folder["folder_id"].ToString());
                        var folder_name = folder["folder_name"].ToString();
                        folders.Add(folder_id, folder_name);
                    }

                    result = new MoveFolderList(LanZouCode.SUCCESS, _success_msg, folders);
                }
            }

            LogResult(result, nameof(GetMoveFolders));
            return result;
        }

        /// <summary>
        /// 重命名文件夹及其描述
        /// </summary>
        /// <param name="folder_id"></param>
        /// <param name="folder_name"></param>
        /// <param name="desc"></param>
        /// <returns></returns>
        private async Task<Result> SetFolderInfo(long folder_id, string folder_name, string desc = "")
        {
            LogInfo($"Set folder info file id: {folder_id}, folder name: {folder_name}, description: {desc}", nameof(SetFolderInfo));

            // 不能用于重命名文件，id 无效仍然返回成功
            folder_name = name_format(folder_name);
            var post_data = _post_data("task", $"{4}", "folder_id", $"{folder_id}", "folder_name", $"{folder_name}", "folder_description", $"{desc}");
            var text = await _post_text(_doupload_url, post_data);
            var result = _get_result(text);

            LogResult(result, nameof(SetFolderInfo));
            return result;
        }
        #endregion


        #region Public APIs （需要登录）
        /// <summary>
        /// 设置单文件大小限制(会员用户可超过 100M)
        /// </summary>
        /// <param name="max_size">最大文件大小(MB)</param>
        /// <returns></returns>
        public Result SetMaxSize(int max_size = 100)
        {
            LogInfo($"Set file max size: {max_size}", nameof(SetMaxSize));

            Result result;
            if (max_size < 100)
            {
                result = new Result(LanZouCode.FAILED, $"文件大小({max_size})不能小于100MB");
            }
            else
            {
                _max_size = max_size;
                result = new Result(LanZouCode.SUCCESS, _success_msg);
            }

            LogResult(result, nameof(SetMaxSize));
            return result;
        }

        /// <summary>
        /// 通过cookie登录，在浏览器中获得网站的Cookie。
        /// </summary>
        /// <param name="ylogin">woozooo.com -> Cookie -> ylogin</param>
        /// <param name="phpdisk_info">pc.woozooo.com -> Cookie -> phpdisk_info</param>
        /// <returns></returns>
        public async Task<Result> Login(string ylogin, string phpdisk_info)
        {
            var print_pinfo = !string.IsNullOrEmpty(phpdisk_info) && phpdisk_info.Length > 10
                                ? phpdisk_info.Substring(0, 10) + "..."
                                : phpdisk_info;

            LogInfo($"Login with cookie ylogin: {ylogin}, phpdisk_info: {print_pinfo}", nameof(Login));

            Result result;
            _set_cookie("woozooo.com", "ylogin", ylogin);
            _set_cookie("pc.woozooo.com", "phpdisk_info", phpdisk_info);
            var html = await _get_text(_account_url);

            if (string.IsNullOrEmpty(html))
            {
                result = _get_result(html);
            }
            else if (html.Contains("网盘用户登录"))
            {
                result = new Result(LanZouCode.FAILED, "登录失败，Cookie已过期或不存在。");
            }
            else
            {
                result = new Result(LanZouCode.SUCCESS, _success_msg);
            }

            LogResult(result, nameof(Login));
            return result;
        }

        /// <summary>
        /// 注销
        /// </summary>
        /// <returns></returns>
        public async Task<Result> Logout()
        {
            LogInfo("Logout", nameof(Logout));

            Result result;
            var html = await _get_text($"{_account_url}?action=logout");

            if (string.IsNullOrEmpty(html))
            {
                result = _get_result(html);
            }
            else if (html.Contains("退出系统成功"))
            {
                result = new Result(LanZouCode.SUCCESS, _success_msg);
            }
            else
            {
                result = new Result(LanZouCode.FAILED, "退出系统失败");
            }

            LogResult(result, nameof(Logout));
            return result;
        }

        /// <summary>
        /// 把文件放到回收站
        /// </summary>
        /// <param name="file_id"></param>
        /// <param name="is_file"></param>
        /// <returns></returns>
        public async Task<Result> DeleteFile(long file_id)
        {
            LogInfo($"Delete file of file id: {file_id}", nameof(DeleteFile));
            var post_data = _post_data("task", $"{6}", "file_id", $"{file_id}");
            var text = await _post_text(_doupload_url, post_data);
            var result = _get_result(text);
            LogResult(result, nameof(DeleteFile));
            return result;
        }

        /// <summary>
        /// 把文件夹放到回收站，必须是单层文件夹（无子文件夹的）
        /// </summary>
        /// <param name="folder_id"></param>
        /// <param name="is_file"></param>
        /// <returns></returns>
        public async Task<Result> DeleteFolder(long folder_id)
        {
            LogInfo($"Delete folder of folder id: {folder_id}", nameof(DeleteFolder));
            var post_data = _post_data("task", $"{3}", "folder_id", $"{folder_id}");
            var text = await _post_text(_doupload_url, post_data);
            var result = _get_result(text);
            LogResult(result, nameof(DeleteFolder));
            return result;
        }

        /// <summary>
        /// 获取文件列表
        /// </summary>
        /// <param name="folder_id">文件夹ID，默认值 -1 表示根路径</param>
        /// <param name="page_begin">开始页数，1为起始页</param>
        /// <param name="page_end">结束页数（包含）</param>
        /// <returns></returns>
        public async Task<CloudFileList> GetFileList(long folder_id = -1, int page_begin = 1, int page_end = 99)
        {
            LogInfo($"Get file list of folder id: {folder_id}, begin page: {page_begin}, end page: {page_end}", nameof(GetFileList));

            CloudFileList result;
            var page = page_begin;
            var file_list = new List<CloudFile>();
            while (page <= page_end)
            {
                var post_data = _post_data("task", $"{5}", "folder_id", $"{folder_id}", "pg", $"{page}");
                var text = await _post_text(_doupload_url, post_data);
                var _res = _get_result(text);
                if (_res.code != LanZouCode.SUCCESS)
                {
                    result = new CloudFileList(_res.code, _res.message, file_list);
                    LogResult(result, nameof(GetFileList));
                    return result;
                }

                var json = JsonMapper.ToObject(text);

                if (int.Parse(json["info"].ToString()) == 0)         // 已经拿到了全部的文件信息
                    break;

                foreach (var _json in json["text"])
                {
                    var f_json = (JsonData)_json;
                    file_list.Add(new CloudFile()
                    {
                        id = long.Parse(f_json["id"].ToString()),                               // 文件ID
                        name = f_json["name_all"].ToString().Replace("&amp;", "&"),             // 文件名
                        time = time_format((string)f_json["time"]),                             // 上传时间
                        size = f_json["size"].ToString().Replace(",", ""),                      // 文件大小
                        type = Path.GetExtension(f_json["name_all"].ToString()).Substring(1),   // 文件类型
                        downloads = int.Parse(f_json["downs"].ToString()),                      // 下载次数
                        hasPassword = int.Parse(f_json["onof"].ToString()) == 1,                // 是否存在提取码
                        hasDescription = int.Parse(f_json["is_des"].ToString()) == 1,           // 是否存在描述
                    });
                }

                page += 1;  // 下一页
            }

            result = new CloudFileList(LanZouCode.SUCCESS, _success_msg, file_list);
            LogResult(result, nameof(GetFileList));
            return result;
        }

        /// <summary>
        /// 获取子文件夹列表
        /// </summary>
        /// <param name="folder_id">文件夹ID，默认值 -1 表示根路径</param>
        /// <returns></returns>
        public async Task<CloudFolderList> GetFolderList(long folder_id = -1)
        {
            LogInfo($"Get folder list of folder id: {folder_id}", nameof(GetFolderList));
            CloudFolderList result;

            var folder_list = new List<CloudFolder>();
            var post_data = _post_data("task", $"{47}", "folder_id", $"{folder_id}");
            var text = await _post_text(_doupload_url, post_data);
            var _res = _get_result(text);

            if (_res.code != LanZouCode.SUCCESS)
            {
                result = new CloudFolderList(_res.code, _res.message, folder_list);
            }
            else
            {
                var json = JsonMapper.ToObject(text);

                foreach (var _json in json["text"])
                {
                    var f_json = (JsonData)_json;
                    folder_list.Add(new CloudFolder()
                    {
                        id = long.Parse(f_json["fol_id"].ToString()),
                        name = f_json["name"].ToString(),
                        hasPassword = int.Parse(f_json["onof"].ToString()) == 1,
                        description = f_json["folder_des"].ToString().Trim('[', ']'),
                    });
                }
                result = new CloudFolderList(LanZouCode.SUCCESS, _success_msg, folder_list);
            }

            LogResult(result, nameof(GetFolderList));
            return result;
        }

        /// <summary>
        /// 通过文件ID，获取文件各种信息(包括下载直链)
        /// </summary>
        /// <param name="file_id">文件ID</param>
        /// <returns></returns>
        public async Task<CloudFileInfo> GetFileInfo(long file_id)
        {
            LogInfo($"Get file info of file id: {file_id}", nameof(GetFileInfo));

            CloudFileInfo result = null;

            var _result = await GetFileShareInfo(file_id);
            if (_result.code != LanZouCode.SUCCESS)
            {
                result = new CloudFileInfo(_result.code, _result.message);
            }
            else
            {
                result = await GetFileInfoByUrl(_result.url, _result.password);
            }

            LogResult(result, nameof(GetFileInfo));
            return result;
        }


        /// <summary>
        /// 通过文件夹ID，获取文件夹及其子文件信息
        /// </summary>
        /// <param name="folder_id">文件夹ID</param>
        /// <param name="page_begin">开始页数，1为起始页</param>
        /// <param name="page_end">结束页数（包含）</param>
        /// <returns></returns>
        public async Task<CloudFolderInfo> GetFolderInfo(long folder_id, int page_begin = 1, int page_end = 99)
        {
            LogInfo($"Get folder info of folder id: {folder_id}, page begin: {page_begin}, page end: {page_end}", nameof(GetFolderInfo));

            CloudFolderInfo result;

            var _share = await GetFolderShareInfo(folder_id);
            if (_share.code != LanZouCode.SUCCESS)
            {
                result = new CloudFolderInfo(_share.code, _share.message);
            }
            else
            {
                result = await GetFolderInfoByUrl(_share.url, _share.password, page_begin, page_end);
            }

            LogResult(result, nameof(GetFolderInfo));
            return result;
        }

        /// <summary>
        /// 获取文件提取码、分享链接
        /// </summary>
        /// <param name="file_id"></param>
        /// <returns></returns>
        public async Task<ShareInfo> GetFileShareInfo(long file_id)
        {
            LogInfo($"Get file share info of file id: {file_id}", nameof(GetFileShareInfo));

            ShareInfo result;

            var post_data = _post_data("task", $"{22}", "file_id", $"{file_id}");

            // 获取分享链接和密码用
            var text = await _post_text(_doupload_url, post_data);
            var _res = _get_result(text);
            if (_res.code != LanZouCode.SUCCESS)
            {
                result = new ShareInfo(LanZouCode.NETWORK_ERROR, _res.message);
            }
            else
            {
                var f_info = JsonMapper.ToObject(text)["info"];

                // 有效性校验
                if (f_info.ContainsKey("f_id") && f_info["f_id"].ToString() == "i")
                {
                    result = new ShareInfo(LanZouCode.ID_ERROR, "ID校验失败");
                }
                else
                {
                    // onof=1 时，存在有效的提取码; onof=0 时不存在提取码，但是 pwd 字段还是有一个无效的随机密码
                    var pwd = f_info["onof"].ToString() == "1" ? f_info["pwd"].ToString() : "";
                    var url = f_info["is_newd"] + "//" + f_info["f_id"];        // 文件的分享链接需要拼凑
                    var post_data_1 = _post_data("task", $"{12}", "file_id", $"{file_id}");
                    var _text = await _post_text(_doupload_url, post_data_1);   // 文件信息
                    var __res = _get_result(_text);
                    if (__res.code != LanZouCode.SUCCESS)
                    {
                        result = new ShareInfo(LanZouCode.NETWORK_ERROR, _res.message);
                    }
                    else
                    {
                        var file_info = JsonMapper.ToObject(_text);
                        // 无后缀的文件名(获得后缀又要发送请求,没有就没有吧,尽可能减少请求数量)
                        var name = file_info["text"].ToString();
                        var desc = file_info["info"].ToString();

                        result = new ShareInfo(LanZouCode.SUCCESS, _success_msg, name, url, desc, pwd);
                    }
                }
            }

            LogResult(result, nameof(GetFileShareInfo));
            return result;
        }

        /// <summary>
        /// 获取文件夹提取码、分享链接
        /// </summary>
        /// <param name="folder_id"></param>
        /// <returns></returns>
        public async Task<ShareInfo> GetFolderShareInfo(long folder_id)
        {
            LogInfo($"Get folder share info of folder id: {folder_id}", nameof(GetFolderShareInfo));

            ShareInfo result;

            var post_data = _post_data("task", $"{18}", "folder_id", $"{folder_id}");

            // 获取分享链接和密码用
            var text = await _post_text(_doupload_url, post_data);
            var _res = _get_result(text);
            if (_res.code != LanZouCode.SUCCESS)
            {
                result = new ShareInfo(LanZouCode.NETWORK_ERROR, _res.message);
            }
            else
            {
                var f_info = JsonMapper.ToObject(text)["info"];

                // 有效性校验
                if (f_info.ContainsKey("name") && string.IsNullOrEmpty(f_info["name"].ToString()))
                {
                    result = new ShareInfo(LanZouCode.ID_ERROR, "Name校验失败");
                }
                else
                {
                    // onof=1 时，存在有效的提取码; onof=0 时不存在提取码，但是 pwd 字段还是有一个无效的随机密码
                    var pwd = f_info["onof"].ToString() == "1" ? f_info["pwd"].ToString() : "";
                    var url = f_info["new_url"].ToString();     // 文件夹的分享链接可以直接拿到
                    var name = f_info["name"].ToString();       // 文件夹名
                    var desc = f_info["des"].ToString();        // 文件夹描述
                    result = new ShareInfo(LanZouCode.SUCCESS, _success_msg, name, url, desc, pwd);
                }
            }

            LogResult(result, nameof(GetFolderShareInfo));
            return result;
        }

        /// <summary>
        /// <para>设置文件的提取码</para>
        /// <para>id 无效或者 id 类型不对应仍然返回成功 :(</para>
        /// <para>文件提取码 2 - 6 位</para>
        /// </summary>
        /// <param name="file_id"></param>
        /// <param name="pwd"></param>
        /// <param name="is_file"></param>
        /// <returns></returns>
        public async Task<Result> SetFilePassword(long file_id, string pwd = "")
        {
            LogInfo($"Set file password of file id: {file_id}", nameof(SetFilePassword));

            var pwd_status = string.IsNullOrEmpty(pwd) ? 0 : 1;  // 是否开启密码
            var post_data = _post_data("task", $"{23}", "file_id", $"{file_id}", "shows", $"{pwd_status}", "shownames", $"{pwd}");
            var text = await _post_text(_doupload_url, post_data);
            var result = _get_result(text);

            LogResult(result, nameof(SetFilePassword));
            return result;
        }

        /// <summary>
        /// <para>设置文件夹的提取码, 现在非会员用户不允许关闭提取码</para>
        /// <para>id 无效或者 id 类型不对应仍然返回成功 :(</para>
        /// <para>文件夹提取码长度 0 - 12 位</para>
        /// </summary>
        /// <param name="folder_id"></param>
        /// <param name="pwd"></param>
        /// <param name="is_file"></param>
        /// <returns></returns>
        public async Task<Result> SetFolderPassword(long folder_id, string pwd = "")
        {
            LogInfo($"Set folder password of folder id: {folder_id}", nameof(SetFolderPassword));

            var pwd_status = string.IsNullOrEmpty(pwd) ? 0 : 1;  // 是否开启密码
            var post_data = _post_data("task", $"{16}", "folder_id", $"{folder_id}", "shows", $"{pwd_status}", "shownames", $"{pwd}");
            var text = await _post_text(_doupload_url, post_data);
            var result = _get_result(text);

            LogResult(result, nameof(SetFolderPassword));
            return result;
        }

        /// <summary>
        /// 创建文件夹(同时设置描述)
        /// </summary>
        /// <param name="folder_name"></param>
        /// <param name="parent_id"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public async Task<CreateFolderInfo> CreateFolder(string folder_name, long parent_id = -1, string description = "")
        {
            LogInfo($"Create folder named: {folder_name} in folder id: {parent_id}, description: {description}", nameof(CreateFolder));

            CreateFolderInfo result = null;
            CloudFolder exist_folder = null;
            MoveFolderList raw_move_folder_list = null;

            folder_name = folder_name.Replace(' ', '_');    // 文件夹名称不能包含空格
            folder_name = name_format(folder_name);         // 去除非法字符

            var folder_list = await GetFolderList(parent_id);
            if (folder_list.code != LanZouCode.SUCCESS)
            {
                result = new CreateFolderInfo(folder_list.code, folder_list.message);
            }
            else if ((exist_folder = folder_list.folders.Find(a => a.name == folder_name)) != null)
            {
                // 如果文件夹已经存在，直接返回
                result = new CreateFolderInfo(LanZouCode.SUCCESS, _success_msg, exist_folder.id, exist_folder.name, exist_folder.description);
            }
            else if ((raw_move_folder_list = await GetMoveFolders()).code != LanZouCode.SUCCESS)
            {
                result = new CreateFolderInfo(raw_move_folder_list.code, raw_move_folder_list.message);
            }

            if (result != null && result.code != LanZouCode.SUCCESS)
            {
                LogResult(result, nameof(CreateFolder));
                return result;
            }

            var post_data = _post_data("task", $"{2}", "parent_id", $"{parent_id}", "folder_name", $"{folder_name}", "folder_description", $"{description}");
            var text = await _post_text(_doupload_url, post_data);  // 创建文件夹
            var _res = _get_result(text);
            if (_res.code != LanZouCode.SUCCESS)
            {
                result = new CreateFolderInfo(_res.code, _res.message);
                LogResult(result, nameof(CreateFolder));
                return result;
            }

            // 允许在不同路径创建同名文件夹, 移动时可通过 get_move_paths() 区分
            var now_move_folder_list = await GetMoveFolders();
            if (now_move_folder_list.code != LanZouCode.SUCCESS)
            {
                result = new CreateFolderInfo(now_move_folder_list.code, now_move_folder_list.message);
                LogResult(result, nameof(CreateFolder));
                return result;
            }

            foreach (var kv in now_move_folder_list.folders)
            {
                if (!raw_move_folder_list.folders.ContainsKey(kv.Key)) // 不在原始列表中，即新增文件夹
                {
                    // 创建文件夹成功
                    result = new CreateFolderInfo(LanZouCode.SUCCESS, _success_msg, kv.Key, kv.Value, description);
                    LogResult(result, nameof(CreateFolder));
                    return result;
                }
            }

            // 没有找到匹配的文件夹，创建失败
            result = new CreateFolderInfo(LanZouCode.FAILED, $"Move folders no match: {folder_name}");
            LogResult(result, nameof(CreateFolder));
            return result;
        }

        /// <summary>
        /// 设置文件描述
        /// </summary>
        /// <param name="file_id"></param>
        /// <param name="description">文件描述（一旦设置了值，就不能再设置为空）</param>
        /// <returns></returns>
        public async Task<Result> SetFileDescription(long file_id, string description = "")
        {
            LogInfo($"Set file description: {description} of file id: {file_id}", nameof(SetFileDescription));

            // 文件描述一旦设置了值，就不能再设置为空
            var post_data = _post_data("task", $"{11}", "file_id", $"{file_id}", "desc", $"{description}");
            var text = await _post_text(_doupload_url, post_data);
            var result = _get_result(text);

            LogResult(result, nameof(SetFileDescription));
            return result;
        }

        /// <summary>
        /// 设置文件夹描述
        /// </summary>
        /// <param name="folder_id"></param>
        /// <param name="description">文件夹描述（可以置空）</param>
        /// <returns></returns>
        public async Task<Result> SetFolderDescription(long folder_id, string description = "")
        {
            LogInfo($"Set folder description: {description} of folder id: {folder_id}", nameof(SetFolderDescription));

            Result result;
            var share = await GetFolderShareInfo(folder_id);
            if (share.code != LanZouCode.SUCCESS)
            {
                result = new Result(share.code, share.message);
            }
            else
            {
                result = await SetFolderInfo(folder_id, share.name, description);
            }

            LogResult(result, nameof(SetFolderDescription));
            return result;
        }

        /// <summary>
        /// 允许会员重命名文件(无法修后缀名)
        /// </summary>
        /// <param name="file_id"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task<Result> RenameFile(long file_id, string filename)
        {
            LogInfo($"Rename file to: {filename} of file id: {file_id}", nameof(RenameFile));

            // 重命名文件要开会员
            var post_data = _post_data("task", $"{46}", "file_id", $"{file_id}", "file_name", $"{name_format(filename)}", "type", $"{2}");
            var text = await _post_text(_doupload_url, post_data);
            var result = _get_result(text);

            LogResult(result, nameof(RenameFile));
            return result;
        }

        /// <summary>
        /// 重命名文件夹
        /// </summary>
        /// <param name="folder_id"></param>
        /// <param name="folder_name"></param>
        /// <returns></returns>
        public async Task<Result> RenameFolder(long folder_id, string folder_name)
        {
            LogInfo($"Rename foler to: {folder_name} of folder id: {folder_id}", nameof(RenameFolder));

            Result result;

            var share = await GetFolderShareInfo(folder_id);
            if (share.code != LanZouCode.SUCCESS)
            {
                result = new Result(share.code, share.message);
            }
            else
            {
                result = await SetFolderInfo(folder_id, folder_name, share.description);
            }

            LogResult(result, nameof(RenameFolder));
            return result;
        }

        /// <summary>
        /// 移动文件到指定文件夹
        /// </summary>
        /// <param name="file_id"></param>
        /// <param name="parent_folder_id"></param>
        /// <returns></returns>
        public async Task<Result> MoveFile(long file_id, long parent_folder_id = -1)
        {
            LogInfo($"MoveFile file to folder id: {parent_folder_id} of file id: {file_id}", nameof(MoveFile));

            // 移动回收站文件也返回成功(实际上行不通) (+_+)?
            var post_data = _post_data("task", $"{20}", "file_id", $"{file_id}", "folder_id", $"{parent_folder_id}");
            var text = await _post_text(_doupload_url, post_data);
            var result = _get_result(text);

            LogResult(result, nameof(MoveFile));
            return result;
        }

        /// <summary>
        /// 移动文件夹(官方并没有直接支持此功能)
        /// 这里只允许移动单层文件夹（即没有子文件夹）
        /// </summary>
        /// <param name="folder_id"></param>
        /// <param name="parent_folder_id"></param>
        /// <returns></returns>
        public async Task<Result> MoveFolder(long folder_id, long parent_folder_id = -1)
        {
            LogInfo($"MoveFile folder to folder id: {parent_folder_id} of foler id: {folder_id}", nameof(MoveFolder));

            Result result = null;
            MoveFolderList move_folder_list = null;
            CloudFolderList sub_folder_list = null;
            ShareInfo share = null;
            CreateFolderInfo new_folder = null;
            Result setpwd = null;
            CloudFileList file_list = null;
            string folder_name = null;

            // 禁止移动文件夹到自身，禁止移动到 -2 这样的文件夹(文件还在,但是从此不可见)
            if (folder_id == parent_folder_id || parent_folder_id < -1)
            {
                result = new Result(LanZouCode.FAILED, $"Invalid parent folder id: {parent_folder_id}");
            }
            else if ((move_folder_list = await GetMoveFolders()).code != LanZouCode.SUCCESS)
            {
                result = new Result(move_folder_list.code, move_folder_list.message);
            }
            else if (!move_folder_list.folders.TryGetValue(folder_id, out folder_name))
            {
                result = new Result(LanZouCode.FAILED, $"Not found folder id: {folder_id}");
            }
            else if ((sub_folder_list = await GetFolderList(folder_id)).code != LanZouCode.SUCCESS)
            {
                // 存在子文件夹，禁止移动
                result = new Result(sub_folder_list.code, sub_folder_list.message);
            }
            else if (sub_folder_list.folders.Count > 0)
            {
                // 递归操作可能会产生大量请求,这里只允许移动单层文件夹
                result = new Result(LanZouCode.FAILED, $"Found subdirectory in folder id: {folder_id}");
            }
            else if ((share = await GetFolderShareInfo(folder_id)).code != LanZouCode.SUCCESS)
            {
                // 在目标文件夹下创建同名文件夹
                result = new Result(share.code, share.message);
            }
            else if ((new_folder = await CreateFolder(folder_name, parent_folder_id, share.description))
                .code != LanZouCode.SUCCESS)
            {
                result = new Result(new_folder.code, new_folder.message);
            }
            else if (new_folder.id == folder_id)
            {
                // 不可以 移动文件夹 到同一目录
                result = new Result(LanZouCode.FAILED, $"Create Folder is same id: {folder_id}");
            }
            else if ((setpwd = await SetFolderPassword(new_folder.id, share.password)).code != LanZouCode.SUCCESS)
            {
                // 保持密码一致
                result = new Result(setpwd.code, setpwd.message);
            }
            else if ((file_list = await GetFileList(folder_id)).code != LanZouCode.SUCCESS)
            {
                // 移动子文件至新目录下
                result = new Result(file_list.code, file_list.message);
            }

            // 以上步骤失败，直接返回
            if (result != null && result.code != LanZouCode.SUCCESS)
            {
                LogResult(result, nameof(MoveFolder));
                return result;
            }

            foreach (var file in file_list.files)
            {
                var moveFile = await MoveFile(file.id, new_folder.id);
                if (moveFile.code != LanZouCode.SUCCESS)
                {
                    // 任意文件移动失败，直接返回
                    result = new Result(moveFile.code, moveFile.message);
                    LogResult(result, nameof(MoveFolder));
                    return result;
                }
            }

            // 全部移动完成后删除原文件夹
            var del = await DeleteFolder(folder_id);
            if (del.code != LanZouCode.SUCCESS)
            {
                result = new Result(del.code, del.message);
                // TODO: 删除回收站？
            }
            else
            {
                result = new Result(LanZouCode.SUCCESS, _success_msg);
            }

            LogResult(result, nameof(MoveFolder));
            return result;
        }

        /// <summary>
        /// 登录用户通过id下载文件(无需提取码)
        /// </summary>
        /// <param name="file_id"></param>
        /// <param name="save_dir"></param>
        /// <param name="overwrite"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task<DownloadInfo> DownloadFile(long file_id, string save_dir,
            bool overwrite = false, IProgress<ProgressInfo> progress = null)
        {
            LogInfo($"Download file of file id: {file_id}, save to: {save_dir}, overwrire: {overwrite}", nameof(DownloadFile));

            DownloadInfo result;
            var share = await GetFileShareInfo(file_id);
            if (share.code != LanZouCode.SUCCESS)
            {
                result = new DownloadInfo(share.code, share.message);
            }
            else
            {
                result = await DownloadFileByUrl(share.url, save_dir, share.password, overwrite, progress);
            }

            LogResult(result, nameof(DownloadFile));
            return result;
        }

        /// <summary>
        /// 上传不超过 max_size 的文件
        /// </summary>
        /// <param name="file_path"></param>
        /// <param name="folder_id"></param>
        /// <param name="overwrite"></param>
        /// <param name="progress"></param>
        public async Task<UploadInfo> UploadFile(string file_path, long folder_id = -1, bool overwrite = false,
            IProgress<ProgressInfo> progress = null)
        {
            LogInfo($"Upload file: {file_path} to folder id: {folder_id}, overwrire: {overwrite}", nameof(UploadFile));

            UploadInfo result = null;

            file_path = Path.GetFullPath(file_path);
            file_path = file_path.Replace("\\", "/");

            if (!File.Exists(file_path))
            {
                result = new UploadInfo(LanZouCode.PATH_ERROR, $"File not found: {file_path}", Path.GetFileName(file_path), file_path);
                LogResult(result, nameof(UploadFile));
                return result;
            }

            var filename = name_format(Path.GetFileName(file_path));
            var file_size = new FileInfo(file_path).Length;

            var p_start = new ProgressInfo(ProgressState.Start, filename, 0, file_size);
            progress?.Report(p_start);

            if (file_size > _max_size * 1024 * 1024)
            {
                result = new UploadInfo(LanZouCode.OFFICIAL_LIMITED, $"上传超过最大文件大小({_max_size}MB): {file_path}", filename, file_path);
            }
            else if (!is_name_valid(file_path))
            {
                // 不允许上传的格式
                result = new UploadInfo(LanZouCode.OFFICIAL_LIMITED, $"文件后缀名不符合官方限制: {file_path}", filename, file_path);
            }

            // 粗略判断，直接返回
            if (result != null && result.code != LanZouCode.SUCCESS)
            {
                LogResult(result, nameof(UploadFile));
                return result;
            }

            // 文件已经存在同名文件就删除
            if (overwrite)
            {
                var file_list = await GetFileList(folder_id);
                if (file_list.code != LanZouCode.SUCCESS)
                {
                    result = new UploadInfo(file_list.code, file_list.message, filename, file_path);
                    LogResult(result, nameof(UploadFile));
                    return result;
                }

                var same_files = file_list.files.FindAll(a => a.name == filename);
                foreach (var file in same_files)
                {
                    Log($"Upload {filename}, overwrite file id: {file.id}", LogLevel.Info, nameof(UploadFile));
                    var del = await DeleteFile(file.id);
                    if (del.code != LanZouCode.SUCCESS)
                    {
                        // 删除失败，只输出警告信息，不终止流程
                        Log($"Upload {filename}, overwrite file id: {file.id} failed.", LogLevel.Warning, nameof(UploadFile));
                    }
                }
            }

            var p_ready = new ProgressInfo(ProgressState.Ready, filename, 0, file_size);
            progress?.Report(p_ready);

            Log($"Upload stream begin, file: {file_path} to folder id: {folder_id}", LogLevel.Info, nameof(UploadFile));

            string text = null;

            var upload_url = "https://pc.woozooo.com/fileup.php";

            for (int i = 0; i < http_retries; i++)
            {
                try
                {
                    using (var fileStream = new FileStream(file_path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var _content = new MultipartFormDataContent();

                        _content.Add(new StringContent("1"), "task");
                        _content.Add(new StringContent(folder_id.ToString()), "folder_id");
                        _content.Add(new StringContent("WU_FILE_0"), "id");
                        _content.Add(new StringContent(filename, Encoding.UTF8), "name");
                        _content.Add(new UTF8EncodingStreamContent(fileStream, "upload_file", filename));

                        HttpContent content;
                        if (progress != null)
                        {
                            var p_uploading = new ProgressInfo(ProgressState.Progressing, filename, 0, file_size);
                            content = new ProgressableStreamContent(_content, _chunk_size, (_current, _total) =>
                            {
                                p_uploading.current = _current;
                                p_uploading.total = _total;
                                progress?.Report(p_uploading);
                            });
                        }
                        else
                        {
                            content = _content;
                        }
                        using (content)
                        {
                            using (var client = _get_client(null, 3600))
                            {
                                using (var resp = await client.PostAsync(upload_url, content))
                                {
                                    resp.EnsureSuccessStatusCode();
                                    text = await resp.Content.ReadAsStringAsync();
                                    Print(JsonMapper.ToObject(text).ToJson(), LogLevel.Info);
                                }
                            }
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    Log($"Http Error: {ex.Message}", LogLevel.Error, nameof(UploadFile));
                    if (i < http_retries) Log($"Retry({i + 1}): {upload_url}", LogLevel.Info, nameof(UploadFile));
                }
            }

            var _res = _get_result(text);
            if (_res.code != LanZouCode.SUCCESS)
            {
                result = new UploadInfo(_res.code, _res.message, filename, file_path);
            }
            else
            {
                var json = JsonMapper.ToObject(text);
                var file_id = long.Parse(json["text"][0]["id"].ToString());
                var f_id = json["text"][0]["f_id"].ToString();
                var is_newd = json["text"][0]["is_newd"].ToString();
                var share_url = is_newd + "/" + f_id;

                var p_finish = new ProgressInfo(ProgressState.Finish, filename, file_size, file_size);
                progress?.Report(p_finish);

                await Task.Yield(); // 保证 progress report 到达
                await Task.Yield(); // 保证 progress report 到达

                result = new UploadInfo(LanZouCode.SUCCESS, _success_msg, filename, file_path, file_id, share_url);
            }

            LogResult(result, nameof(UploadFile));
            return result;
        }

        #endregion


        #region Public APIs （无需登录）

        /// <summary>
        /// 通过分享链接，下载文件(需提取码)
        /// </summary>
        /// <param name="share_url"></param>
        /// <param name="save_dir"></param>
        /// <param name="pwd"></param>
        /// <param name="overwrite">文件已存在时是否强制覆盖</param>
        /// <param name="progress">用于显示下载进度</param>
        /// <returns></returns>
        public async Task<DownloadInfo> DownloadFileByUrl(string share_url, string save_dir,
            string pwd = "", bool overwrite = false, IProgress<ProgressInfo> progress = null)
        {
            LogInfo($"Download file of url: {share_url}, save to: {save_dir}, overwrire: {overwrite}", nameof(DownloadFileByUrl));

            DownloadInfo result = null;
            CloudFileInfo file_info = null;

            var p_start = new ProgressInfo(ProgressState.Start);
            progress?.Report(p_start);

            if (!await is_file_url(share_url))
            {
                result = new DownloadInfo(LanZouCode.URL_INVALID, $"Invalid url: {share_url}", share_url);
            }
            else if ((file_info = await GetFileInfoByUrl(share_url, pwd)).code != LanZouCode.SUCCESS)
            {
                result = new DownloadInfo(file_info.code, file_info.message, share_url);
            }

            if (result != null && result.code != LanZouCode.SUCCESS)
            {
                LogResult(result, nameof(DownloadFileByUrl));
                return result;
            }

            // 只请求头
            var _con_len = (await _get_headers(file_info.durl))?.ContentLength;

            // 对于 txt 文件, 可能出现没有 Content-Length 的情况
            // 此时文件需要下载一次才会出现 Content-Length
            // 这时候我们先读取一点数据, 再尝试获取一次, 通常只需读取 1 字节数据
            if (_con_len == null)
            {
                Log("Not found Content-Length in response headers", LogLevel.Warning, nameof(DownloadFileByUrl));

                for (int i = 0; i < http_retries; i++)
                {
                    try
                    {
                        // 请求内容
                        using (var client = _get_client())
                        {
                            using (var _stream = await client.GetStreamAsync(file_info.durl))
                            {
                                var _buffer = new byte[1];
                                var max_retries = 5;  // 5 次拿不到就算了

                                while (_con_len == null && max_retries > 0)
                                {
                                    max_retries -= 1;
                                    await _stream.ReadAsync(_buffer, 0, 1);

                                    // 再请求一次试试，只请求头
                                    _con_len = (await _get_headers(file_info.durl))?.ContentLength;
                                    Log($"Retry to get Content-Length: {_con_len}", LogLevel.Info, nameof(DownloadFileByUrl));
                                }
                            }
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log($"Http Error: {ex.Message}", LogLevel.Error, nameof(DownloadFileByUrl));
                        if (i < http_retries) Log($"Retry({i + 1}): {file_info.durl}", LogLevel.Info, nameof(DownloadFileByUrl));
                    }
                }
            }

            // 应该不会出现这种情况
            if (_con_len == null)
            {
                result = new DownloadInfo(LanZouCode.FAILED, "Not found Content-Length", share_url, file_info.name);
                LogResult(result, nameof(DownloadFileByUrl));
                return result;
            }

            var content_length = _con_len.GetValueOrDefault();

            // 如果本地存在同名文件且设置了 overwrite, 则覆盖原文件
            // 否则修改下载文件路径, 自动在文件名后加序号
            var file_path = Path.Combine(save_dir, file_info.name);
            file_path = Path.GetFullPath(file_path);
            file_path = file_path.Replace("\\", "/");

            if (File.Exists(file_path))
            {
                if (overwrite)
                {
                    Log($"Overwrite file {file_path}", LogLevel.Info, nameof(DownloadFileByUrl));
                    File.Delete(file_path);     // 删除旧文件
                }
                else
                {
                    file_path = _auto_rename(file_path);    // 自动重命名文件
                    Log($"File has already exists, auto rename to {file_path}", LogLevel.Info, nameof(DownloadFileByUrl));
                }
            }

            var tmp_file_path = file_path + ".download";  // 正在下载中的文件名
            Log($"Save file to tmp path: {tmp_file_path}", LogLevel.Info, nameof(DownloadFileByUrl));

            // 支持断点续传下载
            long now_size = 0;
            bool is_continue = false;
            bool is_downloaded = false;
            if (File.Exists(tmp_file_path))
            {
                now_size = new FileInfo(tmp_file_path).Length;  // 本地已经下载的文件大小
                is_continue = true;
                if (now_size > content_length)      // 大小错误，删除重新下载
                {
                    File.Delete(tmp_file_path);
                    now_size = 0;
                    is_continue = false;
                }
                else if (now_size == content_length) // 已经下载完成，只是未改后缀名
                {
                    is_downloaded = true;
                }
            }

            var filename = Path.GetFileName(file_path);

            var p_ready = new ProgressInfo(ProgressState.Ready, filename, now_size, content_length);
            progress?.Report(p_ready);
            bool isDownloadSuccess = false;

            if (is_downloaded)
            {
                Log($"Has already downloaded: {tmp_file_path}", LogLevel.Info, nameof(DownloadFileByUrl));
                var p_downloading = new ProgressInfo(ProgressState.Progressing, filename, now_size, content_length);
                progress?.Report(p_downloading);
                isDownloadSuccess = true;
            }
            else
            {
                await Task.Run(async () =>
                {
                    var headers = new Dictionary<string, string>(_headers);
                    headers.Add("Range", $"bytes={now_size}-");
                    int chunk_size = _chunk_size;
                    var chunk = new byte[chunk_size];
                    var p_downloading = new ProgressInfo(ProgressState.Progressing, filename, now_size, content_length);
                    for (int i = 0; i < http_retries; i++)
                    {
                        try
                        {
                            using (var client = _get_client(headers))
                            {
                                using (var netStream = await client.GetStreamAsync(file_info.durl))
                                {
                                    if (!Directory.Exists(save_dir))
                                    {
                                        Log($"Save dir not exsit, auto create: {save_dir}", LogLevel.Info, nameof(DownloadFileByUrl));
                                        Directory.CreateDirectory(save_dir);
                                    }

                                    using (var fileStream = new FileStream(tmp_file_path, FileMode.Append,
                                         FileAccess.Write, FileShare.Read, chunk_size))
                                    {
                                        while (true)
                                        {
                                            var readLength = await netStream.ReadAsync(chunk, 0, chunk_size);
                                            if (readLength == 0)
                                                break;

                                            await fileStream.WriteAsync(chunk, 0, readLength);
                                            now_size += readLength;

                                            p_downloading.current = now_size;
                                            progress?.Report(p_downloading);
                                        }
                                    }
                                }
                            }

                            isDownloadSuccess = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log($"Http Error: {ex.Message}", LogLevel.Error, nameof(DownloadFileByUrl));
                            if (i < http_retries) Log($"Retry({i + 1}): {file_info.durl}", LogLevel.Info, nameof(DownloadFileByUrl));
                        }
                    }

                });
            }

            if (!isDownloadSuccess)
            {
                result = new DownloadInfo(LanZouCode.NETWORK_ERROR, "Download failed, retry over.");
            }
            else
            {
                // 下载完成，改回正常文件名
                Log($"Move file to real path: {file_path}", LogLevel.Info, nameof(DownloadFileByUrl));
                File.Move(tmp_file_path, file_path);

                var p_finish = new ProgressInfo(ProgressState.Finish, filename, now_size, content_length);
                progress?.Report(p_finish);

                await Task.Yield(); // 保证 progress report 到达
                await Task.Yield(); // 保证 progress report 到达

                result = new DownloadInfo(LanZouCode.SUCCESS, _success_msg, share_url, filename, file_path, is_continue);
            }

            LogResult(result, nameof(DownloadFileByUrl));
            return result;
        }

        /// <summary>
        /// 通过文件分享链接，获取文件各种信息(包括下载直链，需提取码)
        /// </summary>
        /// <param name="share_url">文件分享链接</param>
        /// <param name="pwd">文件提取码(如果有的话)</param>
        /// <returns></returns>
        public async Task<CloudFileInfo> GetFileInfoByUrl(string share_url, string pwd = "")
        {
            LogInfo($"Get file info of url: {share_url}", nameof(GetFileInfoByUrl));

            CloudFileInfo result = null;
            string first_page = null;

            if (!await is_file_url(share_url))  // 非文件链接返回错误
            {
                result = new CloudFileInfo(LanZouCode.URL_INVALID, $"Invalid url: {share_url}", pwd, share_url);
            }
            else if (string.IsNullOrEmpty(first_page = await _get_text(share_url)))  // 文件分享页面(第一页)
            {
                result = new CloudFileInfo(LanZouCode.NETWORK_ERROR, _network_error_msg, pwd, share_url);
            }
            else if (first_page.Contains("acw_sc__v2"))
            {
                // 在页面被过多访问或其他情况下，有时候会先返回一个加密的页面，其执行计算出一个acw_sc__v2后放入页面后再重新访问页面才能获得正常页面
                // 若该页面进行了js加密，则进行解密，计算acw_sc__v2，并加入cookie
                var acw_sc__v2 = calc_acw_sc__v2(first_page);
                _set_cookie(new Uri(share_url).Host, "acw_sc__v2", $"{acw_sc__v2}");
                Log($"Set Cookie: acw_sc__v2={acw_sc__v2}", LogLevel.Info, nameof(GetFileInfoByUrl));
                first_page = await _get_text(share_url);   // 文件分享页面(第一页)
                if (string.IsNullOrEmpty(first_page))
                {
                    result = new CloudFileInfo(LanZouCode.NETWORK_ERROR, _network_error_msg, pwd, share_url);
                }
            }

            if (result != null && result.code != LanZouCode.SUCCESS)
            {
                LogResult(result, nameof(GetFileInfoByUrl));
                return result;
            }

            first_page = remove_notes(first_page);  // 去除网页里的注释
            if (first_page.Contains("文件取消") || first_page.Contains("文件不存在"))
            {
                result = new CloudFileInfo(LanZouCode.FILE_CANCELLED, $"文件取消或不存在: {share_url}", pwd, share_url);
                LogResult(result, nameof(GetFileInfoByUrl));
                return result;
            }

            JsonData link_info;
            string f_name;
            string f_time;
            string f_size;
            string f_desc;
            string f_type;

            // 这里获取下载直链 304 重定向前的链接
            if (first_page.Contains("id=\"pwdload\"") || first_page.Contains("id=\"passwddiv\""))   // 文件设置了提取码时
            {
                if (string.IsNullOrEmpty(pwd))
                {
                    result = new CloudFileInfo(LanZouCode.LACK_PASSWORD, $"分享链接需要提取码: {share_url}", pwd, share_url);  // 没给提取码直接退出
                    LogResult(result, nameof(GetFileInfoByUrl));
                    return result;
                }

                var sign = Regex.Match(first_page, "sign=(\\w+?)&").Groups[1].Value;
                var post_data = _post_data("action", "downprocess", "sign", $"{sign}", "p", $"{pwd}");
                var link_info_str = await _post_text(_host_url + "/ajaxm.php", post_data);  // 保存了重定向前的链接信息和文件名
                var second_page = await _get_text(share_url);  // 再次请求文件分享页面，可以看见文件名，时间，大小等信息(第二页)
                if (string.IsNullOrEmpty(link_info_str) || string.IsNullOrEmpty(second_page))
                {
                    result = new CloudFileInfo(LanZouCode.NETWORK_ERROR, _network_error_msg, pwd, share_url);
                    LogResult(result, nameof(GetFileInfoByUrl));
                    return result;
                }

                link_info = JsonMapper.ToObject(link_info_str);
                second_page = remove_notes(second_page);

                // 提取文件信息
                f_name = link_info["inf"].ToString().Replace("*", "_");
                f_type = Path.GetExtension(f_name).Substring(1);

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

                f_type = Path.GetExtension(f_name).Substring(1);

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
                {
                    result = new CloudFileInfo(LanZouCode.NETWORK_ERROR, _network_error_msg, pwd, share_url, f_name, f_type, f_time, f_size, f_desc);
                    LogResult(result, nameof(GetFileInfoByUrl));
                    return result;
                }

                first_page = remove_notes(first_page);
                // 一般情况 sign 的值就在 data 里，有时放在变量后面
                var sign = Regex.Match(first_page, "'sign':(.+?),").Groups[1].Value;
                if (sign.Length < 20)  // 此时 sign 保存在变量里面, 变量名是 sign 匹配的字符
                    sign = Regex.Match(first_page, $"var {sign}\\s*=\\s*'(.+?)';").Groups[1].Value;

                var post_data = _post_data("action", "downprocess", "sign", $"{sign}", "ves", $"{1}");
                var link_info_str = await _post_text(_host_url + "/ajaxm.php", post_data);
                if (string.IsNullOrEmpty(link_info_str))
                {
                    result = new CloudFileInfo(LanZouCode.NETWORK_ERROR, _network_error_msg, pwd, share_url, f_name, f_type, f_time, f_size, f_desc);
                    LogResult(result, nameof(GetFileInfoByUrl));
                    return result;
                }

                link_info = JsonMapper.ToObject(link_info_str);
            }

            // 这里开始获取文件直链
            if ((int)link_info["zt"] != 1)  //# 返回信息异常，无法获取直链
            {
                result = new CloudFileInfo(LanZouCode.FAILED, "无法获取直链", pwd, share_url, f_name, f_type, f_time, f_size, f_desc);
                LogResult(result, nameof(GetFileInfoByUrl));
                return result;
            }

            var fake_url = link_info["dom"].ToString() + "/file/" + link_info["url"].ToString();  // 假直连，存在流量异常检测
            string download_page_html = null;
            string redirect_url = null;

            for (int i = 0; i < http_retries; i++)
            {
                try
                {
                    using (var client = _get_client(null, 0, false))
                    {
                        using (var resp = await client.GetAsync(fake_url))
                        {
                            if (resp.StatusCode == HttpStatusCode.OK)
                            {
                                // 假直连，需要重新获取
                            }
                            else if (resp.StatusCode == HttpStatusCode.Found)
                            {
                                redirect_url = resp.Headers.Location.AbsoluteUri;// 重定向后的真直链
                            }
                            else // 未知网络错误
                            {
                                result = new CloudFileInfo(LanZouCode.NETWORK_ERROR, _network_error_msg, pwd, share_url, f_name, f_type, f_time, f_size, f_desc);
                                LogResult(result, nameof(GetFileInfoByUrl));
                                return result;
                            }

                            // download_page.encoding = 'utf-8'
                            download_page_html = await resp.Content.ReadAsStringAsync();
                            download_page_html = remove_notes(download_page_html);
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    Log($"Http Error: {ex.Message}", LogLevel.Error, nameof(GetFileInfoByUrl));
                    if (i < http_retries) Log($"Retry({i + 1}): {fake_url}", LogLevel.Info, nameof(GetFileInfoByUrl));
                }
            }

            string direct_url;
            if (!download_page_html.Contains("网络异常"))  // 没有遇到验证码
            {
                direct_url = redirect_url;
            }
            else // 遇到验证码，验证后才能获取下载直链
            {
                Log($"Get direct url need verify code, force sleep 2 seconds.", LogLevel.Warning, nameof(GetFileInfoByUrl));
                var file_token = Regex.Match(download_page_html, "'file':'(.+?)'").Groups[1].Value;
                var file_sign = Regex.Match(download_page_html, "'sign':'(.+?)'").Groups[1].Value;
                var check_api = "https://vip.d0.baidupan.com/file/ajax.php";
                var post_data = _post_data("file", $"{file_token}", "el", $"{2}", "sign", $"{file_sign}");
                await Task.Delay(2000);     // 这里必需等待2s, 否则直链返回 ?SignError
                var text = await _post_text(check_api, post_data);
                var json = JsonMapper.ToObject(text);
                direct_url = json["url"].ToString();
                if (string.IsNullOrEmpty(direct_url) || direct_url.Contains("SignError"))
                {
                    result = new CloudFileInfo(LanZouCode.CAPTCHA_ERROR, "验证失败", pwd, share_url, f_name, f_type, f_time, f_size, f_desc);
                    LogResult(result, nameof(GetFileInfoByUrl));
                    return result;
                }
            }

            result = new CloudFileInfo(LanZouCode.SUCCESS, _success_msg, pwd, share_url, f_name, f_type, f_time, f_size, f_desc, direct_url);
            LogResult(result, nameof(GetFileInfoByUrl));
            return result;
        }

        /// <summary>
        /// 通过分享链接，获取文件夹及其子文件信息（需提取码）
        /// </summary>
        /// <param name="share_url">分享链接</param>
        /// <param name="pwd">提取码</param>
        /// <param name="page_begin">开始页数，1为起始页</param>
        /// <param name="page_end">结束页数（包含）</param>
        /// <returns></returns>
        public async Task<CloudFolderInfo> GetFolderInfoByUrl(string share_url, string pwd = "", int page_begin = 1, int page_end = 99)
        {
            LogInfo($"Get folder info of url: {share_url}, begin page : {page_begin}, end page: {page_end}", nameof(GetFolderInfoByUrl));

            CloudFolderInfo result = null;

            if (await is_file_url(share_url))
            {
                result = new CloudFolderInfo(LanZouCode.URL_INVALID, $"Invalid url: {share_url}");
                LogResult(result, nameof(GetFolderInfoByUrl));
                return result;
            }

            var html = await _get_text(share_url);
            if (string.IsNullOrEmpty(html))
            {
                result = new CloudFolderInfo(LanZouCode.NETWORK_ERROR, _network_error_msg);
            }
            else if (html.Contains("文件不存在") || html.Contains("文件取消"))
            {
                result = new CloudFolderInfo(LanZouCode.FILE_CANCELLED, "文件取消或不存在");
            }
            // 要求输入密码, 用户描述中可能带有"输入密码",所以不用这个字符串判断
            else if (string.IsNullOrEmpty(pwd) && (html.Contains("id=\"pwdload\"") || html.Contains("id=\"passwddiv\"")))
            {
                result = new CloudFolderInfo(LanZouCode.LACK_PASSWORD, $"分享链接需要提取码: {share_url}");
            }
            else if (html.Contains("acw_sc__v2"))
            {
                // 在页面被过多访问或其他情况下，有时候会先返回一个加密的页面，其执行计算出一个acw_sc__v2后放入页面后再重新访问页面才能获得正常页面
                // 若该页面进行了js加密，则进行解密，计算acw_sc__v2，并加入cookie
                var acw_sc__v2 = calc_acw_sc__v2(html);
                _set_cookie(new Uri(share_url).Host, "acw_sc__v2", acw_sc__v2);
                Log($"Set Cookie: acw_sc__v2={acw_sc__v2}", LogLevel.Info, nameof(GetFolderInfoByUrl));
                html = await _get_text(share_url);  // 文件分享页面(第一页)
                if (string.IsNullOrEmpty(html))
                {
                    result = new CloudFolderInfo(LanZouCode.NETWORK_ERROR, _network_error_msg);
                }
            }

            // 以上粗略校验失败，直接返回错误
            if (result != null && result.code != LanZouCode.SUCCESS)
            {
                LogResult(result, nameof(GetFolderInfoByUrl));
                return result;
            }

            // 获取文件需要的参数
            html = remove_notes(html);

            var re_lx = Regex.Match(html, "'lx':'?(\\d)'?,");
            if (!re_lx.Success)
            {
                result = new CloudFolderInfo(LanZouCode.FAILED, $"Regex Failed: lx");
                LogResult(result, nameof(GetFolderInfoByUrl));
                return result;
            }

            var lx = re_lx.Groups[1].Value;

            var re_t = Regex.Match(html, "var [0-9a-z]{6} = '(\\d{10})';");
            if (!re_t.Success)
            {
                result = new CloudFolderInfo(LanZouCode.FAILED, $"Regex Failed: t");
                LogResult(result, nameof(GetFolderInfoByUrl));
                return result;
            }

            var t = re_t.Groups[1].Value;

            var re_k = Regex.Match(html, "var [0-9a-z]{6} = '([0-9a-z]{15,})';");
            if (!re_k.Success)
            {
                result = new CloudFolderInfo(LanZouCode.FAILED, $"Regex Failed: k");
                LogResult(result, nameof(GetFolderInfoByUrl));
                return result;
            }

            var k = re_k.Groups[1].Value;

            // 文件夹的信息
            var re_folder_id = Regex.Match(html, "'fid':'?(\\d+)'?,");
            if (!re_folder_id.Success)
            {
                result = new CloudFolderInfo(LanZouCode.FAILED, $"Regex Failed: folder_id");
                LogResult(result, nameof(GetFolderInfoByUrl));
                return result;
            }

            var folder_id = long.Parse(re_folder_id.Groups[1].Value);

            var re_folder_name = Regex.Match(html, "var.+?='(.+?)';\n.+document.title");
            if (!re_folder_name.Success) re_folder_name = Regex.Match(html, "<div class=\"user-title\">(.+?)</div>");
            if (!re_folder_name.Success)
            {
                result = new CloudFolderInfo(LanZouCode.FAILED, $"Regex Failed: folder_name");
                LogResult(result, nameof(GetFolderInfoByUrl));
                return result;
            }

            var folder_name = re_folder_name.Groups[1].Value;

            var folder_time = "";
            var re_folder_time = Regex.Match(html, "class=\"rets\">([\\d\\-]+?)<a");  // ['%m-%d'] 或者 None (vip自定义)
            if (re_folder_time.Success) folder_time = re_folder_time.Groups[1].Value;

            var folder_desc = "";
            var re_folder_desc = Regex.Match(html, "id=\"filename\">(.+?)</span>");
            if (!re_folder_desc.Success) re_folder_desc = Regex.Match(html, "<div class=\"user-radio-\\d\"></div>(.+?)</div>");
            if (re_folder_desc.Success) folder_desc = re_folder_desc.Groups[1].Value;


            // 提取子文件夹信息(vip用户分享的文件夹可以递归包含子文件夹)
            var sub_folders = new List<SubFolder>();
            // 文件夹描述放在 filesize 一栏, 迷惑行为
            var re_all_sub_folders = Regex.Matches(html, "mbxfolder\"><a href=\"(.+?)\".+class=\"filename\">(.+?)<div class=\"filesize\">(.*?)</div>");
            for (int i = 0; i < re_all_sub_folders.Count; i++)
            {
                var m = re_all_sub_folders[i];
                var url = _host_url + m.Groups[0].Value;
                var name = m.Groups[1].Value;
                var desc = m.Groups[2].Value;
                var folder = new SubFolder() { name = name, description = desc, url = url };
                sub_folders.Add(folder);
            }

            Log($"Get folder info, sub folders: {sub_folders.Count}", LogLevel.Info, nameof(GetFolderInfoByUrl));

            // 提取文件夹下全部文件
            var page = page_begin;
            var sub_files = new List<SubFile>();
            while (page <= page_end)
            {
                if (page > page_begin)  // 连续的请求需要稍等一下
                    await Task.Delay(800);

                Log($"Get folder info page {page}...", LogLevel.Info, nameof(GetFolderInfoByUrl));

                var post_data = _post_data("lx", lx, "pg", $"{page}", "k", k, "t", t, "fid", $"{folder_id}", "pwd", pwd);
                var text = await _post_text(_host_url + "/filemoreajax.php", post_data);
                if (string.IsNullOrEmpty(text))
                {
                    result = new CloudFolderInfo(LanZouCode.NETWORK_ERROR, _network_error_msg);
                    LogResult(result, nameof(GetFolderInfoByUrl));
                    return result;
                }

                var json = JsonMapper.ToObject(text);

                var zt = int.Parse(json["zt"].ToString());
                if (zt == 1)        // 成功获取一页文件信息
                {
                    foreach (var j_f in json["text"])
                    {
                        var _json = (JsonData)j_f;
                        var _name = _json["name_all"].ToString();   // 文件名
                        var _time = _json["time"].ToString();       // 上传时间
                        var _size = _json["size"].ToString();       // 文件大小
                        var _type = Path.GetExtension(_json["name_all"].ToString()).Substring(1); // 文件格式
                        var _url = _host_url + "/" + _json["id"].ToString();    // 文件分享链接
                        var _sub_file = new SubFile() { name = _name, time = _time, size = _size, type = _type, url = _url };
                        sub_files.Add(_sub_file);
                    };
                    page += 1;      // 下一页
                    continue;
                }
                else if (zt == 2)   // 已经拿到全部的文件信息
                {
                    break;
                }
                else if (zt == 3)   // 提取码错误
                {
                    result = new CloudFolderInfo(LanZouCode.PASSWORD_ERROR, $"Password error: {pwd}");
                    LogResult(result, nameof(GetFolderInfoByUrl));
                    return result;
                }
                else if (zt == 4)   // 发送频繁等原因，需要重试
                {
                    Log($"Get folder info page {page} failed({json["info"]})", LogLevel.Warning, nameof(GetFolderInfoByUrl));
                    continue;
                }
                else                // 其它未知错误
                {
                    result = new CloudFolderInfo(LanZouCode.FAILED, $"Unkown error zt:{zt}");
                    LogResult(result, nameof(GetFolderInfoByUrl));
                    return result;
                }
            }

            Log($"Get folder info, sub files: {sub_files.Count}", LogLevel.Info, nameof(GetFolderInfoByUrl));

            result = new CloudFolderInfo(LanZouCode.SUCCESS, _success_msg, folder_id, folder_name,
                folder_time, pwd, folder_desc, share_url, sub_folders, sub_files);
            LogResult(result, nameof(GetFolderInfoByUrl));
            return result;
        }
        #endregion

    }
}
