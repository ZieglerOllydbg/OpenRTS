using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using ZLockstep.Simulation.ECS;
using ZFrame;
using ZLockstep.Simulation.ECS.Components;
using Utils;
using PostHogUnity; // 添加 StringUtils 所在的命名空间

/// <summary>
/// 主面板的子面板控制器 - 封装子面板的UI逻辑和数据
/// </summary>
public class MainBuildingSubPanel
{
    private GameObject root;
    private TMP_Text titleText;
    private Button closeBtn;
    
    // 列表相关（支持ScrollRect）
    private ScrollRect listScrollRect;
    private Transform listContent;      // ScrollRect的Content，列表项放这里
    private GameObject itemTemplate;
    private List<BuildingListItem> listItems = new List<BuildingListItem>();
    
    /// <summary>
    /// 绑定的数据
    /// </summary>
    public SubPanelData Data { get; private set; }
    
    /// <summary>
    /// 关闭按钮点击回调（可选，用于通知父面板）
    /// </summary>
    public System.Action OnCloseClick;
    
    public MainBuildingSubPanel(Transform parent)
    {
        root = parent.Find("Building")?.gameObject;
        if (root == null) return;
        
        // 获取组件引用
        titleText = root.transform.Find("Title")?.GetComponent<TMP_Text>();
        closeBtn = root.transform.Find("CloseBtn")?.GetComponent<Button>();
        
        closeBtn?.onClick.AddListener(OnClose);
        
        // 初始化列表（ScrollRect结构）
        // 预期结构: ListScrollView(ScrollRect) -> Viewport -> Content
        var scrollViewObj = root.transform.Find("ScrollView");
        if (scrollViewObj != null)
        {
            listScrollRect = scrollViewObj.GetComponent<ScrollRect>();
            listContent = listScrollRect?.content;  // ScrollRect.content 自动指向 Content
        }
        
        // 模板可以放在Content下或者单独放置
        itemTemplate = root.transform.Find("ItemTemplate")?.gameObject;
        if (itemTemplate == null && listContent != null)
        {
            // 尝试从Content下查找模板
            itemTemplate = listContent.Find("ItemTemplate")?.gameObject;
        }
        itemTemplate?.SetActive(false); // 隐藏模板

        var testData = new List<BuildItemData>();

        // 可以在这里添加匹配成功的处理逻辑
        Ra2Demo ra2Demo = Object.FindObjectOfType<Ra2Demo>();

        int localPlayerCampID = EcsUtils.GetLocalPlayerCampId(ra2Demo.GetBattleGame());

        // 从配置数据中加载所有建筑信息
        List<ConfBuildingPlace> confBuildingPlaces = ConfigManager.GetAll<ConfBuildingPlace>();
        foreach (var confBuildingPlace in confBuildingPlaces)
        {
            if (confBuildingPlace.Enabled == 0) continue;

            if (confBuildingPlace.CampID != localPlayerCampID) continue;

            // TODO confBuildingPlace.Type = confBuilding.ID，未来需要一张映射表
            var confBuilding = ConfigManager.Get<ConfBuilding>(confBuildingPlace.Type);
            if (confBuilding == null)
            {
                zUDebug.LogError($"[MainBuildingSubPanel] 创建建筑时无法获取建筑配置信息。ID:{confBuildingPlace.Type}");
                continue;
            }

            // 根据配置创建建筑项数据
            var buildItem = new BuildItemData
            {
                ConfBuildingPlaceID = confBuildingPlace.ID,
                BuildingType = (BuildingType)confBuilding.Type,
                Name = $"{confBuilding.Name}(${confBuilding.CostMoney})",
                Description = confBuilding.Description
            };
            
            testData.Add(buildItem);
        }

        // 如果配置数据为空或不完整，使用默认数据作为备选
        if (testData.Count == 0)
        {
            testData.AddRange(new List<BuildItemData>
            {
                new() { BuildingType = BuildingType.Smelter, Name = "采矿场($800)", Description = "可以进行采矿" },
                new() { BuildingType = BuildingType.PowerPlant, Name = "电厂($500)", Description = "可以进行发电" },
                new() { BuildingType = BuildingType.LightFactory, Name = "坦克工厂($1000)", Description = "可以进行建造坦克" },
            });
        }
        
        RefreshList(testData);
    }
    
    /// <summary>
    /// 设置数据并显示
    /// </summary>
    public void Show(SubPanelData data)
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
    /// 刷新UI显示
    /// </summary>
    public void UpdateUI()
    {
        if (Data == null) return;
        
        if (titleText != null) titleText.text = Data.Title;
    }
    
    /// <summary>
    /// 设置显示状态
    /// </summary>
    public void SetActive(bool active)
    {
        root?.SetActive(active);
    }
    
    private void OnClose()
    {
        Hide();
        OnCloseClick?.Invoke();
    }
    
    /// <summary>
    /// 刷新列表数据
    /// 注意：Content 节点需要添加 VerticalLayoutGroup 或 HorizontalLayoutGroup 组件来自动排列子项
    /// </summary>
    public void RefreshList(List<BuildItemData> dataList)
    {
        ClearList();
        
        if (itemTemplate == null || listContent == null)
        {
            zUDebug.LogWarning("列表模板或Content容器未找到");
            return;
        }
        
        // 检查是否有 LayoutGroup，没有的话给出提示
        var layoutGroup = listContent.GetComponent<LayoutGroup>();
        if (layoutGroup == null)
        {
            zUDebug.LogWarning("Content 上没有 LayoutGroup 组件，列表项可能会重叠。请添加 VerticalLayoutGroup 或 HorizontalLayoutGroup");
        }
        
        // 计算单个列表项的高度
        float itemHeight = 0f;
        if (itemTemplate != null)
        {
            var templateRectTransform = itemTemplate.GetComponent<RectTransform>();
            if (templateRectTransform != null)
            {
                itemHeight = templateRectTransform.rect.height;
            }
        }
        
        // 获取LayoutGroup的间距设置
        float spacing = 0f;
        if (layoutGroup is VerticalLayoutGroup verticalLayout)
        {
            spacing = verticalLayout.spacing;
        }
        else if (layoutGroup is HorizontalLayoutGroup horizontalLayout)
        {
            spacing = horizontalLayout.spacing;
        }
        
        foreach (var data in dataList)
        {
            var itemGo = GameObject.Instantiate(itemTemplate, listContent);
            
            // 重置 RectTransform，确保不继承模板的偏移
            var rectTransform = itemGo.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
                // 如果有 LayoutGroup，位置会被自动管理；没有的话重置为零点
                if (layoutGroup == null)
                {
                    rectTransform.anchoredPosition = Vector2.zero;
                }
            }
            
            itemGo.SetActive(true);
            
            var item = new BuildingListItem(itemGo);
            item.SetData(data);
            item.OnItemSelect = OnListItemSelect;
            listItems.Add(item);
        }
        
        // 根据元素数量调整Content容器的高度
        if (layoutGroup != null && itemHeight > 0f)
        {
            var contentRectTransform = listContent as RectTransform;
            if (contentRectTransform != null)
            {
                // 计算总高度：元素数量 × 单个元素高度 + (元素数量-1) × 间距
                float totalHeight = dataList.Count * itemHeight + System.Math.Max(0, dataList.Count - 1) * spacing;
                
                // 设置Content容器的高度
                var sizeDelta = contentRectTransform.sizeDelta;
                contentRectTransform.sizeDelta = new Vector2(sizeDelta.x, totalHeight);
            }
        }
        
        // 强制刷新布局
        if (layoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent as RectTransform);
        }
        
        // 刷新后滚动到顶部
        ScrollToTop();
    }
    
    /// <summary>
    /// 清空列表
    /// </summary>
    private void ClearList()
    {
        foreach (var item in listItems)
        {
            item.Destroy();
        }
        listItems.Clear();
    }
    
    /// <summary>
    /// 滚动到顶部
    /// </summary>
    public void ScrollToTop()
    {
        if (listScrollRect != null)
        {
            listScrollRect.verticalNormalizedPosition = 1f;  // 1 = 顶部
        }
    }
    
    /// <summary>
    /// 滚动到底部
    /// </summary>
    public void ScrollToBottom()
    {
        if (listScrollRect != null)
        {
            listScrollRect.verticalNormalizedPosition = 0f;  // 0 = 底部
        }
    }
    
    /// <summary>
    /// 滚动到指定位置 (0-1, 0=底部, 1=顶部)
    /// </summary>
    public void ScrollTo(float normalizedPosition)
    {
        if (listScrollRect != null)
        {
            listScrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
        }
    }
    
    /// <summary>
    /// 列表项选中回调
    /// </summary>
    private void OnListItemSelect(BuildingListItem item)
    {
        UISound.Instance.PlayClick();
        zUDebug.Log($"选中了列表项: {item.ItemData?.Name}");
        // Frame.DispatchEvent(new SelectBuildingEvent(item.ItemData.BuildingType));
        // TODO: 直接放置

        var confBuildingPlace = ConfigManager.Get<ConfBuildingPlace>(item.ItemData.ConfBuildingPlaceID);
        if (confBuildingPlace == null)
        {
            zUDebug.LogError($"未找到建筑配置: {item.ItemData.ConfBuildingPlaceID}");
            return;
        }

        // 1. 根据类型获取已经放置的建筑数量
        int currentCount = GetBuildingCountByType((BuildingType)confBuildingPlace.Type);
        zUDebug.Log($"当前已放置 {item.ItemData.BuildingType} 数量: {currentCount}");

        // 2. 根据配置Count检查数量限制
        if (currentCount >= confBuildingPlace.Count)
        {
            zUDebug.LogWarning($"建筑数量已达上限: {currentCount}/{confBuildingPlace.Count}");
            MessageUtils.ShowTips("建筑数量已达上限");
            // TODO: 可以显示提示信息给用户
            Hide();
            return;
        }

        // 3. 解析配置中的位置信息
        Vector3[] positions = StringUtils.ParsePositions(confBuildingPlace.Position);
        if (positions.Length == 0)
        {
            zUDebug.LogError($"配置中的位置信息无效: {confBuildingPlace.Position}");
            Hide();
            return;
        }
        
        // 计算下一个可用地点（循环使用配置的位置）
        int nextPositionIndex = currentCount % positions.Length;
        Vector3 targetPosition = positions[nextPositionIndex];
        
        zUDebug.Log($"准备在位置 {targetPosition} 放置建筑，索引: {nextPositionIndex}");
        
        int buildingID = confBuildingPlace.Type;
        // 4. 直接调用放置逻辑
        PlaceBuilding(buildingID, targetPosition);

        Hide();

        // PostHog 埋点，记录放置的什么建筑
        PostHog.Capture("PlaceBuilding", new Dictionary<string, object>
        {
            {"BuildingID", buildingID},
            {"BuildingType", item.ItemData.Name}
        });
    }
    
        /// <summary>
    /// 获取指定类型已放置的建筑数量
    /// </summary>
    private int GetBuildingCountByType(BuildingType buildingType)
    {
        Ra2Demo ra2Demo = Object.FindObjectOfType<Ra2Demo>();
        var componentManager = ra2Demo.GetBattleGame().World.ComponentManager;
        int count = 0;
        
        int localPlayerCampID = EcsUtils.GetLocalPlayerCampId(ra2Demo.GetBattleGame());

        // 获取所有建筑实体
        var buildingEntities = componentManager.GetAllEntityIdsWith<BuildingComponent>();
        
        foreach (var entityId in buildingEntities)
        {
            var entity = new Entity(entityId);
            
            // 检查阵营（假设是玩家阵营0）
            if (!componentManager.HasComponent<CampComponent>(entity))
                continue;
                
            var campComponent = componentManager.GetComponent<CampComponent>(entity);
            if (campComponent.CampId != localPlayerCampID) // 玩家阵营ID
                continue;
            
            // 检查建筑类型
            var buildingComponent = componentManager.GetComponent<BuildingComponent>(entity);
            if ((BuildingType)buildingComponent.BuildingType == buildingType)
            {
                // 检查是否已完成建造（排除正在建造中的建筑）
                // if (componentManager.HasComponent<BuildingConstructionComponent>(entity))
                //     continue;
                    
                count++;
            }
        }
        
        return count;
    }

        /// <summary>
    /// 放置建筑的核心逻辑
    /// </summary>
    private void PlaceBuilding(int confID, Vector3 logicPosition)
    {
        // 创建建筑命令（使用CreateBuildingCommand）
        var createBuildingCommand = new ZLockstep.Sync.Command.Commands.CreateBuildingCommand(
            confID: confID,
            campId: 0,
            position: new zUnity.zVector3(
                zfloat.FromRaw((long)(logicPosition.x * zfloat.SCALE_10000)),
                zfloat.FromRaw((long)(logicPosition.y * zfloat.SCALE_10000)),
                zfloat.FromRaw((long)(logicPosition.z * zfloat.SCALE_10000))
            )
        )
        {
            Source = ZLockstep.Sync.Command.CommandSource.Local,
        };

        // 提交命令到游戏世界
        Ra2Demo ra2Demo = Object.FindObjectOfType<Ra2Demo>();
        ra2Demo.GetBattleGame().SubmitCommand(createBuildingCommand);
        
        zUDebug.Log($"已提交建筑放置命令，配置ID: {confID}, 位置: {logicPosition}");
    }

    /// <summary>
    /// 销毁时调用，清理事件
    /// </summary>
    public void Destroy()
    {
        closeBtn?.onClick.RemoveListener(OnClose);
        OnCloseClick = null;
        
        // 清理列表
        ClearList();
    }
}