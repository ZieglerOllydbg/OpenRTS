using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Demo列表项 - 继承自BaseListItem
/// </summary>
public class DemoListItem : BaseListItem
{
    private Text nameText;
    private Button selectBtn;
    
    /// <summary>
    /// 绑定的数据
    /// </summary>
    public DemoItemData ItemData { get; private set; }
    
    /// <summary>
    /// 选中回调（可选，用于通知父面板）
    /// </summary>
    public Action<DemoListItem> OnItemSelect;
    
    public DemoListItem(GameObject __target) : base(__target)
    {
        // 获取子组件引用
        nameText = __target.transform.Find("NameText")?.GetComponent<Text>();
        selectBtn = __target.transform.Find("SelectBtn")?.GetComponent<Button>();
        selectBtn?.onClick.AddListener(OnSelectClick);
    }
    
    /// <summary>
    /// 绑定数据并刷新UI
    /// </summary>
    public void SetData(DemoItemData data)
    {
        ItemData = data;
        UpdateDataAsync();
    }
    
    /// <summary>
    /// 刷新数据显示
    /// </summary>
    public override void UpdateDataAsync()
    {
        if (nameText != null && ItemData != null)
            nameText.text = ItemData.Name;
    }
    
    private void OnSelectClick()
    {
        // 触发选中回调
        OnItemSelect?.Invoke(this);
    }
    
    /// <summary>
    /// 销毁时调用
    /// </summary>
    public override void Destroy()
    {
        selectBtn?.onClick.RemoveListener(OnSelectClick);
        OnItemSelect = null;
        base.Destroy();
    }
}

