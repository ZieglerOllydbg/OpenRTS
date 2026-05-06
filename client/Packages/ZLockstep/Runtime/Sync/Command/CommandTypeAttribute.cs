using System;

namespace ZLockstep.Sync.Command
{
    /// <summary>
    /// 命令类型特性，用于标记命令类的类型ID
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CommandTypeAttribute : Attribute
    {
        /// <summary>
        /// 命令类型ID
        /// </summary>
        public int CommandType { get; }

        public CommandTypeAttribute(int typeId)
        {
            CommandType = typeId;
        }
    }
}