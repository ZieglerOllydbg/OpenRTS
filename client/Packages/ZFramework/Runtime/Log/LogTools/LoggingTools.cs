using ZLib.Promises;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ZLog
{
    /// <summary>
    /// 一些本地日志的操作工具
    /// </summary>
    public static class LoggingTools
    {
        /// <summary>
        /// 移除过期的日志，保留days天内记录
        /// </summary>
        /// <param name="_curLogPath"></param>
        /// <param name="_preName"></param>
        /// <param name="days">保存的天数</param>
        /// <returns></returns>
        public static void RemoveExpiredLog(string _curLogPath, string _preName, int days)
        {
            if (!string.IsNullOrEmpty(_curLogPath) && !string.IsNullOrEmpty(_preName))
            {
                var dirInfo = Directory.GetParent(_curLogPath);

                if (dirInfo != null)
                {
                    var dirPath = dirInfo.FullName;

                    //遍历文件夹下所有文件，过时的删除
                    List<string> allFileList = null;
                    GetFiles(dirPath, ref allFileList);

                    if (allFileList != null && allFileList.Count > 0)
                    {
                        var nowDay = LogUtils.GetDateTime().DayOfYear;
                        int index1 = _preName.Length;

                        for (int i = 0; i < allFileList.Count; i++)
                        {
                            var file = allFileList[i];
                            var fileName = Path.GetFileName(file);

                            //文件创建时间
                            var fileInfo = new FileInfo(file);

                            if (!fileName.Contains(_preName)) continue;

                            //解析时间
                            int index2 = fileName.IndexOf(".txt");

                            if (index2 - index1 > 0)
                            {
                                var dateStr = fileName.Substring(index1, index2 - index1);

                                if (DateTime.TryParse(dateStr, out var result))
                                {
                                    var dayOfYear = result.DayOfYear;
                                    if (nowDay - dayOfYear > days)
                                    {
                                        //超过一周的删除
                                        File.Delete(file);
                                    }
                                    else
                                    {
                                        //跨年了
                                        if (nowDay - dayOfYear < 0)
                                        {
                                            if (365 - dayOfYear + nowDay > days)
                                            {
                                                File.Delete(file);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取目标目录下所有的文件路径
        /// </summary>
        /// <param name="_dirPath"></param>
        /// <param name="_pathList"></param>
        private static void GetFiles(string _dirPath, ref List<string> _pathList)
        {
            if (Directory.Exists(_dirPath))
            {
                var files = Directory.GetFiles(_dirPath);
                if (_pathList == null)
                {
                    _pathList = new List<string>();
                }
                _pathList.AddRange(files);

                var dirs = Directory.GetDirectories(_dirPath);
                for (int i = 0; i < dirs.Length; i++)
                {
                    var dir = dirs[i];

                    GetFiles(dir, ref _pathList);
                }
            }
        }


        /// <summary>
        /// 移动上次残留的日志文件
        /// 用unity日志文件名做比对
        /// </summary>
        /// <param name="_unityLogPath">unity log 路径</param>
        public static void MoveLastLog(string _unityLogPath, List<MoveLogRuleBase> rules)
        {
            if(rules == null)
            {
                Debug.LogError("移动日志失败，没有提供任何的移动日志策略.");
                return;
            }
            try
            {
                string upLoadPath = "";
                string lastLogDirPath = "";

                var nowTime = LogUtils.GetDateTimeStr("yyyy.MM.dd");//.HH.mm.ss

#if UNITY_EDITOR
                upLoadPath = Path.Combine(Environment.CurrentDirectory, "UpLoadLogFile");
                lastLogDirPath = Path.Combine(upLoadPath, $"LastLog{nowTime}");
#else
            upLoadPath = $"{Application.persistentDataPath}/UpLoadLogFile";
            lastLogDirPath = $"{upLoadPath}/LastLog{nowTime}";
#endif

                //若没有先创建
                if (!Directory.Exists(upLoadPath))
                {
                    Directory.CreateDirectory(upLoadPath);
                }

                //unity 日志文件名
                var unityLogName = Path.GetFileName(_unityLogPath);

                Debug.LogError("unityLogName : " + unityLogName + " // _unityLogPath : " + _unityLogPath);

                //检查是否存在前一天的日志
                bool exitLastLog = false;
                if (!string.IsNullOrEmpty(_unityLogPath))
                {
                    var unityLogDirPath = Path.GetDirectoryName(_unityLogPath);
                    if (Directory.Exists(unityLogDirPath))
                    {
                        //遍历此路径下的所有文件
                        var files = Directory.GetFiles(unityLogDirPath);
                        for (int i = 0; i < files.Length; i++)
                        {
                            var fileName = Path.GetFileName(files[i]);

                            Debug.LogError("fileName : " + fileName + "  // file : " + files[i]);
                            if (fileName != unityLogName)
                            {
                                exitLastLog = true;
                                break;
                            }
                        }
                    }
                }
                Debug.LogError($"exit last log : {exitLastLog}");
                if (!exitLastLog) return;

                //若没有先创建
                if (!Directory.Exists(lastLogDirPath))
                {
                    Directory.CreateDirectory(lastLogDirPath);
                }

                for (int i = 0; i < rules.Count; i++)
                {
                    var moveLog = rules[i];

                    var promise = moveLog?.Move(lastLogDirPath);
                }
            }
            catch (Exception _e)
            {
                Debug.LogError("移动日志文件失败");
                Debug.LogException(_e);
            }
        }
    }

    public abstract class MoveLogRuleBase
    {
        protected abstract string GetLogPath();

        public Promise Move(string _copyDirPath)
        {
            Promise promise = new Promise();

            if (!string.IsNullOrEmpty(_copyDirPath))
            {
                var logUrl = GetLogPath();
                if (string.IsNullOrEmpty(logUrl))
                {
                    this.LogWarning($"获取不到日志源文件路径 : {this.GetType()}");
                }
                else
                {
                    try
                    {
                        //判断是否是文件夹
                        if (Directory.Exists(logUrl))
                        {
                            var dirName = Path.GetFileName(logUrl);
                            Directory.Move(logUrl, Path.Combine(_copyDirPath, dirName));
                        }
                        else
                        {
                            if (File.Exists(logUrl))
                            {
                                //存在文件，直接拷贝
                                var fileName = Path.GetFileName(logUrl);
                                var newPath = Path.Combine(_copyDirPath, fileName);

                                File.Move(logUrl, newPath);
                            }
                        }
                    }
                    catch (Exception _e)
                    {
                        this.LogError("移动日志文件失败", _e);
                    }
                }
            }
            else
            {
                this.LogError("移动日志文件的目标目录路径为空");
            }

            promise.Resolve();

            return promise;
        }
    }

    /// <summary>
    /// 移动日志的规则
    /// </summary>
    public class MoveUnityLogRule : MoveLogRuleBase
    {
        protected override string GetLogPath()
        {
            string logUrl = "";
#if UNITY_EDITOR
            logUrl = Path.Combine(Environment.CurrentDirectory, $"UnityLog");
#else
            logUrl = $"{Application.persistentDataPath}/UnityLog";
#endif
            return logUrl;
        }
    }

}
