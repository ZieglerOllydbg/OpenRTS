using Common.Logging;
using System;
using System.Globalization;
using UnityEngine;

namespace ZLog
{
    /// <summary>
    /// 日志系统工具类
    /// </summary>
    public static class LogUtils
    {
        /// <summary>
        /// 与服务器时差
        /// </summary>
        private static TimeSpan errorTime;
        //东八区时间
        private static readonly DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(8);

        internal static void ResetTime(long _serverTime)
        {
            var serverDate = GetDateFormStamp(_serverTime);

            errorTime = serverDate - DateTime.Now;
        }

        /// <summary>
        /// 从时间戳获取DateTime
        /// </summary>
        /// <param name="jsTimeStamp"></param>
        /// <returns></returns>
        private static DateTime GetDateFormStamp(long jsTimeStamp)
        {
            DateTime dt = startTime.AddMilliseconds(jsTimeStamp);
            return dt;
        }

        /// <summary>
        /// 获取当前时间
        /// </summary>
        /// <returns></returns>
        public static DateTime GetDateTime()
        {
            return DateTime.Now + errorTime;
        }

        /// <summary>
        /// 获得格式化后的日期字符串
        /// </summary>
        /// <param name="_type"></param>
        /// <returns></returns>
        public static string GetDateTimeStr(DateTimeFormatType _type)
        {
            var dateTime = GetDateTime();

            var formatStr = GetDateFormatType(_type);

            return dateTime.ToString(formatStr, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 获得格式化后的日期字符串
        /// </summary>
        /// <param name="_formatStr"></param>
        /// <returns></returns>
        public static string GetDateTimeStr(string _formatStr)
        {
            var dateTime = GetDateTime();

            return dateTime.ToString(_formatStr, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 获得格式化后的日期字符串
        /// </summary>
        /// <returns></returns>
        internal static LogZString GetLogTimeStr()
        {
            var dateTime = GetDateTime();

            return DateTimeFormat(dateTime);
        }

        /// <summary>
        /// 获得LogLevel
        /// </summary>
        /// <param name="_level"></param>
        /// <returns></returns>
        internal static LogLevel GetLogLevel(UnityLogLevel _level)
        {
            LogLevel level = LogLevel.Debug;

            switch (_level)
            {
                case UnityLogLevel.Log:
                    level = LogLevel.Debug;
                    break;
                case UnityLogLevel.Warning:
                    level = LogLevel.Warn;
                    break;
                case UnityLogLevel.Error:
                    level = LogLevel.Error;
                    break;
                case UnityLogLevel.Off:
                    level = LogLevel.Off;
                    break;
            }

            return level;
        }

        /// <summary>
        /// 获得对应的 Unity 类型
        /// </summary>
        /// <param name="_level"></param>
        /// <returns></returns>
        internal static LogType GetLogType(LogLevel _level)
        {
            LogType type = LogType.Log;

            switch (_level)
            {
                case LogLevel.Debug:
                    type = LogType.Log;
                    break;
                case LogLevel.Warn:
                    type = LogType.Warning;
                    break;
                case LogLevel.Error:
                    type = LogType.Error;
                    break;
            }

            return type;
        }

        /// <summary>
        /// 获得日期格式
        /// </summary>
        /// <param name="_type"></param>
        /// <returns></returns>
        internal static string GetDateFormatType(DateTimeFormatType _type)
        {
            string format = "";

            switch (_type)
            {
                case DateTimeFormatType.NONE:
                case DateTimeFormatType.DATE:
                    format = LoggerConst.DATE;
                    break;
                case DateTimeFormatType.NOW_TIME:
                    format = LoggerConst.NOW_TIME;
                    break;
                case DateTimeFormatType.NOW_TIME_OF_DAY:
                    format = LoggerConst.NOW_TIME_OF_DAY;
                    break;
                case DateTimeFormatType.DATE_NOW_TIME:
                    format = LoggerConst.DATE_NOW_TIME;
                    break;
                case DateTimeFormatType.DATE_NOW_TIME_OF_DAY:
                    format = LoggerConst.DATE_NOW_TIME_OF_DAY;
                    break;
            }

            return format;
        }

        /// <summary>
        /// 计算的时间戳
        /// </summary>
        /// <param name="_dateTime"></param>
        /// <returns></returns>
        internal static LogZString DateTimeFormat(DateTime _dateTime)
        {
            var timeOfDay = _dateTime.TimeOfDay;
            var h = timeOfDay.Hours;
            var m = timeOfDay.Minutes;
            var s = timeOfDay.Seconds;

            LogZString hStr = LogZString.Concat(h < 10 ? "0" : "", h);
            LogZString mStr = LogZString.Concat(m < 10 ? "0" : "", m);
            LogZString sStr = LogZString.Concat(s < 10 ? "0" : "", s);

            var ticks = timeOfDay.Ticks;

            //ulong totalH = (ulong)h * 60 * 60 * 10000000;
            //ulong totalM = (ulong)m * 60 * 10000000;
            //ulong totalS = (ulong)s * 10000000;

            //var ms = (long)((ulong)ticks - totalH - totalM - totalS);
            var ms = (long)((ulong)ticks % 10000000);
            LogZString msgStr;
            if (ms < 10)
            {
                msgStr = LogZString.Concat("000000", ms);
            }
            else if (ms < 100)
            {
                msgStr = LogZString.Concat("00000", ms);
            }
            else if (ms < 1000)
            {
                msgStr = LogZString.Concat("0000", ms);
            }
            else if (ms < 10000)
            {
                msgStr = LogZString.Concat("000", ms);
            }
            else if (ms < 100000)
            {
                msgStr = LogZString.Concat("00", ms);
            }
            else if (ms < 1000000)
            {
                msgStr = LogZString.Concat("0", ms);
            }
            else
            {
                msgStr = ms;
            }

            return LogZString.Format("{0}:{1}:{2}.{3}", hStr, mStr, sStr, msgStr);
        }
    }
}
