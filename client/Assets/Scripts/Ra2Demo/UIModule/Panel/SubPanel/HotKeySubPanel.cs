using UnityEngine;
using TMPro;
using ZFrame;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 热键快捷栏子面板控制器 - 封装热键快捷栏的UI逻辑和数据
/// </summary>
public class HotKeySubPanel
{
    private GameObject root;
    private LongPressButton[] hotButtons; // 对应 Hot1, Hot2, Hot3, Hot4 的按钮组件

    /// <summary>
    /// 存储每个热键对应的单位编队
    /// </summary>
    private List<int>[] hotkeyGroups;

    /// <summary>
    /// Ra2Demo实例引用，用于访问选中单位列表
    /// </summary>
    private Ra2Demo ra2Demo;

    /// <summary>
    /// 绑定的数据
    /// </summary>
    public HotKeyPanelData Data { get; private set; }

    public HotKeySubPanel(Transform parent)
    {
        root = parent.Find("HotKeyGroup")?.gameObject;
        if (root == null) return;

        // 获取Ra2Demo实例
        ra2Demo = Object.FindObjectOfType<Ra2Demo>();
        
        // 初始化编队存储数组
        hotkeyGroups = new List<int>[4];
        for (int i = 0; i < hotkeyGroups.Length; i++) 
        {
            hotkeyGroups[i] = new List<int>();
        }

        // 获取四个热键按钮组件
        hotButtons = new LongPressButton[4];
        for (int i = 0; i < 4; i++)
        {
            string keyName = $"Hot{i + 1}";
            Transform child = root.transform.Find(keyName);
            if (child != null)
            {
                hotButtons[i] = child.GetComponent<LongPressButton>();
            }
        }

        // 初始化UI
        UpdateUI();

        // 添加点击和长按事件监听
        for (int i = 0; i < hotButtons.Length; i++)
        {
            if (hotButtons[i] != null)
            {
                int index = i; // 避免闭包问题
                hotButtons[i].onClick.AddListener(() => OnHotKeyClicked(index));
                hotButtons[i].onLongPress.AddListener(() => OnHotKeyLongPressed(index));
            }
        }

        // 清空编队，重置按钮颜色
        for (int i = 0; i < hotkeyGroups.Length; i++)
        {
            hotkeyGroups[i].Clear();
            RestoreButtonColor(i);
        }
    }

    /// <summary>
    /// 热键点击事件处理
    /// </summary>
    /// <param name="index">热键索引</param>
    private void OnHotKeyClicked(int index)
    {
        zUDebug.Log($"热键 {index + 1} 被点击了！");
        // 选择该编队中的单位
        if (ra2Demo != null && index >= 0 && index < hotkeyGroups.Length)
        {
            // 编组中单位为空，提示
            if (hotkeyGroups[index].Count == 0)
            {
                Frame.DispatchEvent(new ShowMessageEvent($"当前编组[{index + 1}]没有单位！"));
                RestoreButtonColor(index);
                return;
            }

            // 清除当前选择
            ra2Demo.ClearAllOutlines();
            
            // 设置新的选择
            var group = hotkeyGroups[index];
            // 创建一个临时列表来存储有效的实体ID
            var validEntities = new List<int>();
            
            foreach (int entityId in group)
            {
                // 尝试为实体启用轮廓，如果成功则添加到有效列表中
                if (ra2Demo.EnableOutlineForEntity(entityId))
                {
                    validEntities.Add(entityId);
                }
            }
            
            // 更新group为只包含有效实体
            group.Clear();
            group.AddRange(validEntities);
            
            // 更新Ra2Demo中的选中单位列表
            ra2Demo.SetSelectedEntityIds(group);
            
            // 如果group为空，恢复按钮颜色
            if (group.Count == 0)
            {
                RestoreButtonColor(index);
            }
        }
    }
    
    /// <summary>
    /// 热键长按事件处理
    /// </summary>
    /// <param name="index">热键索引</param>
    private void OnHotKeyLongPressed(int index)
    {
        zUDebug.Log($"热键 {index + 1} 被长按了！");
        
        // 将当前选中的单位保存到该编队
        if (ra2Demo != null && index >= 0 && index < hotkeyGroups.Length)
        {
            hotkeyGroups[index].Clear();
            hotkeyGroups[index].AddRange(ra2Demo.GetSelectedEntityIds());
            
            // 根据是否有选中单位来设置按钮颜色
            if (ra2Demo.GetSelectedEntityIds().Count > 0)
            {
                // 有选中单位，按钮变为绿色
                SetButtonColor(index, Color.green);
            }
            else
            {
                // 没有选中单位，恢复按钮原始颜色
                RestoreButtonColor(index);
                
                Frame.DispatchEvent(new ShowMessageEvent("请选择单位！"));
            }
        }
    }

    /// <summary>
    /// 设置数据并显示
    /// </summary>
    public void Show(HotKeyPanelData data)
    {
        Data = data;
        UpdateUI();
        // SetActive(true);
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

        for (int i = 0; i < 4; i++)
        {
            if (hotButtons[i] != null && i < Data.HotKeys.Length)
            {
                hotButtons[i].GetComponentInChildren<TMP_Text>().text = Data.HotKeys[i];
            }
        }
    }

    /// <summary>
    /// 设置显示状态
    /// </summary>
    public void SetActive(bool active)
    {
        root?.SetActive(active);
    }

    /// <summary>
    /// 设置按钮颜色
    /// </summary>
    /// <param name="index">按钮索引</param>
    /// <param name="color">要设置的颜色</param>
    private void SetButtonColor(int index, Color color)
    {
        if (index >= 0 && index < hotButtons.Length && hotButtons[index] != null)
        {
            Image buttonImage = hotButtons[index].GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = color;
            }
        }
    }

    /// <summary>
    /// 恢复按钮颜色
    /// </summary>
    /// <param name="index">按钮索引</param>
    private void RestoreButtonColor(int index)
    {
        if (index >= 0 && index < hotButtons.Length && hotButtons[index] != null)
        {
            Image buttonImage = hotButtons[index].GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = Color.white;
            }
        }
    }

    /// <summary>
    /// 销毁时调用，清理事件
    /// </summary>
    public void Destroy()
    {
        // 移除所有按钮的点击和长按事件监听
        for (int i = 0; i < hotButtons.Length; i++)
        {
            if (hotButtons[i] != null)
            {
                hotButtons[i].onClick.RemoveAllListeners();
                hotButtons[i].onLongPress.RemoveAllListeners();
            }
        }
    }
}