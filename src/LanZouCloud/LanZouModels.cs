using LitJson;
using System.Collections.Generic;

namespace LanZouAPI
{
    public class CloudFile
    {
        public long id;
        public string name;
        public string time;
        public string size;
        public string type;
        public int downs;
        public bool has_pwd;
        public bool has_des;

        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }

    public class CloudFolder
    {
        public long id;
        public string name;
        public bool has_pwd;
        public string desc;

        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }

    public class CloudFileList
    {
        public LanZouCode code;
        public List<CloudFile> files;

        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }

    public class CloudFolderList
    {
        public LanZouCode code;
        public List<CloudFolder> folders;

        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }

    public class CloudFolderDetail
    {
        public LanZouCode code;
        public string name;
        public string pwd;
        public string desc;
        public string url;
        public string size;
        public string time;
        public string type;
        public string durl;

        public CloudFolderDetail(LanZouCode code)
        {
            this.code = code;
        }

        public CloudFolderDetail(LanZouCode code, string pwd, string url)
        {
            this.code = code;
            this.pwd = pwd;
            this.url = url;
        }

        public CloudFolderDetail(LanZouCode code, string name, string time, string size, string desc, string pwd, string url)
        {
            this.code = code;
            this.name = name;
            this.time = time;
            this.size = size;
            this.desc = desc;
            this.pwd = pwd;
            this.url = url;
        }

        public CloudFolderDetail(LanZouCode code, string name, string time, string size, string desc, string pwd, string url, string type, string durl)
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

        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }


    public class CloudFileDetail
    {
        public LanZouCode code;
        public string name;
        public string pwd;
        public string desc;
        public string url;
        public string size;
        public string time;
        public string type;
        public string durl;

        public CloudFileDetail(LanZouCode code)
        {
            this.code = code;
        }

        public CloudFileDetail(LanZouCode code, string pwd, string url)
        {
            this.code = code;
            this.pwd = pwd;
            this.url = url;
        }

        public CloudFileDetail(LanZouCode code, string name, string time, string size, string desc, string pwd, string url)
        {
            this.code = code;
            this.name = name;
            this.time = time;
            this.size = size;
            this.desc = desc;
            this.pwd = pwd;
            this.url = url;
        }

        public CloudFileDetail(LanZouCode code, string name, string time, string size, string desc, string pwd, string url, string type, string durl)
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

        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }

    public class ShareInfo
    {
        public LanZouCode code;
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

        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }

    public class DirectUrlInfo
    {
        public LanZouCode code;
        public string name;
        public string durl;

        public DirectUrlInfo(LanZouCode code)
        {
            this.code = code;
        }

        public DirectUrlInfo(LanZouCode code, string name, string durl)
        {
            this.code = code;
            this.name = name;
            this.durl = durl;
        }

        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }

    public class MakeDirInfo
    {
        public LanZouCode code;
        public long id;
        public string name;
        public string desc;

        public MakeDirInfo(LanZouCode code)
        {
            this.code = code;
        }

        public MakeDirInfo(LanZouCode code, long id, string name, string desc)
        {
            this.code = code;
            this.id = id;
            this.name = name;
            this.desc = desc;
        }

        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }

    public class DownloadInfo
    {
        public LanZouCode code;
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

        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }

    public class DownloadProgressInfo
    {
        public enum State
        {
            Start,
            Ready,
            Downloading,
            Finish,
        }

        public State state;
        public long current;
        public long total;
        public string filename;
        public string share_url;
        public bool is_continue;

        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }

    public class UploadInfo
    {
        public LanZouCode code;
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

        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }

    public class UploadProgressInfo
    {
        public enum State
        {
            Start,
            Ready,
            Uploading,
            Finish,
        }

        public State state;
        public long current;
        public long total;
        public string filename;

        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }
}
