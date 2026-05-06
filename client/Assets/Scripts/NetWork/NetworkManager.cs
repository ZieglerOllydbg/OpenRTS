using System;
using System.Collections.Generic;
using Game.Examples;
using Game.RA2.Client;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ZFrame;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Sync;
using ZLockstep.Sync.Command;
using zUnity;

/// <summary>
/// 网络管理器，负责匹配连接、房间凭证缓存、断线自动重连和跨启动房间恢复。
/// </summary>
public class NetworkManager
{
    /// <summary>
    /// 当前 WebSocket 连接建立后的协议用途，用于区分匹配和重连握手。
    /// </summary>
    private enum ConnectionPurpose
    {
        /// <summary>
        /// 普通匹配连接，连接成功后发送 match。
        /// </summary>
        Match,

        /// <summary>
        /// 游戏内自动重连连接，连接成功后发送 reconnect 并携带当前帧。
        /// </summary>
        Reconnect,

        /// <summary>
        /// 跨启动恢复连接，连接成功后从第 0 帧请求补发。
        /// </summary>
        RestoreSavedSession
    }

    private static NetworkManager _instance;
    private static readonly object _lock = new object();

    private const string LOCAL_SERVER_URL = "ws://127.0.0.1:8080/ws";
    private const string REMOTE_SERVER_URL = "wss://www.zhegepai.cn/ws";
    private const string PLAYER_ID = "Player1";
    private const string PREF_HAS_SESSION = "ReconnectSession.HasSession";
    private const string PREF_ROOM_ID = "ReconnectSession.RoomId";
    private const string PREF_TOKEN = "ReconnectSession.Token";
    private const string PREF_PLAYER_NUMBER = "ReconnectSession.PlayerNumber";
    private const string PREF_SERVER_URL = "ReconnectSession.ServerUrl";
    private const string PREF_ROOM_TYPE = "ReconnectSession.RoomType";

    private WebSocketClient _currentWebSocket;
    private Ra2Demo _ra2Demo;
    private RoomType _roomType;
    private string _serverUrl;
    private string _roomId;
    private string _token;
    private int _playerNumber = 1;
    private ConnectionPurpose _connectionPurpose = ConnectionPurpose.Match;
    private bool _isReconnectRunning;
    private readonly List<WebSocketClient> _temporaryHistoryWebSockets = new();

    /// <summary>
    /// 获取全局网络管理器实例。
    /// </summary>
    public static NetworkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new NetworkManager();
                    }
                }
            }

            return _instance;
        }
    }

    /// <summary>
    /// 获取或设置当前是否已经匹配成功。
    /// </summary>
    public bool IsMatched { get; set; } = false;

    /// <summary>
    /// 获取或设置当前客户端是否已经发送准备并进入游戏。
    /// </summary>
    public bool IsReady { get; set; } = false;

    /// <summary>
    /// 获取当前活跃的 WebSocket 客户端。
    /// </summary>
    public WebSocketClient CurrentWebSocket
    {
        get { return _currentWebSocket; }
        private set { _currentWebSocket = value; }
    }

    /// <summary>
    /// 获取当前 WebSocket 最近一次测得的 Ping 值，尚未测得或没有连接时返回 -1。
    /// </summary>
    public long CurrentPing
    {
        get { return _currentWebSocket?.CurrentPing ?? -1; }
    }

    /// <summary>
    /// 获取当前是否正在进行游戏内自动重连。
    /// </summary>
    public bool IsReconnectRunning
    {
        get { return _isReconnectRunning; }
    }

    /// <summary>
    /// 游戏内自动重连开始事件，用于通知战斗界面显示正在重连提示。
    /// </summary>
    public event Action OnReconnectStarted;

    /// <summary>
    /// 游戏内自动重连结束事件，用于通知战斗界面隐藏正在重连提示。
    /// </summary>
    public event Action OnReconnectFinished;

    /// <summary>
    /// 设置 Ra2Demo 引用，供网络回调创建和恢复 BattleGame。
    /// </summary>
    /// <param name="ra2Demo">当前场景中的 Ra2Demo 实例。</param>
    public void SetRa2Demo(Ra2Demo ra2Demo)
    {
        _ra2Demo = ra2Demo;
    }

    /// <summary>
    /// 连接到服务器并进入匹配流程。
    /// </summary>
    /// <param name="roomType">要匹配的房间类型。</param>
    /// <param name="useLocalServer">是否使用本地服务器地址。</param>
    public void ConnectToServer(RoomType roomType, bool useLocalServer)
    {
        ClearReconnectSession();
        _roomType = roomType;
        _serverUrl = useLocalServer ? LOCAL_SERVER_URL : REMOTE_SERVER_URL;
        _connectionPurpose = ConnectionPurpose.Match;
        _isReconnectRunning = false;

        ReplaceCurrentWebSocket(CreateWebSocket(_serverUrl));

        zUDebug.Log($"[NetworkManager] 正在连接服务器: {_serverUrl}");
        _currentWebSocket.Connect();
    }

    /// <summary>
    /// 检查本地是否存在可用于恢复房间身份的重连会话。
    /// </summary>
    /// <returns>存在可恢复会话时返回 true，否则返回 false。</returns>
    public bool HasSavedReconnectSession()
    {
        return PlayerPrefs.GetInt(PREF_HAS_SESSION, 0) == 1 &&
               !string.IsNullOrEmpty(PlayerPrefs.GetString(PREF_ROOM_ID, string.Empty)) &&
               !string.IsNullOrEmpty(PlayerPrefs.GetString(PREF_TOKEN, string.Empty)) &&
               !string.IsNullOrEmpty(PlayerPrefs.GetString(PREF_SERVER_URL, string.Empty));
    }

    /// <summary>
    /// 从 PlayerPrefs 中读取房间凭证并尝试恢复房间身份。
    /// </summary>
    /// <returns>成功发起恢复连接时返回 true；缺少会话或上下文时返回 false。</returns>
    public bool TryReconnectFromSavedSession()
    {
        if (_ra2Demo == null || !LoadReconnectSessionFromPrefs())
        {
            return false;
        }

        _connectionPurpose = ConnectionPurpose.RestoreSavedSession;
        _isReconnectRunning = true;
        PrepareBattleGameForSavedSession();
        ReplaceCurrentWebSocket(CreateWebSocket(_serverUrl));

        zUDebug.Log($"[NetworkManager] 正在从本地会话恢复房间: roomId={_roomId}, server={_serverUrl}");
        _currentWebSocket.Connect();
        return true;
    }

    /// <summary>
    /// 在当前网络对局中重新发起一次重连请求，用于玩家点击重连提示面板的重连按钮。
    /// </summary>
    /// <returns>成功发起重连连接时返回 true；当前状态不允许重连时返回 false。</returns>
    public bool RetryReconnectCurrentGame()
    {
        if (!CanReconnectCurrentGame())
        {
            zUDebug.LogWarning("[NetworkManager] 当前状态不允许手动重连");
            return false;
        }

        _isReconnectRunning = true;
        _connectionPurpose = ConnectionPurpose.Reconnect;

        zUDebug.LogWarning($"[NetworkManager] 手动重新发起重连，lastFrame={GetLastProcessedFrame()}");
        OnReconnectStarted?.Invoke();
        ReplaceCurrentWebSocket(CreateWebSocket(_serverUrl));
        _currentWebSocket.Connect();
        return true;
    }

    /// <summary>
    /// 清除本地和内存中的重连会话凭证。
    /// </summary>
    public void ClearReconnectSession()
    {
        _roomId = null;
        _token = null;
        _playerNumber = 1;
        IsMatched = false;
        IsReady = false;

        PlayerPrefs.DeleteKey(PREF_HAS_SESSION);
        PlayerPrefs.DeleteKey(PREF_ROOM_ID);
        PlayerPrefs.DeleteKey(PREF_TOKEN);
        PlayerPrefs.DeleteKey(PREF_PLAYER_NUMBER);
        PlayerPrefs.DeleteKey(PREF_SERVER_URL);
        PlayerPrefs.DeleteKey(PREF_ROOM_TYPE);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 重置当前游戏网络状态，并清理可重连会话。
    /// </summary>
    public void ResetGame()
    {
        ClearReconnectSession();
        CloseCurrentWebSocket();
        _ra2Demo = null;
    }

    /// <summary>
    /// 关闭并清理当前的 WebSocket 连接。
    /// </summary>
    public void CloseCurrentWebSocket()
    {
        ReleaseWebSocket(_currentWebSocket, true);
        _currentWebSocket = null;
        FinishReconnectState();
    }

    /// <summary>
    /// 取消当前匹配并关闭连接，用于玩家在匹配等待阶段主动返回匹配入口。
    /// </summary>
    public async void CancelMatchAndCloseCurrentWebSocket()
    {
        ClearReconnectSession();

        WebSocketClient webSocket = _currentWebSocket;
        if (webSocket == null)
        {
            CloseCurrentWebSocket();
            return;
        }

        try
        {
            await webSocket.SendCancelMatchRequestAsync();
        }
        catch (Exception e)
        {
            zUDebug.LogWarning($"[NetworkManager] 发送取消匹配请求失败，将继续关闭连接: {e.Message}");
        }
        finally
        {
            if (_currentWebSocket == webSocket)
            {
                CloseCurrentWebSocket();
            }
            else
            {
                ReleaseWebSocket(webSocket, true);
            }
        }
    }

    /// <summary>
    /// 检查 WebSocket 是否已连接。
    /// </summary>
    /// <returns>当前 WebSocket 处于连接状态时返回 true，否则返回 false。</returns>
    public bool IsConnected()
    {
        return _currentWebSocket != null && _currentWebSocket.IsConnected;
    }

    /// <summary>
    /// 分发当前对局连接和临时历史帧连接的 WebSocket 消息队列，确保非 WebGL 平台能收到协议回调。
    /// </summary>
    public void DispatchMessageQueues()
    {
        _currentWebSocket?.DispatchMessageQueue();

        for (int i = _temporaryHistoryWebSockets.Count - 1; i >= 0; i--)
        {
            _temporaryHistoryWebSockets[i]?.DispatchMessageQueue();
        }
    }

    /// <summary>
    /// 创建独立临时连接请求服务器历史帧文件，完成或失败后自动关闭临时连接且不影响当前对局连接。
    /// </summary>
    /// <param name="fileName">服务器历史帧文件名。</param>
    /// <param name="useLocalServer">是否连接本地服务器地址。</param>
    /// <param name="onSuccess">完整收到历史帧后触发的成功回调。</param>
    /// <param name="onFailed">服务器返回失败或连接异常时触发的失败回调。</param>
    public void RequestHistoryFrames(string fileName, bool useLocalServer, Action<HistoryFramesReplayData> onSuccess, Action<HistoryFramesFailedData> onFailed)
    {
        HistoryFramesRequestContext requestContext = new(this, fileName, useLocalServer, onSuccess, onFailed);
        requestContext.Start();
    }

    /// <summary>
    /// 历史帧请求上下文，封装一次临时 WebSocket 连接的事件绑定、帧批次聚合和完成清理流程。
    /// </summary>
    private sealed class HistoryFramesRequestContext
    {
        private readonly NetworkManager owner;
        private readonly string fileName;
        private readonly Action<HistoryFramesReplayData> onSuccess;
        private readonly Action<HistoryFramesFailedData> onFailed;
        private readonly WebSocketClient historyWebSocket;
        private readonly List<HistoryFrameEntryData> frames = new();
        private HistoryFramesStartData startData;
        private bool hasStartData;
        private bool completed;

        /// <summary>
        /// 创建历史帧请求上下文，并根据服务器选择初始化临时 WebSocket 客户端。
        /// </summary>
        /// <param name="owner">拥有临时连接列表的网络管理器。</param>
        /// <param name="fileName">服务器历史帧文件名。</param>
        /// <param name="useLocalServer">是否连接本地服务器地址。</param>
        /// <param name="onSuccess">完整收到历史帧后触发的成功回调。</param>
        /// <param name="onFailed">服务器返回失败或连接异常时触发的失败回调。</param>
        public HistoryFramesRequestContext(NetworkManager owner, string fileName, bool useLocalServer, Action<HistoryFramesReplayData> onSuccess, Action<HistoryFramesFailedData> onFailed)
        {
            this.owner = owner;
            this.fileName = fileName;
            this.onSuccess = onSuccess;
            this.onFailed = onFailed;

            string serverUrl = useLocalServer ? LOCAL_SERVER_URL : REMOTE_SERVER_URL;
            historyWebSocket = new WebSocketClient(serverUrl, PLAYER_ID);
        }

        /// <summary>
        /// 绑定临时连接事件并发起 WebSocket 连接，连接成功后会发送历史帧请求。
        /// </summary>
        public void Start()
        {
            historyWebSocket.OnConnected += OnHistoryConnected;
            historyWebSocket.OnDisconnected += OnHistoryDisconnected;
            historyWebSocket.OnError += OnHistoryError;
            historyWebSocket.OnHistoryFramesStart += OnHistoryFramesStart;
            historyWebSocket.OnHistoryFramesFrame += OnHistoryFramesFrame;
            historyWebSocket.OnHistoryFramesFailed += OnHistoryFramesFailed;
            owner._temporaryHistoryWebSockets.Add(historyWebSocket);

            zUDebug.LogInfo($"[NetworkManager] 正在创建历史帧临时连接: fileName={fileName}");
            historyWebSocket.Connect();
        }

        /// <summary>
        /// 清理临时连接事件绑定，并按需关闭 WebSocket 连接。
        /// </summary>
        /// <param name="disconnect">是否主动断开临时 WebSocket。</param>
        private void Cleanup(bool disconnect)
        {
            historyWebSocket.OnConnected -= OnHistoryConnected;
            historyWebSocket.OnDisconnected -= OnHistoryDisconnected;
            historyWebSocket.OnError -= OnHistoryError;
            historyWebSocket.OnHistoryFramesStart -= OnHistoryFramesStart;
            historyWebSocket.OnHistoryFramesFrame -= OnHistoryFramesFrame;
            historyWebSocket.OnHistoryFramesFailed -= OnHistoryFramesFailed;
            owner._temporaryHistoryWebSockets.Remove(historyWebSocket);

            if (disconnect)
            {
                historyWebSocket.Disconnect();
            }

            historyWebSocket.Dispose();
        }

        /// <summary>
        /// 标记请求成功，向调用方返回元数据和全部历史帧，并关闭临时连接。
        /// </summary>
        private void CompleteSuccess()
        {
            if (completed)
            {
                return;
            }

            completed = true;
            onSuccess?.Invoke(new HistoryFramesReplayData
            {
                StartData = startData,
                Frames = frames
            });
            Cleanup(true);
        }

        /// <summary>
        /// 标记请求失败，向调用方返回失败文件名和原因，并关闭临时连接。
        /// </summary>
        /// <param name="failedFileName">请求失败的历史帧文件名。</param>
        /// <param name="reason">失败原因。</param>
        private void CompleteFailed(string failedFileName, string reason)
        {
            if (completed)
            {
                return;
            }

            completed = true;
            onFailed?.Invoke(new HistoryFramesFailedData
            {
                FileName = failedFileName,
                Reason = reason
            });
            Cleanup(true);
        }

        /// <summary>
        /// 临时连接建立成功后发送 getHistoryFrames 请求。
        /// </summary>
        /// <param name="message">底层 WebSocket 返回的连接成功信息。</param>
        private void OnHistoryConnected(string message)
        {
            zUDebug.LogInfo($"[NetworkManager] 历史帧临时连接成功: {message}");
            historyWebSocket.SendHistoryFramesRequest(fileName);
        }

        /// <summary>
        /// 临时连接断开时结束未完成的历史帧请求。
        /// </summary>
        /// <param name="reason">底层 WebSocket 关闭原因。</param>
        private void OnHistoryDisconnected(string reason)
        {
            zUDebug.LogWarning($"[NetworkManager] 历史帧临时连接断开: {reason}");
            if (!completed)
            {
                CompleteFailed(fileName, $"disconnect:{reason}");
            }
        }

        /// <summary>
        /// 临时连接报错时结束未完成的历史帧请求。
        /// </summary>
        /// <param name="error">底层 WebSocket 错误信息。</param>
        private void OnHistoryError(string error)
        {
            zUDebug.LogError($"[NetworkManager] 历史帧临时连接错误: {error}");
            if (!completed)
            {
                CompleteFailed(fileName, $"websocket_error:{error}");
            }
        }

        /// <summary>
        /// 接收历史帧元数据并在无帧数据时直接完成请求。
        /// </summary>
        /// <param name="data">服务器返回的历史帧元数据。</param>
        private void OnHistoryFramesStart(HistoryFramesStartData data)
        {
            hasStartData = true;
            startData = data;
            zUDebug.LogInfo($"[NetworkManager] 收到历史帧元数据: fileName={data.FileName}, roomId={data.RoomId}, playerCount={data.PlayerCount}, players={data.Players?.Count ?? 0}, isEnd={data.IsEnd}");

            if (data.IsEnd)
            {
                CompleteSuccess();
            }
        }

        /// <summary>
        /// 接收历史帧批次并在最后一批到达后完成请求。
        /// </summary>
        /// <param name="data">服务器返回的历史帧批次数据。</param>
        private void OnHistoryFramesFrame(HistoryFramesFrameBatchData data)
        {
            if (data.Frames != null)
            {
                frames.AddRange(data.Frames);
            }

            zUDebug.LogInfo($"[NetworkManager] 收到历史帧批次: fileName={data.FileName}, count={data.Frames?.Count ?? 0}, total={frames.Count}, isEnd={data.IsEnd}");
            if (data.IsEnd)
            {
                if (!hasStartData)
                {
                    startData = new HistoryFramesStartData
                    {
                        FileName = data.FileName,
                        PlayerCount = 1
                    };
                }

                CompleteSuccess();
            }
        }

        /// <summary>
        /// 接收服务器历史帧失败消息并结束请求。
        /// </summary>
        /// <param name="data">服务器返回的历史帧失败数据。</param>
        private void OnHistoryFramesFailed(HistoryFramesFailedData data)
        {
            zUDebug.LogError($"[NetworkManager] 获取历史帧失败: fileName={data.FileName}, reason={data.Reason}");
            CompleteFailed(data.FileName, data.Reason);
        }
    }

    /// <summary>
    /// 创建并绑定 WebSocket 客户端事件。
    /// </summary>
    /// <param name="serverUrl">要连接的服务器地址。</param>
    /// <returns>已绑定网络事件的新 WebSocket 客户端。</returns>
    private WebSocketClient CreateWebSocket(string serverUrl)
    {
        var webSocket = new WebSocketClient(serverUrl, PLAYER_ID);
        RegisterWebSocketEvents(webSocket);
        return webSocket;
    }

    /// <summary>
    /// 替换当前 WebSocket，并释放旧连接事件绑定。
    /// </summary>
    /// <param name="webSocket">新的 WebSocket 客户端。</param>
    private void ReplaceCurrentWebSocket(WebSocketClient webSocket)
    {
        ReleaseWebSocket(_currentWebSocket, true);
        _currentWebSocket = webSocket;
    }

    /// <summary>
    /// 释放 WebSocket 事件绑定，并按需主动断开连接。
    /// </summary>
    /// <param name="webSocket">要释放的 WebSocket 客户端。</param>
    /// <param name="disconnect">是否主动关闭连接。</param>
    private void ReleaseWebSocket(WebSocketClient webSocket, bool disconnect)
    {
        if (webSocket == null)
        {
            return;
        }

        UnregisterWebSocketEvents(webSocket);
        if (disconnect)
        {
            webSocket.Disconnect();
        }

        webSocket.Dispose();
    }

    /// <summary>
    /// 注册 WebSocket 事件，集中处理匹配、重连、帧同步和心跳状态。
    /// </summary>
    /// <param name="webSocket">需要绑定事件的 WebSocket 客户端。</param>
    private void RegisterWebSocketEvents(WebSocketClient webSocket)
    {
        webSocket.OnConnected += OnConnected;
        webSocket.OnDisconnected += OnDisconnected;
        webSocket.OnError += OnError;
        webSocket.OnMatchSuccess += OnMatchSuccess;
        webSocket.OnReconnectSuccess += OnReconnectSuccess;
        webSocket.OnReconnectFailed += OnReconnectFailed;
        webSocket.OnGameStart += OnGameStart;
        webSocket.OnPingTimeout += OnPingTimeout;
        webSocket.OnPingUpdated += OnPingUpdated;
    }

    /// <summary>
    /// 解除 WebSocket 事件绑定，防止旧连接回调影响新连接。
    /// </summary>
    /// <param name="webSocket">需要解绑事件的 WebSocket 客户端。</param>
    private void UnregisterWebSocketEvents(WebSocketClient webSocket)
    {
        webSocket.OnConnected -= OnConnected;
        webSocket.OnDisconnected -= OnDisconnected;
        webSocket.OnError -= OnError;
        webSocket.OnMatchSuccess -= OnMatchSuccess;
        webSocket.OnReconnectSuccess -= OnReconnectSuccess;
        webSocket.OnReconnectFailed -= OnReconnectFailed;
        webSocket.OnGameStart -= OnGameStart;
        webSocket.OnPingTimeout -= OnPingTimeout;
        webSocket.OnPingUpdated -= OnPingUpdated;
    }

    /// <summary>
    /// Ping 值更新事件处理，用于记录当前网络延迟。
    /// </summary>
    /// <param name="ping">本次测得的 Ping 值，单位毫秒。</param>
    private void OnPingUpdated(long ping)
    {
        zUDebug.Log($"[NetworkManager] Ping更新: {ping}ms");
    }

    /// <summary>
    /// 心跳超时事件处理，主动切换到重连连接。
    /// </summary>
    private void OnPingTimeout()
    {
        zUDebug.LogWarning("[NetworkManager] 心跳超时，开始自动重连");
        TryStartAutoReconnect("ping_timeout");
    }

    /// <summary>
    /// WebSocket 连接成功事件处理，根据连接目的发送匹配或重连协议。
    /// </summary>
    /// <param name="message">连接成功提示文本。</param>
    private void OnConnected(string message)
    {
        zUDebug.Log("[NetworkManager] 连接成功: " + message);

        if (_connectionPurpose == ConnectionPurpose.Match)
        {
            _currentWebSocket.SendMatchRequest(_roomType);
            return;
        }

        int lastFrame = _connectionPurpose == ConnectionPurpose.RestoreSavedSession ? 0 : GetLastProcessedFrame();
        _currentWebSocket.SendReconnectRequest(_roomId, _token, lastFrame);
    }

    /// <summary>
    /// WebSocket 被动断开事件处理，游戏中会自动尝试重连。
    /// </summary>
    /// <param name="reason">底层 WebSocket 关闭原因。</param>
    private void OnDisconnected(string reason)
    {
        zUDebug.LogWarning($"[NetworkManager] 连接断开: {reason}");
        TryStartAutoReconnect($"disconnect:{reason}");
    }

    /// <summary>
    /// WebSocket 错误事件处理，记录错误并保留后续断线重连机会。
    /// </summary>
    /// <param name="error">底层 WebSocket 错误信息。</param>
    private void OnError(string error)
    {
        zUDebug.LogError($"[NetworkManager] WebSocket 错误: {error}");
    }

    /// <summary>
    /// 匹配成功事件处理，创建网络模式战斗世界并保存重连凭证。
    /// </summary>
    /// <param name="data">服务器返回的匹配成功数据。</param>
    private void OnMatchSuccess(MatchSuccessData data)
    {
        IsMatched = true;
        FinishReconnectState();

        int playerNumber = GetPlayerNumberByInitialState(data.InitialState);
        SaveReconnectSession(data.RoomId, data.Token, playerNumber);

        zUDebug.Log($"[NetworkManager] 匹配成功：房间ID={data.RoomId}, 阵营ID={data.CampId}");

        // 重置重置全局随机状态快照
        zRandom.ResetGlobalDeterminismStateRaw(0L);

        _ra2Demo.Mode = GameMode.NetworkClient;
        _ra2Demo.SetBattleGame(new BattleGame(_ra2Demo.Mode, 20, 0, playerNumber));
        _ra2Demo.GetBattleGame().Init();
        _ra2Demo.InitializeUnityView();

        new WebSocketNetworkAdaptor(_ra2Demo, _currentWebSocket);

        zUDebug.Log($"[Ra2Demo] 匹配成功：房间ID={data.RoomId}, 阵营ID={data.CampId}, InitialState={data.InitialState}");

        GlobalInfoComponent globalInfoComponent = new(data.CampId);
        _ra2Demo.GetBattleGame().World.ComponentManager.AddGlobalComponent(globalInfoComponent);
        _ra2Demo.GetBattleGame().CreateWorldByConfig();

        StartReplayRecording(playerNumber);
        StartDeterminismProbe(_ra2Demo.GetBattleGame(), "DouMatch");

        Frame.DispatchEvent(new MatchedEvent(data));
    }

    /// <summary>
    /// 重连成功事件处理，恢复新连接的帧同步适配器并按需打开主界面。
    /// </summary>
    /// <param name="data">服务器返回的重连成功数据。</param>
    private void OnReconnectSuccess(ReconnectSuccessData data)
    {
        if (!string.Equals(data.RoomId, _roomId, StringComparison.Ordinal))
        {
            zUDebug.LogError($"[NetworkManager] 重连房间不匹配，本地={_roomId}, 服务器={data.RoomId}");
            HandleReconnectFailed("room_mismatch");
            return;
        }

        IsMatched = true;
        IsReady = true;
        bool isRestoreSavedSession = _connectionPurpose == ConnectionPurpose.RestoreSavedSession;
        FinishReconnectState();

        new WebSocketNetworkAdaptor(_ra2Demo, _currentWebSocket);

        if (isRestoreSavedSession)
        {
            CompleteSavedSessionBattleGame(data.CampId);
            Frame.DispatchEvent(new ReconnectGameStartEvent());
        }

        zUDebug.Log($"[NetworkManager] 重连成功：roomId={data.RoomId}, campId={data.CampId}, serverFrame={data.CurrentFrame}");
    }

    /// <summary>
    /// 重连失败事件处理，清理会话并返回匹配页。
    /// </summary>
    /// <param name="data">服务器返回的重连失败数据。</param>
    private void OnReconnectFailed(ReconnectFailedData data)
    {
        HandleReconnectFailed(data.Reason);
    }

    /// <summary>
    /// 游戏开始事件处理，确认第 0 帧并打开正常战斗流程。
    /// </summary>
    /// <param name="fileName">服务器下发的本局历史帧持久化文件名。</param>
    private void OnGameStart(string fileName)
    {
        IsReady = true;

        Frame.DispatchEvent(new GameStartEvent(fileName));

        zUDebug.Log("[NetworkManager] 游戏开始");
        if (_ra2Demo.GetBattleGame() != null && _ra2Demo.GetBattleGame().FrameSyncManager != null)
        {
            _ra2Demo.GetBattleGame().FrameSyncManager.ConfirmFrame(0, new List<ICommand>());
        }

        zUDebug.Log("[Ra2Demo] 游戏开始，帧同步已启动");
        _ra2Demo.MoveCameraToOurFactory();
    }

    /// <summary>
    /// 获取当前客户端已处理的最后帧号。
    /// </summary>
    /// <returns>当前帧同步管理器的本地帧号；没有运行中游戏时返回 0。</returns>
    private int GetLastProcessedFrame()
    {
        return _ra2Demo?.GetBattleGame()?.FrameSyncManager?.CurrentFrame ?? 0;
    }

    /// <summary>
    /// 尝试从当前连接状态进入自动重连流程。
    /// </summary>
    /// <param name="reason">触发重连的原因，用于日志。</param>
    private void TryStartAutoReconnect(string reason)
    {
        if (_isReconnectRunning || !CanReconnectCurrentGame())
        {
            return;
        }

        _isReconnectRunning = true;
        _connectionPurpose = ConnectionPurpose.Reconnect;

        zUDebug.LogWarning($"[NetworkManager] 开始自动重连，原因: {reason}, lastFrame={GetLastProcessedFrame()}");
        OnReconnectStarted?.Invoke();
        ReplaceCurrentWebSocket(CreateWebSocket(_serverUrl));
        _currentWebSocket.Connect();
    }

    /// <summary>
    /// 结束当前重连状态并通知界面隐藏重连提示，避免成功、失败和主动退出路径重复分发。
    /// </summary>
    private void FinishReconnectState()
    {
        if (!_isReconnectRunning)
        {
            return;
        }

        _isReconnectRunning = false;
        OnReconnectFinished?.Invoke();
    }

    /// <summary>
    /// 判断当前状态是否允许自动重连。
    /// </summary>
    /// <returns>当前为已匹配的网络对局且存在房间凭证时返回 true。</returns>
    private bool CanReconnectCurrentGame()
    {
        return IsMatched &&
               _ra2Demo != null &&
               _ra2Demo.Mode == GameMode.NetworkClient &&
               !string.IsNullOrEmpty(_serverUrl) &&
               !string.IsNullOrEmpty(_roomId) &&
               !string.IsNullOrEmpty(_token);
    }

    /// <summary>
    /// 按 PlayerPrefs 中保存的会话创建网络模式 BattleGame 基础上下文。
    /// </summary>
    private void PrepareBattleGameForSavedSession()
    {
        // 重置重置全局随机状态快照
        zRandom.ResetGlobalDeterminismStateRaw(0L);

        _ra2Demo.Mode = GameMode.NetworkClient;
        _ra2Demo.SetBattleGame(new BattleGame(_ra2Demo.Mode, 20, 0, _playerNumber));
        _ra2Demo.GetBattleGame().Init();
        _ra2Demo.InitializeUnityView();
    }

    /// <summary>
    /// 在跨启动重连成功后补全本地阵营、创建初始世界并启动记录工具。
    /// </summary>
    /// <param name="campId">服务器确认的本地玩家阵营 ID。</param>
    private void CompleteSavedSessionBattleGame(int campId)
    {
        var battleGame = _ra2Demo.GetBattleGame();
        if (battleGame == null || battleGame.World == null)
        {
            zUDebug.LogError("[NetworkManager] 跨启动重连成功，但 BattleGame 未初始化");
            return;
        }

        if (!battleGame.World.ComponentManager.HasGlobalComponent<GlobalInfoComponent>())
        {
            battleGame.World.ComponentManager.AddGlobalComponent(new GlobalInfoComponent(campId));
        }

        battleGame.CreateWorldByConfig();
        StartReplayRecording(_playerNumber);
        StartDeterminismProbe(battleGame, "Reconnect");
    }

    /// <summary>
    /// 统一处理重连失败，清理会话、关闭连接并返回匹配页。
    /// </summary>
    /// <param name="reason">重连失败原因。</param>
    private void HandleReconnectFailed(string reason)
    {
        zUDebug.LogWarning($"[NetworkManager] 重连失败，返回匹配页。Reason={reason}");
        FinishReconnectState();
        ClearReconnectSession();
        CloseCurrentWebSocket();
        Frame.DispatchEvent(new RestartGameEvent());
    }

    /// <summary>
    /// 根据匹配初始状态推断玩法人数。
    /// </summary>
    /// <param name="initialState">服务器返回的初始状态。</param>
    /// <returns>玩法人数，1 表示单人，2 表示双人。</returns>
    private int GetPlayerNumberByInitialState(JObject initialState)
    {
        if (initialState == null)
        {
            return 1;
        }

        if (initialState.Count == 3)
        {
            return 2;
        }

        return 1;
    }

    /// <summary>
    /// 保存重连会话到内存和 PlayerPrefs。
    /// </summary>
    /// <param name="roomId">匹配成功返回的房间 ID。</param>
    /// <param name="token">匹配成功返回的玩家 Token。</param>
    /// <param name="playerNumber">当前玩法人数。</param>
    private void SaveReconnectSession(string roomId, string token, int playerNumber)
    {
        _roomId = roomId;
        _token = token;
        _playerNumber = playerNumber;

        PlayerPrefs.SetInt(PREF_HAS_SESSION, 1);
        PlayerPrefs.SetString(PREF_ROOM_ID, _roomId);
        PlayerPrefs.SetString(PREF_TOKEN, _token);
        PlayerPrefs.SetInt(PREF_PLAYER_NUMBER, _playerNumber);
        PlayerPrefs.SetString(PREF_SERVER_URL, _serverUrl);
        PlayerPrefs.SetString(PREF_ROOM_TYPE, _roomType.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 从 PlayerPrefs 读取重连会话到内存。
    /// </summary>
    /// <returns>读取到完整会话时返回 true，否则返回 false。</returns>
    private bool LoadReconnectSessionFromPrefs()
    {
        if (!HasSavedReconnectSession())
        {
            return false;
        }

        _roomId = PlayerPrefs.GetString(PREF_ROOM_ID, string.Empty);
        _token = PlayerPrefs.GetString(PREF_TOKEN, string.Empty);
        _playerNumber = PlayerPrefs.GetInt(PREF_PLAYER_NUMBER, 1);
        _serverUrl = PlayerPrefs.GetString(PREF_SERVER_URL, REMOTE_SERVER_URL);
        string roomTypeText = PlayerPrefs.GetString(PREF_ROOM_TYPE, RoomType.DUO.ToString());
        if (!Enum.TryParse(roomTypeText, out _roomType))
        {
            _roomType = RoomType.DUO;
        }

        return true;
    }

    /// <summary>
    /// 开始记录网络对局命令，用于后续回放。
    /// </summary>
    /// <param name="playerNumber">当前玩法人数，用于写入回放文件名。</param>
    private void StartReplayRecording(int playerNumber)
    {
        // TODO: 暂时关闭本地回放功能，服务器端回放功能已实现
        // string fileName = $"replay_{playerNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        // _ra2Demo.GetBattleGame().World.CommandManager.StartRecording(fileName);
    }

    /// <summary>
    /// 启动确定性探针，生成网络对局帧摘要日志。
    /// </summary>
    /// <param name="battleGame">需要记录帧摘要的战斗游戏实例。</param>
    /// <param name="tag">日志文件名中的场景标签。</param>
    private void StartDeterminismProbe(BattleGame battleGame, string tag)
    {
        var probe = battleGame?.World?.DeterminismProbe;
        if (probe == null)
        {
            return;
        }

        string fileName = $"determinism_probe_{tag}_{DateTime.Now:yyyyMMdd_HHmmss}.jsonl";
        string path = System.IO.Path.Combine(Application.persistentDataPath, fileName);
        probe.Start(path, truncate: true);
        zUDebug.Log($"[NetworkManager] DeterminismProbe 已启动: {path}");
    }
}
