using System.Collections.Generic;

namespace LanZou
{
    internal class MoveFolderListResult : Result
    {
        internal Dictionary<long, string> folders { get; set; }

        internal MoveFolderListResult(ResultCode code, string errorMessage, Dictionary<long, string> folders)
        {
            this.code = code;
            this.message = errorMessage;
            this.folders = folders;
        }
    }
}
