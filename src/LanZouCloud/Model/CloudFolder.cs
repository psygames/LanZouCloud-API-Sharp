namespace LanZou
{
    public class CloudFolder : JsonObject
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
        /// 是否存在提取码
        /// </summary>
        public bool hasPassword { get; internal set; }

        /// <summary>
        /// 文件夹描述信息
        /// </summary>
        public string description { get; internal set; }
    }
}
