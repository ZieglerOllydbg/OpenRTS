using UnityEngine;
using UnityEngine.UI;
using ZFrame;
using TMPro;
using System.Collections.Generic;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Simulation.ECS;
using ZLockstep.View;
using ZLib;
using Cinemachine;

/// <summary>
/// 血量面板 - 显示单位血量信息
/// </summary>
[UIModel(
    panelID = "HealthPanel",
    panelPath = "HealthPanel",
    panelName = "血量面板",
    panelUIDepthType = ClientUIDepthTypeID.GameTop
)]
public class HealthPanel : BasePanel
{
    // 血量条预制体引用
    private GameObject greenHpPrefab;
    private GameObject redHpPrefab;
    
    // 存储所有血量条实例的字典，key 为实体 ID
    private Dictionary<int, HealthBarInstance> healthBars = new Dictionary<int, HealthBarInstance>();
    
    // 血条永久显示开关（从 PlayerPrefs 读取）
    private bool showHealthBarAlways = false;
    
    // 血量条实例类
    private class HealthBarInstance
    {
        public GameObject gameObject;
        public Image fillImage;
        public int entityId;
        public bool isSelf;
    }
    
    public HealthPanel(IDispathMessage _processor, UIModelData _modelData, DisableNew _disableNew) 
        : base(_processor, _modelData, _disableNew)
    {
    }

    protected override void OnBecameVisible()
    {
        base.OnBecameVisible();
        
        // 获取血量条预制体引用
        greenHpPrefab = PanelObject.transform.Find("GreenHp")?.gameObject;
        redHpPrefab = PanelObject.transform.Find("RedHp")?.gameObject;
        
        // 确保预制体存在且初始隐藏
        if (greenHpPrefab != null)
        {
            greenHpPrefab.SetActive(false);
        }
        if (redHpPrefab != null)
        {
            redHpPrefab.SetActive(false);
        }

        // 加载血条永久显示设置
        showHealthBarAlways = PlayerPrefs.GetInt("ShowHealthBar", 0) == 1;
    }

    protected override void AddEvent()
    {
        base.AddEvent();
    }

    protected override void RemoveEvent()
    {
        base.RemoveEvent();
    }
    
    /// <summary>
    /// 更新指定实体的血量显示
    /// </summary>
    /// <param name="entityId">实体ID</param>
    /// <param name="isSelf">是否为己方</param>
    /// <param name="currentHealth">当前血量</param>
    /// <param name="maxHealth">最大血量</param>
    public void UpdateHealth(int entityId, bool isSelf)
    {
        // 查找或创建血量条实例
        HealthBarInstance healthBarInstance;
        if (!healthBars.TryGetValue(entityId, out healthBarInstance))
        {
            // 创建新的血量条实例
            healthBarInstance = CreateHealthBar(entityId, isSelf);
            healthBars[entityId] = healthBarInstance;
        }
        
        // 更新血量显示
        if (healthBarInstance.fillImage != null)
        {
            healthBarInstance.fillImage.fillAmount = 1.0f;
        }
    }

    public void UpdateAllHealthBars()
    {
        foreach (var healthBar in healthBars.Values)
        {
            UpdateHealthBar(healthBar.entityId);
        }
    }

    /// <summary>
    /// 获取当前相机缩放级别
    /// </summary>
    /// <returns>相机缩放级别</returns>
    private float GetCameraZoom()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return MainPanel.CAMERA_ZOOM_DEFAULT;
        }
        
        // 对于正交相机，直接使用 orthographicSize 作为缩放级别
        // orthographicSize 越大表示视野越远（缩放级别越大）
        // orthographicSize 越小表示视野越近（缩放级别越小）
        // 这与 Slider 的值范围 (CAMERA_ZOOM_MIN 到 CAMERA_ZOOM_MAX) 一致
        return mainCamera.orthographicSize;
    }

    /// <summary>
    /// 更新血量条的位置
    /// </summary>
    public void UpdateHealthBar(int entityId)
    {
        Ra2Demo ra2Demo = GameObject.FindObjectOfType<Ra2Demo>();
        if (ra2Demo == null)
        {
            return;
        }

        ComponentManager componentManager = ra2Demo.GetBattleGame().World.ComponentManager;

        Entity entity = new Entity(entityId);
        if (!componentManager.HasComponent<TransformComponent>(entity))
        {
            return;
        }

        var transformComponent = componentManager.GetComponent<TransformComponent>(entity);
        Vector3 worldPosition = transformComponent.Position.ToVector3();

        // 获取主相机，将 3D 坐标转换为屏幕坐标
        Camera mainCamera = Camera.main;
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        // 获取单位血量组件
        if (!componentManager.HasComponent<HealthComponent>(entity))
        {
            return;
        }
        var healthComponent = componentManager.GetComponent<HealthComponent>(entity);

        if (healthBars.TryGetValue(entityId, out HealthBarInstance healthBarInstance))
        {
            if (healthBarInstance.gameObject != null)
            {
                // 获取当前相机缩放级别
                float cameraZoom = GetCameraZoom();
                
                // 根据相机缩放级别动态调整血条偏移量和大小
                // orthographicSize 越小（视野越近），offsetY 应该越大，使血条显示更高，scale 也越大
                // orthographicSize 越大（视野越远），offsetY 应该越小，使血条显示更低，scale 也越小
                // 基准值：cameraZoom=15 时，offsetY=60，scale=1
                float baseOffsetY = 60f;
                float baseCameraZoom = MainPanel.CAMERA_ZOOM_DEFAULT;
                // 使用反比关系：zoomRatio = baseCameraZoom / cameraZoom
                // cameraZoom=10 时，zoomRatio = 15/10 = 1.5，offsetY = 60 * 1.5 = 90，scale = 1.5
                // cameraZoom=30 时，zoomRatio = 15/30 = 0.5，offsetY = 60 * 0.5 = 30，scale = 0.5
                float zoomRatio = baseCameraZoom / cameraZoom;
                float offsetY = baseOffsetY * zoomRatio;
                
                float offsetX = 0f;
                // 添加向上的偏移量，使血条显示在单位上方
                if (componentManager.HasComponent<BuildingComponent>(entity))
                {
                    int buildingType = componentManager.GetComponent<BuildingComponent>(entity).BuildingType;
                    ConfBuilding confBuilding = ConfigManager.Get<ConfBuilding>(buildingType);
                    if (confBuilding != null)
                    {
                        offsetX = confBuilding.HpOffsetX;
                        // 建筑物的偏移也按相同比例缩放
                        offsetY = confBuilding.HpOffsetY * zoomRatio;
                    }
                    else
                    {
                        offsetY = 120f * zoomRatio;
                    }
                }
                
                healthBarInstance.gameObject.transform.position = new Vector3(screenPosition.x + offsetX, screenPosition.y + offsetY, 0);
                
                // 根据相机缩放级别动态调整血条大小
                // 保持血条在世界空间中的视觉大小一致
                float baseScale = 1f;
                float scale = baseScale * zoomRatio;
                healthBarInstance.gameObject.transform.localScale = new Vector3(scale, scale, scale);
            }

            if (healthBarInstance.fillImage != null)
            {
                healthBarInstance.fillImage.fillAmount = healthComponent.HealthPercent.ToFloat();
            }

            // 根据永久显示设置决定是否显示血条
            float healthPercent = healthComponent.HealthPercent.ToFloat();
            if (showHealthBarAlways)
            {
                // 永久显示模式：只要血量大于 0 就显示
                if (healthPercent > 0.0f)
                {
                    ShowHealthBar(entityId);
                }
                else
                {
                    HideHealthBar(entityId);
                }
            }
            else
            {
                // 默认模式：满血（>=100%）或死亡（<=0%）时不显示
                if (healthPercent >= 1.0f || healthPercent <= 0.0f)
                {
                    HideHealthBar(entityId);
                }
                else
                {
                    ShowHealthBar(entityId);
                }
            }
        }
    }
    
    /// <summary>
    /// 隐藏指定实体的血量条
    /// </summary>
    /// <param name="entityId">实体 ID</param>
    public void HideHealthBar(int entityId)
    {
        if (healthBars.TryGetValue(entityId, out HealthBarInstance healthBarInstance))
        {
            if (healthBarInstance.gameObject != null)
            {
                healthBarInstance.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 删除指定实体的血量条实例
    /// </summary>
    /// <param name="entityId">实体 ID</param>
    public void DeleteHealthBar(int entityId)
    {
        if (healthBars.TryGetValue(entityId, out HealthBarInstance healthBarInstance))
        {
            if (healthBarInstance.gameObject != null)
            {
                GameObject.Destroy(healthBarInstance.gameObject);
            }
            healthBars.Remove(entityId);
        }
    }

    /// <summary>
    /// 显示指定实体的血量条
    /// </summary>
    /// <param name="entityId">实体 ID</param>
    public void ShowHealthBar(int entityId)
    {
        if (healthBars.TryGetValue(entityId, out HealthBarInstance healthBarInstance))
        {
            if (healthBarInstance.gameObject != null)
            {
                healthBarInstance.gameObject.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// 创建血量条实例
    /// </summary>
    /// <param name="entityId">实体ID</param>
    /// <param name="isSelf">是否为己方</param>
    /// <returns>血量条实例</returns>
    private HealthBarInstance CreateHealthBar(int entityId, bool isSelf)
    {
        // 选择对应的预制体
        GameObject prefab = isSelf ? redHpPrefab : greenHpPrefab;
        if (prefab == null)
        {
            Debug.LogError($"血量条预制体未找到！isSelf: {isSelf}");
            return null;
        }
        
        // 创建实例
        GameObject healthBarGO = GameObject.Instantiate(prefab, PanelObject.transform);
        healthBarGO.name = $"HealthBar_{(isSelf ? "Green" : "Red")}_{entityId}";
        healthBarGO.SetActive(true);
        
        // 获取Fill图像组件
        Image fillImage = healthBarGO.transform.Find("Fill")?.GetComponent<Image>();
        if (fillImage == null)
        {
            Debug.LogError("无法找到Fill图像组件！");
        }
        
        // 创建实例对象
        HealthBarInstance instance = new HealthBarInstance
        {
            gameObject = healthBarGO,
            fillImage = fillImage,
            entityId = entityId,
            isSelf = isSelf
        };
        
        return instance;
    }
    
    /// <summary>
    /// 清理所有血量条
    /// </summary>
    public void ClearAllHealthBars()
    {
        foreach (var kvp in healthBars)
        {
            if (kvp.Value.gameObject != null)
            {
                GameObject.Destroy(kvp.Value.gameObject);
            }
        }
        healthBars.Clear();
    }
    
    protected override void OnBecameInvisible()
    {
        base.OnBecameInvisible();
        // 面板隐藏时清理所有血量条
        ClearAllHealthBars();

    }

    /// <summary>
    /// 血条设置变更事件处理（由 Ra2Processor 调用）
    /// </summary>
    internal void OnHealthBarSettingChanged(HealthBarSettingChangedEvent e)
    {
        showHealthBarAlways = e.ShowAlways;
        zUDebug.Log($"[HealthPanel] 血条永久显示设置已更新为：{(e.ShowAlways ? "开启" : "关闭")}");
        
        // 立即刷新所有现有血量条的显示状态
        UpdateAllHealthBars();
    }
}
