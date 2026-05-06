using System.Collections.Generic;

namespace ZLockstep.Sync.Command
{
    /// <summary>
    /// 命令缓冲区
    /// 用于本地输入、AI和网络输入的命令收集
    /// </summary>
    public class CommandBuffer
    {
        private readonly List<ICommand> _commands = new List<ICommand>();
        private readonly CommandManager _commandManager;

        public CommandBuffer(CommandManager commandManager)
        {
            _commandManager = commandManager;
        }

        /// <summary>
        /// 添加命令到缓冲区
        /// </summary>
        public void Add(ICommand command)
        {
            _commands.Add(command);
        }

        /// <summary>
        /// 批量添加命令
        /// </summary>
        public void AddRange(IEnumerable<ICommand> commands)
        {
            _commands.AddRange(commands);
        }

        /// <summary>
        /// 提交缓冲区中的所有命令到CommandManager
        /// </summary>
        public void Submit()
        {
            if (_commands.Count > 0)
            {
                _commandManager.SubmitCommands(_commands);
                _commands.Clear();
            }
        }

        /// <summary>
        /// 清空缓冲区（不提交）
        /// </summary>
        public void Clear()
        {
            _commands.Clear();
        }

        /// <summary>
        /// 获取缓冲区中的命令数量
        /// </summary>
        public int Count => _commands.Count;
    }
}

