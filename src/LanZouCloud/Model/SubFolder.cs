namespace LanZou
{
    /// <summary>
    /// 专为 CloudFolderInfo 使用，指其下子文件夹
    /// </summary>
    public class SubFolder : JsonObject
    {
        /// <summary>
        /// 文件夹名
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
    }
}
