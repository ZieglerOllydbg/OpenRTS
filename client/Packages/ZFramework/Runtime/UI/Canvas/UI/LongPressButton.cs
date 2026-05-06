using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 支持长按事件的按钮组件
/// </summary>
public class LongPressButton : Button
{
    /// <summary>
    /// 长按触发时间（秒）
    /// </summary>
    public const float LONG_PRESS_DURATION = 1f;
    
    /// <summary>
    /// 点击事件
    /// </summary>
    public UnityEvent onClick = new UnityEvent();
    
    /// <summary>
    /// 长按事件
    /// </summary>
    public UnityEvent onLongPress = new UnityEvent();
    
    private bool isPressed = false;
    private float pressedTime;

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        isPressed = true;
        pressedTime = Time.time;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        isPressed = false;
        
        // 如果抬起时按下时间小于长按时间，则视为点击
        if (Time.time - pressedTime < LONG_PRESS_DURATION)
        {
            onClick?.Invoke();
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        isPressed = false;
    }

    private void Update()
    {
        // 检查是否长按
        if (isPressed && Time.time - pressedTime >= LONG_PRESS_DURATION)
        {
            isPressed = false; // 确保只触发一次
            onLongPress?.Invoke();
        }
    }
}