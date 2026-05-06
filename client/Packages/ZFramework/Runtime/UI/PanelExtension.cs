using System;
using System.Collections.Generic;
using UnityEngine;
using ZFrame;

/// <summary>
/// 为Panel提供一些扩展方法
/// @author Ollydbg
/// @date 2018-2-24
/// </summary>
public static class PanelExtension
{
    /// <summary>
    /// 构造保护
    /// </summary>
    private static DisableNew disableNew { get; } = Activator.CreateInstance(typeof(DisableNew), true) as DisableNew;

    /// <summary>
    /// 管理构造器，禁止私自构造
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_processor"></param>
    /// <param name="_data"></param>
    /// <returns></returns>
    public static T NewPanel<T>(IDispathMessage _processor, UIModelData _data) where T : BasePanel
    {
        if (_processor is UIBaseProcessor baseProcesser)
        {
            return Activator.CreateInstance(typeof(T), baseProcesser, _data, disableNew) as T;
        }
        else
        {
            return Activator.CreateInstance(typeof(T), _processor, _data, disableNew) as T;
        }
    }



    /// <summary>
    /// 非泛型的常见panel对象
    /// </summary>
    /// <param name="_processor"></param>
    /// <param name="_data"></param>
    /// <returns></returns>
    public static BasePanel NewPanelNonGen(IDispathMessage _processor, UIModelData _data)
    {
        if (_processor is UIBaseProcessor baseProcesser)
        {
            return Activator.CreateInstance(_data.ClassType, baseProcesser, _data, disableNew) as BasePanel;
        }
        else
        {
            return Activator.CreateInstance(_data.ClassType, _processor, _data, disableNew) as BasePanel;
        }
    }

    /// <summary>
    /// 构造方法
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_t"></param>
    /// <param name="_processor"></param>
    /// <param name="_panelID"></param>
    /// <returns></returns>
    public static T New<T>(this T _t, IDispathMessage _processor, string _panelID) where T : BasePanel
    {
        var panel = PanelManager.instance.GetPanelByID(_panelID);
        if (panel != null)
        {
            if (panel.GetType() == typeof(T))
                return panel as T;
            else
            {
                //TipManager.Tip(_t, _content: $"PanelManager 里存在 PanelID 为 : {_panelID} 的 Panel，但获取时传入的类型 : {typeof(T)} 与缓存 : {panel.GetType()} 不一致", _owner: "制作该功能的人", _stayTime: 0);
            }
        }

        var uiModel = UIModuleDiscover.GetUIModel(_panelID);

        return NewPanel<T>(_processor, uiModel);
    }

    /// <summary>
    /// 构造方法（支持PanelID枚举，向后兼容）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_t"></param>
    /// <param name="_processor"></param>
    /// <param name="_panelID"></param>
    /// <returns></returns>
    public static T New<T>(this T _t, IDispathMessage _processor, PanelID _panelID) where T : BasePanel
    {
        return New(_t, _processor, _panelID.ToString());
    }


    #region  UI 适配针对相关资源的处理


    /// <summary>
    /// 美术制作图的基准比例  4:3 
    /// UI 上默认的像素分辨率 宽
    /// </summary>
    static float UI_Art_PixWidth = 2048;

    /// <summary>
    /// 美术制作图的基准比例  4:3 
    /// UI 上默认的像素分辨率 高
    /// </summary>
    static float UI_Art_PixHeight = 1536;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="_panel"></param>
    public static void CanvasRectTransAdapt(this BasePanel _panel)
    {
        if (_panel != null && _panel.ModelData != null)
        {
            //针对这5个层次，进行 逻辑UI 的相关适配
            if (list_Adapter_Depths != null && list_Adapter_Depths.Contains(_panel.ModelData.PanelUIDepthType))
            {
                switch (_panel.ModelData.AdapterType)
                {
                    case PanelAdapterType.FullScreen:
                        CanvasRectObjectAdaptFullScreen(_panel.PanelObject);
                        break;

                    case PanelAdapterType.Default:
                        CanvasRectObjectAdapt(_panel.PanelObject);
                        break;
                }
            }
            else
            {
                //默认铺满全屏
                CanvasRectObjectAdaptFullScreen(_panel.PanelObject);
            }
        }
    }

    /// <summary>
    /// 适配层级列表
    /// </summary>
    private static readonly List<ClientUIDepthTypeID> list_Adapter_Depths = new List<ClientUIDepthTypeID>()
    {
        ClientUIDepthTypeID.GameDefault,
        ClientUIDepthTypeID.GameMid,
        ClientUIDepthTypeID.GameConstant,
        ClientUIDepthTypeID.GameTop,
        ClientUIDepthTypeID.GameMaxTop
    };

    /// <summary>
    /// 将prefab 的调整到 跟随父级全屏适配
    /// </summary>
    /// <param name="_obj"></param>
    public static void CanvasRectObjectAdaptFullScreen(GameObject _obj)
    {
        //if (GameData.instance.userData.IsTeacher)
        //{
        //    return;
        //}

        var panelObject = _obj;

        if (panelObject != null)
        {
            var rectTransform = panelObject.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                Debug.Log($"[CheckError] CanvasRectObjectAdaptFullScreen 修改Panel适配布局");
                rectTransform.offsetMin = new Vector2(0.0f, 0.0f);
                rectTransform.offsetMin = new Vector2(0.0f, 0.0f);
                rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.SetAsFirstSibling();
            }
        }
    }
    /// <summary>
    /// 将OBJ 的像素调整
    /// </summary>
    /// <param name="_obj"></param>
    public static void CanvasRectObjectAdapt(GameObject _obj, bool setSibling = true)
    {
        //if (GameData.instance.userData.IsTeacher)
        //{
        //    return;
        //}

        var panelObject = _obj;

        if (panelObject != null)
        {
            var rectTransform = panelObject.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                Debug.Log($"[CheckError] CanvasRectObjectAdapt 修改Panel适配布局");
                rectTransform.sizeDelta = new Vector2(UI_Art_PixWidth, UI_Art_PixHeight);
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                if (setSibling) rectTransform.SetAsFirstSibling();
            }
        }
    }
    #endregion

    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        var component = gameObject.GetComponent<T>();
        if (component != null)
        {
            return component;
        }
        else
        {
            return gameObject.AddComponent<T>();
        }
    }
}
