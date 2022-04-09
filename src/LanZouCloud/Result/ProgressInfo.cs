namespace LanZou
{
    /// <summary>
    /// 上传/下载 进度信息
    /// </summary>
    public class ProgressInfo : JsonStringObject
    {
        /// <summary>
        /// 状态
        /// </summary>
        public ProgressState state { get; internal set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string fileName { get; internal set; }

        /// <summary>
        /// 当前大小（字节）
        /// </summary>
        public long current { get; internal set; }

        /// <summary>
        /// 总大小（字节）
        /// </summary>
        public long total { get; internal set; }

        internal ProgressInfo(ProgressState state, string filename = null, long current = 0, long total = 0)
        {
            this.state = state;
            this.fileName = filename;
            this.current = current;
            this.total = total;
        }
    }
}
