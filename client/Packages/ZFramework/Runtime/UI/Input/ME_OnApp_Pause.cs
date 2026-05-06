using ZFrame;

/// <summary>
/// APP 切后台信息发送
/// </summary>
public class ME_OnApp_Pause : ModuleEvent
{
    /// <summary>
    /// 焦点中
    /// </summary>
    public bool IsPause
    {
        private set;
        get;
    }

    public ME_OnApp_Pause(bool __pause)
    {
        this.IsPause = __pause;
    }
}
