namespace LanZou.Model
{
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
}
