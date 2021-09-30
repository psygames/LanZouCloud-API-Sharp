using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace LanZouCloud
{
    public class Log
    {
        public static void Error(object log)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogError($"[EasyHttp][Error] {log}");
#else
            Console.WriteLine($"[EasyHttp][Error] {log}");
#endif
        }

        public static void Warning(object log)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogError($"[EasyHttp][Warning] {log}");
#else
            Console.WriteLine($"[EasyHttp][Warning] {log}");
#endif
        }
    }
}
