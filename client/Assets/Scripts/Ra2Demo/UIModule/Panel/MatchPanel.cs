using System.Collections.Generic;
using Game.Examples;
using Game.RA2.Client;
using Newtonsoft.Json.Linq;
using PostHogUnity;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using ZFrame;
using ZLockstep.RVO;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Sync;
using zUnity;

/// <summary>
/// Demo 面板 - 示例如何在业务层配置路径、名称和深度类型
/// </summary>
[UIModel(
    panelID = "MatchPanel",
    panelPath = "MatchPanel",
    panelName = "Match 面板",
    panelUIDepthType = ClientUIDepthTypeID.GameMid
)]
public class MatchPanel : BasePanel
{
    /// <summary>
    /// 编辑器偏好设置键，用于让 GM 工具面板控制 Solo 按钮是否直接启动本地单机对局。
    /// </summary>
    public const string SoloButtonStartsStandalonePrefsKey = "SimpleRTS.MatchPanel.SoloButtonStartsStandalone";

    // 1. 声明 UI 组件引用
    private Button _soloButton;
    private Button _duoButton;
    private Button _quadButton;
    private Button _replayButton;
    private Button _cancelButton;
    private Toggle _useLocalNetToggle;
    private Transform _matchGroup;
    private Transform _matchingGroup;
    private TMP_Text _nameText;
    private RawImage _headImg;
    private TMP_Text _versionText;
    private ConfirmDialogComponent _confirmDialog;
    
    // 数据缓存字段
    private string _cachedNickname;
    private string _cachedAvatarUrl;

    // 回放子面板控制器
    private MatchReplaySubPanel _replaySubPanelController;
    
    public MatchPanel(IDispathMessage _processor, UIModelData _modelData, DisableNew _disableNew) 
        : base(_processor, _modelData, _disableNew)
    {
    }

    // 2. 在面板显示前获取组件引用
    protected override void OnBecameVisible()
    {
        base.OnBecameVisible();
        
        // 获取 Match 按钮组和 Matching 组容器
        _matchGroup = PanelObject.transform.Find("Match");
        _matchingGroup = PanelObject.transform.Find("Matching");
        _nameText = PanelObject.transform.Find("Match/Nickname")?.GetComponent<TMP_Text>();
        _headImg = PanelObject.transform.Find("Match/HeadImg")?.GetComponent<RawImage>();
        
        // 从 PanelObject 获取按钮组件
        _soloButton = PanelObject.transform.Find("Match/SOLO")?.GetComponent<Button>();
        _duoButton = PanelObject.transform.Find("Match/DUO")?.GetComponent<Button>();
        _quadButton = PanelObject.transform.Find("Match/QUAD")?.GetComponent<Button>();
        _replayButton = PanelObject.transform.Find("Match/REPLAY")?.GetComponent<Button>();
        _cancelButton = PanelObject.transform.Find("Matching/CancelBtn")?.GetComponent<Button>();
        
        // 获取 UseLocalNet Toggle 组件
        _useLocalNetToggle = PanelObject.transform.Find("Match/UseLocalNet")?.GetComponent<Toggle>();
        Transform confirmTransform = PanelObject.transform.Find("Confirm");
        if (confirmTransform != null)
        {
            _confirmDialog = new ConfirmDialogComponent(confirmTransform);
        }

        // 获取 Version 文本组件并设置版本号
        _versionText = PanelObject.transform.Find("Version")?.GetComponent<TMP_Text>();
        if (_versionText != null)
        {
            _versionText.text = $"版本号：{Application.version}";
        }

        // 加载保存的 UseLocalNet 选项
        LoadUseLocalNetOption();

        _replaySubPanelController?.Destroy();
        _replaySubPanelController = new MatchReplaySubPanel(PanelObject.transform, GetIsLocalNet);
        _replaySubPanelController.Hide();

        // 显示 Match 按钮组
        _matchGroup.gameObject.SetActive(true);
        // 隐藏匹配中界面
        _matchingGroup.gameObject.SetActive(false);
        
        // 恢复缓存的玩家信息
        RestorePlayerInfo();

        TryShowReconnectDialog();
    }

    // 设置玩家名称显示
    public void SetPlayerName(string name)
    {
        // 缓存玩家名称
        _cachedNickname = name;
        
        if (_nameText != null)
        {
            _nameText.text = name;
            zUDebug.Log($"[MatchPanel] 玩家名称已设置为：{name}");
        }
        else
        {
            zUDebug.LogWarning("[MatchPanel] _nameText 组件未找到，无法设置玩家名称");
        }
    }

    // 设置玩家头像显示
    public void SetHeadImg(string avatarUrl)
    {
        // 缓存头像 URL
        _cachedAvatarUrl = avatarUrl;
        
        if (_headImg != null)
        {
            LoadAvatarTexture(avatarUrl);
            zUDebug.Log($"[MatchPanel] 开始加载玩家头像：{avatarUrl}");
        }
        else
        {
            zUDebug.LogWarning("[MatchPanel] _headImg 组件未找到，无法设置玩家头像");
        }
    }
    
    // 恢复缓存的玩家信息
    private void RestorePlayerInfo()
    {
        // 恢复玩家名称
        if (!string.IsNullOrEmpty(_cachedNickname) && _nameText != null)
        {
            _nameText.text = _cachedNickname;
            zUDebug.Log($"[MatchPanel] 恢复玩家名称：{_cachedNickname}");
        }
        
        // 恢复头像
        if (!string.IsNullOrEmpty(_cachedAvatarUrl) && _headImg != null)
        {
            LoadAvatarTexture(_cachedAvatarUrl);
            zUDebug.Log($"[MatchPanel] 恢复玩家头像：{_cachedAvatarUrl}");
        }
    }

    private void LoadAvatarTexture(string url)
    {
        var www = UnityWebRequestTexture.GetTexture(url);
        
        // 将 www 作为参数传入，避免闭包捕获（更安全）
        var operation = www.SendWebRequest();
        operation.completed += (AsyncOperation op) =>
        {
            // 使用 op.webRequest 来获取原始请求（Unity 2020+ 支持）
            // 或者继续使用 www，但要确保它没被提前 GC
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                zUDebug.LogError($"[MatchPanel] 加载头像失败：{www.error}");
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                if (texture != null && _headImg != null)
                {
                    _headImg.texture = texture;
                    zUDebug.Log($"[MatchPanel] 头像加载成功，尺寸：{texture.width}x{texture.height}");
                }
            }

            www.Dispose(); // 安全释放
        };
    }

    // 3. 在 AddEvent 中添加按钮事件（面板显示时自动调用）
    protected override void AddEvent()
    {
        base.AddEvent();
        
        if (_soloButton != null)
        {
            _soloButton.onClick.AddListener(OnSoloButtonClick);
        }

        if (_duoButton != null)
        {
            _duoButton.onClick.AddListener(OnDuoButtonClick);
        }

        if (_quadButton != null)
        {
            _quadButton.onClick.AddListener(OnQuadButtonClick);
        }
        
        if (_replayButton != null)
        {
            _replayButton.onClick.AddListener(OnReplayButtonClick);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.AddListener(OnCancelButtonClick);
        }
        
        // 为 UseLocalNet Toggle 添加值变化监听
        if (_useLocalNetToggle != null)
        {
            _useLocalNetToggle.onValueChanged.AddListener(OnUseLocalNetValueChanged);
        }
        
    }

    // 4. 在 RemoveEvent 中移除按钮事件（面板关闭时自动调用）
    protected override void RemoveEvent()
    {
        base.RemoveEvent();
        
        if (_soloButton != null)
        {
            _soloButton.onClick.RemoveListener(OnSoloButtonClick);
        }

        if (_duoButton != null)
        {
            _duoButton.onClick.RemoveListener(OnDuoButtonClick);
        }

        if (_quadButton != null)
        {
            _quadButton.onClick.RemoveListener(OnQuadButtonClick);
        }
        
        if (_replayButton != null)
        {
            _replayButton.onClick.RemoveListener(OnReplayButtonClick);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveListener(OnCancelButtonClick);
        }
        
        // 移除 UseLocalNet Toggle 的值变化监听
        if (_useLocalNetToggle != null)
        {
            _useLocalNetToggle.onValueChanged.RemoveListener(OnUseLocalNetValueChanged);
        }
    }

    /// <summary>
    /// 面板隐藏时收尾回放子面板状态与动态列表，避免重复打开时残留旧节点与按钮监听。
    /// </summary>
    protected override void OnBecameInvisible()
    {
        base.OnBecameInvisible();
        _replaySubPanelController?.Hide();
        _replaySubPanelController?.Destroy();
        _replaySubPanelController = null;
        _confirmDialog?.UnregisterEvents();
        _confirmDialog = null;
    }

    /// <summary>
    /// 处理 Solo 按钮点击，根据编辑器 GM 工具开关选择本地单机启动或联网 Solo 匹配流程。
    /// </summary>
    private void OnSoloButtonClick()
    {
        UISound.Instance.PlayClick();
        PostHog.Capture("click_solo_match");

        if (ShouldStartStandaloneGameFromSoloButton())
        {
            StartStandaloneGame();
        }
        else
        {
            NetworkManager.Instance.ConnectToServer(RoomType.SOLO, GetIsLocalNet());
        }

        HideButtons();
    }

    /// <summary>
    /// 判断 Solo 按钮当前是否应走编辑器单机启动流程，非编辑器环境固定使用联网 Solo 流程。
    /// </summary>
    /// <returns>需要直接启动本地单机对局时返回 true，否则返回 false。</returns>
    private static bool ShouldStartStandaloneGameFromSoloButton()
    {
#if UNITY_EDITOR
        return EditorPrefs.GetBool(SoloButtonStartsStandalonePrefsKey, true);
#else
        return true;
#endif
    }

    /// <summary>
    /// 处理 Duo 按钮点击，提交双人匹配请求并切换到匹配中界面。
    /// </summary>
    private void OnDuoButtonClick()
    {
        UISound.Instance.PlayClick();
        PostHog.Capture("click_duo_match");

        NetworkManager.Instance.ConnectToServer(RoomType.DUO, GetIsLocalNet());
        HideButtons();
    }

    /// <summary>
    /// 创建并初始化本地单人对局，负责配置单机模式、创建战斗世界、启动录像与确定性探针，并把镜头移动到己方基地。
    /// </summary>
    private void StartStandaloneGame()
    {
        // 重置重置全局随机状态快照
        zRandom.ResetGlobalDeterminismStateRaw(0L);

        // 可以在这里添加匹配成功的处理逻辑
        Ra2Demo ra2Demo = UnityEngine.Object.FindObjectOfType<Ra2Demo>();
        // 创建BattleGame实例
        ra2Demo.Mode = GameMode.Standalone;
        ra2Demo.SetBattleGame(new BattleGame(ra2Demo.Mode, 20, 0, 1));

        ra2Demo.GetBattleGame().Init();

        // 初始化Unity视图层
        ra2Demo.InitializeUnityView();

        // 全局数据
        GlobalInfoComponent globalInfoComponent = new(1);
        ra2Demo.GetBattleGame().World.ComponentManager.AddGlobalComponent(globalInfoComponent);
        // 创建世界
        ra2Demo.GetBattleGame().CreateWorldByConfig();

        // 启动命令记录
        StartCommandRecording(ra2Demo.GetBattleGame());

        // 启动确定性探针
        StartDeterminismProbe(ra2Demo.GetBattleGame(), "standalone");

        ra2Demo.MoveCameraToOurFactory();

        Frame.DispatchEvent(new SoloGameStartEvent());
    }

    // Replay按钮点击处理方法
    private void OnReplayButtonClick()
    {
        UISound.Instance.PlayClick();
        // Capture a simple event
        PostHog.Capture("click_replay");
        _replaySubPanelController?.ShowAndRefresh();
    }

    /// <summary>
    /// 检查本地重连会话，存在未结束对局时弹出重连确认框。
    /// </summary>
    private void TryShowReconnectDialog()
    {
        if (_confirmDialog == null || !NetworkManager.Instance.HasSavedReconnectSession())
        {
            return;
        }

        _confirmDialog.Show(
            onConfirm: ConfirmReconnect,
            onCancel: CancelReconnect,
            message: "检测到未结束的对局，是否重连？"
        );
    }

    /// <summary>
    /// 确认重连时尝试恢复本地保存的房间会话。
    /// </summary>
    private void ConfirmReconnect()
    {
        UISound.Instance.PlayClick();
        PostHog.Capture("click_reconnect");

        if (NetworkManager.Instance.TryReconnectFromSavedSession())
        {
            HideButtons();
            return;
        }

        NetworkManager.Instance.ClearReconnectSession();
        zUDebug.LogWarning("[MatchPanel] 本地重连会话不可用，已清理重连入口。");
    }

    /// <summary>
    /// 取消重连时放弃本地保存的房间会话并停留在匹配页。
    /// </summary>
    private void CancelReconnect()
    {
        UISound.Instance.PlayClick();
        NetworkManager.Instance.ClearReconnectSession();
        zUDebug.LogInfo("[MatchPanel] 玩家放弃重连，已清理本地重连会话。");
    }

    private bool GetIsLocalNet()
    {
        return _useLocalNetToggle?.isOn ?? false;
    }



    private void OnQuadButtonClick()
    {
        bool isLocalNet = GetIsLocalNet();
        NetworkManager.Instance.ConnectToServer(RoomType.QUAD, isLocalNet);
        HideButtons();
    }

    private void OnCancelButtonClick()
    {
        UISound.Instance.PlayClick();
        NetworkManager.Instance.CancelMatchAndCloseCurrentWebSocket();
        ShowMatchButtons();
    }

    // UseLocalNet Toggle值变化处理方法
    private void OnUseLocalNetValueChanged(bool value)
    {
        zUDebug.Log($"UseLocalNet状态变化为: {value}");
        SaveUseLocalNetOption(value);
    }

    // 保存UseLocalNet选项
    private void SaveUseLocalNetOption(bool value)
    {
        PlayerPrefs.SetInt("UseLocalNet", value ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    // 加载保存的UseLocalNet选项
    private void LoadUseLocalNetOption()
    {
        if (_useLocalNetToggle != null)
        {
            bool savedValue = PlayerPrefs.GetInt("UseLocalNet", 0) == 1;
            _useLocalNetToggle.isOn = savedValue;
        }
    }

    // 隐藏所有按钮并显示匹配中界面
    private void HideButtons()
    {
        if (_matchGroup != null)
        {
            _matchGroup.gameObject.SetActive(false);
        }
        
        if (_matchingGroup != null)
        {
            _matchingGroup.gameObject.SetActive(true);
        }
    }

    private void ShowMatchButtons()
    {
        if (_matchGroup != null)
        {
            _matchGroup.gameObject.SetActive(true);
        }

        if (_matchingGroup != null)
        {
            _matchingGroup.gameObject.SetActive(false);
        }
    }

    public void HideMatchingGroup()
    {
        if (_matchingGroup != null)
        {
            _matchingGroup.gameObject.SetActive(false);
        }
    }

    private void StartDeterminismProbe(BattleGame battleGame, string tag)
    {
        var probe = battleGame?.World?.DeterminismProbe;
        if (probe == null)
        {
            return;
        }

        string fileName = $"determinism_probe_{tag}_{System.DateTime.Now:yyyyMMdd_HHmmss}.jsonl";
        string path = System.IO.Path.Combine(Application.persistentDataPath, fileName);
        probe.Start(path, truncate: true);
        zUDebug.Log($"[MatchPanel] DeterminismProbe 已启动: {path}");
    }

    /// <summary>
    /// 启动命令记录，用于回放功能
    /// </summary>
    /// <param name="battleGame">战斗游戏实例</param>
    private void StartCommandRecording(BattleGame battleGame)
    {
        if (battleGame == null || battleGame.World == null)
        {
            return;
        }

        string fileName = $"replay_1_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
        battleGame.World.CommandManager.StartRecording(fileName);
        zUDebug.Log($"[MatchPanel] 命令记录已启动: {fileName}");
    }
    
}