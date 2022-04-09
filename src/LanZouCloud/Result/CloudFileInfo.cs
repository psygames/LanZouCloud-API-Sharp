using LanZou;
using System.Collections.Generic;

namespace LanZou
{
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

        internal CloudFileInfo(LanZouCode code, string errorMessage, string password = null, string url = null,
             string name = null, string type = null, string time = null, string size = null,
             string description = null, string durl = null)
        {
            this.code = code;
            this.message = errorMessage;
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
}
