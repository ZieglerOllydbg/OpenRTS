
/// <summary>
/// 面板移除操作类型
/// @author Ollydbg
/// @date 2018-3-15
/// </summary>
public enum PanelRemoveActionType
{
    /// <summary>
    /// 默认操作是在关闭之后 自动进入缓存中 缓存超时自动释放掉
    /// </summary>
    Default,

    /// <summary>
    /// 在关闭后 只是隐藏起来不显示 资源会一直在内存中 不会销毁
    /// </summary>
    JustHide,
}
