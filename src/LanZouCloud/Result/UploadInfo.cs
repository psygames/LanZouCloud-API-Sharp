using LanZou;
using System.Collections.Generic;

namespace LanZou
{
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

        internal UploadInfo(LanZouCode code, string errorMessage, string fileName = null,
            string filePath = null, long id = 0, string url = null)
        {
            this.code = code;
            this.message = errorMessage;
            this.fileName = fileName;
            this.filePath = filePath;
            this.id = id;
            this.url = url;
        }
    }
}
