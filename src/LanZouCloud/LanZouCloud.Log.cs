using System;

namespace LanZouCloudAPI
{
    public partial class LanZouCloud
    {
        public enum LogLevel
        {
            None = 0,
            Error = 1,
            Warning = 2,
            Info = 3,
        }

        private LogLevel _print_log_level = LogLevel.Error;
        private LogLevel _write_log_level = LogLevel.None;

        /// <summary>
        /// 设置日志等级
        /// </summary>
        /// <param name="level"></param>
        public void SetLogLevel(LogLevel level)
        {
            this._print_log_level = level;
            this._write_log_level = level;
        }

        /// <summary>
        /// 设置日志等级
        /// </summary>
        /// <param name="printLevel">打印日志等级</param>
        /// <param name="writeLevel">写入文件日志等级</param>
        public void SetLogLevel(LogLevel printLevel, LogLevel writeLevel)
        {
            this._print_log_level = printLevel;
            this._write_log_level = writeLevel;
        }

        private void Log(object log, LogLevel level, string module)
        {
            if (level == LogLevel.None)
            {
                return;
            }

            if (_print_log_level < level && _write_log_level < level)
            {
                return;
            }

            // log format:
            // time|lanzou|level|module|log
            // example:
            // 11.22.03.456|LanZou|E|Login|login failed cause network error.
            var time = DateTime.Now.ToString("HH:mm:ss.fff");
            var _level = level.ToString().Substring(0, 1);
            var _max_module_lens = 16;
            if (module.Length > _max_module_lens) module = module.Substring(0, _max_module_lens);
            else if (module.Length < _max_module_lens) module = module + new string(' ', _max_module_lens - module.Length);
            var _log = $"{time}|LanZou|{_level}|{module}|{log}";

            if (_print_log_level >= level)
            {
                Print(_log, level);
            }

            if (_write_log_level >= level)
            {
                Write(_log, level);
            }
        }

        private void Print(string log, LogLevel level)
        {
#if UNITY_5_3_OR_NEWER
            if (level == LogLevel.Info) UnityEngine.Debug.Log($"{log}");
            else if (level == LogLevel.Warning) UnityEngine.Debug.LogWarning($"{log}");
            else if (level == LogLevel.Error) UnityEngine.Debug.LogError($"{log}");
#else
            Console.WriteLine($"{log}");
#endif
        }


        private void Write(string log, LogLevel level)
        {
#if UNITY_5_3_OR_NEWER
            // UnityEngine.Debug.LogError($"{log}");
#else
            // Console.WriteLine($"{log}");
#endif
        }
    }
}
