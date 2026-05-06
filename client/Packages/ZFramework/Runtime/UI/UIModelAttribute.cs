using System;

/// <summary>
/// 配置在Panel上的元信息 来辅助做一些决策 或者把面板分类处理
/// @data: 2019-01-16
/// @author: LLL
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class UIModelAttribute : Attribute
{
    /// <summary>
    /// 面板类型（字符串ID，业务层可以使用枚举通过ToString()转换）
    /// </summary>
    public string panelID = "";

    #region 业务层配置（路径、名称、深度等）

    /// <summary>
    /// 面板资源路径（Prefab名称，业务层配置）
    /// </summary>
    public string panelPath = "";

    /// <summary>
    /// 面板显示名称（业务层配置）
    /// </summary>
    public string panelName = "";

    /// <summary>
    /// 面板UI深度类型（业务层配置）
    /// </summary>
    public ClientUIDepthTypeID panelUIDepthType = ClientUIDepthTypeID.GameMid;

    #endregion

    #region 程序控制

    /// <summary>
    /// 数据准备类型
    /// </summary>
    public PanelDataReadyType ReadyType { get; } = PanelDataReadyType.Default;
    /// <summary>
    /// 面板路径类型
    /// </summary>
    public PanelPathType pathType = PanelPathType.Default;
    /// <summary>
    /// 加载类型
    /// </summary>
    public PanelLoadType loadType = PanelLoadType.Default;
    /// <summary>
    /// 面板移除类型
    /// </summary>
    public PanelRemoveActionType removeType = PanelRemoveActionType.Default;

    /// <summary>
    /// UI界面适配 屏幕类型
    /// </summary>
    public PanelAdapterType adapterType = PanelAdapterType.Default;
    #endregion
}
