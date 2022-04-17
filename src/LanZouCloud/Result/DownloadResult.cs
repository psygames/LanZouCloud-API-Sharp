using LanZou;
using System.Collections.Generic;

namespace LanZou
{
    public class DownloadResult : Result
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

        internal DownloadResult(ResultCode code, string errorMessage, string url = null,
            string fileName = null, string filePath = null, bool isContinue = false)
        {
            this.code = code;
            this.message = errorMessage;
            this.url = url;
            this.fileName = fileName;
            this.filePath = filePath;
            this.isContinue = isContinue;
        }
    }
}
