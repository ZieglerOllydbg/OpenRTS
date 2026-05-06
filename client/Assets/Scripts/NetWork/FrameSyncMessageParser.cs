using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Utils;
using ZLockstep.Sync.Command;

/// <summary>
/// 帧同步消息解析工具，负责把服务器下发的 frameSync JSON 消息转换为可提交到命令系统的命令列表。
/// </summary>
public static class FrameSyncMessageParser
{
    /// <summary>
    /// 解析 frameSync 消息中的所有玩家输入命令，并按服务器消息中的顺序返回命令列表。
    /// </summary>
    /// <param name="message">服务器下发的 frameSync 消息 JSON。</param>
    /// <param name="logPrefix">日志前缀，用于区分实时同步和历史帧回放来源。</param>
    /// <returns>解析成功的命令列表；消息为空或没有命令时返回空列表。</returns>
    public static List<ICommand> ParseCommands(JObject message, string logPrefix)
    {
        List<ICommand> commandList = new();
        if (message == null)
        {
            zUDebug.LogWarning($"{logPrefix} frameSync 消息为空，跳过解析。");
            return commandList;
        }

        var commandsArray = message["data"] as JArray;
        if (commandsArray == null)
        {
            return commandList;
        }

        foreach (JToken commandToken in commandsArray)
        {
            try
            {
                var commandObj = commandToken as JObject;
                var inputs = commandObj?["inputs"] as JArray;
                if (inputs == null)
                {
                    continue;
                }

                for (int i = 0; i < inputs.Count; i++)
                {
                    var input = inputs[i] as JObject;
                    int commandType = input?["commandType"]?.ToObject<int>() ?? 0;
                    string commandJson = input?["command"]?.ToString();
                    if (commandType == 0 || string.IsNullOrEmpty(commandJson))
                    {
                        zUDebug.LogWarning($"{logPrefix} 命令数据无效: {input}");
                        continue;
                    }

                    ICommand command = FrameInputProcessor.DeserializeCommand(commandType, commandJson);
                    if (command != null)
                    {
                        commandList.Add(command);
                        zUDebug.LogInfo($"{logPrefix} 接收到的操作命令: {command}");
                    }
                }
            }
            catch (Exception ex)
            {
                zUDebug.LogError($"{logPrefix} 解析命令时出错: {ex.Message}");
            }
        }

        return commandList;
    }
}
