using System;
using ZFrame;

/// <summary>
/// 打开或者关闭UI的消息 通过PanelID来统一控制
/// @data: 2019-01-18
/// @author: LLL
/// </summary>
public class ME_OpenCloseUI : ModuleEvent
{
    /// <summary>
    /// 操作类型
    /// </summary>
    public OpenOrClose OperateType { get; }
    /// <summary>
    /// 携带的数据
    /// </summary>
    public object Data { get; }
    /// <summary>
    /// 是否需要添加到栈中
    /// </summary>
    public bool NeedAddToStack { get; } = true;
    /// <summary>
    /// 具体的模块类型（字符串ID）
    /// </summary>
    public string PanelType { get; } = "";
    /// <summary>
    /// 从哪个面板来的（字符串ID）
    /// </summary>
    public string FromPanelType { get; } = "";
    /// <summary>
    /// 当面板有 Tab 时，具体到哪个 Tab
    /// </summary>
    public int TabType { get; } = -1;
    /// <summary>
    /// 窗口有堆叠关系时，入栈面板类型 ID
    /// </summary>
    public int UI_Stack_Groud { get; } = -1;

    /// <summary>
    /// 换皮肤UI ID
    /// </summary>
    public PanelSkinID panelSkinID { get; } = PanelSkinID.None;

    /// <summary>
    /// 界面已经展示的回调
    /// </summary>
    public Action PanelDisplayedCallBack;

    /// <summary>
    /// 关闭的时候是否立即销毁资源
    /// </summary>
    public bool isSynchronousDestory = false;
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="_operateType"></param>
    /// <param name="_data"></param>
    /// <param name="_needAddToStack"></param>
    /// <param name="_panelType"></param>
    /// <param name="_fromPanelType"></param>
    /// <param name="_tabType"></param>
    /// <param name="_ui_stack_groud"></param>
    /// <param name="__panelSkinID"></param>
    /// <param name="_showed"></param>
    public ME_OpenCloseUI(OpenOrClose _operateType = OpenOrClose.OPEN,
        object _data = null,
        bool _needAddToStack = true,
        string _panelType = "",
        string _fromPanelType = "",
        int _tabType = -1,
        int _ui_stack_groud = -1, PanelSkinID __panelSkinID = PanelSkinID.None, Action _showed = null)
    {
        this.OperateType = _operateType;
        this.Data = _data;
        this.NeedAddToStack = _needAddToStack;
        this.PanelType = _panelType ?? "";
        this.FromPanelType = _fromPanelType ?? "";
        this.TabType = _tabType;
        this.UI_Stack_Groud = _ui_stack_groud;
        this.panelSkinID = __panelSkinID;
        this.PanelDisplayedCallBack = _showed;
    }

    /// <summary>
    /// 构造方法（支持PanelID枚举，向后兼容）
    /// </summary>
    /// <param name="_operateType"></param>
    /// <param name="_data"></param>
    /// <param name="_needAddToStack"></param>
    /// <param name="_panelType"></param>
    /// <param name="_fromPanelType"></param>
    /// <param name="_tabType"></param>
    /// <param name="_ui_stack_groud"></param>
    /// <param name="__panelSkinID"></param>
    /// <param name="_showed"></param>
    public ME_OpenCloseUI(OpenOrClose _operateType,
        object _data,
        bool _needAddToStack,
        PanelID _panelType,
        PanelID _fromPanelType,
        int _tabType,
        int _ui_stack_groud, PanelSkinID __panelSkinID, Action _showed)
        : this(_operateType, _data, _needAddToStack, 
            _panelType == PanelID.PREFABE_ID_INVAILD ? "" : _panelType.ToString(),
            _fromPanelType == PanelID.PREFABE_ID_INVAILD ? "" : _fromPanelType.ToString(),
            _tabType, _ui_stack_groud, __panelSkinID, _showed)
    {
    }
}
