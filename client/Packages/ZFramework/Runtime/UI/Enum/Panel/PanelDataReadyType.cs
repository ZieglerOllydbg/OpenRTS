
/// <summary>
/// Panel的数据驱动类型
/// @author Ollydbg
/// @date 2018-3-15
/// </summary>
public enum PanelDataReadyType
{
    /// <summary>
    /// 默认情况下 数据在上线的时候已经准备好了，或者根本不需要数据驱动显示
    /// </summary>
    Default,
    /// <summary>
    /// Panel需要在打开以后向服务器请求数据，等服务器返回后才能正确显示
    /// </summary>
    ByNet,
}
