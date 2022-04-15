using System.Collections.Generic;

namespace LanZou
{
    public static class Settings
    {
        public const int chunkSize = 4096;                  // 上传或下载是的块大小
        public const int timeout = 15;                      // 每个请求的超时(不包含下载响应体的用时)
        public const int maxFileSize = 100;                 // 单个文件大小上限 MB
        public const string hostUrl = "https://pan.lanzoui.com";
        public const string accountUrl = "https://pc.woozooo.com/account.php";
        public const string postUploadUrl = "https://pc.woozooo.com/doupload.php";
        public const string fileUploadUrl = "https://pc.woozooo.com/fileup.php";

        public static readonly Dictionary<string, string> headers = new Dictionary<string, string>()
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.100 Safari/537.36" },
            { "Referer", "https://pc.woozooo.com/mydisk.php" },
            { "Accept-Language", "zh-CN,zh;q=0.9" },  // 提取直连必需设置这个，否则拿不到数据
        };
    }
}
