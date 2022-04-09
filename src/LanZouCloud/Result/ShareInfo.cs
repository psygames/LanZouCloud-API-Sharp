using LanZou;
using System.Collections.Generic;

namespace LanZou
{
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

        internal ShareInfo(LanZouCode code, string errorMessage,
            string name = null, string url = null, string description = null,
            string password = null)
        {
            this.code = code;
            this.message = errorMessage;
            this.description = description;
            this.name = name;
            this.password = password;
            this.url = url;
        }
    }
}
