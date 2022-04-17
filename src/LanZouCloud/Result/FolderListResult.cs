using LanZou;
using System.Collections.Generic;

namespace LanZou
{
    public class FolderListResult : Result
    {
        /// <summary>
        /// 文件夹列表
        /// </summary>
        public List<CloudFolder> folders { get; internal set; }

        internal FolderListResult(ResultCode code, string errorMessage, List<CloudFolder> folders = null)
        {
            this.code = code;
            this.message = errorMessage;
            this.folders = folders;
        }
    }
}
