using LitJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public class CloudFileDetail
    {
        public ResultCode code;
        public string name;
        public string pwd;
        public string desc;
        public string url;
        public string size;
        public string time;
        public string type;
        public string durl;

        public CloudFileDetail(ResultCode code)
        {
            this.code = code;
        }

        public CloudFileDetail(ResultCode code, string pwd, string url)
        {
            this.code = code;
            this.pwd = pwd;
            this.url = url;
        }

        public CloudFileDetail(ResultCode code, string name, string time, string size, string desc, string pwd, string url)
        {
            this.code = code;
            this.name = name;
            this.time = time;
            this.size = size;
            this.desc = desc;
            this.pwd = pwd;
            this.url = url;
        }

        public CloudFileDetail(ResultCode code, string name, string time, string size, string desc, string pwd, string url, string type, string durl)
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
        public ResultCode code;
        public string name;
        public string desc;
        public string url;
        public string pwd;

        public ShareInfo(ResultCode code)
        {
            this.code = code;
        }

        public ShareInfo(ResultCode code, string name, string url, string desc, string pwd)
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
}
