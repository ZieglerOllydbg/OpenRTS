using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class RtsCameraController : MonoBehaviour
{
    [Header("相机组件")]
    [SerializeField] private Camera mainCamera;

    [Header("平移设置")]
    [SerializeField] private float panSpeed = 30f;

    [Header("缩放设置")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoomHeight = 10f;
    [SerializeField] private float maxZoomHeight = 80f;

    [Header("地图边界")]
    [SerializeField] private Vector2 xBounds = new Vector2(-50, 50);
    [SerializeField] private Vector2 zBounds = new Vector2(-50, 50);

    private @RTSControl controls;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        controls = new @RTSControl();
    }

    private void OnEnable()
    {
        controls.Camera.Enable();
        controls.Camera.Zoom.performed += OnZoomPerformed;
    }

    private void OnDisable()
    {
        controls.Camera.Disable();
        controls.Camera.Zoom.performed -= OnZoomPerformed;
    }

    private void LateUpdate()
    {
        HandlePan();
        ApplyBounds(); // 确保相机不飞出地图
    }

    private void HandlePan()
    {
        if (!Pointer.current.press.isPressed) return;

        // --- 修正后的UI检测逻辑 ---
        // 检查指针是否在UI元素上
        bool isOverUI = false;
        // 移动端（触摸）路径
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            isOverUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue());
        }
        // PC端（鼠标）路径
        else
        {
            isOverUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
        }

        if (isOverUI)
        {
            return;
        }
        // --- UI检测逻辑结束 ---

        Vector2 panDelta = controls.Camera.Pan.ReadValue<Vector2>();

        // 核心逻辑：相对于地面的平移
        Vector3 forward = mainCamera.transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = mainCamera.transform.right;

        Vector3 moveDirection = (forward * panDelta.y + right * panDelta.x) * -1;

        // 使用 Space.World 确保移动是基于世界坐标系
        transform.Translate(moveDirection * panSpeed * Time.deltaTime, Space.World);
    }

    private void OnZoomPerformed(InputAction.CallbackContext context)
    {
        float scrollValue = context.ReadValue<Vector2>().y;

        // 计算新的Y轴高度
        float newHeight = transform.position.y - scrollValue * zoomSpeed * Time.deltaTime * 50f; // 乘50让手感更灵敏

        // 直接应用并限制高度
        Vector3 newPosition = transform.position;
        newPosition.y = Mathf.Clamp(newHeight, minZoomHeight, maxZoomHeight);
        transform.position = newPosition;
    }

    private void ApplyBounds()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, xBounds.x, xBounds.y);
        pos.z = Mathf.Clamp(pos.z, zBounds.x, zBounds.y);
        transform.position = pos;
    }
}