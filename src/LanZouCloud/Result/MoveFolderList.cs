using System.Collections.Generic;

namespace LanZou
{
    internal class MoveFolderList : Result
    {
        internal Dictionary<long, string> folders { get; set; }

        internal MoveFolderList(LanZouCode code, string errorMessage, Dictionary<long, string> folders)
        {
            this.code = code;
            this.message = errorMessage;
            this.folders = folders;
        }
    }
}
