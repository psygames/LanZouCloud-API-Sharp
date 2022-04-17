namespace LanZou
{
    public class CloudFile : JsonObject
    {
        /// <summary>
        /// 文件唯一ID
        /// </summary>
        public long id { get; internal set; }

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
        /// 下载次数
        /// </summary>
        public int downloads { get; internal set; }

        /// <summary>
        /// 是否存在提取码
        /// </summary>
        public bool hasPassword { get; internal set; }

        /// <summary>
        /// 是否有描述信息
        /// </summary>
        public bool hasDescription { get; internal set; }
    }
}
