using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json.Linq;
using ZLockstep.Sync.Command;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.RA2.Client
{
    /// <summary>
    /// WebSocket 网络客户端，负责连接服务器、发送协议消息、分发服务器回调，并维护基于 Unity 主线程时间的心跳检测。
    /// </summary>
    public class WebSocketClient : IDisposable
    {
        private WebSocket webSocket;
        private string clientId;
        private bool isConnected = false;
        private bool matched = false;
        private bool gameStarted = false;
        
        private bool heartbeatRunning;
        private float nextPingTime;
        private DateTime lastPongTime;
        private DateTime lastPingTime; // 记录发送ping的时间
        private long currentPing = -1; // 当前ping值（毫秒），-1表示未计算
        private readonly float pingIntervalSeconds = 3f; // 3秒发送一次ping
        private readonly int pongTimeout = 10000; // 10秒内没收到pong认为断开
        
        // 移除了消息队列，直接使用事件
        public event Action<string> OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string> OnError;
        public event Action<MatchSuccessData> OnMatchSuccess;

        /// <summary>
        /// 重连成功事件，服务器校验房间身份并绑定新连接后触发。
        /// </summary>
        public event Action<ReconnectSuccessData> OnReconnectSuccess;

        /// <summary>
        /// 重连失败事件，服务器拒绝恢复房间身份时触发。
        /// </summary>
        public event Action<ReconnectFailedData> OnReconnectFailed;

        /// <summary>
        /// 游戏开始事件，参数为服务器下发的本局历史帧持久化文件名。
        /// </summary>
        public event Action<string> OnGameStart;
        public event Action<FrameSyncData> OnFrameSync;

        /// <summary>
        /// 历史帧文件开始返回事件，携带文件元数据与是否已结束标记。
        /// </summary>
        public event Action<HistoryFramesStartData> OnHistoryFramesStart;

        /// <summary>
        /// 历史帧文件批次返回事件，携带本批次历史帧消息。
        /// </summary>
        public event Action<HistoryFramesFrameBatchData> OnHistoryFramesFrame;

        /// <summary>
        /// 历史帧文件请求失败事件，携带服务器返回的失败原因。
        /// </summary>
        public event Action<HistoryFramesFailedData> OnHistoryFramesFailed;

        public event Action OnPingTimeout; // 新增：Ping超时事件
        public event Action<long> OnPingUpdated; // 新增：Ping值更新事件
        private bool pingTimeoutReported;

        /// <summary>
        /// 创建 WebSocket 网络客户端，并注册底层 WebSocket 事件回调。
        /// </summary>
        /// <param name="serverUrl">服务器 WebSocket 地址。</param>
        /// <param name="clientId">当前客户端标识，用于协议字段和日志输出。</param>
        public WebSocketClient(string serverUrl, string clientId)
        {
            this.clientId = clientId;
            this.webSocket = new WebSocket(serverUrl);

            // 初始化心跳相关组件
            lastPongTime = DateTime.UtcNow;
            lastPingTime = DateTime.UtcNow; // 初始化ping时间
            currentPing = -1; // 初始化ping值为未计算状态
            
            // 注册事件处理器
            webSocket.OnOpen += HandleWebSocketOpen;
            webSocket.OnMessage += HandleWebSocketMessage;
            webSocket.OnClose += HandleWebSocketClose;
            webSocket.OnError += HandleWebSocketError;
        }

        /// <summary>
        /// 启动心跳检测，并安排下一次 Ping 在 Unity 主线程更新中发送。
        /// </summary>
        public void StartHeartbeat()
        {
            heartbeatRunning = true;
            nextPingTime = Time.realtimeSinceStartup + pingIntervalSeconds;
            zUDebug.LogInfo($"[{clientId}] 心跳检测已启动");
        }

        /// <summary>
        /// 停止心跳检测，后续帧更新不会再主动发送 Ping 或触发心跳超时判断。
        /// </summary>
        public void StopHeartbeat()
        {
            heartbeatRunning = false;
            zUDebug.LogInfo($"[{clientId}] 心跳检测已停止");
        }

        /// <summary>
        /// 在连接可用时向服务器发送 Ping 消息，并记录发送时间用于计算延迟。
        /// </summary>
        public void SendPing()
        {
            if (IsConnected)
            {
                // 记录发送ping的时间
                lastPingTime = DateTime.UtcNow;
                Ping ping = new()
                {
                    type = "ping"
                };

                string json = JsonConvert.SerializeObject(ping);
                SendMessage(json);
                zUDebug.LogInfo($"[{clientId}] 发送ping");
            }
        }

        /// <summary>
        /// 开始连接服务器，连接完成后由 WebSocket 回调启动后续协议流程。
        /// </summary>
        public async void Connect()
        {
            if (!isConnected)
            {
                await webSocket.Connect();
            }
        }

        /// <summary>
        /// 主动关闭当前 WebSocket 连接。
        /// </summary>
        public async void Disconnect()
        {
            if (isConnected)
            {
                await webSocket.Close();
            }
        }
        
        public void SendMatchRequest()
        {
            SendMatchRequest(RoomType.DUO); // 默认为双人房间
        }
        
        public void SendMatchRequest(RoomType roomType)
        {
            var request = new JObject
            {
                ["type"] = "match",
                ["data"] = new JObject
                {
                    ["name"] = clientId,
                    ["roomType"] = roomType.ToString()
                }
            };
            
            SendMessage(request.ToString());
            zUDebug.LogInfo($"[{clientId}] 发送匹配请求: {request}");
        }

        public void SendReady()
        {
            var request = new JObject
            {
                ["type"] = "ready"
            };

            SendMessage(request.ToString());
            zUDebug.LogInfo($"[{clientId}] 发送准备就绪");
        }

        /// <summary>
        /// 发送重连房间请求，用匹配成功时获得的房间凭证恢复玩家身份。
        /// </summary>
        /// <param name="roomId">匹配成功时返回的房间 ID。</param>
        /// <param name="token">匹配成功时返回的玩家 Token。</param>
        /// <param name="lastFrame">客户端已处理的最后帧，服务器会从下一帧开始补发。</param>
        public void SendReconnectRequest(string roomId, string token, int lastFrame)
        {
            var request = new JObject
            {
                ["type"] = "reconnect",
                ["data"] = new JObject
                {
                    ["roomId"] = roomId,
                    ["token"] = token,
                    ["lastFrame"] = lastFrame
                }
            };

            SendMessage(request.ToString());
            zUDebug.LogInfo($"[{clientId}] 发送重连请求: roomId={roomId}, lastFrame={lastFrame}");
        }

        /// <summary>
        /// 发送取消匹配请求，用于玩家仍处于匹配等待阶段时通知服务器退出匹配队列。
        /// </summary>
        /// <returns>表示取消匹配消息发送过程的异步任务。</returns>
        public async Task SendCancelMatchRequestAsync()
        {
            var request = new JObject
            {
                ["type"] = "cancelMatch"
            };

            await SendMessageAsync(request.ToString());
            zUDebug.LogInfo($"[{clientId}] 发送取消匹配请求: {request}");
        }

        /// <summary>
        /// 发送历史帧文件请求，要求服务器按文件名读取已持久化的历史帧 JSON 文件。
        /// </summary>
        /// <param name="fileName">服务器历史帧文件名，只允许基础 JSON 文件名。</param>
        public void SendHistoryFramesRequest(string fileName)
        {
            var request = new JObject
            {
                ["type"] = "getHistoryFrames",
                ["data"] = new JObject
                {
                    ["fileName"] = fileName
                }
            };

            SendMessage(request.ToString());
            zUDebug.LogInfo($"[{clientId}] 发送历史帧请求: {request}");
        }

        public class Ping
        {
            public string type;
        }

        public class FrameInput
        {
            public string type;
            public int frame;
            
            public List<MyCommand> data;
        }
        
        public class MyCommand
        {
            public int commandType;
            public ICommand command;
        }
        
        public void SendFrameInput(int frame, ICommand command)
        {
            string json = Utils.FrameInputProcessor.SerializeFrameInput(frame, command);
            
            SendMessage(json);
            zUDebug.LogInfo($"[{clientId}] 发送第 {frame} 帧输入数据: {json}");
        }
        
        /// <summary>
        /// 发送文本消息但不要求调用方等待完成，用于普通实时协议消息。
        /// </summary>
        /// <param name="message">需要发送到服务器的 JSON 文本。</param>
        private async void SendMessage(string message)
        {
            await SendMessageAsync(message);
        }

        /// <summary>
        /// 发送文本消息并允许调用方等待底层 WebSocket 发送完成。
        /// </summary>
        /// <param name="message">需要发送到服务器的 JSON 文本。</param>
        /// <returns>表示 WebSocket 文本发送过程的异步任务。</returns>
        private async Task SendMessageAsync(string message)
        {
            if (isConnected && webSocket.State == WebSocketState.Open)
            {
                await webSocket.SendText(message);
            }
        }

        /// <summary>
        /// 分发 WebSocket 消息队列，并在 Unity 主线程帧更新中推进心跳和超时检测。
        /// </summary>
        public void DispatchMessageQueue()
        {
            // WebGL平台不需要手动分发消息队列，基于回调机制
#if !UNITY_WEBGL || UNITY_EDITOR
            webSocket?.DispatchMessageQueue();
#endif
            UpdateHeartbeat();
            // 检查pong超时
            CheckPongTimeout();
        }

        /// <summary>
        /// 根据 Unity 实时时间推进心跳计时，到达发送间隔后主动发送一次 Ping。
        /// </summary>
        private void UpdateHeartbeat()
        {
            if (!heartbeatRunning || !isConnected)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (now < nextPingTime)
            {
                return;
            }

            SendPing();
            nextPingTime = now + pingIntervalSeconds;
        }

        /// <summary>
        /// 检查 Pong 响应是否超过允许等待时间，超时后只上报一次心跳超时事件。
        /// </summary>
        private void CheckPongTimeout()
        {
            if (isConnected && heartbeatRunning && !pingTimeoutReported)
            {
                var elapsed = DateTime.UtcNow - lastPongTime;
                if (elapsed.TotalMilliseconds > pongTimeout)
                {
                    pingTimeoutReported = true;
                    zUDebug.LogInfo($"[{clientId}] 心跳超时，连接可能已断开");
                    OnPingTimeout?.Invoke();
                }
            }
        }
        
        // WebSocket事件处理
        private void HandleWebSocketOpen()
        {
            isConnected = true;
            pingTimeoutReported = false;
            zUDebug.LogInfo($"[{clientId}] 连接建立成功");
            
            OnConnected?.Invoke("连接建立成功");
            
            // 启动心跳检测
            StartHeartbeat();
            
            // 注意：不再在连接成功后立即发送匹配请求
            // 而是由调用者决定何时发送匹配请求
        }
        
        private void HandleWebSocketMessage(byte[] data)
        {
            string message = Encoding.UTF8.GetString(data);
            ProcessMessage(message);
        }
        
        private void ProcessMessage(string message)
        {
            JObject json;
            try
            {
                json = JObject.Parse(message);
            }
            catch (Exception e)
            {
                zUDebug.LogError($"解析消息时发生错误: {e.Message}，message：{message}");
                return;
            }

            string messageType = json["type"]?.ToString();
            switch (messageType)
            {
                case "matchSuccess":
                    HandleMatchSuccess(json);
                    break;
                case "reconnectSuccess":
                    HandleReconnectSuccess(json);
                    break;
                case "reconnectFailed":
                    HandleReconnectFailed(json);
                    break;
                case "gameStart":
                    HandleGameStart(json);
                    break;
                case "frameSync":
                    HandleFrameSync(json);
                    break;
                case "historyFramesStart":
                    HandleHistoryFramesStart(json);
                    break;
                case "historyFramesFrame":
                    HandleHistoryFramesFrame(json);
                    break;
                case "historyFramesFailed":
                    HandleHistoryFramesFailed(json);
                    break;
                case "pong":
                    HandlePong();
                    break;
                default:
                    zUDebug.LogWarning($"[{clientId}] 收到未知消息类型: {messageType}");
                    break;
            }
        }
        
        // 处理pong消息
        private void HandlePong()
        {
            lastPongTime = DateTime.UtcNow;
            pingTimeoutReported = false;
            
            // 计算ping值（毫秒）
            currentPing = (long)(lastPongTime - lastPingTime).TotalMilliseconds;
            
            // 触发ping更新事件
            OnPingUpdated?.Invoke(currentPing);
        }
        
        private void HandleMatchSuccess(JObject message)
        {
            matched = true;
            
            string roomId = message["roomId"]?.ToString();
            int campId = message["yourCampId"]?.ToObject<int>() ?? 0;
            string token = message["yourToken"]?.ToString();
            var initialState = message["initialState"] as JObject; // 解析初始状态
    
            zUDebug.LogInfo($"[{clientId}] 匹配成功. 房间ID: {roomId}, CampID: {campId}, Token: {token}. message: {message}");
            
            var matchData = new MatchSuccessData
            {
                RoomId = roomId,
                CampId = campId,
                Token = token,
                InitialState = initialState // 保存初始状态
            };
            
            OnMatchSuccess?.Invoke(matchData);
        }

        /// <summary>
        /// 处理服务器返回的重连成功消息，并向上层传递房间、阵营和服务器当前帧。
        /// </summary>
        /// <param name="message">服务器下发的 reconnectSuccess JSON 消息。</param>
        private void HandleReconnectSuccess(JObject message)
        {
            matched = true;
            gameStarted = true;

            var reconnectData = new ReconnectSuccessData
            {
                RoomId = message["roomId"]?.ToString(),
                CampId = message["yourCampId"]?.ToObject<int>() ?? 0,
                CurrentFrame = message["currentFrame"]?.ToObject<int>() ?? 0
            };

            zUDebug.LogInfo($"[{clientId}] 重连成功. 房间ID: {reconnectData.RoomId}, CampID: {reconnectData.CampId}, CurrentFrame: {reconnectData.CurrentFrame}");
            OnReconnectSuccess?.Invoke(reconnectData);
        }

        /// <summary>
        /// 处理服务器返回的重连失败消息，并向上层传递失败原因。
        /// </summary>
        /// <param name="message">服务器下发的 reconnectFailed JSON 消息。</param>
        private void HandleReconnectFailed(JObject message)
        {
            var reconnectData = new ReconnectFailedData
            {
                Reason = message["reason"]?.ToString()
            };

            zUDebug.LogWarning($"[{clientId}] 重连失败. Reason: {reconnectData.Reason}");
            OnReconnectFailed?.Invoke(reconnectData);
        }
        
        /// <summary>
        /// 处理服务器下发的游戏开始消息，并提取本局历史帧持久化文件名。
        /// </summary>
        /// <param name="message">服务器下发的 gameStart JSON 消息。</param>
        private void HandleGameStart(JObject message)
        {
            gameStarted = true;
            string fileName = message["fileName"]?.ToString();
            zUDebug.LogInfo($"[{clientId}] 游戏开始，历史帧文件名: {fileName}");
            
            OnGameStart?.Invoke(fileName);
        }
        
        private void HandleFrameSync(JObject message)
        {
            int frame = message["frame"]?.ToObject<int>() ?? 0;
            
            var frameData = new FrameSyncData
            {
                Frame = frame,
                Data = message
            };
            
            OnFrameSync?.Invoke(frameData);
        }

        /// <summary>
        /// 处理历史帧文件元数据开始消息，并把基础信息传递给上层请求流程。
        /// </summary>
        /// <param name="message">服务器下发的 historyFramesStart JSON 消息。</param>
        private void HandleHistoryFramesStart(JObject message)
        {
            JObject metadata = message["data"] as JObject;
            var startData = new HistoryFramesStartData
            {
                FileName = message["fileName"]?.ToString() ?? metadata?["fileName"]?.ToString(),
                RoomId = metadata?["roomId"]?.ToString(),
                PlayerCount = metadata?["playerCount"]?.ToObject<int>() ?? 0,
                Players = metadata?["players"] as JArray,
                Data = metadata,
                IsEnd = message["isEnd"]?.ToObject<bool>() ?? false
            };

            OnHistoryFramesStart?.Invoke(startData);
        }

        /// <summary>
        /// 处理历史帧文件帧批次消息，并把当前批次的帧号与 frameSync 文本传递给上层请求流程。
        /// </summary>
        /// <param name="message">服务器下发的 historyFramesFrame JSON 消息。</param>
        private void HandleHistoryFramesFrame(JObject message)
        {
            var batchData = new HistoryFramesFrameBatchData
            {
                FileName = message["fileName"]?.ToString(),
                Frames = new List<HistoryFrameEntryData>(),
                IsEnd = message["isEnd"]?.ToObject<bool>() ?? false
            };

            var frames = message["frames"] as JArray;
            if (frames != null)
            {
                foreach (JToken frameToken in frames)
                {
                    batchData.Frames.Add(new HistoryFrameEntryData
                    {
                        Frame = frameToken["frame"]?.ToObject<int>() ?? 0,
                        Message = frameToken["message"]?.ToString()
                    });
                }
            }

            OnHistoryFramesFrame?.Invoke(batchData);
        }

        /// <summary>
        /// 处理历史帧文件请求失败消息，并把失败文件名与原因传递给上层请求流程。
        /// </summary>
        /// <param name="message">服务器下发的 historyFramesFailed JSON 消息。</param>
        private void HandleHistoryFramesFailed(JObject message)
        {
            var failedData = new HistoryFramesFailedData
            {
                FileName = message["fileName"]?.ToString(),
                Reason = message["reason"]?.ToString()
            };

            OnHistoryFramesFailed?.Invoke(failedData);
        }
        
        private void HandleWebSocketClose(WebSocketCloseCode closeCode)
        {
            isConnected = false;
            string reason = GetCloseCodeDescription(closeCode);
            zUDebug.LogInfo($"[{clientId}] 连接关闭: {reason}");
            
            // 停止心跳检测
            StopHeartbeat();
            
            OnDisconnected?.Invoke(reason);
        }
        
        private void HandleWebSocketError(string errorMsg)
        {
            zUDebug.LogError($"[{clientId}] WebSocket错误: {errorMsg}");
            OnError?.Invoke(errorMsg);
        }
        
        private string GetCloseCodeDescription(WebSocketCloseCode code)
        {
            switch (code)
            {
                case WebSocketCloseCode.Normal: return "正常关闭";
                case WebSocketCloseCode.Abnormal: return "异常关闭";
                case WebSocketCloseCode.ProtocolError: return "协议错误";
                case WebSocketCloseCode.UnsupportedData: return "无效数据";
                case WebSocketCloseCode.PolicyViolation: return "策略违规";
                case WebSocketCloseCode.TooBig: return "消息过大";
                case WebSocketCloseCode.MandatoryExtension: return "需要扩展";
                case WebSocketCloseCode.ServerError: return "服务器错误";
                case WebSocketCloseCode.TlsHandshakeFailure: return "TLS握手失败";
                default: return $"未知代码: {(int)code}";
            }
        }

        /// <summary>
        /// 释放客户端持有的心跳状态和底层 WebSocket 连接。
        /// </summary>
        public void Dispose()
        {
            // 停止心跳检测
            StopHeartbeat();
            webSocket?.Close();
        }

        /// <summary>
        /// 获取当前 Ping 值，单位为毫秒，尚未计算时返回 -1。
        /// </summary>
        public long CurrentPing => currentPing;

        /// <summary>
        /// 获取当前 WebSocket 是否已经连接并处于打开状态。
        /// </summary>
        public bool IsConnected => isConnected && webSocket?.State == WebSocketState.Open;

        /// <summary>
        /// 获取当前客户端是否已经完成匹配。
        /// </summary>
        public bool IsMatched => matched;

        /// <summary>
        /// 获取当前客户端是否已经收到游戏开始消息。
        /// </summary>
        public bool IsGameStarted => gameStarted;
    }
    
    public struct MatchSuccessData
    {
        public string RoomId;
        public int CampId;
        public string Token;
        public JObject InitialState; // 添加初始状态数据
    }

    /// <summary>
    /// 重连成功数据，记录服务器确认的房间身份和当前房间帧号。
    /// </summary>
    public struct ReconnectSuccessData
    {
        /// <summary>
        /// 重连成功的房间 ID，用于校验客户端恢复的会话是否仍指向原房间。
        /// </summary>
        public string RoomId;

        /// <summary>
        /// 当前客户端在房间中的阵营 ID，用于恢复本地阵营组件。
        /// </summary>
        public int CampId;

        /// <summary>
        /// 服务器当前房间帧号，用于日志和追帧状态观察。
        /// </summary>
        public int CurrentFrame;
    }

    /// <summary>
    /// 重连失败数据，记录服务器拒绝恢复房间身份的原因。
    /// </summary>
    public struct ReconnectFailedData
    {
        /// <summary>
        /// 重连失败原因，例如 room_not_found、token_invalid 或 room_destroyed。
        /// </summary>
        public string Reason;
    }
    
    public struct FrameSyncData
    {
        public int Frame;
        public JObject Data;
    }

    /// <summary>
    /// 历史帧文件元数据，保存服务器开始返回历史帧时下发的基础信息。
    /// </summary>
    public struct HistoryFramesStartData
    {
        /// <summary>
        /// 获取服务器历史帧文件名。
        /// </summary>
        public string FileName;

        /// <summary>
        /// 获取历史帧所属房间 ID。
        /// </summary>
        public string RoomId;

        /// <summary>
        /// 获取历史帧记录的玩家数量。
        /// </summary>
        public int PlayerCount;

        /// <summary>
        /// 获取历史帧记录的玩家列表原始 JSON。
        /// </summary>
        public JArray Players;

        /// <summary>
        /// 获取不包含 frames 字段的历史帧元数据原始 JSON。
        /// </summary>
        public JObject Data;

        /// <summary>
        /// 获取当前开始消息是否已经是本次历史帧回发的最后消息。
        /// </summary>
        public bool IsEnd;
    }

    /// <summary>
    /// 历史帧批次数据，保存服务器按批次返回的 frameSync 消息列表。
    /// </summary>
    public struct HistoryFramesFrameBatchData
    {
        /// <summary>
        /// 获取服务器历史帧文件名。
        /// </summary>
        public string FileName;

        /// <summary>
        /// 获取当前批次包含的历史帧条目。
        /// </summary>
        public List<HistoryFrameEntryData> Frames;

        /// <summary>
        /// 获取当前批次是否为本次历史帧回发的最后消息。
        /// </summary>
        public bool IsEnd;
    }

    /// <summary>
    /// 单条历史帧数据，保存帧号和服务器持久化的 frameSync 消息文本。
    /// </summary>
    public struct HistoryFrameEntryData
    {
        /// <summary>
        /// 获取历史帧帧号。
        /// </summary>
        public int Frame;

        /// <summary>
        /// 获取服务器持久化的 frameSync JSON 文本。
        /// </summary>
        public string Message;
    }

    /// <summary>
    /// 历史帧请求完整结果，保存开始元数据和全部批次合并后的帧列表。
    /// </summary>
    public struct HistoryFramesReplayData
    {
        /// <summary>
        /// 获取服务器返回的历史帧文件元数据。
        /// </summary>
        public HistoryFramesStartData StartData;

        /// <summary>
        /// 获取服务器返回的全部历史帧条目。
        /// </summary>
        public List<HistoryFrameEntryData> Frames;
    }

    /// <summary>
    /// 历史帧请求失败数据，保存服务器拒绝或读取失败的原因。
    /// </summary>
    public struct HistoryFramesFailedData
    {
        /// <summary>
        /// 获取请求失败的历史帧文件名。
        /// </summary>
        public string FileName;

        /// <summary>
        /// 获取服务器返回的失败原因。
        /// </summary>
        public string Reason;
    }
    
    // 房间类型枚举
    public enum RoomType
    {
        SOLO = 1,   // 1人房间
        DUO = 2,    // 2人房间
        TRIO = 3,   // 3人房间
        QUAD = 4,   // 4人房间
        OCTO = 8    // 8人房间
    }
    
// 阵营颜色枚举已移至独立文件 CampColor.cs 中
}
