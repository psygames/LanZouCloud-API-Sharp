using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanZouAPI
{
    public enum ResultCode
    {
        FAILED = -1,
        SUCCESS = 0,
        ID_ERROR = 1,
        PASSWORD_ERROR = 2,
        LACK_PASSWORD = 3,
        ZIP_ERROR = 4,
        MKDIR_ERROR = 5,
        URL_INVALID = 6,
        FILE_CANCELLED = 7,
        PATH_ERROR = 8,
        NETWORK_ERROR = 9,
        CAPTCHA_ERROR = 10,
        OFFICIAL_LIMITED = 11,
    }
}
