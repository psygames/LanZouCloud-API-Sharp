using LitJson;
using System.Collections.Generic;

namespace LanZouAPI
{
    /// <summary>
    /// 重写ToString，以JSON格式输出
    /// </summary>
    public class JsonStringObject
    {
        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }

    /// <summary>
    /// 蓝奏云返回结果信息
    /// </summary>
    public class Result : JsonStringObject
    {
        /// <summary>
        /// 蓝奏云结果码
        /// </summary>
        public LanZouCode code { get; internal set; }

        /// <summary>
        /// 结果码返回为错误时的消息
        /// </summary>
        public string errorMessage { get; internal set; }
    }

    internal class MoveFolderList : Result
    {
        internal Dictionary<long, string> folders { get; set; }
    }

    public class CloudFile : JsonStringObject
    {
        /// <summary>
        /// 文件唯一ID
        /// </summary>
        public long id { get; internal set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string name { get; internal set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        public string time { get; internal set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        public string size { get; internal set; }

        /// <summary>
        /// 文件类型
        /// </summary>
        public string type { get; internal set; }

        /// <summary>
        /// 下载次数
        /// </summary>
        public int downloads { get; internal set; }

        /// <summary>
        /// 是否存在提取码
        /// </summary>
        public bool hasPassword { get; internal set; }

        /// <summary>
        /// 是否有描述信息
        /// </summary>
        public bool hasDescription { get; internal set; }
    }

    public class CloudFolder : JsonStringObject
    {
        /// <summary>
        /// 文件夹唯一ID
        /// </summary>
        public long id { get; internal set; }

        /// <summary>
        /// 文件夹名
        /// </summary>
        public string name { get; internal set; }

        /// <summary>
        /// 是否存在提取码
        /// </summary>
        public bool hasPassword { get; internal set; }

        /// <summary>
        /// 文件夹描述信息
        /// </summary>
        public string description { get; internal set; }
    }

    public class CloudFileList : Result
    {
        /// <summary>
        /// 文件列表
        /// </summary>
        public List<CloudFile> files { get; internal set; }
    }

    public class CloudFolderList : Result
    {
        /// <summary>
        /// 文件夹列表
        /// </summary>
        public List<CloudFolder> folders { get; internal set; }
    }

    /// <summary>
    /// 文件分享页信息
    /// </summary>
    public class CloudFileInfo : Result
    {
        /// <summary>
        /// 文件名称
        /// </summary>
        public string name { get; internal set; }

        /// <summary>
        /// 提取码
        /// </summary>
        public string password { get; internal set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string description { get; internal set; }

        /// <summary>
        /// 分享链接
        /// </summary>
        public string url { get; internal set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        public string size { get; internal set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        public string time { get; internal set; }

        /// <summary>
        /// 文件类型
        /// </summary>
        public string type { get; internal set; }

        /// <summary>
        /// 直连地址（下载地址）
        /// </summary>
        public string durl { get; internal set; }

        public CloudFileInfo(LanZouCode code, string password = null, string url = null,
             string name = null, string type = null, string time = null, string size = null,
             string description = null, string durl = null)
        {
            this.code = code;
            this.password = password;
            this.url = url;
            this.name = name;
            this.type = type;
            this.time = time;
            this.size = size;
            this.description = description;
            this.durl = durl;
        }
    }


    /// <summary>
    /// 专为 CloudFolderInfo 使用，指其下子文件夹
    /// </summary>
    public class SubFolder : JsonStringObject
    {
        /// <summary>
        /// 文件夹名
        /// </summary>
        public string name { get; internal set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string description { get; internal set; }

        /// <summary>
        /// 分享链接
        /// </summary>
        public string url { get; internal set; }
    }

    /// <summary>
    /// 专为 CloudFolderInfo 使用，指其下子文件
    /// </summary>
    public class SubFile : JsonStringObject
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string name { get; internal set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        public string time { get; internal set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        public string size { get; internal set; }

        /// <summary>
        /// 文件类型
        /// </summary>
        public string type { get; internal set; }

        /// <summary>
        /// 分享链接
        /// </summary>
        public string url { get; internal set; }
    }

    /// <summary>
    /// 文件夹分享页信息，包括子文件（夹）信息
    /// </summary>
    public class CloudFolderInfo : Result
    {
        /// <summary>
        /// 文件夹唯一ID
        /// </summary>
        public long id { get; internal set; }

        /// <summary>
        /// 文件夹名
        /// </summary>
        public string name { get; internal set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public string time { get; internal set; }

        /// <summary>
        /// 提取码
        /// </summary>
        public string password { get; internal set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string description { get; internal set; }

        /// <summary>
        /// 分享链接
        /// </summary>
        public string url { get; internal set; }

        /// <summary>
        /// 子文件夹列表
        /// </summary>
        public List<SubFolder> folders { get; internal set; }

        /// <summary>
        /// 子文件列表
        /// </summary>
        public List<SubFile> files { get; internal set; }

        public CloudFolderInfo(LanZouCode code)
        {
            this.code = code;
        }

        public CloudFolderInfo(LanZouCode code, long id, string name, string time, string password,
            string description, string url, List<SubFolder> folders, List<SubFile> files)
        {
            this.code = code;
            this.id = id;
            this.name = name;
            this.time = time;
            this.description = description;
            this.password = password;
            this.url = url;
            this.folders = folders;
            this.files = files;
        }
    }

    /// <summary>
    /// 分享文件（夹）信息
    /// </summary>
    public class ShareInfo : Result
    {
        /// <summary>
        /// 文件（夹）名
        /// </summary>
        public string name { get; internal set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string description { get; internal set; }

        /// <summary>
        /// 分享链接
        /// </summary>
        public string url { get; internal set; }

        /// <summary>
        /// 提取码
        /// </summary>
        public string password { get; internal set; }

        public ShareInfo(LanZouCode code)
        {
            this.code = code;
        }

        public ShareInfo(LanZouCode code, string name, string url, string description, string password)
        {
            this.code = code;
            this.description = description;
            this.name = name;
            this.password = password;
            this.url = url;
        }
    }

    /// <summary>
    /// 创建文件夹返回结果
    /// </summary>
    public class CreateFolderInfo : Result
    {
        /// <summary>
        /// 文件夹唯一ID
        /// </summary>
        public long id { get; internal set; }

        /// <summary>
        /// 文件夹名
        /// </summary>
        public string name { get; internal set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string description { get; internal set; }

        public CreateFolderInfo(LanZouCode code)
        {
            this.code = code;
        }

        public CreateFolderInfo(LanZouCode code, long id, string name, string description)
        {
            this.code = code;
            this.id = id;
            this.name = name;
            this.description = description;
        }
    }

    public class DownloadInfo : Result
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string fileName { get; internal set; }

        /// <summary>
        /// 下载路径
        /// </summary>
        public string filePath { get; internal set; }

        /// <summary>
        /// 分享链接
        /// </summary>
        public string url { get; internal set; }

        /// <summary>
        /// 直连地址（下载地址）
        /// </summary>
        public string durl { get; internal set; }

        /// <summary>
        /// 是否断点续传
        /// </summary>
        public bool isContinue { get; internal set; }

        public DownloadInfo(LanZouCode code, string share_url = null,
            string filename = null, string file_path = null, bool is_continue = false)
        {
            this.code = code;
            this.url = share_url;
            this.fileName = filename;
            this.filePath = file_path;
            this.isContinue = is_continue;
        }
    }

    public class UploadInfo : Result
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string fileName { get; internal set; }

        /// <summary>
        /// 本地文件路径
        /// </summary>
        public string filePath { get; internal set; }

        /// <summary>
        /// 文件唯一ID
        /// </summary>
        public long id { get; internal set; }

        /// <summary>
        /// 分享链接
        /// </summary>
        public string url { get; internal set; }

        public UploadInfo(LanZouCode code, string filename = null,
            string file_path = null, long file_id = 0, string share_url = null)
        {
            this.code = code;
            this.fileName = filename;
            this.filePath = file_path;
            this.id = file_id;
            this.url = share_url;
        }
    }

    public enum ProgressState
    {
        Start,
        Ready,
        Progressing,
        Finish,
    }

    /// <summary>
    /// 上传/下载 进度信息
    /// </summary>
    public class ProgressInfo : JsonStringObject
    {
        /// <summary>
        /// 状态
        /// </summary>
        public ProgressState state { get; internal set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string fileName { get; internal set; }

        /// <summary>
        /// 当前大小（字节）
        /// </summary>
        public long current { get; internal set; }

        /// <summary>
        /// 总大小（字节）
        /// </summary>
        public long total { get; internal set; }

        internal ProgressInfo(ProgressState state, string filename = null, long current = 0, long total = 0)
        {
            this.state = state;
            this.fileName = filename;
            this.current = current;
            this.total = total;
        }
    }

}
