using LanZou.Model;
using System.Collections.Generic;

namespace LanZou.Result
{
    public class CloudFolderList : Result
    {
        /// <summary>
        /// 文件夹列表
        /// </summary>
        public List<CloudFolder> folders { get; internal set; }

        internal CloudFolderList(LanZouCode code, string errorMessage, List<CloudFolder> folders = null)
        {
            this.code = code;
            this.message = errorMessage;
            this.folders = folders;
        }
    }
}
