
namespace ZLog
{
    /// <summary>
    /// unity log 等级
    /// </summary>
    public enum UnityLogLevel
    {
        /// <summary>
        /// Log
        /// </summary>
        Log,
        /// <summary>
        /// Warning
        /// </summary>
        Warning,
        /// <summary>
        /// Error
        /// </summary>
        Error,
        /// <summary>
        /// 关闭log输出
        /// </summary>
        Off,
    }

    /// <summary>
    /// 日期格式
    /// </summary>
    public enum DateTimeFormatType
    {
        /// <summary>
        /// 无格式
        /// </summary>
        NONE,
        /// <summary>
        /// 年-月-日
        /// </summary>
        DATE,
        /// <summary>
        /// 时:分:秒
        /// </summary>
        NOW_TIME,
        /// <summary>
        /// 时:分:秒.毫秒
        /// </summary>
        NOW_TIME_OF_DAY,
        /// <summary>
        /// 年-月-日 时:分:秒
        /// </summary>
        DATE_NOW_TIME,
        /// <summary>
        /// 年-月-日 时:分:秒.毫秒
        /// </summary>
        DATE_NOW_TIME_OF_DAY,
    }

    /// <summary>
    /// Log 激活模式
    /// </summary>
    public enum LogEnableType
    {
        /// <summary>
        /// 使用 Unity API 打印 log
        /// </summary>
        UseUnityEngineAPI,
        /// <summary>
        /// 仅写入文件，不使用 Unity API 打印 Log
        /// </summary>
        OnlyWrite,
        /// <summary>
        /// 关闭 Log 调用
        /// </summary>
        Close,
    }
}