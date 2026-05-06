using ZFrame;

/// <summary>
/// 重连恢复游戏事件，用于跨启动房间恢复成功后关闭匹配页并打开战斗主界面。
/// </summary>
public class ReconnectGameStartEvent : ModuleEvent
{
    /// <summary>
    /// 创建重连恢复游戏事件。
    /// </summary>
    public ReconnectGameStartEvent() : base()
    {
    }
}
