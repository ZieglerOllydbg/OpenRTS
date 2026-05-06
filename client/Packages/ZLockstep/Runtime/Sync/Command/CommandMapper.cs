using System;
using System.Collections.Generic;
using System.Reflection;
using zUnity;

namespace ZLockstep.Sync.Command
{
    /// <summary>
    /// 命令类型映射管理器
    /// 用于建立命令类型ID与命令类的映射关系
    /// </summary>
    public static class CommandMapper
    {
        /// <summary>
        /// 命令类型ID与命令类类型的映射关系
        /// </summary>
        public static readonly Dictionary<int, Type> CommandTypeMap = new Dictionary<int, Type>();
        
        /// <summary>
        /// 命令类类型与命令类型ID的反向映射关系
        /// </summary>
        public static readonly Dictionary<Type, int> TypeToCommandTypeMap = new Dictionary<Type, int>();

        /// <summary>
        /// 初始化命令映射关系
        /// 在程序启动时调用一次
        /// </summary>
        public static void Initialize()
        {
            // 获取包含命令类的程序集 (ZLockstep程序集)
            Assembly assembly = typeof(BaseCommand).Assembly;
            Type[] types = assembly.GetTypes();
            
            // 查找所有BaseCommand的子类
            foreach (Type type in types)
            {
                // 确保类型在正确的命名空间中并且是BaseCommand的非抽象子类
                if (type.Namespace == "ZLockstep.Sync.Command.Commands" && 
                    type.IsSubclassOf(typeof(BaseCommand)) && 
                    !type.IsAbstract)
                {
                    // 通过特性获取CommandType值，完全避免实例化对象
                    try
                    {
                        // 获取CommandType特性
                        CommandTypeAttribute commandTypeAttribute = type.GetCustomAttribute<CommandTypeAttribute>();
                        if (commandTypeAttribute != null)
                        {
                            int commandType = commandTypeAttribute.CommandType;
                            
                            CommandTypeMap[commandType] = type;
                            TypeToCommandTypeMap[type] = commandType;
                            zUDebug.Log($"[CommandMapper] 建立命令映射: {type.Name} <-> {commandType}");
                        }
                    }
                    catch (Exception ex)
                    {
                        zUDebug.LogWarning($"[CommandMapper] 无法获取CommandType特性 {type.Name}: {ex.Message}");
                    }
                }
            }
            
            zUDebug.Log($"[CommandMapper] 初始化完成，共找到 {CommandTypeMap.Count} 个命令类型映射");
        }
        
        /// <summary>
        /// 根据命令类类型获取命令类型ID
        /// </summary>
        /// <param name="type">命令类类型</param>
        /// <returns>命令类型ID</returns>
        public static int GetCommandType(Type type)
        {
            if (TypeToCommandTypeMap.ContainsKey(type))
            {
                return TypeToCommandTypeMap[type];
            }
            return -1;
        }
    }
}