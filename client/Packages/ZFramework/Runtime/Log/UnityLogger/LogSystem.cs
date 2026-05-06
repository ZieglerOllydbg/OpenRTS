using Common.Logging;
using System;
using System.Threading;
using UnityEngine;

namespace ZLog
{
    /// <summary>
    /// 日志系统
    /// 日志系统对外提供的唯一需要在前期调用的函数，在初始化本类后，才可以正常使用日志系统
    /// 主要对外提供的API如下
    /// 1.初始化日志系统
    /// 2.设置日志激活等级
    /// 3.设置日志本地写入路径
    /// 4.设置日志的回调接口
    /// 5.清理等功能
    /// </summary>
    public class LogSystem
    {
        private static UnityLoggerFactoryAdapter adapter;

        /// <summary>
        /// 读写分离的锁
        /// </summary>
        private static readonly ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

        private static readonly string LogEnableTypeKey = "LogEnableType";

        private static LogEnableType enableType = LogEnableType.UseUnityEngineAPI;
        /// <summary>
        /// 是否使用 Unity 的 log api,默认 true
        /// </summary>
        public static LogEnableType EnableType
        {
            get
            {
                cacheLock.EnterReadLock();

                try
                {
                    return enableType;
                }
                finally
                {
                    cacheLock.ExitReadLock();
                }
            }
            set
            {
                cacheLock.EnterWriteLock();

                try
                {
                    enableType = value;

                    //不使用 unity 的api 打印log，就直接关掉unity 的log
                    if(enableType != LogEnableType.UseUnityEngineAPI)
                    {
                        //Debug.unityLogger.logEnabled = false;
                        Debug.unityLogger.filterLogType = LogType.Exception;
                    }
                    else
                    {
                        //Debug.unityLogger.logEnabled = true;
                        Debug.unityLogger.filterLogType = LogType.Error;
                    }

                    PlayerPrefs.SetInt(LogEnableTypeKey, (int)enableType);
                    PlayerPrefs.Save();
                }
                finally
                {
                    cacheLock.ExitWriteLock();
                }
            }
        }

        #region LogSystem InIt

        /// <summary>
        /// 日志系统初始化
        /// </summary>
        /// <param name="_param"></param>
        public static void Init(LogParam _param)
        {
            //FactoryAdapterConfiguration adapterConfiguration = new FactoryAdapterConfiguration();
            //adapterConfiguration.Type = "Common.Logging.Unity.UnityLoggerFactoryAdapter";
            //adapterConfiguration.Arguments = dic;
            //LogConfiguration configuration = new LogConfiguration();
            //configuration.FactoryAdapter = adapterConfiguration;
            var localEnableType = PlayerPrefs.GetInt(LogEnableTypeKey, 0);
            if(localEnableType != (int)enableType)
            {
                EnableType = (LogEnableType)localEnableType;
            }

            CustomLoggerWriter.Init(_param);

            if (adapter == null)
            {
                LogLevel level = LogUtils.GetLogLevel(_param.Level);
                adapter = new UnityLoggerFactoryAdapter(level);
                LogManager.Adapter = adapter;
            }
        }

        #endregion

        #region LogWrite

        /// <summary>
        /// 设置日志写入路径<see cref="CustomLoggerWriter" />
        /// </summary>
        /// <param name="_path">新的日志路径，为空则仅获取当前日志文件路径</param>
        public static string SetLogPath(string _path = "")
        {
            return CustomLoggerWriter.SetLogPath(_path);
        }

        /// <summary>
        /// 注册用于日志上报的回调
        /// </summary>
        /// <param name="_callback"></param>
        public static void SetLogReportCallback(Action<string, string, string, LogType> _callback)
        {
            CustomLoggerWriter.LogReportCallback += _callback;
        }

        /// <summary>
        /// 取消注册用于日志上报的回调
        /// </summary>
        /// <param name="_callback"></param>
        public static void UnSetLogReportCallback(Action<string, string, string, LogType> _callback)
        {
            CustomLoggerWriter.LogReportCallback -= _callback;
        }

        #endregion

        #region Reset Time

        /// <summary>
        /// 重设服务器时间
        /// </summary>
        /// <param name="_serverTime"></param>
        public static void ResetTime(long _serverTime)
        {
            LogUtils.ResetTime(_serverTime);
        }

        #endregion

        #region Clear

        /// <summary>
        /// 清理
        /// </summary>
        public static void Clear()
        {
            if (adapter != null)
                adapter.Clear();

            LogManager.Reset();
            CustomLoggerWriter.Close();
        }

        #endregion
    }
}
