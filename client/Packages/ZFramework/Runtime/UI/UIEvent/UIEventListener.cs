using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 主要用于获取GO上的UIEventPointer，UIEventDrag 没有就添加
/// @data: 2019-01-30
/// @author: LLL
/// </summary>
public class UIEventListener
{
    private static string moduleName = "UIEventListener";

    /// <summary>
    /// 外部传入的按钮事件回调(带参数)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public delegate void UIEventCallBack<T>(T _t = default(T));
    /// <summary>
    /// 外部传入的按钮事件回调(不带参数)
    /// </summary>
    public delegate void UIEventCallBack();

    /// <summary>
    /// 扩展回调中获得PointerEventData引用的回调 有自定义参数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_eventData"></param>
    /// <param name="_t"></param>
    public delegate void UIEventCallBackContainEventData<T>(PointerEventData _eventData, T _t = default(T));
    /// <summary>
    /// 扩展回调中获得PointerEventData引用的回调 无自定义参数
    /// </summary>
    public delegate void UIEventCallBackContainEventData(PointerEventData _eventData);



    /// <summary>
    /// 按钮事件回调
    /// </summary>
    /// <param name="_go"></param>
    /// <param name="_eventData"></param>
    public delegate void UIEventDelegate(GameObject _go, PointerEventData _eventData);
    /// <summary>
    /// 按钮事件回调
    /// </summary>
    /// <param name="_go"></param>
    /// <param name="_eventData"></param>
    public delegate void UIBaseEventDelegate(GameObject _go, BaseEventData _eventData);

    #region 点击监听

    /// <summary>
    /// 获得点击 UIEventPointer
    /// </summary>
    /// <param name="_go"></param>
    /// <returns></returns>
    public static UIEventPointer GetPointer(GameObject _go)
    {
        if (_go == null)
        {
            Debugger.LogError(moduleName, $"GetPointer GameObject is null!");
            return null;
        }

        UIEventPointer pointer = _go.GetComponent<UIEventPointer>() ?? _go.AddComponent<UIEventPointer>();
        return pointer;
    }

    /// <summary>
    /// 获得长按 UIEventPointer
    /// </summary>
    /// <param name="_go"></param>
    /// <param name="_pressTimeDuration"></param>
    /// <returns></returns>
    public static UIEventPointer GetLongPointer(GameObject _go, float _pressTimeDuration = 1f)
    {
        if (_go == null)
        {
            Debugger.LogError(moduleName, $"GetLongPointer GameObject is null!");
            return null;
        }

        UIEventPointer pointer = _go.GetComponent<UIEventPointer>() ?? _go.AddComponent<UIEventPointer>();
        pointer.SetPressTimeDuration(_pressTimeDuration);
        return pointer;
    }

    #endregion

    #region 拖拽监听

    /// <summary>
    /// 获得拖拽 UIEventDrager
    /// </summary>
    /// <param name="_go"></param>
    /// <returns></returns>
    public static UIEventDrager GetDrager(GameObject _go)
    {
        if (_go == null)
        {
            Debugger.LogError(moduleName, $"GetDrag GameObject is null!");
            return null;
        }

        UIEventDrager drager = _go.GetComponent<UIEventDrager>() ?? _go.AddComponent<UIEventDrager>();
        return drager;
    }

    /// <summary>
    /// 获得 ScrollRect 拖拽 UIEventDrager
    /// </summary>
    /// <param name="_sc"></param>
    /// <returns></returns>
    public static UIEventDrager GetDrager(ScrollRect _sc)
    {
        if (_sc == null)
        {
            Debugger.LogError(moduleName, $"GetDrag ScrollRect is null!");
            return null;
        }

        UIEventDrager drager = _sc.GetComponent<UIEventDrager>() ?? _sc.gameObject.AddComponent<UIEventDrager>();
        drager.sc = _sc;
        return drager;
    }

    #endregion
}
