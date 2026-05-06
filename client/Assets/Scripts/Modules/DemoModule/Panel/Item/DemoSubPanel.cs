using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 子面板控制器 - 封装子面板的UI逻辑和数据
/// </summary>
public class DemoSubPanel
{
    private GameObject root;
    private Text titleText;
    private Text contentText;
    private Button closeBtn;
    
    /// <summary>
    /// 绑定的数据
    /// </summary>
    public SubPanelData Data { get; private set; }
    
    /// <summary>
    /// 关闭按钮点击回调（可选，用于通知父面板）
    /// </summary>
    public Action OnCloseClick;
    
    public DemoSubPanel(Transform parent)
    {
        root = parent.Find("SubPanel")?.gameObject;
        if (root == null) return;
        
        // 获取组件引用
        titleText = root.transform.Find("TitleText")?.GetComponent<Text>();
        contentText = root.transform.Find("ContentText")?.GetComponent<Text>();
        closeBtn = root.transform.Find("CloseBtn")?.GetComponent<Button>();
        
        closeBtn?.onClick.AddListener(OnClose);
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
    
    /// <summary>
    /// 刷新UI显示
    /// </summary>
    public void UpdateUI()
    {
        if (Data == null) return;
        
        if (titleText != null) titleText.text = Data.Title;
        if (contentText != null) contentText.text = Data.Content;
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
    /// 销毁时调用，清理事件
    /// </summary>
    public void Destroy()
    {
        closeBtn?.onClick.RemoveListener(OnClose);
        OnCloseClick = null;
    }
}

