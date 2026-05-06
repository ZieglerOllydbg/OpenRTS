using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using ZFrame;
using ZLib;
using PostHogUnity;

/// <summary>
/// 主面板的生产子面板控制器 - 封装子面板的UI逻辑和数据
/// </summary>
public class MainProducerSubPanel
{
    private GameObject root;
    private TMP_Text titleText;
    private Button closeBtn;
    
    // 列表相关（支持ScrollRect）
    private ScrollRect listScrollRect;
    private Transform listContent;      // ScrollRect的Content，列表项放这里
    private GameObject itemTemplate;
    private List<ProducerListItem> listItems = new List<ProducerListItem>();
    
    // 添加游戏实例引用
    private ZLockstep.Sync.Game game;
    
    // 定时刷新相关
    private int refreshTimerId = -1;
    private const float REFRESH_INTERVAL = 0.1f; // 每秒刷新一次
    
    // 添加生产记录追踪 - 记录用户在当前会话中的增减操作
    private Dictionary<string, int> productionRecords = new Dictionary<string, int>();
    
    /// <summary>
    /// 绑定的数据
    /// </summary>
    public SubPanelData Data { get; private set; }
    
    /// <summary>
    /// 关闭按钮点击回调（可选，用于通知父面板）
    /// </summary>
    public Action OnCloseClick;
    
    public MainProducerSubPanel(Transform parent)
    {
        root = parent.Find("Producer")?.gameObject;
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
    }
    
    /// <summary>
    /// 设置游戏实例引用
    /// </summary>
    public void SetGameContext(ZLockstep.Sync.Game game)
    {
        this.game = game;
    }
    
    /// <summary>
    /// 设置数据并显示
    /// </summary>
    public void Show(SubPanelData data)
    {
        RefreshProducerList();
        Data = data;
        UpdateUI();
        SetActive(true);
        
        // 启动定时刷新
        StartAutoRefresh();
        
        // 清空之前的记录（打开面板时重置）
        productionRecords.Clear();
    }

    private void StartAutoRefresh()
    {
        // 如果已经有定时器在运行，先停止它
        if (refreshTimerId != -1)
        {
            // 注意：这里可能需要根据实际的API来停止定时器
            refreshTimerId = -1;
        }
        
        // 启动新的定时器，每秒刷新一次
        refreshTimerId = Tick.SetTimeout(RefreshProductionData, REFRESH_INTERVAL);
    }
    
    /// <summary>
    /// 刷新生产数据
    /// </summary>
    private void RefreshProductionData()
    {
        // 检查面板是否处于激活状态
        if (root != null && root.activeInHierarchy)
        {
            UpdateProducerListData(); // 只更新数据，不重建控件
            
            // 重新启动定时器以实现持续刷新
            if (refreshTimerId != -1)
            {
                refreshTimerId = Tick.SetTimeout(RefreshProductionData, REFRESH_INTERVAL);
            }
        }
    }
    
    private void RefreshProducerList()
    {
        // 获取建筑列表
        if (game == null)
            return;

        // 使用GetComponentsWithCondition一次性获取符合条件的实体
        var components = game.World.ComponentManager
            .GetComponentsWithCondition<ProduceComponent>(entity =>
                game.World.ComponentManager.HasComponent<LocalPlayerComponent>(entity) &&
                !game.World.ComponentManager.HasComponent<BuildingConstructionComponent>(entity) &&
                game.World.ComponentManager.HasComponent<BuildingComponent>(entity));

        var dataList = new List<ProducerItemData>();

        foreach (var (produceComponent, entity) in components)
        {
            // 为每个支持的单位类型创建一个ProducerItemData
            foreach (var unitType in produceComponent.SupportedUnitTypes)
            {
                ConfUnit confUnit = ConfigManager.Get<ConfUnit>((int)unitType);
                if (confUnit == null)
                {
                    zUDebug.LogError($"Invalid unit type: {unitType}");
                    continue;
                }

                // 获取当前生产数量
                int currentProduceCount = 0;
                if (produceComponent.ProduceNumbers.ContainsKey(unitType))
                {
                    currentProduceCount = produceComponent.ProduceNumbers[unitType];
                }
                
                // 获取当前生产进度
                int productionProgressPercentage = 0;
                if (produceComponent.ProduceProgress.ContainsKey(unitType))
                {
                    productionProgressPercentage = produceComponent.ProduceProgress[unitType];
                }
                
                // 构造带生产数量和进度的名称
                string displayName = confUnit.Name;
                if (currentProduceCount > 0)
                {
                    displayName = $"{displayName}(+{currentProduceCount}){productionProgressPercentage / confUnit.ProduceTime * 100}%";
                }
                
                var itemData = new ProducerItemData
                {
                    Name = displayName,
                    BelongFactory = "$" + confUnit.CostMoney,
                    Description = confUnit.Description,
                    UnitType = unitType,
                    FactoryEntityId = entity.Id,
                };
                dataList.Add(itemData);
            }
        }

        RefreshList(dataList);
    }
    
    /// <summary>
    /// 更新生产者列表数据，不重建控件
    /// </summary>
    private void UpdateProducerListData()
    {
        if (game == null || listItems == null)
            return;

        // 使用 GetComponentsWithCondition 一次性获取符合条件的实体
        var components = game.World.ComponentManager
            .GetComponentsWithCondition<ProduceComponent>(entity =>
                game.World.ComponentManager.HasComponent<LocalPlayerComponent>(entity) &&
                !game.World.ComponentManager.HasComponent<BuildingConstructionComponent>(entity) &&
                game.World.ComponentManager.HasComponent<BuildingComponent>(entity));

        // 计算预期的列表项总数
        int expectedItemCount = 0;
        foreach (var (produceComponent, entity) in components)
        {
            expectedItemCount += produceComponent.SupportedUnitTypes.Count;
        }

        // 如果列表项数量发生变化，需要重建控件
        if (expectedItemCount != listItems.Count)
        {
            zUDebug.Log($"[生产列表] 列表项数量发生变化：{listItems.Count} -> {expectedItemCount}，重建列表");
            RefreshProducerList();
            return;
        }

        // 数量未变化，只更新现有项的数据
        int itemIndex = 0;
        foreach (var (produceComponent, entity) in components)
        {
            // 为每个支持的单位类型创建一个 ProducerItemData
            foreach (var unitType in produceComponent.SupportedUnitTypes)
            {
                if (itemIndex >= listItems.Count)
                    return; // 避免索引越界

                ConfUnit confUnit = ConfigManager.Get<ConfUnit>((int)unitType);
                if (confUnit == null)
                {
                    zUDebug.LogError($"Invalid unit type: {unitType}");
                    continue;
                }

                // 获取当前生产数量
                int currentProduceCount = 0;
                if (produceComponent.ProduceNumbers.ContainsKey(unitType))
                {
                    currentProduceCount = produceComponent.ProduceNumbers[unitType];
                }
                
                // 获取当前生产进度
                int productionProgressPercentage = 0;
                if (produceComponent.ProduceProgress.ContainsKey(unitType))
                {
                    productionProgressPercentage = produceComponent.ProduceProgress[unitType];
                }
                
                // 构造带生产数量和进度的名称
                string displayName = confUnit.Name;
                if (currentProduceCount > 0)
                {
                    displayName = $"{displayName}(+{currentProduceCount}){productionProgressPercentage / confUnit.ProduceTime * 100}%";
                }
                
                var itemData = new ProducerItemData
                {
                    Name = displayName,
                    BelongFactory = "$" + confUnit.CostMoney,
                    Description = confUnit.Description,
                    UnitType = unitType,
                    FactoryEntityId = entity.Id,
                };
                
                // 更新对应索引的列表项数据
                listItems[itemIndex].SetData(itemData);
                itemIndex++;
            }
        }

    }

    
    /// <summary>
    /// 隐藏子面板
    /// </summary>
    public void Hide()
    {
        // 关闭面板时发送 PostHog 记录
        SendProductionRecordToPostHog();
        
        SetActive(false);
        // 停止定时刷新
        StopAutoRefresh();
    }

    /// <summary>
    /// 发送生产记录到 PostHog
    /// </summary>
    private void SendProductionRecordToPostHog()
    {
        if (productionRecords.Count == 0)
        {
            zUDebug.Log("[生产记录] 没有生产记录，跳过上报");
            return;
        }

        // 构建事件属性
        var properties = new Dictionary<string, object>();
        
        // 添加每种生产类型的详细数据
        foreach (var kvp in productionRecords)
        {
            if (kvp.Value != 0) // 只上报有变化的记录
            {
                properties[$"{kvp.Key}"] = kvp.Value;
            }
        }
        
        // 发送事件到 PostHog
        try
        {
            PostHog.Capture("producer_units", properties);
            zUDebug.Log($"[生产记录] 已发送 PostHog 事件：producer_units");
        }
        catch (Exception ex)
        {
            zUDebug.LogError($"[生产记录] 发送 PostHog 事件失败：{ex.Message}");
        }

        // 清空记录
        productionRecords.Clear();
        zUDebug.Log("[生产记录] 已清空生产记录");
    }

    public bool IsVisiable()
    {
        return root?.activeSelf ?? false;
    }
    
    private void StopAutoRefresh()
    {
        if (refreshTimerId != -1)
        {
            // 注意：这里可能需要根据实际的API来停止定时器
            refreshTimerId = -1;
        }
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
    public void RefreshList(List<ProducerItemData> dataList)
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
            
            var item = new ProducerListItem(itemGo);
            item.SetData(data);
            item.OnItemAdd = OnListItemAdd;
            item.OnItemSub = OnListItemSub;
            item.OnItemAddMulti = OnListItemAddMulti;
            listItems.Add(item);
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
    /// 列表项添加回调
    /// </summary>
    private void OnListItemAdd(ProducerListItem item)
    {
        zUDebug.Log($"请求添加生产项: {item.ItemData?.Name}");
        
        // 记录添加操作
        if (item.ItemData != null && game != null)
        {
            string key = $"{item.ItemData.UnitType}";
            if (!productionRecords.ContainsKey(key))
            {
                productionRecords[key] = 0;
            }
            productionRecords[key]++;
            
            zUDebug.Log($"[生产记录] 添加: {key}, 当前数量={productionRecords[key]}");
        }
        
        // 实现添加逻辑，例如发送命令增加单位生产
        if (item.ItemData != null && game != null)
        {
            SendProduceCommand(item.ItemData.FactoryEntityId, item.ItemData.UnitType, 1);
        }
    }

    private void OnListItemAddMulti(ProducerListItem item)
    {
        zUDebug.Log($"请求添加生产项: {item.ItemData?.Name}");
        
        // 记录添加操作
        if (item.ItemData != null && game != null)
        {
            string key = $"{item.ItemData.UnitType}";
            if (!productionRecords.ContainsKey(key))
            {
                productionRecords[key] = 0;
            }
            productionRecords[key] += 5;
            
            zUDebug.Log($"[生产记录] 添加: {key}, 当前数量={productionRecords[key]}");
        }
        
        // 实现添加逻辑，例如发送命令增加单位生产
        if (item.ItemData != null && game != null)
        {
            for (int i = 0; i < 5; i++)
            {
                SendProduceCommand(item.ItemData.FactoryEntityId, item.ItemData.UnitType, 1);
            }
        }
    }

    
    /// <summary>
    /// 列表项减少回调
    /// </summary>
    private void OnListItemSub(ProducerListItem item)
    {
        zUDebug.Log($"请求减少生产项: {item.ItemData?.Name}");
        
        // 记录减少操作
        if (item.ItemData != null && game != null)
        {
            string key = $"{item.ItemData.UnitType}";
            if (!productionRecords.ContainsKey(key))
            {
                productionRecords[key] = 0;
            }
            productionRecords[key]--;
            
            zUDebug.Log($"[生产记录] 减少: {key}, 当前数量={productionRecords[key]}");
        }
        
        // 实现减少逻辑，例如发送命令减少单位生产
        if (item.ItemData != null && game != null)
        {
            SendProduceCommand(item.ItemData.FactoryEntityId, item.ItemData.UnitType, -1);
        }
    }
 
    
    /// <summary>
    /// 发送生产命令
    /// </summary>
    /// <param name="factoryEntityId">工厂实体ID</param>
    /// <param name="unitType">单位类型</param>
    /// <param name="changeValue">变化值</param>
    private void SendProduceCommand(int factoryEntityId, UnitType unitType, int changeValue)
    {
        if (game == null || factoryEntityId == -1)
            return;

        var produceCommand = new ZLockstep.Sync.Command.Commands.ProduceCommand(
            campId: 0,
            entityId: factoryEntityId,
            unitType: unitType,
            changeValue: changeValue
        )
        {
            Source = ZLockstep.Sync.Command.CommandSource.Local
        };

        game.SubmitCommand(produceCommand);
        zUDebug.Log($"[Test] 发送生产命令: 工厂{factoryEntityId} 单位类型{unitType} 变化值{changeValue}");
    }
    
    /// <summary>
    /// 销毁时调用，清理事件
    /// </summary>
    public void Destroy()
    {
        closeBtn?.onClick.RemoveListener(OnClose);
        OnCloseClick = null;
        
        // 停止定时刷新
        StopAutoRefresh();
        
        // 清理列表
        ClearList();
        
        // 清空生产记录
        productionRecords.Clear();
    }
}