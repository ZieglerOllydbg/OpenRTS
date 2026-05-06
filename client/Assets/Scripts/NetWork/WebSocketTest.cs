using UnityEngine;
using Game.RA2.Client;
using System;

public class WebSocketTest : MonoBehaviour
{
    private WebSocketClient client;
    private string playerName = "Player1";
    private string status = "未连接";
    private string matchInfo = "";
    private Vector2 scrollPosition;
    private string logText = "";
    private string serverUrl = "ws://101.126.136.178:8080/ws";
    
    private void Start()
    {
        // 加载之前保存的玩家名字
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            playerName = PlayerPrefs.GetString("PlayerName");
        }
        
        AddLog("客户端已启动");
    }
    
    private void Update()
    {
        // 每帧分发WebSocket消息
        client?.DispatchMessageQueue();
    }
    
    private void OnDestroy()
    {
        client?.Dispose();
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 400, 600));
        
        // 标题
        GUILayout.Label("网络测试客户端", new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold });
        
        GUILayout.Space(20);
        
        // 状态显示
        GUILayout.Label($"状态: {status}", new GUIStyle(GUI.skin.label) { fontSize = 14 });
        
        GUILayout.Space(10);
        
        // 玩家名称输入
        GUILayout.Label("角色名:");
        string newName = GUILayout.TextField(playerName, GUILayout.Width(200));
        if (newName != playerName)
        {
            playerName = newName;
            // 保存玩家名字到PlayerPrefs
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
        }
        
        GUILayout.Space(10);
        
        // 连接/断开按钮
        if (client == null || !client.IsConnected)
        {
            if (GUILayout.Button("连接服务器", GUILayout.Width(200), GUILayout.Height(30)))
            {
                Connect();
            }
        }
        else
        {
            if (GUILayout.Button("断开连接", GUILayout.Width(200), GUILayout.Height(30)))
            {
                Disconnect();
            }
        }
        
        GUILayout.Space(20);
        
        // 匹配按钮
        if (client != null && client.IsConnected && !client.IsMatched)
        {
            if (GUILayout.Button("开始匹配", GUILayout.Width(200), GUILayout.Height(40)))
            {
                client.SendMatchRequest();
                status = "匹配中...";
                AddLog("开始匹配...");
            }
        }
        
        // 准备按钮
        if (client != null && client.IsMatched && !client.IsGameStarted)
        {
            GUILayout.Label(matchInfo);
            if (GUILayout.Button("准备游戏", GUILayout.Width(200), GUILayout.Height(40)))
            {
                client.SendReady();
                status = "已准备，等待游戏开始";
                AddLog("发送准备就绪");
            }
        }
        
        // 游戏状态显示
        if (client != null && client.IsGameStarted)
        {
            GUILayout.Label("游戏进行中...", new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.green } });
        }
        
        GUILayout.Space(20);
        
        // 日志区域
        GUILayout.Label("操作日志:");
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(380), GUILayout.Height(300));
        GUILayout.TextArea(logText, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();
        
        // 清空日志按钮
        if (GUILayout.Button("清空日志", GUILayout.Width(100)))
        {
            logText = "";
        }
        
        GUILayout.EndArea();
    }
    
    private void Connect()
    {
        if (string.IsNullOrEmpty(playerName))
        {
            status = "请输入角色名";
            return;
        }
        
        // 创建WebSocketClient并连接
        client = new WebSocketClient(serverUrl, playerName);
        
        // 注册事件
        client.OnConnected += OnClientConnected;
        client.OnDisconnected += OnClientDisconnected;
        client.OnError += OnClientError;
        client.OnMatchSuccess += OnClientMatchSuccess;
        client.OnGameStart += OnClientGameStart;
        client.OnFrameSync += OnClientFrameSync;
        
        client.Connect();
        status = "连接中...";
        AddLog($"开始连接服务器，角色名: {playerName}");
    }
    
    private void Disconnect()
    {
        client?.Disconnect();
        client = null;
        status = "已断开连接";
        matchInfo = "";
        AddLog("断开连接");
    }
    
    private void AddLog(string log)
    {
        logText = $"[{System.DateTime.Now:HH:mm:ss}] {log}\n{logText}";
    }
    
    // WebSocketClient 事件回调
    private void OnClientConnected(string msg)
    {
        status = $"已连接: {msg}";
        AddLog($"连接成功: {msg}");
    }
    
    private void OnClientDisconnected(string msg)
    {
        status = $"连接断开: {msg}";
        AddLog($"连接断开: {msg}");
    }
    
    private void OnClientError(string msg)
    {
        status = $"错误: {msg}";
        AddLog($"错误: {msg}");
    }
    
    private void OnClientMatchSuccess(MatchSuccessData data)
    {
        matchInfo = $"匹配成功!\n房间: {data.RoomId}\n阵营: {data.CampId}\nToken: {data.Token}";
        status = "匹配成功，请准备";
        AddLog($"匹配成功: Room={data.RoomId}, Camp={data.CampId}, Token={data.Token}");
    }
    
    /// <summary>
    /// 处理测试客户端收到的游戏开始事件，并把服务器历史帧文件名写入调试日志。
    /// </summary>
    /// <param name="fileName">服务器下发的本局历史帧持久化文件名。</param>
    private void OnClientGameStart(string fileName)
    {
        status = "游戏开始!";
        AddLog($"游戏开始! 历史帧文件名: {fileName}");
    }
    
    private void OnClientFrameSync(FrameSyncData data)
    {
        AddLog($"收到帧同步: Frame={data.Frame}");
    }
}
