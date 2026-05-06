using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZFrame;

/// <summary>
/// Demo面板 - 示例如何在业务层配置路径、名称和深度类型
/// 包含子面板控制和动态列表功能示例
/// </summary>
[UIModel(
    panelID = "DemoPanel",
    panelPath = "DemoPanel",
    panelName = "Demo面板",
    panelUIDepthType = ClientUIDepthTypeID.GameMid,
    adapterType = PanelAdapterType.Default
)]
public class DemoPanel : BasePanel
{
    #region UI组件引用
    
    // 按钮
    private Button myButton;
    
    // 子面板控制器
    private DemoSubPanel subPanel;
    
    // 列表相关（支持ScrollRect）
    private ScrollRect listScrollRect;
    private Transform listContent;      // ScrollRect的Content，列表项放这里
    private GameObject itemTemplate;
    private List<DemoListItem> listItems = new List<DemoListItem>();
    
    #endregion
    
    public DemoPanel(IDispathMessage _processor, UIModelData _modelData, DisableNew _disableNew) 
        : base(_processor, _modelData, _disableNew)
    {
    }

    #region 生命周期方法
    
    // 在面板显示前获取组件引用
    protected override void OnBecameVisible()
    {
        base.OnBecameVisible();
        
        // 获取按钮组件
        myButton = PanelObject.transform.Find("MyButton")?.GetComponent<Button>();
        
        // 初始化子面板
        subPanel = new DemoSubPanel(PanelObject.transform);
        subPanel.OnCloseClick = OnSubPanelClosed;
        
        // 初始化列表（ScrollRect结构）
        // 预期结构: ListScrollView(ScrollRect) -> Viewport -> Content
        var scrollViewObj = PanelObject.transform.Find("ListScrollView");
        if (scrollViewObj != null)
        {
            listScrollRect = scrollViewObj.GetComponent<ScrollRect>();
            listContent = listScrollRect?.content;  // ScrollRect.content 自动指向 Content
        }
        
        // 模板可以放在Content下或者单独放置
        itemTemplate = PanelObject.transform.Find("ItemTemplate")?.gameObject;
        if (itemTemplate == null && listContent != null)
        {
            // 尝试从Content下查找模板
            itemTemplate = listContent.Find("ItemTemplate")?.gameObject;
        }
        itemTemplate?.SetActive(false); // 隐藏模板
    }

    // 添加事件（面板显示时自动调用）
    protected override void AddEvent()
    {
        base.AddEvent();
        
        if (myButton != null)
        {
            myButton.onClick.AddListener(OnMyButtonClick);
        }
    }

    // 移除事件（面板关闭时自动调用）
    protected override void RemoveEvent()
    {
        base.RemoveEvent();
        
        if (myButton != null)
        {
            myButton.onClick.RemoveListener(OnMyButtonClick);
        }
    }

    // 面板关闭前调用
    protected override void OnBecameInvisible()
    {
        base.OnBecameInvisible();
        
        // 清理子面板
        subPanel?.Destroy();
        subPanel = null;
        
        // 清理列表
        ClearList();
    }
    
    #endregion
    
    #region 子面板控制
    
    /// <summary>
    /// 显示子面板并传入数据
    /// </summary>
    public void ShowSubPanel(SubPanelData data)
    {
        subPanel?.Show(data);
    }
    
    /// <summary>
    /// 隐藏子面板
    /// </summary>
    public void HideSubPanel()
    {
        subPanel?.Hide();
    }
    
    /// <summary>
    /// 子面板关闭回调
    /// </summary>
    private void OnSubPanelClosed()
    {
        // 处理子面板关闭后的逻辑
        Debug.Log("子面板已关闭");
    }
    
    #endregion
    
    #region 列表管理
    
    /// <summary>
    /// 刷新列表数据
    /// 注意：Content 节点需要添加 VerticalLayoutGroup 或 HorizontalLayoutGroup 组件来自动排列子项
    /// </summary>
    public void RefreshList(List<DemoItemData> dataList)
    {
        ClearList();
        
        if (itemTemplate == null || listContent == null)
        {
            Debug.LogWarning("列表模板或Content容器未找到");
            return;
        }
        
        // 检查是否有 LayoutGroup，没有的话给出提示
        var layoutGroup = listContent.GetComponent<LayoutGroup>();
        if (layoutGroup == null)
        {
            Debug.LogWarning("Content 上没有 LayoutGroup 组件，列表项可能会重叠。请添加 VerticalLayoutGroup 或 HorizontalLayoutGroup");
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
            
            var item = new DemoListItem(itemGo);
            item.SetData(data);
            item.OnItemSelect = OnListItemSelect;
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
    /// 列表项选中回调
    /// </summary>
    private void OnListItemSelect(DemoListItem item)
    {
        Debug.Log($"选中了列表项: {item.ItemData?.Name}");
        
        // 示例：选中列表项后显示子面板
        if (item.ItemData != null)
        {
            ShowSubPanel(new SubPanelData
            {
                Title = item.ItemData.Name,
                Content = $"这是 {item.ItemData.Name} 的详细内容"
            });
        }
    }
    
    #endregion
    
    #region 按钮事件
    
    /// <summary>
    /// 按钮点击处理方法
    /// </summary>
    private void OnMyButtonClick()
    {
        Debug.Log("按钮被点击了！");
        
        // 示例：点击按钮时刷新列表
        var testData = new List<DemoItemData>
        {
            new DemoItemData { Id = 1, Name = "项目1" },
            new DemoItemData { Id = 2, Name = "项目2" },
            new DemoItemData { Id = 3, Name = "项目3" }
        };
        RefreshList(testData);
    }
    
    #endregion
}
