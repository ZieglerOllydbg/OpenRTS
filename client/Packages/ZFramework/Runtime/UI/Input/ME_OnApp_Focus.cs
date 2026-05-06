using ZFrame;

/// <summary>
/// APP 焦点信息发送
/// </summary>
public class ME_OnApp_Focus : ModuleEvent
{
    /// <summary>
    /// 焦点中
    /// </summary>
    public bool IsFocus
    {
        private set;
        get;
    }

    public ME_OnApp_Focus(bool __focus)
    {
        this.IsFocus = __focus;
    }
}

