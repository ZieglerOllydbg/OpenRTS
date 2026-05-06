using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using ZFrame;

/// <summary>
/// 设置面板的子面板控制器 - 封装设置面板的 UI 逻辑和数据
/// </summary>
public class SettingSubPanel
{
    private GameObject root;
    private TMP_Text titleText;
    private Button closeBtn;
    
    /// <summary>
    /// 绑定的数据
    /// </summary>
    public SubPanelData Data { get; private set; }
    
    /// <summary>
    /// 关闭按钮点击回调（可选，用于通知父面板）
    /// </summary>
    public System.Action OnCloseClick;
    
    // 直接引用 UI 元素（不使用模板）
    private Slider musicSlider;
    // 背景音乐音量文本显示
    private TMP_Text musicValueText;
    // 背景音乐 AudioSource 引用
    private AudioSource bgmAudioSource;
    // 血条永久显示 Toggle
    private Toggle healthBarToggle;
    // 单位统计按钮
    private Button unitStatsBtn;
    // 模拟按钮
    private Button simulationBtn;
    // 寻路按钮
    private Button pathfindingBtn;
    // 关闭调试按钮
    private Button closeDebugBtn;

    public SettingSubPanel(Transform parent)
    {
        root = parent.Find("Setting")?.gameObject;
        if (root == null) return;
        
        // 获取组件引用
        titleText = root.transform.Find("Title")?.GetComponent<TMP_Text>();
        closeBtn = root.transform.Find("CloseBtn")?.GetComponent<Button>();
        
        closeBtn?.onClick.AddListener(OnClose);
        
        // 获取音乐 Slider
        musicSlider = root.transform.Find("Scroll View/Viewport/Content/Music/Slider")?.GetComponent<Slider>();
        
        // 获取音乐音量文本
        musicValueText = root.transform.Find("Scroll View/Viewport/Content/Music/Title")?.GetComponent<TMP_Text>();

        if (musicSlider != null)
        {
            // 加载保存的音乐音量
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
            musicSlider.value = musicVolume; // 转换为 0-100 范围
            zUDebug.Log($"[SettingSubPanel] 背景音乐音量已设置为：{musicVolume * 100:F0}%");
            
            // 更新文本显示
            UpdateMusicValueText(musicVolume);
            
            // 添加值变化监听
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        // 获取 BGM 的 AudioSource 组件
        GameObject bgmObject = GameObject.Find("BGM");
        if (bgmObject != null)
        {
            bgmAudioSource = bgmObject.GetComponent<AudioSource>();
            if (bgmAudioSource != null)
            {
                // 获取保存的背景音乐音量
                if (!bgmAudioSource.isPlaying)
                {
                    bgmAudioSource.volume = musicSlider.value;
                    bgmAudioSource.Play();
                }
            }
        }
        
        // 获取血条永久显示 Toggle
        healthBarToggle = root.transform.Find("Scroll View/Viewport/Content/HealthBar/Toggle")?.GetComponent<Toggle>();
        
        if (healthBarToggle != null)
        {
            // 加载保存的血条显示设置，默认为 false（不永久显示）
            bool showHealthBar = PlayerPrefs.GetInt("ShowHealthBar", 0) == 1;
            healthBarToggle.isOn = showHealthBar;
            
            // 添加值变化监听
            healthBarToggle.onValueChanged.AddListener(OnHealthBarToggleChanged);
        }
        
        // 获取单位统计按钮
        unitStatsBtn = root.transform.Find("Scroll View/Viewport/Content/Debug/UnitStatsBtn")?.GetComponent<Button>();
        
        if (unitStatsBtn != null)
        {
            // 添加点击监听
            unitStatsBtn.onClick.AddListener(OnUnitStatsBtnClick);
        }
        
        // 获取模拟按钮
        simulationBtn = root.transform.Find("Scroll View/Viewport/Content/Debug/SimulationBtn")?.GetComponent<Button>();
        
        if (simulationBtn != null)
        {
            // 添加点击监听
            simulationBtn.onClick.AddListener(OnSimulationBtnClick);
        }
        
        // 获取寻路按钮
        pathfindingBtn = root.transform.Find("Scroll View/Viewport/Content/Debug/PathfindingBtn")?.GetComponent<Button>();
        
        if (pathfindingBtn != null)
        {
            // 添加点击监听
            pathfindingBtn.onClick.AddListener(OnPathfindingBtnClick);
        }
        
        // 获取关闭调试按钮
        closeDebugBtn = root.transform.Find("Scroll View/Viewport/Content/Debug/CloseBtn")?.GetComponent<Button>();
        
        if (closeDebugBtn != null)
        {
            // 添加点击监听
            closeDebugBtn.onClick.AddListener(OnCloseDebugBtnClick);
        }
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
    /// 刷新 UI 显示
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
    /// 更新音乐音量文本显示
    /// </summary>
    private void UpdateMusicValueText(float value)
    {
        if (musicValueText != null)
        {
            musicValueText.text = $"背景音乐 {value * 100:F0}%";
        }
    }
    
    /// <summary>
    /// 音乐音量变化处理
    /// </summary>
    private void OnMusicVolumeChanged(float value)
    {
        // 保存到 PlayerPrefs
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
        
        // 应用到 AudioSource
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = value;
        }
        
        // 更新文本显示
        UpdateMusicValueText(value);
        
        zUDebug.Log($"[SettingSubPanel] 背景音乐音量已设置为：{value * 100:F0}%");
    }
    
    /// <summary>
    /// 血条永久显示开关变化处理
    /// </summary>
    private void OnHealthBarToggleChanged(bool isOn)
    {
        // 保存到 PlayerPrefs（1 表示开启，0 表示关闭）
        PlayerPrefs.SetInt("ShowHealthBar", isOn ? 1 : 0);
        PlayerPrefs.Save();
        
        zUDebug.Log($"[SettingSubPanel] 血条永久显示已{(isOn ? "开启" : "关闭")}");
        
        // 发送事件通知血量面板更新设置
        Frame.DispatchEvent(new HealthBarSettingChangedEvent(isOn));
    }
    
    /// <summary>
    /// 单位统计按钮点击处理
    /// </summary>
    private void OnUnitStatsBtnClick()
    {
        // 查找 Ra2DemoDebugger 组件并调用 SetDebugType(2)
        Ra2DemoDebugger debugger = Object.FindObjectOfType<Ra2DemoDebugger>();
        if (debugger != null)
        {
            debugger.SetDebugType(2);
            zUDebug.Log("[SettingSubPanel] 已切换到单位统计面板");
        }
    }
    
    /// <summary>
    /// 模拟按钮点击处理
    /// </summary>
    private void OnSimulationBtnClick()
    {
        // 查找 Ra2DemoDebugger 组件并调用 SetDebugType(4)
        Ra2DemoDebugger debugger = Object.FindObjectOfType<Ra2DemoDebugger>();
        if (debugger != null)
        {
            debugger.SetDebugType(4);
            zUDebug.Log("[SettingSubPanel] 已切换到模拟面板");
        }
    }
    
    /// <summary>
    /// 寻路按钮点击处理
    /// </summary>
    private void OnPathfindingBtnClick()
    {
        // 查找 Ra2DemoDebugger 组件并调用 SetDebugType(5)
        Ra2DemoDebugger debugger = Object.FindObjectOfType<Ra2DemoDebugger>();
        if (debugger != null)
        {
            debugger.SetDebugType(5);
            zUDebug.Log("[SettingSubPanel] 已切换到寻路面板");
        }
    }
    
    /// <summary>
    /// 关闭调试按钮点击处理
    /// </summary>
    private void OnCloseDebugBtnClick()
    {
        // 查找 Ra2DemoDebugger 组件并调用 SetDebugType(0)
        Ra2DemoDebugger debugger = Object.FindObjectOfType<Ra2DemoDebugger>();
        if (debugger != null)
        {
            debugger.SetDebugType(0);
            zUDebug.Log("[SettingSubPanel] 已关闭调试界面");
        }
    }
    
    /// <summary>
    /// 销毁时调用，清理事件
    /// </summary>
    public void Destroy()
    {
        closeBtn?.onClick.RemoveListener(OnClose);
        OnCloseClick = null;
        
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        }
        
        if (healthBarToggle != null)
        {
            healthBarToggle.onValueChanged.RemoveListener(OnHealthBarToggleChanged);
        }
        
        if (unitStatsBtn != null)
        {
            unitStatsBtn.onClick.RemoveListener(OnUnitStatsBtnClick);
        }
        
        if (simulationBtn != null)
        {
            simulationBtn.onClick.RemoveListener(OnSimulationBtnClick);
        }
        
        if (pathfindingBtn != null)
        {
            pathfindingBtn.onClick.RemoveListener(OnPathfindingBtnClick);
        }
        
        if (closeDebugBtn != null)
        {
            closeDebugBtn.onClick.RemoveListener(OnCloseDebugBtnClick);
        }
    }
}