using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_WEBGL || WEIXINMINIGAME
using WeChatWASM;
#endif
using ZLockstep.Sync.Command;

namespace Utils
{
    /// <summary>
    /// 命令录制器负责把帧命令序列化到本地回放文件中，并根据运行平台选择可写目录与文件系统接口。
    /// 默认平台写入 Unity 的持久化目录，微信小游戏平台写入微信 SDK 提供的用户数据目录。
    /// </summary>
    public class CommandRecorder
    {
        private string _filePath;
        private bool _isRecording;

        /// <summary>
        /// 根据回放文件名创建录制文件，并清空旧内容以准备写入新的命令记录。
        /// </summary>
        /// <param name="fileName">回放文件名，不包含外部拼接的持久化目录。</param>
        public void StartRecording(string fileName)
        {
            _filePath = BuildRecordingFilePath(fileName);
            _isRecording = true;

#if UNITY_WEBGL || WEIXINMINIGAME
            ClearWxFile();
#else
            WriteEmptyRecordingFile(_filePath);
#endif
            zUDebug.LogInfo($"[CommandRecorder] 开始记录命令，文件路径: {_filePath}");
        }

        /// <summary>
        /// 在录制文件目录中创建或清空指定文件名对应的空文件，不改变当前命令录制状态。
        /// </summary>
        /// <param name="fileName">需要创建的文件名；如果传入完整路径，会自动只取文件名。</param>
        public static void CreateEmptyRecordingFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                zUDebug.LogWarning("[CommandRecorder] 历史帧文件名为空，跳过创建空文件");
                return;
            }

            try
            {
                string filePath = BuildRecordingFilePath(fileName);
                WriteEmptyRecordingFile(filePath);
                zUDebug.LogInfo($"[CommandRecorder] 已创建历史帧空文件，文件路径: {filePath}");
            }
            catch (Exception ex)
            {
                zUDebug.LogError($"[CommandRecorder] 创建历史帧空文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 将帧输入列表写入本地文件，使用与客户端回放文件相同的 JSON 格式。
        /// </summary>
        /// <param name="fileName">目标文件名。</param>
        /// <param name="frameInputs">需要写入的帧输入列表。</param>
        public static void WriteFrameInputsToFile(string fileName, List<FrameInput> frameInputs)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                zUDebug.LogWarning("[CommandRecorder] 写入文件失败：文件名为空。");
                return;
            }

            if (frameInputs == null || frameInputs.Count == 0)
            {
                zUDebug.LogWarning("[CommandRecorder] 写入文件失败：帧数据为空。");
                return;
            }

            string filePath = BuildRecordingFilePath(fileName);

            try
            {
                // 创建或清空文件
                WriteEmptyRecordingFile(filePath);

                // 逐行写入每个 FrameInput
                foreach (FrameInput frameInput in frameInputs)
                {
                    if (frameInput?.data == null || frameInput.data.Count == 0)
                    {
                        continue;
                    }

                    // 为每个命令创建单独的 FrameInput 行（与客户端回放文件格式一致）
                    foreach (MyCommand myCommand in frameInput.data)
                    {
                        if (myCommand?.command == null)
                        {
                            continue;
                        }

                        string serializedData = FrameInputProcessor.SerializeFrameInput(frameInput.frame, myCommand.command);
#if UNITY_WEBGL || WEIXINMINIGAME
                        WXFileSystemManager fs = WX.GetFileSystemManager();
                        string currentContent = fs.ReadFileSync(filePath, "utf-8");
                        fs.WriteFileSync(filePath, currentContent + serializedData + Environment.NewLine, "utf-8");
#else
                        File.AppendAllText(filePath, serializedData + Environment.NewLine);
#endif
                    }
                }

                zUDebug.LogInfo($"[CommandRecorder] 成功将 {frameInputs.Count} 个帧写入文件: {filePath}");
            }
            catch (Exception ex)
            {
                zUDebug.LogError($"[CommandRecorder] 写入文件失败: {filePath}, error={ex.Message}");
            }
        }

        /// <summary>
        /// 把指定帧的命令序列化后追加到当前录制文件末尾。
        /// </summary>
        /// <param name="frame">命令所属的逻辑帧号。</param>
        /// <param name="command">需要写入回放文件的命令对象。</param>
        public void RecordCommand(int frame, ICommand command)
        {
            if (!_isRecording || string.IsNullOrEmpty(_filePath))
            {
                zUDebug.LogWarning("[CommandRecorder] 录制未开始，请先调用StartRecording方法");
                return;
            }

            try
            {
                // 使用FrameInputProcessor序列化frameInput
                string serializedData = FrameInputProcessor.SerializeFrameInput(frame, command);

#if UNITY_WEBGL || WEIXINMINIGAME
                AppendWxText(serializedData + Environment.NewLine);
#else
                File.AppendAllText(_filePath, serializedData + Environment.NewLine);
#endif
            }
            catch (Exception ex)
            {
                zUDebug.LogError($"[CommandRecorder] 记录命令时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止当前回放命令录制，后续命令不会继续写入文件。
        /// </summary>
        public void StopRecording()
        {
            if (_isRecording)
            {
                _isRecording = false;
                zUDebug.LogInfo("[CommandRecorder] 停止记录命令");
            }
        }

        /// <summary>
        /// 获取当前命令录制器是否处于录制状态。
        /// </summary>
        public bool IsRecording => _isRecording;

        /// <summary>
        /// 根据当前运行平台和文件名生成真实可写的录制文件路径。
        /// </summary>
        /// <param name="fileName">调用方传入的录制或占位文件名。</param>
        /// <returns>当前平台下用于写入数据的完整文件路径。</returns>
        public static string BuildRecordingFilePath(string fileName)
        {
            string recordingFileName = Path.GetFileName(fileName);
#if UNITY_WEBGL || WEIXINMINIGAME
            return Path.Combine(WX.env.USER_DATA_PATH, recordingFileName);
#else
            return Path.Combine(Application.persistentDataPath, recordingFileName);
#endif
        }

        /// <summary>
        /// 使用当前平台的文件系统接口创建或清空指定录制文件。
        /// </summary>
        /// <param name="filePath">平台可写目录下的完整文件路径。</param>
        private static void WriteEmptyRecordingFile(string filePath)
        {
#if UNITY_WEBGL || WEIXINMINIGAME
            WXFileSystemManager fs = WX.GetFileSystemManager();
            fs.WriteFileSync(filePath, string.Empty, "utf-8");
#else
            File.WriteAllText(filePath, string.Empty);
#endif
        }

#if UNITY_WEBGL || WEIXINMINIGAME
        /// <summary>
        /// 使用微信小游戏文件系统创建或清空当前录制文件。
        /// </summary>
        private void ClearWxFile()
        {
            WriteEmptyRecordingFile(_filePath);
        }

        /// <summary>
        /// 使用微信小游戏文件系统把文本追加到当前录制文件末尾。
        /// </summary>
        /// <param name="text">需要追加写入的序列化命令文本。</param>
        private void AppendWxText(string text)
        {
            WXFileSystemManager fs = WX.GetFileSystemManager();
            string content = string.Empty;
            if (fs.AccessSync(_filePath).Equals("access:ok"))
            {
                content = fs.ReadFileSync(_filePath, "utf-8");
            }

            fs.WriteFileSync(_filePath, content + text, "utf-8");
        }
#endif
    }
}
