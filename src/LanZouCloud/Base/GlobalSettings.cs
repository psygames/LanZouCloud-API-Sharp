using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanZou
{
    public static class GlobalSettings
    {
        public const int _chunk_size = 4096;             // 上传或下载是的块大小
        public const int _timeout = 15;                  // 每个请求的超时(不包含下载响应体的用时)
        public const int _max_size = 100;                // 单个文件大小上限 MB
        public const string _host_url = "https://pan.lanzoui.com";
        public const string _doupload_url = "https://pc.woozooo.com/doupload.php";
        public const string _account_url = "https://pc.woozooo.com/account.php";
        public static readonly Dictionary<string, string> _headers = new Dictionary<string, string>()
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.100 Safari/537.36" },
            { "Referer", "https://pc.woozooo.com/mydisk.php" },
            { "Accept-Language", "zh-CN,zh;q=0.9" },  // 提取直连必需设置这个，否则拿不到数据
        };
    }
}
