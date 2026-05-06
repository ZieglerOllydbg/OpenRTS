using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.Threading.Tasks;

/// <summary>
/// 生产面板列表项 - 继承自 BaseListItem
/// </summary>
public class ProducerListItem : BaseListItem
{
    private Image iconImage;
    private TMP_Text producerNameText;
    private TMP_Text factoryNameText;
    private TMP_Text descriptionText;
    private Button addBtn;
    private Button subBtn;
    private Button addMultiBtn;
    
    /// <summary>
    /// 绑定的数据
    /// </summary>
    public ProducerItemData ItemData { get; private set; }
    
    /// <summary>
    /// 添加回调（用于通知父面板增加单位生产）
    /// </summary>
    public Action<ProducerListItem> OnItemAdd;
    
    /// <summary>
    /// 减少回调（用于通知父面板减少单位生产）
    /// </summary>
    public Action<ProducerListItem> OnItemSub;
    
    /// <summary>
    /// 批量添加回调（一次生产 5 个，用于通知父面板批量增加单位生产）
    /// </summary>
    public Action<ProducerListItem> OnItemAddMulti;
    
    public ProducerListItem(GameObject _target) : base(_target)
    {
        // 获取子组件引用
        iconImage = _target.transform.Find("Image")?.GetComponent<Image>();
        producerNameText = _target.transform.Find("ProducerName")?.GetComponent<TMP_Text>();
        factoryNameText = _target.transform.Find("FactoryName")?.GetComponent<TMP_Text>();
        descriptionText = _target.transform.Find("Description")?.GetComponent<TMP_Text>();

        addBtn = _target.transform.Find("AddBtn")?.GetComponent<Button>();
        subBtn = _target.transform.Find("SubBtn")?.GetComponent<Button>();
        addMultiBtn = _target.transform.Find("AddMultiBtn")?.GetComponent<Button>();
        
        addBtn?.onClick.AddListener(OnAddClick);
        subBtn?.onClick.AddListener(OnSubClick);
        addMultiBtn?.onClick.AddListener(OnAddMultiClick);
    }
    
    /// <summary>
    /// 绑定数据并刷新UI
    /// </summary>
    public void SetData(ProducerItemData data)
    {
        ItemData = data;
        UpdateDataAsync();
    }
    
    /// <summary>
    /// 刷新数据显示
    /// </summary>
    public override async void UpdateDataAsync()
    {
        if (producerNameText != null && ItemData != null)
        {
            producerNameText.text = ItemData.Name;
            factoryNameText.text = ItemData.BelongFactory;
            descriptionText.text = ItemData.Description;

            ConfUnit confUnit = ConfigManager.Get<ConfUnit>((int)ItemData.UnitType);
            if (confUnit != null)
            {
                iconImage.sprite = await AssetManager.GetSpriteAsync(confUnit.Icon);
            }
        }
    }
    
    private void OnAddClick()
    {
        // 触发添加回调
        OnItemAdd?.Invoke(this);
    }
    
    private void OnSubClick()
    {
        // 触发减少回调
        OnItemSub?.Invoke(this);
    }
    
    private void OnAddMultiClick()
    {
        // 触发批量添加回调（一次生产 5 个）
        OnItemAddMulti?.Invoke(this);
    }
    
    /// <summary>
    /// 销毁时调用
    /// </summary>
    public override void Destroy()
    {
        addBtn?.onClick.RemoveListener(OnAddClick);
        subBtn?.onClick.RemoveListener(OnSubClick);
        addMultiBtn?.onClick.RemoveListener(OnAddMultiClick);
        OnItemAdd = null;
        OnItemSub = null;
        OnItemAddMulti = null;
        base.Destroy();
    }
}
