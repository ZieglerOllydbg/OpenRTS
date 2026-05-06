using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image handle;
    [SerializeField] private float inputRange = 50f; // 摇杆最大偏移像素

    private RectTransform _backgroundRect;
    private Vector2 _inputVector = Vector2.zero;

    private VirtualJoystickDevice _device;

    void Awake()
    {
        _backgroundRect = background.rectTransform;
        // 创建并注册虚拟设备
        _device = InputSystem.AddDevice<VirtualJoystickDevice>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _backgroundRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            _inputVector = localPoint / inputRange;
            _inputVector = Vector2.ClampMagnitude(_inputVector, 1f);

            handle.rectTransform.anchoredPosition = 
                new Vector2(_inputVector.x * inputRange, _inputVector.y * inputRange);

            // 更新虚拟设备的值
            _device.SetJoystickValue(_inputVector);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _inputVector = Vector2.zero;
        handle.rectTransform.anchoredPosition = Vector2.zero;
        
        // 摇杆释放时也要传递零向量，表示停止移动
        _device.SetJoystickValue(_inputVector);
    }

    // 可选：外部访问当前值
    public Vector2 GetInputDirection() => _inputVector;
}