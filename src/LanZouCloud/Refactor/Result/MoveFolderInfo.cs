using LanZou;
using System.Collections.Generic;

namespace LanZou
{
    /// <summary>
    /// 移动文件夹返回结果
    /// </summary>
    public class MoveFolderInfo : Result
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
        /// 描述
        /// </summary>
        public string description { get; internal set; }

        internal MoveFolderInfo(LanZouCode code, string errorMessage,
            long id = 0, string name = null, string description = null)
        {
            this.code = code;
            this.message = errorMessage;
            this.id = id;
            this.name = name;
            this.description = description;
        }
    }
}
