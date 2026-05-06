using System;
using System.Collections.Generic;

/// <summary>
/// 面板上的一些元数据
/// </summary>
public class UIModelData
{
    #region 静态数据

    public string ParentID { get; } = "";
    /// <summary>
    /// 面板ID类型（字符串ID）
    /// </summary>
    public string currentPanelID { get; } = "";
    /// <summary>
    /// 面板深度类型
    /// </summary>
    public ClientUIDepthTypeID PanelUIDepthType { get; }
    /// <summary>
    /// 窗口级别
    /// </summary>
    public ClientUITypeID PanelUIType { get; }
    /// <summary>
    /// 面板数据驱动类型
    /// </summary>
    public PanelDataReadyType ReadyType { get; }
    /// <summary>
    /// 加载路径类型
    /// </summary>
    public PanelPathType PathType { get; } = PanelPathType.Default;
    /// <summary>
    /// 加载类型
    /// </summary>
    public PanelLoadType LoadType { get; }
    /// <summary>
    /// 面板移除操作类型
    /// </summary>
    public PanelRemoveActionType RemoveActionType { get; internal set; }

    /// <summary>
    /// 面板适配类型
    /// </summary>
    public PanelAdapterType AdapterType { get; internal set; }
    /// <summary>
    /// 互斥关系
    /// </summary>
    //public PanelID[] mutexPanelIDs { get; }
    /// <summary>
    /// 共存面板
    /// </summary>
    //public PanelID[] coexistPanelIDs { get; }
    /// <summary>
    /// 共同关闭
    /// </summary>
    //public PanelID[] closeCoexistPanelIDs { get; }

    private string panelName;
    /// <summary>
    /// 面板名字
    /// </summary>
    public string PanelName
    {
        set => panelName = value;
        //TODO 后期从 Language 表获取
        get => panelName;
    }

    /// <summary>
    /// 当前面板索引
    /// </summary>
    public int TabIndex { get; } = 0;
    /// <summary>
    /// 父面板上的挂点
    /// </summary>
    public string ParentLinkPoint { get; }
    /// <summary>
    /// 打开声音
    /// </summary>
    public int OpenSoundID { get; }
    /// <summary>
    /// 关闭声音
    /// </summary>
    public int CloseSoundID { get; }

    /// <summary>
    /// UI面板是否能打开
    /// </summary>
    public bool CanOpen { get; }
    /// <summary>
    /// 不能打开的原因
    /// </summary>
    public int CannotOpenReason { get; }
    /// <summary>
    /// Tips 偏移
    /// </summary>
    //public (int x,int y) TipsOffset { get; }
    /// <summary>
    /// 面板路径
    /// </summary>
    public string PanelPath { get; }

    #endregion

    /// <summary>
    /// 面板类型
    /// </summary>
    public Type ClassType { get; }
    /// <summary>
    /// 窗口有堆叠关系时，入栈面板类型 ID
    /// </summary>
    public int UI_Stack_Group { get; }

    /// <summary>
    /// UIModel Data 缓存数据
    /// </summary>
    private static Dictionary<string, UIModelData> cacheModelDataDic = new Dictionary<string, UIModelData>();
    /// <summary>
    /// 深度类型分类
    /// </summary>
    private static Dictionary<string, ClientUIDepthTypeID> panelTypeDic = new Dictionary<string, ClientUIDepthTypeID>();

    private UIModelData()
    { }

    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="_attribute"></param>
    /// <param name="_config"></param>
    /// <param name="_classType"></param>
    public UIModelData(UIModelAttribute _attribute, Type _classType)
    {
        this.currentPanelID = _attribute.panelID;
        this.ClassType = _classType;

        this.ReadyType = _attribute.ReadyType;
        this.PathType = _attribute.pathType;
        this.LoadType = _attribute.loadType;
        this.RemoveActionType = _attribute.removeType;



        //if (_config != null)
        //{
        this.TabIndex = 0;// _config.ui_inparent_tabindex;
        this.ParentID = "";// _config.ui_parent_id;
        this.ParentLinkPoint = "";// _config.ui_parent_link_point;
        
        // 从业务层配置的属性中读取路径、名称和深度类型
        // 如果业务层未配置（panelPath为空），则尝试使用默认方法（向后兼容）
        bool hasBusinessConfig = !string.IsNullOrEmpty(_attribute.panelPath);
        
        this.PanelPath = hasBusinessConfig 
            ? _attribute.panelPath 
            : GetDemoPanelPrefabName(currentPanelID);
        
        // 如果业务层配置了panelPath，则使用业务层配置的panelUIDepthType
        // 否则尝试使用默认方法（向后兼容）
        this.PanelUIDepthType = hasBusinessConfig
            ? _attribute.panelUIDepthType 
            : GetDemoUIDepthType(currentPanelID);
        
        this.PanelName = hasBusinessConfig && !string.IsNullOrEmpty(_attribute.panelName)
            ? _attribute.panelName 
            : (hasBusinessConfig ? "" : GetDemoPanelCHName(currentPanelID));
        
        this.PanelUIType = ClientUITypeID.System;// (ClientUITypeID)_config.ui_type;
        this.OpenSoundID = 0;// _config.ui_open_sound_id;
        this.CloseSoundID = 0;// _config.ui_close_sound_id;
        this.UI_Stack_Group = 0;// _config.ui_stack_group;
        this.CanOpen = true;// _config.ui_can_open == 0 ? true : false;
        this.CannotOpenReason = 0;// _config.ui_cannot_open_reson;
        this.AdapterType = PanelAdapterType.Default;// (PanelAdapterType)_config.adapter_screen;
                                                    //}


        if (string.IsNullOrEmpty(currentPanelID))
            throw new Exception("PanelID cannot be null or empty!");

        if (!cacheModelDataDic.ContainsKey(currentPanelID))
            cacheModelDataDic[currentPanelID] = this;
        else
            throw new Exception($"UIModelData Init Repeated, PanelID: { currentPanelID }");
    }

    private string GetDemoPanelPrefabName(string panelID)
    {
        // 尝试解析为PanelID枚举以支持向后兼容
        if (Enum.TryParse<PanelID>(panelID, out PanelID enumPanelID))
        {
            switch (enumPanelID)
            {
                case PanelID.Demo_ShowPicture:
                    return "Panel_ShowPicture";

                case PanelID.Demo_CharacterCollection:
                    return "Panel_CharacterCollection";

                case PanelID.Demo_ExcitationStar:
                    return "Panel_ExcitationStar";

                case PanelID.Demo_SpeechRecognition:
                    return "Panel_SpeechRecognition";

                case PanelID.Demo_Bubble:
                    return "Panel_Bubble";

                case PanelID.Demo_PauseBack:
                    return "Panel_PauseBack";

                case PanelID.Demo_MoonGame:
                    return "Panel_MoonGame";

                case PanelID.Demo_SkyGame:
                    return "Panel_SkyGame";

                case PanelID.Demo_ChineseCharacterTeach:
                    return "Panel_ChineseCharacterTeach";

                case PanelID.Exercise3Game:
                    return "Panel_Exercise3Game";
            }
        }

        return "";
    }

    private string GetDemoPanelCHName(string panelID)
    {
        // 尝试解析为PanelID枚举以支持向后兼容
        if (Enum.TryParse<PanelID>(panelID, out PanelID enumPanelID))
        {
            switch (enumPanelID)
            {
                case PanelID.Demo_ShowPicture:
                    return "Demo照片展示UI";

                case PanelID.Demo_CharacterCollection:
                    return "Demo汉字收集UI";

                case PanelID.Demo_ExcitationStar:
                    return "Demo激励送星UI";

                case PanelID.Demo_SpeechRecognition:
                    return "Demo语音识别UI";

                case PanelID.Demo_Bubble:
                    return "Demo气泡UI";

                case PanelID.Demo_PauseBack:
                    return "Demo暂停退出UI";

                case PanelID.Demo_MoonGame:
                    return "Demo月字收集UI";

                case PanelID.Demo_SkyGame:
                    return "Demo天字收集UI";

                case PanelID.Demo_ChineseCharacterTeach:
                    return "Demo汉字教学UI";

                case PanelID.Exercise3Game:
                    return "3-1到3-1练习题";
            }
        }

        return "";
    }

    /// <summary>
    /// demo  ui层级
    /// </summary>
    /// <param name="panelID"></param>
    /// <returns></returns>
    private ClientUIDepthTypeID GetDemoUIDepthType(string panelID)
    {
        // 尝试解析为PanelID枚举以支持向后兼容
        if (Enum.TryParse<PanelID>(panelID, out PanelID enumPanelID))
        {
            switch (enumPanelID)
            {
                case PanelID.Demo_ShowPicture:
                    return ClientUIDepthTypeID.GameMid;

                case PanelID.Demo_CharacterCollection:
                    return ClientUIDepthTypeID.GameTop;

                case PanelID.Demo_ExcitationStar:
                    return ClientUIDepthTypeID.GameMaxTop;

                case PanelID.Demo_SpeechRecognition:
                    return ClientUIDepthTypeID.GameMaxTop;

                case PanelID.Demo_Bubble:
                    return ClientUIDepthTypeID.GameMaxTop;

                case PanelID.Demo_PauseBack:
                    return ClientUIDepthTypeID.GameSpecial;

                case PanelID.Demo_MoonGame:
                    return ClientUIDepthTypeID.GameMid;

                case PanelID.Demo_SkyGame:
                    return ClientUIDepthTypeID.GameMid;

                case PanelID.Demo_ChineseCharacterTeach:
                    return ClientUIDepthTypeID.GameMid;

                case PanelID.Exercise3Game:
                    return ClientUIDepthTypeID.GameMid;
            }
        }

        return ClientUIDepthTypeID.GameMid;
    }


    #region 获取皮肤路径
    /// <summary>
    /// 获取子皮肤路径
    /// </summary>
    /// <param name="__panslSkinID"></param>
    /// <returns></returns>
    public string GetCacheSkinUIResPath(PanelSkinID __panelSkinID)
    {
        //var tableConfig = Table_Client_Ui_Skin_Config.GetPrimary((int)__panelSkinID);

        //if (tableConfig != null)
        //{
        //    return tableConfig.res_path;
        //}
        //else
        //{
        //    Debugger.LogError(typeof(UIModelData), $"Can't get Table_Client_Ui_Skin_Config by __panelSkinID: { __panelSkinID }");
        //}

        return string.Empty;
    }
    #endregion
    /// <summary>
    /// 获取缓存的ModelData
    /// </summary>
    /// <param name="_panelID"></param>
    /// <returns></returns>
    public static UIModelData GetCacheModelData(string _panelID)
    {
        if (cacheModelDataDic.TryGetValue(_panelID, out UIModelData data))
            return data;

        throw new Exception($"UIModelData has't init, panelID: { _panelID }");
    }

    /// <summary>
    /// 获取缓存的ModelData（支持PanelID枚举，向后兼容）
    /// </summary>
    /// <param name="_panelID"></param>
    /// <returns></returns>
    public static UIModelData GetCacheModelData(PanelID _panelID)
    {
        return GetCacheModelData(_panelID.ToString());
    }

    /// <summary>
    /// 获取面板类型
    /// </summary>
    /// <param name="_panelID"></param>
    /// <returns></returns>
    public static ClientUIDepthTypeID GetPanelType(string _panelID)
    {
        if (panelTypeDic.TryGetValue(_panelID, out ClientUIDepthTypeID type))
            return type;

        throw new Exception($"UIModelData has't init, panelID: { _panelID }");
    }

    /// <summary>
    /// 获取面板类型（支持PanelID枚举，向后兼容）
    /// </summary>
    /// <param name="_panelID"></param>
    /// <returns></returns>
    public static ClientUIDepthTypeID GetPanelType(PanelID _panelID)
    {
        return GetPanelType(_panelID.ToString());
    }
}
