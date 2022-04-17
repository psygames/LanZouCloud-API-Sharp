namespace LanZou
{
    /// <summary>
    /// 蓝奏云返回结果信息
    /// </summary>
    public class Result : JsonObject
    {
        /// <summary>
        /// 蓝奏云结果码
        /// </summary>
        public ResultCode code { get; internal set; }

        /// <summary>
        /// 错误消息 或 成功消息
        /// </summary>
        public string message { get; internal set; }

        internal Result() { }

        public Result(ResultCode code, string errorMessage)
        {
            this.code = code;
            this.message = errorMessage;
        }
    }
}
