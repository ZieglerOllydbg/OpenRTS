using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZLockstep.Sync.Command;

namespace Utils
{
    public static class FrameInputProcessor
    {
        public static string SerializeFrameInput(int frame, ICommand command)
        {
            FrameInput frameInput = new FrameInput
            {
                type = "frameInput",
                frame = frame,
                data = new List<MyCommand>()
            };

            int commandType = CommandMapper.GetCommandType(command.GetType());

            MyCommand myCommand = new MyCommand
            {
                commandType = commandType,
                command = command
            };

            frameInput.data.Add(myCommand);

            string json = JsonConvert.SerializeObject(frameInput);
            
            return json;
        }

        public static ICommand DeserializeCommand(int commandType, string json)
        {
            ICommand command = null;
            
            // 使用映射关系替代switch语句
            if (CommandMapper.CommandTypeMap.ContainsKey(commandType))
            {
                Type commandClass = CommandMapper.CommandTypeMap[commandType];
                command = (ICommand)JsonConvert.DeserializeObject(json, commandClass);
            }
            else
            {
                zUDebug.LogWarning($"[FrameInputProcessor] 未知的命令类型: {commandType}");
            }

            return command;
        }
    }

    public class FrameInput
    {
        public string type;
        public int frame;
        
        public List<MyCommand> data;
    }
    
    public class MyCommand
    {
        public int commandType;
        public ICommand command;
    }
}