using LitJson;
using System;
using System.Collections.Generic;

namespace LanZouAPI
{
    public class JsonStringObject
    {
        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }

    public class Result : JsonStringObject
    {
        public LanZouCode code;
        public string error_message;
    }

    internal class MoveFolderList : Result
    {
        internal Dictionary<long, string> folders;
    }

    public class CloudFile : JsonStringObject
    {
        public long id;
        public string name;
        public string time;
        public string size;
        public string type;
        public int downs;
        public bool has_pwd;
        public bool has_des;
    }

    public class CloudFolder : JsonStringObject
    {
        public long id;
        public string name;
        public bool has_pwd;
        public string desc;
    }

    public class CloudFileList : Result
    {
        public List<CloudFile> files;
    }

    public class CloudFolderList : Result
    {
        public List<CloudFolder> folders;
    }

    public class CloudFileInfo : Result
    {
        public string name;
        public string pwd;
        public string desc;
        public string url;
        public string size;
        public string time;
        public string type;
        public string durl;

        public CloudFileInfo(LanZouCode code)
        {
            this.code = code;
        }

        public CloudFileInfo(LanZouCode code, string pwd, string url)
        {
            this.code = code;
            this.pwd = pwd;
            this.url = url;
        }

        public CloudFileInfo(LanZouCode code, string name, string time, string size, string desc, string pwd, string url)
        {
            this.code = code;
            this.name = name;
            this.time = time;
            this.size = size;
            this.desc = desc;
            this.pwd = pwd;
            this.url = url;
        }

        public CloudFileInfo(LanZouCode code, string name, string time, string size, string desc, string pwd, string url, string type, string durl)
        {
            this.code = code;
            this.name = name;
            this.time = time;
            this.size = size;
            this.desc = desc;
            this.pwd = pwd;
            this.url = url;
            this.type = type;
            this.durl = durl;
        }
    }


    /// <summary>
    /// 专为 CloudFolderInfo 使用，指其下子文件夹
    /// </summary>
    public class SubFolder : JsonStringObject
    {
        public string name;
        public string desc;
        public string url;
    }

    /// <summary>
    /// 专为 CloudFolderInfo 使用，指其下子文件
    /// </summary>
    public class SubFile : JsonStringObject
    {
        public string name;
        public string time;
        public string size;
        public string type;
        public string url;
    }

    public class CloudFolderInfo : Result
    {
        public long id;
        public string name;
        public string time;
        public string pwd;
        public string desc;
        public string url;

        public List<SubFolder> folders;
        public List<SubFile> files;

        public CloudFolderInfo(LanZouCode code)
        {
            this.code = code;
        }

        public CloudFolderInfo(LanZouCode code, long id, string name, string time, string pwd,
            string desc, string url, List<SubFolder> folders, List<SubFile> files)
        {
            this.code = code;
            this.id = id;
            this.name = name;
            this.time = time;
            this.desc = desc;
            this.pwd = pwd;
            this.url = url;
            this.folders = folders;
            this.files = files;
        }
    }

    public class ShareInfo : Result
    {
        public string name;
        public string desc;
        public string url;
        public string pwd;

        public ShareInfo(LanZouCode code)
        {
            this.code = code;
        }

        public ShareInfo(LanZouCode code, string name, string url, string desc, string pwd)
        {
            this.code = code;
            this.desc = desc;
            this.name = name;
            this.pwd = pwd;
            this.url = url;
        }
    }

    public class CreateFolderInfo : Result
    {
        public long id;
        public string name;
        public string desc;

        public CreateFolderInfo(LanZouCode code)
        {
            this.code = code;
        }

        public CreateFolderInfo(LanZouCode code, long id, string name, string desc)
        {
            this.code = code;
            this.id = id;
            this.name = name;
            this.desc = desc;
        }
    }

    public class DownloadInfo : Result
    {
        public string filename;
        public string file_path;
        public string share_url;
        public bool is_continue;

        public DownloadInfo(LanZouCode code)
        {
            this.code = code;
        }

        public DownloadInfo(LanZouCode code, string share_url)
        {
            this.code = code;
            this.share_url = share_url;
        }

        public DownloadInfo(LanZouCode code, string share_url, string filename)
        {
            this.code = code;
            this.share_url = share_url;
            this.filename = filename;
        }

        public DownloadInfo(LanZouCode code, string share_url, string filename, string file_path, bool is_continue)
        {
            this.code = code;
            this.share_url = share_url;
            this.filename = filename;
            this.file_path = file_path;
            this.is_continue = is_continue;
        }
    }

    public class UploadInfo : Result
    {
        public string filename;
        public string file_path;
        public long file_id;
        public string share_url;

        public UploadInfo(LanZouCode code, string filename, string file_path)
        {
            this.code = code;
            this.filename = filename;
            this.file_path = file_path;
        }

        public UploadInfo(LanZouCode code, string filename, string file_path, long file_id, string share_url)
        {
            this.code = code;
            this.filename = filename;
            this.file_path = file_path;
            this.file_id = file_id;
            this.share_url = share_url;
        }
    }

    public enum ProgressState
    {
        Start,
        Ready,
        Progressing,
        Finish,
    }

    public class ProgressInfo : JsonStringObject
    {
        public ProgressState state;
        public string filename;
        public long current;
        public long total;

        internal ProgressInfo(ProgressState state, string filename = null, long current = 0, long total = 0)
        {
            this.state = state;
            this.filename = filename;
            this.current = current;
            this.total = total;
        }
    }

}
