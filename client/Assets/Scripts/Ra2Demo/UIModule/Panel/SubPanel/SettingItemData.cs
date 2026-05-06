using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 设置项数据结构 - 用于 SettingSubPanel 显示设置信息
/// </summary>
public class SettingItemData
{
    /// <summary>
    /// 设置项名称
    /// </summary>
    public string Name;
    
    /// <summary>
    /// 设置项描述
    /// </summary>
    public string Description;
    
    /// <summary>
    /// 设置项类型
    /// </summary>
    public SettingType SettingType;
    
    /// <summary>
    /// 当前值（用于 Slider 等控件）
    /// </summary>
    public float CurrentValue;
    
    /// <summary>
    /// 最小值
    /// </summary>
    public float MinValue;
    
    /// <summary>
    /// 最大值
    /// </summary>
    public float MaxValue;
}

/// <summary>
/// 设置项类型枚举
/// </summary>
public enum SettingType
{
    Music,      // 背景音乐
    SoundEffect,// 音效
    Graphics,   // 图形设置
    Other       // 其他设置
}
