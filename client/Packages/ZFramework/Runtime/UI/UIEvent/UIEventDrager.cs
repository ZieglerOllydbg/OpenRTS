using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 拖拽事件监听
/// @data: 2019-01-30
/// @author: LLL
/// </summary>
public class UIEventDrager : MonoBehaviour, IEventSystemHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static event EventHandler<bool> UIEventHandler;

    /// <summary>
    /// 开始拖拽
    /// </summary>
    public UIEventListener.UIEventDelegate onBeginDrag;
    /// <summary>
    /// 拖拽
    /// </summary>
    public UIEventListener.UIEventDelegate onDrag;
    /// <summary>
    /// 拖拽结束
    /// </summary>
    public UIEventListener.UIEventDelegate onEndDrag;
    /// <summary>
    /// 滚动区域对象
    /// </summary>
    public ScrollRect sc;

    /// <summary>
    /// 开始拖拽
    /// </summary>
    /// <param name="eventData"></param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        sc?.OnBeginDrag(eventData);

        UIEventHandler?.Invoke(null, true);

        onBeginDrag?.Invoke(gameObject, eventData);
    }

    /// <summary>
    /// 拖拽中
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDrag(PointerEventData eventData)
    {
        sc?.OnDrag(eventData);

        onDrag?.Invoke(gameObject, eventData);
    }

    /// <summary>
    /// 拖拽结束
    /// </summary>
    /// <param name="eventData"></param>
    public void OnEndDrag(PointerEventData eventData)
    {
        sc?.OnEndDrag(eventData);

        UIEventHandler?.Invoke(null, false);

        onEndDrag?.Invoke(gameObject, eventData);
    }

    /// <summary>
    /// 移除拖拽事件
    /// </summary>
    public void RemoveDragEvent()
    {
        onBeginDrag = null;
        onDrag = null;
        onEndDrag = null;
    }
}