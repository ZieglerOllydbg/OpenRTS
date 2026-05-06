using UnityEngine;
using ZFrame;

/// <summary>
/// 血量面板 LateUpdate 事件 - 用于在 LateUpdate 中更新血量条位置
/// </summary>
public class HealthPanelLateUpdateEvent : ModuleEvent
{
    public HealthPanelLateUpdateEvent() : base(typeof(Ra2Module))
    {
    }
}