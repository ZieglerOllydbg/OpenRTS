using ZFrame;

/// <summary>
/// UI Processor 封装一些和 UI 模块相关的方法
/// @data: 2019-01-23
/// @author: LLL
/// </summary>
public abstract class UIBaseProcessor : BaseProcessor
{
    public UIBaseProcessor(Module _module)
        : base(_module)
    { }

    /// <summary>
    /// 收到打开或关闭的消息
    /// </summary>
    /// <param name="_me"></param>
    protected virtual void OnOpenOrCloseUI(ME_OpenCloseUI _me)
    { }
}
