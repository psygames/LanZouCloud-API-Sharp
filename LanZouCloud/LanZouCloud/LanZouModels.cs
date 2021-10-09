using LitJson;

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

    }
}
