using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

[InputControlLayout]
public class VirtualJoystickDevice : InputDevice
{
    [InputControl] 
    public Vector2Control Vector { get; private set; }

    protected override void FinishSetup()
    {
        base.FinishSetup();
        Vector = GetChildControl<Vector2Control>("vector");
    }

    public void SetJoystickValue(Vector2 value)
    {
        // 限制在 -1 到 1 范围内（可选）
        value = Vector2.ClampMagnitude(value, 1f);
        
        // 使用事件系统更新设备状态
        using (StateEvent.From(this, out var eventPtr))
        {
            Vector.WriteValueIntoEvent(value, eventPtr);
            InputSystem.QueueEvent(eventPtr);
            zUDebug.Log($"SetJoystickValue:{value}");
        }
    }
}