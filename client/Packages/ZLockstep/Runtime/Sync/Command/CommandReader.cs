using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
#if UNITY_WEBGL || WEIXINMINIGAME
using WeChatWASM;
#endif
using ZLockstep.Sync.Command;
using Utils;

namespace Utils
{
    /// <summary>
    /// 命令读取器负责从回放文件中读取帧输入记录，并把序列化命令还原为可提交的命令对象。
    /// 默认平台读取 Unity 持久化目录文件，微信小游戏平台通过微信文件系统读取真实持久化文件。
    /// </summary>
    public class CommandReader
    {
        private List<FrameInput> _frameInputs;
        
        /// <summary>
        /// 获取已经解析出的帧输入记录列表，尚未加载时返回空列表实例。
        /// </summary>
        public List<FrameInput> FrameInputs 
        { 
            get { return _frameInputs ?? (_frameInputs = new List<FrameInput>()); } 
        }

        /// <summary>
        /// 从指定回放文件名读取并解析命令记录，真实文件路径由读取器根据当前平台生成。
        /// </summary>
        /// <param name="fileName">回放记录文件名；如果传入完整路径，会自动只取文件名。</param>
        public void LoadFromFile(string fileName)
        {
            string filePath = BuildReplayFilePath(fileName);
            if (!ReplayFileExists(filePath))
            {
                zUDebug.LogWarning($"[CommandReader] 文件不存在: {filePath}");
                _frameInputs = new List<FrameInput>();
                return;
            }

            try
            {
                _frameInputs = new List<FrameInput>();
                
                string[] lines = ReadReplayFileLines(filePath);
                
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                        
                    try
                    {
                        // 解析JSON字符串
                        JObject jsonObject = JObject.Parse(line);
                        
                        // 检查是否是frameInput类型
                        if (jsonObject["type"]?.ToString() == "frameInput")
                        {
                            // 反序列化为FrameInput对象
                            FrameInput frameInput = new FrameInput
                            {
                                type = jsonObject["type"]?.ToString(),
                                frame = int.Parse(jsonObject["frame"]?.ToString()),
                                data = new List<MyCommand>()
                            };
                            
                            // 处理命令数据，将JSON字符串转换为具体的命令对象
                            foreach (var myCommand in jsonObject["data"])
                            {
                                int commandType = myCommand["commandType"]?.ToObject<int>() ?? 0;
                                if (commandType != 0 && !string.IsNullOrEmpty(myCommand["command"].ToString()))
                                {
                                    // 使用FrameInputProcessor反序列化命令
                                    ICommand command = FrameInputProcessor.DeserializeCommand(commandType, myCommand["command"].ToString());
                                    if (command != null)
                                    {
                                        frameInput.data.Add(new MyCommand
                                        {
                                            commandType = commandType,
                                            command = command
                                        });
                                    } else
                                    {
                                        zUDebug.LogWarning($"[CommandReader] 命令反序列化失败: {myCommand}");
                                    }
                                } else
                                {
                                    zUDebug.LogWarning($"[CommandReader] 命令数据无效: {myCommand}");
                                }
                            }
                            
                            _frameInputs.Add(frameInput);
                        }
                    }
                    catch (JsonException ex)
                    {
                        zUDebug.LogError($"[CommandReader] 解析JSON行时出错: {ex.Message}, 内容: {line}");
                    }
                }
                
                zUDebug.LogInfo($"[CommandReader] 成功加载 {_frameInputs.Count} 条frameInput记录，从文件: {filePath}");
            }
            catch (Exception ex)
            {
                zUDebug.LogError($"[CommandReader] 读取文件时发生错误: {ex.Message}");
                _frameInputs = new List<FrameInput>();
            }
        }

        /// <summary>
        /// 读取当前平台持久化目录下的全部文件名，不返回目录路径。
        /// </summary>
        /// <returns>当前平台持久化目录下的文件名数组；目录不可访问时返回空数组。</returns>
        public static string[] GetPersistentDataFileNames()
        {
#if UNITY_WEBGL || WEIXINMINIGAME
            string persistentPath = GetPersistentDataPath();
            WXFileSystemManager fs = WX.GetFileSystemManager();
            if (!fs.AccessSync(persistentPath).Equals("access:ok"))
            {
                zUDebug.LogWarning($"[CommandReader] 微信用户数据目录不可访问: {persistentPath}");
                return Array.Empty<string>();
            }

            return fs.ReaddirSync(persistentPath);
#else
            string persistentPath = GetPersistentDataPath();
            if (!Directory.Exists(persistentPath))
            {
                zUDebug.LogWarning($"[CommandReader] persistentDataPath 不存在: {persistentPath}");
                return Array.Empty<string>();
            }

            string[] filePaths = Directory.GetFiles(persistentPath);
            string[] fileNames = new string[filePaths.Length];
            for (int i = 0; i < filePaths.Length; i++)
            {
                fileNames[i] = Path.GetFileName(filePaths[i]);
            }

            return fileNames;
#endif
        }

        /// <summary>
        /// 判断指定回放文件名在当前平台持久化目录下是否存在。
        /// </summary>
        /// <param name="fileName">需要检测的回放文件名；如果传入完整路径，会自动只取文件名。</param>
        /// <returns>文件存在并可访问时返回 true，否则返回 false。</returns>
        public static bool ReplayFileExistsByName(string fileName)
        {
            return ReplayFileExists(BuildReplayFilePath(fileName));
        }

        /// <summary>
        /// 获取指定逻辑帧需要执行的命令列表。
        /// </summary>
        /// <param name="frame">需要查询的逻辑帧号。</param>
        /// <returns>指定帧的命令列表；未找到时返回空列表。</returns>
        public List<MyCommand> GetCommandsByFrame(int frame)
        {
            if (_frameInputs == null)
                return new List<MyCommand>();

            var commands = new List<MyCommand>();
            
            foreach (var frameInput in _frameInputs)
            {
                if (frameInput.frame == frame && frameInput.data != null)
                {
                    commands.AddRange(frameInput.data);
                }
            }
            
            return commands;
        }

        /// <summary>
        /// 获取已加载记录中出现过的所有逻辑帧号。
        /// </summary>
        /// <returns>升序排列且不重复的帧号列表。</returns>
        public List<int> GetAllFrames()
        {
            if (_frameInputs == null)
                return new List<int>();

            var frames = new HashSet<int>();
            
            foreach (var frameInput in _frameInputs)
            {
                frames.Add(frameInput.frame);
            }
            
            var sortedFrames = new List<int>(frames);
            sortedFrames.Sort();
            
            return sortedFrames;
        }

        /// <summary>
        /// 判断指定回放文件是否存在于当前平台的文件系统中。
        /// </summary>
        /// <param name="filePath">需要检测的完整文件路径。</param>
        /// <returns>文件存在并可访问时返回 true，否则返回 false。</returns>
        private static bool ReplayFileExists(string filePath)
        {
#if UNITY_WEBGL || WEIXINMINIGAME
            WXFileSystemManager fs = WX.GetFileSystemManager();
            return fs.AccessSync(filePath).Equals("access:ok");
#else
            return File.Exists(filePath);
#endif
        }

        /// <summary>
        /// 从当前平台的文件系统读取回放文件并拆分为文本行。
        /// </summary>
        /// <param name="filePath">需要读取的完整回放文件路径。</param>
        /// <returns>回放文件中的全部文本行。</returns>
        private static string[] ReadReplayFileLines(string filePath)
        {
#if UNITY_WEBGL || WEIXINMINIGAME
            WXFileSystemManager fs = WX.GetFileSystemManager();
            string content = fs.ReadFileSync(filePath, "utf-8");
            return content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
#else
            return File.ReadAllLines(filePath);
#endif
        }

        /// <summary>
        /// 根据当前平台返回回放文件所在的持久化目录。
        /// </summary>
        /// <returns>微信用户数据目录或 Unity 持久化目录。</returns>
        private static string GetPersistentDataPath()
        {
#if UNITY_WEBGL || WEIXINMINIGAME
            return WX.env.USER_DATA_PATH;
#else
            return Application.persistentDataPath;
#endif
        }

        /// <summary>
        /// 根据回放文件名生成当前平台可读取的完整路径。
        /// </summary>
        /// <param name="fileName">回放文件名；如果传入完整路径，会自动只取文件名。</param>
        /// <returns>当前平台下用于读取回放数据的完整文件路径。</returns>
        private static string BuildReplayFilePath(string fileName)
        {
            string replayFileName = Path.GetFileName(fileName);
            return Path.Combine(GetPersistentDataPath(), replayFileName);
        }
    }
}
