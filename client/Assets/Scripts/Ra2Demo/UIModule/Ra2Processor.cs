using System;
using System.Collections.Generic;
using PostHogUnity;
using Utils;
using ZFrame;
using ZLib;
using ZLockstep.Simulation.ECS.Components; // 添加这个 using 语句以使用 Tick

public class Ra2Processor : BaseProcessor
{
    private Ra2Demo _ra2Demo;

    private MatchPanel _matchPanel;
    public MatchPanel MatchPanel
    {
        get
        {
            if (_matchPanel == null)
            {
                // 使用字符串ID，业务层可以定义自己的枚举并通过ToString()转换
                _matchPanel = _matchPanel.New<MatchPanel>(this, "MatchPanel");
            }
            return _matchPanel;
        }
    }

    // loading 面板
    private LoadingPanel _loadingPanel;
    public LoadingPanel LoadingPanel
    {
        get
        {
            if (_loadingPanel == null)
            {
                // 使用字符串ID，业务层可以定义自己的枚举并通过ToString()转换
                _loadingPanel = _loadingPanel.New<LoadingPanel>(this, "LoadingPanel");
            }
            return _loadingPanel;
        }
    }

    private MainPanel _mainPanel;
    public MainPanel MainPanel
    {
        get
        {
            if (_mainPanel == null)
            {
                // 使用字符串ID，业务层可以定义自己的枚举并通过ToString()转换
                _mainPanel = _mainPanel.New<MainPanel>(this, "MainPanel");
            }
            return _mainPanel;
        }
    }

    private ReplayPanel _replayPanel;
    public ReplayPanel ReplayPanel
    {
        get
        {
            if (_replayPanel == null)
            {
                _replayPanel = _replayPanel.New<ReplayPanel>(this, "ReplayPanel");
            }
            return _replayPanel;
        }
    }

    // 结算面板
    private SettlePanel _settlePanel;
    public SettlePanel SettlePanel
    {
        get
        {
            if (_settlePanel == null)
            {
                _settlePanel = _settlePanel.New<SettlePanel>(this, "SettlePanel");
            }
            return _settlePanel;
        }
    }

    // 血量面板
    private HealthPanel _healthPanel;
    public HealthPanel HealthPanel
    {
        get
        {
            if (_healthPanel == null)
            {
                // 使用字符串 ID，业务层可以定义自己的枚举并通过 ToString() 转换
                _healthPanel = _healthPanel.New<HealthPanel>(this, "HealthPanel");
            }
            return _healthPanel;
        }
    }

    // 登录面板
    private LoginPanel _loginPanel;
    public LoginPanel LoginPanel
    {
        get
        {
            if (_loginPanel == null)
            {
                _loginPanel = _loginPanel.New<LoginPanel>(this, "LoginPanel");
            }
            return _loginPanel;
        }
    }

    public Ra2Processor(Module _module) : base(_module)
    {
    }

    protected override List<Type> ListenModuleEvents()
    {
        return new List<Type>()
        {
            typeof(Ra2StartUpEvent),
            typeof(MatchedEvent),
            typeof(GameStartEvent),
            typeof(EconomyEvent),
            typeof(ShowMessageEvent),
            typeof(SettleEvent),
            typeof(RestartGameEvent),
            typeof(ReconnectGameStartEvent),
            typeof(SoloGameStartEvent),
            typeof(ReplayGameStartEvent),
            typeof(HealthEvent),
            typeof(HealthPanelLateUpdateEvent),
            typeof(HealthBarSettingChangedEvent),
            typeof(LoginEvent),
            typeof(ConfirmSellBuildingEvent),
        };
    }

    protected override void ReceivedMessage(object __msg)
    {
    }

    protected override void ReceiveModuleEvent(ModuleEvent __me)
    {
        switch (__me)
        {
            case Ra2StartUpEvent e:
                {
                    _ra2Demo = e.Ra2Demo;
                    // MainPanel.Open();
                    LoginPanel.Open();
                }
                break;
            case MatchedEvent e:
                {
                    // 匹配成功
                    zUDebug.Log("[Ra2Processor] 匹配成功");
                    MatchPanel.Close();

                    // 获取对战人数
                    LoadingPanel.Open();
                    int playerNum = e.data.InitialState.Count - 1;
                    LoadingPanel.SetProgress(0.5f);
                    LoadingPanel.SetPlayerCount(playerNum);
                    
                    // 延迟2秒后调用SendReady
                    Tick.SetTimeout(() => {
                        NetworkManager.Instance.CurrentWebSocket.SendReady();
                    }, 2.0f);
                }
                break;
            case GameStartEvent e:
                {
                    // GameObjectPoolManager.ClearAllPools(true);
                    CommandRecorder.CreateEmptyRecordingFile(e.FileName);
                    zUDebug.Log("[Ra2Processor] 开始游戏");
                    LoadingPanel.Close();
                    HealthPanel.Open();
                    MainPanel.Ra2Demo = _ra2Demo;
                    MainPanel.Open();
                    
                    RefreshEconomy();
                }
                break;
            case ReconnectGameStartEvent:
                {
                    zUDebug.Log("[Ra2Processor] 重连恢复游戏");
                    MatchPanel.Close();
                    LoadingPanel.Close();
                    HealthPanel.Open();
                    MainPanel.Ra2Demo = _ra2Demo;
                    MainPanel.Open();

                    RefreshEconomy();
                    _ra2Demo.MoveCameraToOurFactory();
                }
                break;
            case SoloGameStartEvent:
                {
                    // GameObjectPoolManager.ClearAllPools(true);
                    zUDebug.Log("[Ra2Processor]  solo 模式开始游戏");
                    MatchPanel.Close();
                    HealthPanel.Open();
                    MainPanel.Ra2Demo = _ra2Demo;
                    MainPanel.Open();

                    RefreshEconomy();
                }
                break;
            case ReplayGameStartEvent:
                {
                    // GameObjectPoolManager.ClearAllPools(true);
                    zUDebug.Log("[Ra2Processor]  replay 模式开始游戏");
                    MatchPanel.Close();
                    ReplayPanel.Ra2Demo = _ra2Demo;
                    ReplayPanel.Open();

                    RefreshEconomy();
                }
                break;
            case EconomyEvent e:
                RefreshEconomy();
                break;
            case ShowMessageEvent e:
                MainPanel.ShowMessage(e.Message);
                break;
            case ConfirmSellBuildingEvent e:
                // 显示确认售卖建筑对话框
                MainPanel.ShowConfirmSellBuilding(e.EntityId);
                break;
            case SettleEvent e:
                HealthPanel.Close();
                MainPanel.Close();
                ReplayPanel.Close();
                SettlePanel.Open();
                SettlePanel.SetResult(e.IsVictory);
                break;
            case RestartGameEvent e:
                _ra2Demo.RestartGame();
                HealthPanel.Close();
                MainPanel.Close();
                ReplayPanel.Close();
                SettlePanel.Close();
                MatchPanel.Open();
                break;
            case HealthEvent e:
                {
                    // 确保血量面板始终开启
                    HealthPanel.Open();
                    
                    // 根据 IsVisible 属性决定单个血条的显示/隐藏
                    if (e.IsVisible)
                    {
                        // 更新指定实体的血量显示
                        HealthPanel.UpdateHealth(e.Id, e.IsSelf);
                        HealthPanel.ShowHealthBar(e.Id);
                    }
                    else
                    {
                        // 删除指定实体的血量条
                        HealthPanel.DeleteHealthBar(e.Id);
                    }
                }
                break;
            case HealthPanelLateUpdateEvent:
                {
                    // 在 LateUpdate 中更新所有血量条的位置和血量值
                    HealthPanel.UpdateAllHealthBars();
                }
                break;
            case HealthBarSettingChangedEvent e:
                {
                    // 转发血条设置变更事件到 HealthPanel
                    HealthPanel.OnHealthBarSettingChanged(e);
                }
                break;
            case LoginEvent e:
                {
                    zUDebug.Log($"[Ra2Processor] 更新用户信息为：{e.Nickname}");
                    LoginPanel.Close();
                    MatchPanel.Open();
                    MatchPanel.SetPlayerName(e.Nickname);
                    MatchPanel.SetHeadImg(e.AvatarUrl);

                    // TODO PostHog 上报用户信息
                    PostHog.IdentifyAsync(e.Nickname, new Dictionary<string, object>()
                    {
                        { "avatarUrl", e.AvatarUrl },
                        { "nickName", e.Nickname }
                    });
                }
                break;
        }
    }

    /// <summary>
    /// 刷新经济显示
    /// </summary>
    private void RefreshEconomy()
    {
        EcsUtils.GetLocalPlayerEconomy(_ra2Demo.GetBattleGame(), out int money, out int power);
        
        MainPanel.SetMoney(money);
        MainPanel.SetPower(power);
        ReplayPanel.SetMoney(money);
        ReplayPanel.SetPower(power);
    }

}
