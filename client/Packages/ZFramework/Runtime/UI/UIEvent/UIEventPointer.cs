using System;
using UnityEngine;
using UnityEngine.EventSystems;
using ZLib;

/// <summary>
/// 按钮事件监听
/// @data: 2019-01-30
/// @author: LLL
/// </summary>
public class UIEventPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IUpdateSelectedHandler, ISelectHandler
{
    public static event EventHandler<bool> UIEventHandler;

    /// <summary>
    /// 点击
    /// </summary>
    public UIEventListener.UIEventDelegate onClick;
    /// <summary>
    /// 按下
    /// </summary>
    public UIEventListener.UIEventDelegate onDown;
    /// <summary>
    /// 进入
    /// </summary>
    public UIEventListener.UIEventDelegate onEnter;
    /// <summary>
    /// 退出
    /// </summary>
    public UIEventListener.UIEventDelegate onExit;
    /// <summary>
    /// 抬起
    /// </summary>
    public UIEventListener.UIEventDelegate onUp;
    /// <summary>
    /// 拖拽
    /// </summary>
    public UIEventListener.UIEventDelegate onDrag;
    /// <summary>
    /// 拖拽结束
    /// </summary>
    public UIEventListener.UIEventDelegate onEndDrag;
    /// <summary>
    /// 长按
    /// </summary>
    public UIEventListener.UIEventDelegate onLongPress;
    /// <summary>
    /// 选择
    /// </summary>
    public UIEventListener.UIBaseEventDelegate onSelect;
    /// <summary>
    /// 更新选择
    /// </summary>
    public UIEventListener.UIBaseEventDelegate onUpdateSelect;

    /// <summary>
    /// 记录长按时间
    /// </summary>
    private float recordPressTime = 0;
    /// <summary>
    /// 长按时间间隔
    /// </summary>
    private float pressTimeDuration = 1;

    /// <summary>
    /// 进入
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        onEnter?.Invoke(gameObject, eventData);
    }

    /// <summary>
    /// 退出
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerExit(PointerEventData eventData)
    {
        recordPressTime = Time.time;

        if (onLongPress != null)
            onUp?.Invoke(gameObject, eventData);

        onExit?.Invoke(gameObject, eventData);
    }

    /// <summary>
    /// 按下
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(PointerEventData eventData)
    {
        onDown?.Invoke(gameObject, eventData);

        UIEventHandler?.Invoke(null, true);

        //若长按事件存在，按下时开始计时
        if (onLongPress != null)
        {
            recordPressTime = Time.time;
            Tick.SetTimeout(CheckLongPress, eventData, pressTimeDuration);
        }
    }

    /// <summary>
    /// 抬起
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(PointerEventData eventData)
    {
        UIEventHandler?.Invoke(null, false);

        onUp?.Invoke(gameObject, eventData);
    }

    /// <summary>
    /// 点击
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {

        if (onLongPress == null || Time.time - recordPressTime < pressTimeDuration)
        {
            UIEventHandler?.Invoke(null, false);

            onClick?.Invoke(gameObject, eventData);
        }

        recordPressTime = Time.time;
    }

    /// <summary>
    /// 更新选中状态
    /// </summary>
    /// <param name="eventData"></param>
    public void OnUpdateSelected(BaseEventData eventData)
    {
        onUpdateSelect?.Invoke(gameObject, eventData);
    }

    /// <summary>
    /// 选中
    /// </summary>
    /// <param name="eventData"></param>
    public void OnSelect(BaseEventData eventData)
    {
        UIEventHandler?.Invoke(null, false);

        onSelect?.Invoke(gameObject, eventData);
    }

    /// <summary>
    /// 设置长按间隔时间
    /// </summary>
    /// <param name="_duration"></param>
    public void SetPressTimeDuration(float _duration)
        => this.pressTimeDuration = _duration;

    /// <summary>
    /// 检测长按事件执行
    /// </summary>
    /// <param name="_eventData"></param>
    private void CheckLongPress(PointerEventData _eventData)
    {
        if (this == null)
        {
            this.LogError("This UIEventPointer is null!");
            return;
        }

        if (onLongPress != null && Time.time - recordPressTime > pressTimeDuration)
            onLongPress(gameObject, _eventData);
    }

    #region 移除事件

    /// <summary>
    /// 移除全部事件
    /// </summary>
    public void RemoveEvent()
    {
        onClick = null;
        onDown = null;
        onEnter = null;
        onExit = null;
        onUp = null;
        onDrag = null;
        onEndDrag = null;
        onLongPress = null;
        onSelect = null;
        onUpdateSelect = null;
    }

    #endregion
}
