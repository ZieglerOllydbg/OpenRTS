using Common.Logging;
using ZLog;
using System;

/// <summary>
/// 对外提供的日志输出类
/// 主要提供以下功能
/// 1.在静态函数中直接可以提供日志功能
/// 样例：
/// Debugger.Log("模块名称","日志内容");
/// 2.扩展了object，也就是说所有对象身上都有能力直接打印日志。
/// 样例：
/// this.Log("日志内容");
/// 在日志系统关闭后，调用这里的函数打印会没有任何反应。
/// </summary>
public static class Debugger
{
    #region Log

    /// <summary>
    /// Log by T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_t"></param>
    /// <param name="_msg"></param>
    public static void Log<T>(this T _t, object _msg)
    {
        if (!LogEnable()) return;

        var log = LogManager.GetLogger<T>();
        log.Debug(_msg, null, false);
    }

    /// <summary>
    /// Log by string key
    /// </summary>
    /// <param name="_key"></param>
    /// <param name="_msg"></param>
    public static void Log(string _key, object _msg)
    {
        if (!LogEnable()) return;

        var log = LogManager.GetLogger(_key);
        log.Debug(_msg, null, false);
    }

    /// <summary>
    /// Log by Type
    /// </summary>
    /// <param name="_type"></param>
    /// <param name="_msg"></param>
    public static void Log(Type _type, object _msg)
    {
        if (!LogEnable()) return;

        var log = LogManager.GetLogger(_type);
        log.Debug(_msg, null, false);
    }

    #endregion

    #region Warning

    /// <summary>
    /// Warning by T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_t"></param>
    /// <param name="_msg"></param>
    public static void LogWarning<T>(this T _t, object _msg)
    {
        if (!LogEnable()) return;

        var log = LogManager.GetLogger<T>();
        log.Warn(_msg, null, false);
    }

    /// <summary>
    /// Warning by string key
    /// </summary>
    /// <param name="_key"></param>
    /// <param name="_msg"></param>
    public static void LogWarning(string _key, object _msg)
    {
        if (!LogEnable()) return;

        var log = LogManager.GetLogger(_key);
        log.Warn(_msg, null, false);
    }

    /// <summary>
    /// Warning by Type
    /// </summary>
    /// <param name="_type"></param>
    /// <param name="_msg"></param>
    public static void LogWarning(Type _type, object _msg)
    {
        if (!LogEnable()) return;

        var log = LogManager.GetLogger(_type);
        log.Warn(_msg, null, false);
    }

    #endregion

    #region Error

    /// <summary>
    /// Error by T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_t"></param>
    /// <param name="_msg"></param>
    /// <param name="_e"></param>
    public static void LogError<T>(this T _t, object _msg, Exception _e = null)
    {
        if (!LogEnable()) return;

        var log = LogManager.GetLogger<T>();
        log.Error(_msg, _e, false);
    }

    /// <summary>
    /// Error by string key
    /// </summary>
    /// <param name="_key"></param>
    /// <param name="_msg"></param>
    /// <param name="_e"></param>
    public static void LogError(string _key, object _msg, Exception _e = null)
    {
        if (!LogEnable()) return;

        var log = LogManager.GetLogger(_key);
        log.Error(_msg, _e, false);
    }

    /// <summary>
    /// Error by Type
    /// </summary>
    /// <param name="_type"></param>
    /// <param name="_msg"></param>
    /// <param name="_e"></param>
    public static void LogError(Type _type, object _msg, Exception _e = null)
    {
        if (!LogEnable()) return;

        var log = LogManager.GetLogger(_type);
        log.Error(_msg, _e, false);
    }

    /// <summary>
    /// Log 是否打开
    /// </summary>
    /// <returns></returns>
    private static bool LogEnable()
    {
        return LogSystem.EnableType != LogEnableType.Close;
    }

    #endregion

    #region AlertError
    /// <summary>
    /// AlertError的消息
    /// </summary>
    public static event EventHandler<AlertEventData> AlertEvent;

    /// <summary>
    /// 弹窗log，最终信息 = message + author
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_t"></param>
    /// <param name="_msg">消息</param>
    /// <param name="_author">作者（弹窗找谁）</param>
    /// <param name="_level">报错等级</param>
    /// <param name="_e"></param>
    /// <returns></returns>
    public static T LogAlert<T>(this T _t, object _msg, object _author, int _level, Exception _e = null)
    {
        //if (!LogEnable()) return _t;

        var log = LogManager.GetLogger<T>();
        log.Error(_msg, _e, false);
        if (AlertEvent != null)
        {
            AlertEventData data = new AlertEventData
            {
                message = _msg != null ? _msg.ToString() : "",
                author = _author != null ? _author.ToString() : "",
                level = _level
            };
            AlertEvent(_t, data);
        }
        return _t;
    }

    #endregion

}

/// <summary>
/// 弹框数据
/// </summary>
public class AlertEventData : EventArgs
{
    /// <summary>
    /// 消息内容
    /// </summary>
    public string message;
    /// <summary>
    /// 作者
    /// </summary>
    public string author;

    /// <summary>
    /// log等级
    /// </summary>
    public int level;
}