using Newtonsoft.Json.Linq;
using ZLockstep.Simulation;

namespace ZLockstep.Sync.Command
{
    /// <summary>
    /// 命令来源
    /// </summary>
    public enum CommandSource
    {
        Local = 0,      // 本地玩家
        AI = 1,         // AI
        Network = 2,    // 网络
        Replay = 3      // 回放
    }

    /// <summary>
    /// 命令接口
    /// 所有游戏指令都必须实现此接口
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 命令类型ID（用于序列化和网络传输）
        /// </summary>
        // int CommandType { get; }

        /// <summary>
        /// 发出命令的玩家ID
        /// </summary>
        int CampId { get; set; }

        /// <summary>
        /// 命令来源
        /// </summary>
        CommandSource Source { get; set; }

        /// <summary>
        /// 命令应该在哪一帧执行（用于网络同步）
        /// </summary>
        int ExecuteFrame { get; set; }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="world">游戏世界</param>
        void Execute(zWorld world);
    }
}

