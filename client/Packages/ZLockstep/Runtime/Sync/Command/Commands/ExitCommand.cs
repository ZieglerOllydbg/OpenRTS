using ZLockstep.Simulation;
using zUnity;

namespace ZLockstep.Sync.Command.Commands
{
    /// <summary>
    /// 退出命令。
    /// 用于在逻辑帧中统一执行退出流程：停止命令录制、暂停游戏推进并派发重开事件。
    /// </summary>
    [CommandType(CommandTypes.Exit)]
    public class ExitCommand : BaseCommand
    {
        /// <summary>
        /// 创建退出命令实例。
        /// </summary>
        /// <param name="campId">发出退出命令的阵营ID。</param>
        public ExitCommand(int campId) : base(campId)
        {
        }

        /// <summary>
        /// 执行退出流程。
        /// 该流程会作为停止标记生效，使当前对局在退出命令执行后不再继续推进和录制后续命令。
        /// </summary>
        /// <param name="world">当前逻辑世界实例。</param>
        public override void Execute(zWorld world)
        {
            if (world == null)
            {
                zUDebug.LogWarning("[ExitCommand] world 为空，退出流程中止。");
                return;
            }

            world.CommandManager?.StopRecording();
            // 停止帧对比工具
            world.DeterminismProbe?.Stop();
            // 暂停游戏推进
            world.GameInstance?.Pause();
        }
    }
}
