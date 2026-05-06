using UnityEngine;
using UnityEngine.UI;
using ZFrame;
using TMPro;
using ZLib;
using ZLockstep.View;

/// <summary>
/// 回放面板负责展示回放模式的顶部信息、底部控制按钮、小地图和退出确认逻辑，是回放场景中的主 HUD 面板。
/// </summary>
[UIModel(
    panelID = "ReplayPanel",
    panelPath = "ReplayPanel",
    panelName = "回放面板",
    panelUIDepthType = ClientUIDepthTypeID.GameTop
)]
public class ReplayPanel : BasePanel
{
    // 经济显示文本
    private TMP_Text moneyText;
    private TMP_Text powerText;
    private TMP_Text speedText;
    private TMP_Text frameText;
    private TMP_Text timeText;
    private TMP_Text pingText;

    // 退出按钮
    private Button exitBtn;
    private Button speedBtn;
    private Button pauseBtn;
    private Image pauseBtnImage;
    private Sprite pauseSprite;
    private Sprite resumeSprite;
    private bool isReplayPaused;
    private int pauseSpriteRequestId;

    // 确认对话框组件
    private ConfirmDialogComponent confirmDialog;

    // 小地图子面板
    private MiniMapSubPanel miniMapSubPanel;

    // 小地图刷新定时器
    private int miniMapRefreshTimerId = 0;
    private const float MINIMAP_REFRESH_INTERVAL = 0.1f;

    /// <summary>
    /// 获取或设置当前回放面板绑定的演示场景控制器，用于读取回放状态、刷新小地图和控制暂停/倍速。
    /// </summary>
    public Ra2Demo Ra2Demo { get; set; }

    /// <summary>
    /// 创建回放面板实例，并交由 UI 框架管理面板生命周期。
    /// </summary>
    /// <param name="_processor">负责分发 UI 消息的处理器。</param>
    /// <param name="_modelData">当前面板的模型配置数据。</param>
    /// <param name="_disableNew">限制外部直接构造面板的框架标记。</param>
    public ReplayPanel(IDispathMessage _processor, UIModelData _modelData, DisableNew _disableNew)
        : base(_processor, _modelData, _disableNew)
    {
    }

    /// <summary>
    /// 面板显示时查找 UI 节点、初始化小地图、刷新回放信息并启动周期刷新。
    /// </summary>
    protected override void OnBecameVisible()
    {
        base.OnBecameVisible();

        moneyText = PanelObject.transform.Find("Info/Money/Value")?.GetComponent<TMP_Text>();
        powerText = PanelObject.transform.Find("Info/Power/Value")?.GetComponent<TMP_Text>();
        timeText = PanelObject.transform.Find("Info/TimePing/TimeText")?.GetComponent<TMP_Text>();
        pingText = PanelObject.transform.Find("Info/TimePing/PingText")?.GetComponent<TMP_Text>();
        exitBtn = PanelObject.transform.Find("ExitBtn")?.GetComponent<Button>();
        speedBtn = PanelObject.transform.Find("Bottom/SpeedBtn")?.GetComponent<Button>();
        pauseBtn = PanelObject.transform.Find("Bottom/PauseBtn")?.GetComponent<Button>();
        pauseBtnImage = pauseBtn?.GetComponent<Image>();
        speedText = PanelObject.transform.Find("Bottom/SpeedBtn/Text (TMP)")?.GetComponent<TMP_Text>();
        frameText = PanelObject.transform.Find("Bottom/FrameText")?.GetComponent<TMP_Text>();

        confirmDialog = new ConfirmDialogComponent(PanelObject.transform.Find("Confirm"));

        miniMapSubPanel = new MiniMapSubPanel(PanelObject.transform);
        miniMapSubPanel.Show(new MiniMapPanelData() { Title = "小地图", SizeText = "地图尺寸：128*128" });

        if (Ra2Demo != null)
        {
            miniMapSubPanel.SetMiniMapTexture(Ra2Demo.GetMiniMapTexture());
        }

        RefreshSpeedText();
        ResetPauseButtonState();
        LoadPauseButtonSprites();
        RefreshFrameText();
        RefreshTimePingInfo();
        StartMiniMapRefresh();
    }

    /// <summary>
    /// 注册回放面板按钮事件，确保退出、倍速和暂停按钮可以响应点击。
    /// </summary>
    protected override void AddEvent()
    {
        base.AddEvent();

        if (exitBtn != null)
        {
            exitBtn.onClick.AddListener(OnExitButtonClick);
        }

        if (speedBtn != null)
        {
            speedBtn.onClick.AddListener(OnSpeedButtonClick);
        }

        if (pauseBtn != null)
        {
            pauseBtn.onClick.AddListener(OnPauseButtonClick);
        }
    }

    /// <summary>
    /// 移除回放面板按钮事件，避免面板关闭或重新打开后重复触发。
    /// </summary>
    protected override void RemoveEvent()
    {
        base.RemoveEvent();

        if (exitBtn != null)
        {
            exitBtn.onClick.RemoveListener(OnExitButtonClick);
        }

        if (speedBtn != null)
        {
            speedBtn.onClick.RemoveListener(OnSpeedButtonClick);
        }

        if (pauseBtn != null)
        {
            pauseBtn.onClick.RemoveListener(OnPauseButtonClick);
        }
    }

    /// <summary>
    /// 面板隐藏时停止小地图刷新、恢复可能被暂停的回放，并释放子面板事件。
    /// </summary>
    protected override void OnBecameInvisible()
    {
        base.OnBecameInvisible();

        if (miniMapRefreshTimerId != 0)
        {
            Tick.ClearTimeout(miniMapRefreshTimerId);
            miniMapRefreshTimerId = 0;
        }

        ResumeReplayIfPaused();
        pauseSpriteRequestId++;

        miniMapSubPanel?.Destroy();
        miniMapSubPanel = null;

        if (confirmDialog != null)
        {
            confirmDialog.UnregisterEvents();
        }
    }

    /// <summary>
    /// 设置回放面板中的金钱显示文本。
    /// </summary>
    /// <param name="money">需要显示的当前金钱数值。</param>
    public void SetMoney(int money)
    {
        if (moneyText != null)
        {
            moneyText.text = $"{money}";
        }
    }

    /// <summary>
    /// 设置回放面板中的电力显示文本。
    /// </summary>
    /// <param name="power">需要显示的当前电力数值。</param>
    public void SetPower(int power)
    {
        if (powerText != null)
        {
            powerText.text = $"{power}";
        }
    }

    /// <summary>
    /// 启动小地图和回放状态的定时刷新，重复调用时会先清理旧定时器。
    /// </summary>
    private void StartMiniMapRefresh()
    {
        if (miniMapRefreshTimerId != 0)
        {
            Tick.ClearTimeout(miniMapRefreshTimerId);
        }

        miniMapRefreshTimerId = Tick.SetTimeout(RefreshMiniMapTexture, MINIMAP_REFRESH_INTERVAL);
    }

    /// <summary>
    /// 刷新小地图纹理、帧进度和运行时间信息，并安排下一次刷新。
    /// </summary>
    private void RefreshMiniMapTexture()
    {
        if (Ra2Demo != null && miniMapSubPanel != null)
        {
            miniMapSubPanel.SetMiniMapTexture(Ra2Demo.GetMiniMapTexture());
        }

        RefreshFrameText();
        RefreshTimePingInfo();
        StartMiniMapRefresh();
    }

    /// <summary>
    /// 处理退出按钮点击，弹出确认框并在确认后返回匹配流程。
    /// </summary>
    private void OnExitButtonClick()
    {
        UISound.Instance.PlayClick();

        confirmDialog.Show(
            onConfirm: () =>
            {
                Frame.DispatchEvent(new RestartGameEvent());
            },
            message: "确定要退出回放吗？"
        );
    }

    /// <summary>
    /// 处理倍速按钮点击，轮换回放倍速并刷新按钮文字。
    /// </summary>
    private void OnSpeedButtonClick()
    {
        UISound.Instance.PlayClick();
        Ra2Demo?.CycleReplaySpeed();
        RefreshSpeedText();
    }

    /// <summary>
    /// 处理暂停按钮点击，根据当前暂停状态切换回放推进，并刷新按钮背景图片。
    /// </summary>
    private void OnPauseButtonClick()
    {
        UISound.Instance.PlayClick();

        if (isReplayPaused)
        {
            Ra2Demo?.GetBattleGame()?.Resume();
            isReplayPaused = false;
        }
        else
        {
            Ra2Demo?.GetBattleGame()?.Pause();
            isReplayPaused = true;
        }

        RefreshPauseButtonImage();
    }

    /// <summary>
    /// 异步加载暂停和恢复按钮图片资源，加载完成后按当前暂停状态刷新按钮背景。
    /// </summary>
    private async void LoadPauseButtonSprites()
    {
        int requestId = ++pauseSpriteRequestId;
        pauseSprite = await AssetManager.GetSpriteAsync("ImagePause");
        resumeSprite = await AssetManager.GetSpriteAsync("ImageResume");

        if (requestId != pauseSpriteRequestId)
        {
            return;
        }

        RefreshPauseButtonImage();
    }

    /// <summary>
    /// 重置暂停按钮的逻辑状态，确保面板初始显示为可执行暂停操作的按钮。
    /// </summary>
    private void ResetPauseButtonState()
    {
        isReplayPaused = false;
        RefreshPauseButtonImage();
    }

    /// <summary>
    /// 如果回放面板曾经暂停游戏，则在面板隐藏时恢复游戏，避免暂停状态泄漏到后续流程。
    /// </summary>
    private void ResumeReplayIfPaused()
    {
        if (!isReplayPaused)
        {
            return;
        }

        Ra2Demo?.GetBattleGame()?.Resume();
        isReplayPaused = false;
        RefreshPauseButtonImage();
    }

    /// <summary>
    /// 根据暂停状态设置暂停按钮背景，未暂停时显示暂停图，已暂停时显示恢复图。
    /// </summary>
    private void RefreshPauseButtonImage()
    {
        if (pauseBtnImage == null)
        {
            return;
        }

        Sprite targetSprite = isReplayPaused ? resumeSprite : pauseSprite;
        if (targetSprite != null)
        {
            pauseBtnImage.sprite = targetSprite;
        }
    }

    /// <summary>
    /// 刷新倍速按钮文字，显示当前回放推进倍率。
    /// </summary>
    private void RefreshSpeedText()
    {
        if (speedText == null)
        {
            return;
        }

        float speed = Ra2Demo != null ? Ra2Demo.GetReplaySpeed() : 1f;
        speedText.text = $"X{speed:0.#}";
    }

    /// <summary>
    /// 刷新当前帧、总帧数和回放进度百分比文本。
    /// </summary>
    private void RefreshFrameText()
    {
        if (frameText == null)
        {
            return;
        }

        var battleGame = Ra2Demo?.GetBattleGame();
        int totalFrame = battleGame?.World?.CommandManager?.GetMaxFutureCommandFrame() ?? 0;
        int currentFrame = battleGame?.World?.Tick ?? 0;
        float progress = totalFrame > 0 ? (float)currentFrame / totalFrame : 0f;
        frameText.text = $"当前帧：{currentFrame} / {totalFrame}，进度：{progress:P1}";
    }

    /// <summary>
    /// 刷新回放运行时间和网络延迟文本，运行时间来自锁步时间管理器，Ping 来自当前网络连接。
    /// </summary>
    private void RefreshTimePingInfo()
    {
        if (timeText != null)
        {
            float seconds = 0f;
            var timeManager = Ra2Demo?.GetBattleGame()?.World?.TimeManager;
            if (timeManager != null)
            {
                seconds = timeManager.Time.ToFloat();
            }

            timeText.text = FormatBattleTime(seconds);
        }

        if (pingText != null)
        {
            long ping = NetworkManager.Instance.CurrentPing;
            pingText.text = ping >= 0 ? $"{ping}ms" : "--ms";
        }
    }

    /// <summary>
    /// 将回放秒数格式化为面板可读的时间文本，一小时内显示分秒，一小时后显示时分秒。
    /// </summary>
    /// <param name="seconds">回放已经运行的秒数。</param>
    /// <returns>格式化后的时间文本。</returns>
    private string FormatBattleTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int hours = totalSeconds / 3600;
        int minutes = totalSeconds % 3600 / 60;
        int remainingSeconds = totalSeconds % 60;

        if (hours > 0)
        {
            return $"{hours}:{minutes:00}:{remainingSeconds:00}";
        }

        return $"{minutes:00}:{remainingSeconds:00}";
    }
}
