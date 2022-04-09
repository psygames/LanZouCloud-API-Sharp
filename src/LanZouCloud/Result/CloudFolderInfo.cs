using LanZou;
using System.Collections.Generic;

namespace LanZou
{
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

        internal CloudFolderInfo(LanZouCode code, string errorMessage, long id = 0,
            string name = null, string time = null, string password = null,
            string description = null, string url = null,
            List<SubFolder> folders = null, List<SubFile> files = null)
        {
            this.code = code;
            this.message = errorMessage;
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
}
