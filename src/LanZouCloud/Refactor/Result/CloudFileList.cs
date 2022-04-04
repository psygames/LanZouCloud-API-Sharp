using LanZou.Model;
using System.Collections.Generic;

namespace LanZou.Result
{
    public class CloudFileList : Result
    {
        /// <summary>
        /// 文件列表
        /// </summary>
        public List<CloudFile> files { get; internal set; }

        internal CloudFileList(LanZouCode code, string errorMessage, List<CloudFile> files = null)
        {
            this.code = code;
            this.message = errorMessage;
            this.files = files;
        }
    }
}
