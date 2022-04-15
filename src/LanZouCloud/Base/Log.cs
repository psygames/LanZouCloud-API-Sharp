using System;

namespace LanZou
{
    public static class Log
    {
        public enum LogLevel
        {
            None = 0,
            Error = 1,
            Warning = 2,
            Info = 3,
        }

        public static string tag = "LanZou";
        public static LogLevel printLogLevel = LogLevel.Info;
        public static LogLevel writeLogLevel = LogLevel.Info;

        public static void Error(object log, string module = null)
        {
            LogWithLevel(log, LogLevel.Error, module);
        }

        public static void Warning(object log, string module = null)
        {
            LogWithLevel(log, LogLevel.Warning, module);
        }

        public static void Info(object log, string module = null)
        {
            LogWithLevel(log, LogLevel.Info, module);
        }

        private static void LogWithLevel(object log, LogLevel level, string module = null)
        {
            if (level == LogLevel.None)
            {
                return;
            }

            if (printLogLevel < level && writeLogLevel < level)
            {
                return;
            }

            // log format:
            // time|lanzou|level|module|log
            // example:
            // 11.22.03.456|TAG|E|Login|login failed cause network error.
            var time = DateTime.Now.ToString("HH:mm:ss.fff");
            var _level = level.ToString().Substring(0, 1);
            var _max_module_lens = 16;
            if (module != null)
            {
                if (module.Length > _max_module_lens) module = module.Substring(0, _max_module_lens);
                else if (module.Length < _max_module_lens) module = module + new string(' ', _max_module_lens - module.Length);
            }
            else
            {
                module = "module";
            }

            var _log = $"{time}|{tag}|{_level}|{module}|{log}";
            if (printLogLevel >= level)
            {
                Print(_log, level);
            }

            if (writeLogLevel >= level)
            {
                Write(_log, level);
            }
        }

        private static void Print(string log, LogLevel level)
        {
#if UNITY_5_3_OR_NEWER
            if (level == LogLevel.Info) UnityEngine.Debug.Log($"{log}");
            else if (level == LogLevel.Warning) UnityEngine.Debug.LogWarning($"{log}");
            else if (level == LogLevel.Error) UnityEngine.Debug.LogError($"{log}");
#else
            Console.WriteLine($"{log}");
#endif
        }


        private static void Write(string log, LogLevel level)
        {
            //TODO: Write log
        }
    }
}
