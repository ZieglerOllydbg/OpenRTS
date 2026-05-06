/// <summary>
/// 面板状态枚举
/// 使用单一状态枚举替代多个bool标志，简化状态管理
/// @date 2025-11-11
/// </summary>
public enum PanelState
{
    /// <summary>
    /// 未初始化（刚创建）
    /// </summary>
    Uninitialized,

    /// <summary>
    /// 已关闭（有资源或无资源）
    /// </summary>
    Closed,

    /// <summary>
    /// 正在加载资源
    /// </summary>
    Loading,

    /// <summary>
    /// 资源已加载，但未显示
    /// </summary>
    Loaded,

    /// <summary>
    /// 正在打开（播放动画）
    /// </summary>
    Opening,

    /// <summary>
    /// 已打开并显示
    /// </summary>
    Opened,

    /// <summary>
    /// 正在关闭（播放动画）
    /// </summary>
    Closing,
}

