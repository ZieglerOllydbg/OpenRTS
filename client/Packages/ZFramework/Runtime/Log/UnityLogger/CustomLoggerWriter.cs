using Common.Logging;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ZLog
{
    /// <summary>
    /// 本类被设计为订阅Unity的日志打印系统，写入到自定义的文件当中。
    /// 一般不需要上层用户主动调用，在调用日志系统的时候会自动调用这里的功能。
    /// </summary>
    internal class CustomLoggerWriter
    {
        /// <summary>
        /// 日志上报回调注册
        /// </summary>
        public static Action<string, string, string, LogType> LogReportCallback;

        /// <summary>
        /// 线程锁
        /// </summary>
        private static readonly object lockObj = new object();

        /// <summary>
        /// 日志路径
        /// </summary>
        private static string logPath;

        /// <summary>
        /// 日期格式
        /// </summary>
        //private static string dateTimeFormat;

        /// <summary>
        /// 日志写入流
        /// </summary>
        //private static FileStream stream;
        private static StreamWriter sw;

        private static bool init;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="_param"></param>
        public static void Init(LogParam _param)
        {
            if (init)
            {
                Debug.LogError("日志写入模块已经初始化");
                return;
            }

            init = true;

            //dateTimeFormat = LogUtils.GetDateFormatType(_param.DataFormatType);
            logPath = _param.LogPath;

            if (!CheckPath(logPath))
            {
                Debug.LogError($"日志路径不合法，没有正常初始化日志系统{logPath}");
                return;
            }

            var stream = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            sw = new StreamWriter(stream, Encoding.UTF8);

            var timeStamp = LogUtils.GetDateTimeStr(LoggerConst.DATE_NOW_TIME);
            //写入分割线
            WriteInFile(LogType.Log, "LoggerWrite", "------------------------------------------------------------------------------------------", out _);
            WriteInFile(LogType.Log, "LoggerWrite", "----------------------------------------Log起始位置---------------------------------------", out _);
            WriteInFile(LogType.Log, "LoggerWrite", "------------------------------------------------------------------------------------------", out _);

            Application.logMessageReceivedThreaded += OnLog;
        }

        /// <summary>
        /// 设置路径
        /// </summary>
        /// <param name="_logPath"></param>
        public static string SetLogPath(string _logPath)
        {
            lock (lockObj)
            {
                if (!string.IsNullOrEmpty(_logPath))
                {
                    if (string.Compare(_logPath, logPath, true) == 0)
                    {
                        //路径一样
                        Debugger.LogError("LoggerWrite", string.Format("New logUrl:{0} is equals old logUrl:{1}", _logPath, logPath));
                    }
                    else
                    {
                        var lastUrl = logPath;
                        logPath = _logPath;

                        return lastUrl;
                    }
                }
                //Debugger.LogError("LoggerWrite", "New LogUrl is null");
                return logPath;
            }
        }

        /// <summary>
        /// Unity 的 Log 回调接口
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="stackTrace"></param>
        /// <param name="type"></param>
        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            WriteInFile(type, null, condition, out string timeStamp);

            if (type == LogType.Exception)
            {
                WriteInFile(type, null, stackTrace, out timeStamp);
            }

            try
            {
                LogReportCallback?.Invoke(timeStamp, condition, stackTrace, type);
            }
            catch (Exception _e)
            {
                WriteInFile(LogType.Exception, null, !string.IsNullOrEmpty(_e.StackTrace) ? _e.StackTrace : _e.Message, out _);
            }
        }

        /// <summary>
        /// 外部调用的日志写入
        /// </summary>
        /// <param name="_level"></param>
        /// <param name="_name"></param>
        /// <param name="_msg"></param>
        /// <param name="_e"></param>
        public static void OnWrite(LogLevel _level, string _name, string _msg, Exception _e)
        {
            var logType = LogUtils.GetLogType(_level);

            WriteInFile(logType, _name, _msg, out string timeStamp);

            if (_e != null)
            {
                WriteInFile(LogType.Exception, _name, !string.IsNullOrEmpty(_e.StackTrace) ? _e.StackTrace : _e.Message, out timeStamp);
            }

            try
            {
                LogReportCallback?.Invoke(timeStamp, _msg, _e == null ? "" : (!string.IsNullOrEmpty(_e.StackTrace) ? _e.StackTrace : _e.Message), logType);
            }
            catch (Exception exp)
            {
                WriteInFile(LogType.Exception, _name, !string.IsNullOrEmpty(exp.StackTrace) ? exp.StackTrace : exp.Message, out _);
            }
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="_type"></param>
        /// <param name="_name"></param>
        /// <param name="_log"></param>
        /// <param name="_timeStamp"></param>
        public static void WriteInFile(LogType _type, string _name, string _log, out string _timeStamp)
        {
            lock (lockObj)
            {
                if (/*stream == null || */sw == null)
                {
                    _timeStamp = null;
                    return;
                }

                try
                {
                    using (LogZString.Block())
                    {
                        _timeStamp = LogUtils.GetLogTimeStr();

                        LogZString content;
                        if (string.IsNullOrEmpty(_name))
                        {
                            content = LogZString.Format("{0} {1} {2}", _timeStamp, GetLogTypeStr(_type), string.IsNullOrEmpty(_log) ? "NullLog" : _log);
                        }
                        else
                        {
                            content = LogZString.Format("{0} {1} [{2}] {3}", _timeStamp, GetLogTypeStr(_type), _name, string.IsNullOrEmpty(_log) ? "NullLog" : _log);
                        }

                        sw.Write(content);
                        sw.Write("\r\n");
                        sw.Flush();
                    }
                }
                catch (Exception _e)
                {
                    _timeStamp = null;
                    Debugger.LogError("LoggerWrite", $"写入日志异常 {_type}", _e);
                }
            }
        }

        private static string GetLogTypeStr(LogType _type)
        {
            if (_type == LogType.Log)
            {
                return "[Log]";
            }
            else if (_type == LogType.Warning)
            {
                return "[Warning]";
            }
            else if (_type == LogType.Error)
            {
                return "[Error]";
            }
            else if (_type == LogType.Exception)
            {
                return "[Exception]";
            }
            else
            {
                return "[Assert]";
            }
        }


        /// <summary>
        /// 检查本地路径合法性
        /// 同时会尝试创建目录
        /// </summary>
        /// <param name="_path"></param>
        /// <returns></returns>
        private static bool CheckPath(string _path)
        {
            if (string.IsNullOrEmpty(_path))
            {
                Debug.LogError("LogWrite url is null!");
                return false;
            }

            if (!File.Exists(_path))
            {
                try
                {
                    var parentDir = Directory.GetParent(_path);
                    if (!parentDir.Exists)
                    {
                        parentDir.Create();
                    }

                    var file = File.Create(_path);
                    if (file != null)
                    {
                        file.Dispose();
                        file.Close();
                    }
                }
                catch (Exception _e)
                {
                    Debug.LogError($"LogWrite url is null! Exception : { _e.Message }");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 清理
        /// </summary>
        public static void Close()
        {
            try
            {
                LogReportCallback = null;

                if (sw != null)
                {
                    sw.Close();
                    sw = null;
                }
            }
            catch (Exception _e)
            {
                Debug.LogError($"关闭日志写入异常 : {_e.Message}");
            }
        }
    }
}
