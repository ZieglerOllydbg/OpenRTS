using UnityEngine;
using UnityEngine.UI;
using ZFrame;
using TMPro;
using System.Collections.Generic;
using ZLockstep.Simulation.ECS;
using ZLib;

/// <summary>
/// 通用确认对话框组件（非 MonoBehaviour 版本）
/// 可复用于各种需要用户确认的场景
/// </summary>
public class ConfirmDialogComponent
{
    // UI 组件引用
    private Button confirmOkBtn;
    private Button confirmCancelBtn;
    private TMP_Text confirmText;
    private GameObject dialogPanel;
    
    // 回调委托
    private System.Action _currentConfirmCallback;
    private System.Action _currentCancelCallback;
    
    /// <summary>
    /// 初始化组件，获取必要的 UI 引用
    /// </summary>
    /// <param name="parent">确认对话框父对象 Transform</param>
    public ConfirmDialogComponent(Transform parent)
    {
        dialogPanel = parent.gameObject;
        
        confirmOkBtn = parent.Find("OK")?.GetComponent<Button>();
        confirmCancelBtn = parent.Find("Cancel")?.GetComponent<Button>();
        confirmText = parent.Find("Text")?.GetComponent<TMP_Text>();
        
        // 注册按钮事件
        RegisterEvents();
        
        // 初始隐藏
        Hide();
    }
    
    /// <summary>
    /// 注册按钮点击事件
    /// </summary>
    private void RegisterEvents()
    {
        if (confirmOkBtn != null)
        {
            confirmOkBtn.onClick.AddListener(OnConfirmClick);
        }
        
        if (confirmCancelBtn != null)
        {
            confirmCancelBtn.onClick.AddListener(OnCancelClick);
        }
    }
    
    /// <summary>
    /// 移除按钮点击事件
    /// </summary>
    public void UnregisterEvents()
    {
        if (confirmOkBtn != null)
        {
            confirmOkBtn.onClick.RemoveListener(OnConfirmClick);
        }
        
        if (confirmCancelBtn != null)
        {
            confirmCancelBtn.onClick.RemoveListener(OnCancelClick);
        }
    }
    
    /// <summary>
    /// 显示确认对话框
    /// </summary>
    /// <param name="onConfirm">确认时的回调</param>
    /// <param name="onCancel">取消时的回调（可选）</param>
    /// <param name="message">显示的消息内容（可选）</param>
    public void Show(System.Action onConfirm, System.Action onCancel = null, string message = null)
    {
        // 保存回调
        _currentConfirmCallback = onConfirm;
        _currentCancelCallback = onCancel;
        
        // 设置消息文本（如果有）
        if (!string.IsNullOrEmpty(message) && confirmText != null)
        {
            confirmText.text = message;
        }
        
        // 显示面板
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// 隐藏确认对话框
    /// </summary>
    public void Hide()
    {
        ClearCallbacks();
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 确认按钮点击处理
    /// </summary>
    private void OnConfirmClick()
    {
        _currentConfirmCallback?.Invoke();
        Hide();
    }
    
    /// <summary>
    /// 取消按钮点击处理
    /// </summary>
    private void OnCancelClick()
    {
        _currentCancelCallback?.Invoke();
        Hide();
    }
    
    /// <summary>
    /// 清理回调委托
    /// </summary>
    private void ClearCallbacks()
    {
        _currentConfirmCallback = null;
        _currentCancelCallback = null;
    }
    
    /// <summary>
    /// 设置对话框文本内容
    /// </summary>
    /// <param name="message">消息内容</param>
    public void SetMessage(string message)
    {
        if (confirmText != null)
        {
            confirmText.text = message;
        }
    }
}