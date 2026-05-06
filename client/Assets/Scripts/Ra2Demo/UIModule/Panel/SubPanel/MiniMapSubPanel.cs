using UnityEngine;
using TMPro;
using ZFrame;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using ZLockstep.Flow;

/// <summary>
/// 主面板的小地图子面板控制器 - 封装小地图的 UI 逻辑和数据
/// </summary>
public class MiniMapSubPanel
{
    private GameObject root;
    private TMP_Text titleText;
    private Image mapImage;
    private TMP_Text sizeText;

    /// <summary>
    /// 绑定的数据
    /// </summary>
    public MiniMapPanelData Data { get; private set; }

    public MiniMapSubPanel(Transform parent)
    {
        root = parent.Find("MiniMap")?.gameObject;
        if (root == null) return;

        // 获取组件引用
        titleText = root.transform.Find("LabelTitle")?.GetComponent<TMP_Text>();
        mapImage = root.transform.Find("Image")?.GetComponent<Image>();
        sizeText = root.transform.Find("LabelSize")?.GetComponent<TMP_Text>();

        // 初始化 UI
        UpdateUI();
        
        // 设置点击监听
        SetupClickHandler();
    }

    /// <summary>
    /// 设置点击事件监听器
    /// </summary>
    private void SetupClickHandler()
    {
        if (mapImage == null) return;
        
        // 添加 EventTrigger 组件来监听点击事件
        var eventTrigger = mapImage.gameObject.AddComponent<EventTrigger>();
        
        // 创建 PointerClick 事件条目
        var clickEntry = new EventTrigger.Entry();
        clickEntry.eventID = EventTriggerType.PointerClick;
        clickEntry.callback.AddListener(OnPointerClick);
        eventTrigger.triggers.Add(clickEntry);
    }

    /// <summary>
    /// 设置数据并显示
    /// </summary>
    public void Show(MiniMapPanelData data)
    {
        Data = data;
        UpdateUI();
        SetActive(true);
    }

    /// <summary>
    /// 隐藏子面板
    /// </summary>
    public void Hide()
    {
        SetActive(false);
    }

    public bool IsVisiable()
    {
        return root != null && root.activeSelf;
    }

    /// <summary>
    /// 刷新 UI 显示
    /// </summary>
    public void UpdateUI()
    {
        if (Data == null) return;

        if (titleText != null) titleText.text = Data.Title;
        if (sizeText != null) sizeText.text = Data.SizeText;
    }

    /// <summary>
    /// 设置显示状态
    /// </summary>
    public void SetActive(bool active)
    {
        root?.SetActive(active);
    }

    /// <summary>
    /// 设置小地图纹理
    /// </summary>
    /// <param name="texture">小地图渲染纹理</param>
    public void SetMiniMapTexture(RenderTexture texture)
    {
        if (mapImage != null && texture != null)
        {
            // 从 RenderTexture 创建 Texture2D
            Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            
            // 激活并读取 RenderTexture
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = texture;
            texture2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = previousActive;
            
            // 使用 Texture2D 创建 Sprite
            Sprite sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
            mapImage.sprite = sprite;
        }
    }

    /// <summary>
    /// 处理点击事件
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    private void OnPointerClick(BaseEventData eventData)
    {
        if (mapImage == null) return;
        
        var pointerData = (PointerEventData)eventData;

        RectTransform rectTransform = mapImage.rectTransform;

        // 将屏幕点击点转换为相对于 Image 的局部坐标（以 Image 的中心为原点）
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                pointerData.position,
                pointerData.pressEventCamera,
                out localPoint))
        {
            // localPoint 是相对于 Image pivot 的坐标
            // 如果你想以左下角为 (0,0)，可以进一步转换：
            Vector2 normalizedPos = new Vector2(
                (localPoint.x + rectTransform.rect.width * rectTransform.pivot.x) / rectTransform.rect.width,
                (localPoint.y + rectTransform.rect.height * rectTransform.pivot.y) / rectTransform.rect.height
            );

            zUDebug.Log($"点击局部坐标: {localPoint}");
            zUDebug.Log($"归一化坐标 (0~1): {normalizedPos}");

            // 示例：根据点击位置移动主地图视角
            HandleMiniMapClick(normalizedPos);
        }
        
        // 输出被点击对象的详细信息（调试用）
        zUDebug.Log($"[MiniMap] 当前选中对象：{EventSystem.current?.currentSelectedGameObject?.name ?? "null"}");
    }

    void HandleMiniMapClick(Vector2 normalizedPos)
    {
        zUDebug.Log($"[MiniMap] 点击归一化坐标: {normalizedPos}");
        // 假设 normalizedPos.x 和 .y 在 [0,1] 范围内
        // // 你可以映射到世界坐标的某个范围
        Ra2Demo ra2Demo = UnityEngine.Object.FindObjectOfType<Ra2Demo>();
        
        int width = ra2Demo.GetBattleGame().MapManager.GetWidth();
        int height = ra2Demo.GetBattleGame().MapManager.GetHeight();

        float worldX = Mathf.Lerp(0, width, normalizedPos.x);
        float worldZ = Mathf.Lerp(0, height, normalizedPos.y);
        
        // 移动主相机或角色
        ra2Demo._cameraTarget.transform.position = new Vector3(worldX, 0, worldZ);
    }

    /// <summary>
    /// 销毁时调用，清理事件
    /// </summary>
    public void Destroy()
    {
    }
}