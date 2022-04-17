namespace LanZou
{
    public enum ResultCode
    {
        FAILED = -1,
        SUCCESS = 0,
        PASSWORD_ERROR = 2,
        LACK_PASSWORD = 3,
        URL_INVALID = 6,
        FILE_CANCELLED = 7,
        PATH_ERROR = 8,
        NETWORK_ERROR = 9,
        CAPTCHA_ERROR = 10,
        OFFICIAL_LIMITED = 11,
        NOT_LOGIN = 12,
        TASK_CANCELED = 13,
    }
}
