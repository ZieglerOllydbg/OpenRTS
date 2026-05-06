# 帧同步（Lockstep）实现指南

> 回放不同步定位工具请查看：[DETERMINISM_PROBE_GUIDE.md](./DETERMINISM_PROBE_GUIDE.md)

## ═══════════════════════════════════════════════════════════
## 核心原理
## ═══════════════════════════════════════════════════════════

### 什么是帧同步？

**帧同步（Lockstep）** 是一种多人游戏同步技术：
- 所有玩家在**同一逻辑帧**执行**相同的命令**
- 通过确定性计算，保证所有客户端的游戏状态完全一致
- 网络只需传输**玩家输入**（命令），而不是游戏状态

### 为什么需要"锁"？

**关键问题**：客户端不能随意推进逻辑帧，必须等待服务器确认。

```
错误示例（无锁）：
Frame 0  1  2  3  4  5
客户端A: ✓  ✓  ✓  ✓  ✓  ✓  (超前执行)
客户端B: ✓  ✓  ❌ (等待网络)
→ 结果：两个客户端状态不一致！

正确示例（有锁）：
Frame 0  1  2  3  4  5
客户端A: ✓  ✓  ⏳ ⏳ ✓  ✓  (等待确认)
客户端B: ✓  ✓  ⏳ ⏳ ✓  ✓  (等待确认)
服务器:  确认0 确认1 确认2...
→ 结果：两个客户端同步前进
```

---

## ═══════════════════════════════════════════════════════════
## 完整流程
## ═══════════════════════════════════════════════════════════

### Frame N 的执行流程

```
┌─────────────────────────────────────────────────────────┐
│ 1. 玩家操作                                              │
│    - 点击移动、发起攻击等                                │
│    - 创建 Command（如 MoveCommand）                      │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 2. 客户端提交命令                                        │
│    FrameSyncManager.SubmitLocalCommand(cmd)             │
│    → 发送到服务器                                        │
│    → 加入 pendingLocalCommands                           │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 3. 服务器收集命令                                        │
│    - 收集所有玩家的命令                                  │
│    - 即使某个玩家无操作，也要发送"空命令"                │
│    - 打包为 Frame N 的命令集                             │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 4. 服务器确认并广播                                      │
│    → 广播消息："Frame N confirmed"                       │
│    → 附带命令集：[cmd1, cmd2, cmd3...]                   │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 5. 客户端接收确认                                        │
│    OnReceiveFrameConfirmation(frame, commands)          │
│    FrameSyncManager.ConfirmFrame(N, commands)           │
│    → 更新 confirmedFrame = N                             │
│    → 存储命令到 frameCommands[N]                         │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 6. 客户端执行逻辑帧                                      │
│    FixedUpdate()                                        │
│    → Game.Update()                                      │
│    → FrameSyncManager.CanAdvanceFrame()                 │
│    → 检查：currentFrame + 1 <= confirmedFrame ?         │
│       ├─ 是：PrepareNextFrame() → ExecuteLogicFrame()  │
│       └─ 否：等待（本次FixedUpdate不执行）              │
└─────────────────────────────────────────────────────────┘
```

---

## ═══════════════════════════════════════════════════════════
## 核心类：FrameSyncManager
## ═══════════════════════════════════════════════════════════

### 关键字段

```csharp
// 服务器已确认的最大帧号
private int _confirmedFrame = -1;

// 客户端当前执行到的帧号
private int _currentFrame = -1;

// 未来帧的命令缓冲区
private Dictionary<int, List<ICommand>> _frameCommands;
```

### 关键方法

#### 1. `CanAdvanceFrame()` - 检查是否可以推进

```csharp
public bool CanAdvanceFrame()
{
    int nextFrame = _currentFrame + 1;
    
    // 检查下一帧是否已确认
    if (nextFrame > _confirmedFrame)
    {
        // 未确认，等待
        return false;
    }
    
    return true; // 可以推进
}
```

**关键逻辑**：
- `nextFrame > confirmedFrame` → 等待（返回false）
- `nextFrame <= confirmedFrame` → 可以推进（返回true）

#### 1.5. `PrepareNextFrame()` - 准备下一帧

```csharp
public int PrepareNextFrame()
{
    int nextFrame = _currentFrame + 1;
    
    // 从缓冲区取出命令并提交到 CommandManager
    if (_frameCommands.TryGetValue(nextFrame, out var commands))
    {
        // 按确定性顺序提交
        commands.Sort(...);
        foreach (var cmd in commands)
        {
            _world.CommandManager.SubmitCommand(cmd);
        }
    }
    
    _currentFrame = nextFrame;
    return nextFrame;
}
```

**关键点**：
- 只准备命令，**不执行 world.Update()**
- 实际执行由 `Game.ExecuteLogicFrame()` 完成
- 保证 **所有模式都统一走 ExecuteLogicFrame()**

#### 2. `ConfirmFrame()` - 服务器确认帧

```csharp
public void ConfirmFrame(int frame, List<ICommand> commands)
{
    // 更新确认帧
    _confirmedFrame = frame;
    
    // 存储命令
    _frameCommands[frame] = commands;
}
```

**调用时机**：
- 网络层收到服务器消息时调用
- 由 `NetworkAdapter` 或 `NetworkManager` 调用

---

### Game 类的执行流程（重要！）

**所有模式都统一使用 `ExecuteLogicFrame()`**：

```csharp
// Game.Update() - NetworkClient 模式
public bool Update()
{
    // 1. 检查是否可以推进
    if (!FrameSyncManager.CanAdvanceFrame())
        return false; // 等待
    
    // 2. 准备命令（不执行 world.Update）
    FrameSyncManager.PrepareNextFrame();
    
    // 3. 执行逻辑帧（统一入口）
    ExecuteLogicFrame();
    
    return true;
}

// ExecuteLogicFrame - 所有模式共用
private void ExecuteLogicFrame()
{
    World.EventManager.Clear();  // ← 清空事件
    World.Update();              // ← 执行逻辑
}
```

**为什么这样设计？**
- ✅ 所有模式都走 `ExecuteLogicFrame()`，保证一致性
- ✅ `EventManager.Clear()` 在所有模式下都执行
- ✅ `FrameSyncManager` 只负责判断和准备，不直接执行
- ✅ 遵循分层原则：FrameSyncManager 不知道 EventManager

---

### Tick 同步机制（重要！）

**问题**：`world.Tick` 和 `FrameSyncManager._currentFrame` 必须同步！

```csharp
// 错误❌：TimeManager 自动递增
world.Update()
{
    TimeManager.Advance();  // Tick++，无条件
    CommandManager.ExecuteFrame();  // 使用 world.Tick
}

// 正确✅：锁帧系统控制 Tick
world.Update(targetTick: 5)
{
    TimeManager.SetTick(5);  // 明确设置为 5
    CommandManager.ExecuteFrame();  // 查找 _futureCommands[5]
}
```

**执行流程**：

```
网络模式：
1. FrameSyncManager.PrepareNextFrame()
   → nextFrame = 5
   → cmd.ExecuteFrame = 5
   → 提交到 _futureCommands[5]
   → _currentFrame = 5

2. Game.ExecuteLogicFrame(5)
   → World.Update(targetTick: 5)
   → TimeManager.SetTick(5)
   → world.Tick = 5 ✅

3. CommandManager.ExecuteFrame()
   → 使用 world.Tick = 5
   → 查找 _futureCommands[5] ✅
   → 执行命令

单机模式：
1. Game.ExecuteLogicFrame()
   → World.Update(targetTick: null)
   → TimeManager.Advance()
   → world.Tick++ ✅
```

#### 3. `SubmitLocalCommand()` - 提交本地命令

```csharp
public void SubmitLocalCommand(ICommand command, INetworkAdapter networkAdapter)
{
    // 发送到服务器
    networkAdapter.SendCommandToServer(command);
    
    // 加入待确认列表
    _pendingLocalCommands.Add(command);
}
```

---

## ═══════════════════════════════════════════════════════════
## 游戏模式
## ═══════════════════════════════════════════════════════════

### 模式对比

| 模式 | 是否等待 | 使用场景 | FrameSyncManager |
|-----|---------|---------|------------------|
| `Standalone` | ❌ 不等待 | 单机游戏 | ❌ 不创建 |
| `NetworkClient` | ✅ 等待确认 | 网络客户端 | ✅ 创建 |
| `NetworkServer` | ❌ 不等待 | 服务器 | ❌ 不创建 |
| `Replay` | ❌ 不等待 | 回放系统 | ❌ 不创建 |

### 代码示例

#### 单机模式

```csharp
// 创建单机游戏
var game = new Game(GameMode.Standalone, frameRate: 20, playerId: 0);
game.Init();

// 主循环
while (running)
{
    game.Update(); // 每帧都执行
}
```

#### 网络客户端模式

```csharp
// 创建网络客户端
var game = new Game(GameMode.NetworkClient, frameRate: 20, playerId: 0);
game.Init();

// 主循环
while (running)
{
    bool executed = game.Update(); 
    // executed = true → 执行了逻辑
    // executed = false → 等待服务器确认
}

// 接收服务器消息
void OnReceiveServerMessage(int frame, List<ICommand> commands)
{
    game.FrameSyncManager.ConfirmFrame(frame, commands);
}
```

---

## ═══════════════════════════════════════════════════════════
## 网络适配器（INetworkAdapter）
## ═══════════════════════════════════════════════════════════

### 接口定义

```csharp
public interface INetworkAdapter
{
    void SendCommandToServer(ICommand command);
}
```

### 实现示例（使用Unity Netcode）

```csharp
public class UnityNetworkAdapter : MonoBehaviour, INetworkAdapter
{
    public void SendCommandToServer(ICommand command)
    {
        // 序列化命令
        byte[] data = command.Serialize();
        
        // 发送到服务器
        NetworkManager.Singleton.SendToServer(data);
    }
}
```

### 实现示例（使用WebSocket）

```csharp
public class WebSocketAdapter : INetworkAdapter
{
    private WebSocket _socket;
    
    public void SendCommandToServer(ICommand command)
    {
        // 序列化为JSON
        string json = JsonUtility.ToJson(command);
        
        // 发送
        _socket.Send(json);
    }
}
```

---

## ═══════════════════════════════════════════════════════════
## 服务器实现（伪代码）
## ═══════════════════════════════════════════════════════════

### 服务器职责

1. **收集命令**：接收所有玩家的命令
2. **打包帧**：将命令打包为 Frame N
3. **广播确认**：告诉所有客户端"Frame N 可以执行了"

### 伪代码

```csharp
public class LockstepServer
{
    private int _currentFrame = 0;
    private Dictionary<int, List<ICommand>> _frameCommands = new();
    private List<PlayerConnection> _players = new();
    
    // 固定帧率更新（如20FPS）
    void FixedUpdate()
    {
        // 1. 收集当前帧的所有命令
        var commands = CollectCommandsForFrame(_currentFrame);
        
        // 2. 广播给所有客户端
        BroadcastFrameConfirmation(_currentFrame, commands);
        
        // 3. 推进服务器帧
        _currentFrame++;
    }
    
    // 收集命令
    List<ICommand> CollectCommandsForFrame(int frame)
    {
        var commands = new List<ICommand>();
        
        foreach (var player in _players)
        {
            // 获取该玩家在这一帧的命令
            var cmd = player.GetCommandForFrame(frame);
            if (cmd != null)
            {
                commands.Add(cmd);
            }
            // 如果没有命令，可以发送一个 EmptyCommand
        }
        
        return commands;
    }
    
    // 广播确认
    void BroadcastFrameConfirmation(int frame, List<ICommand> commands)
    {
        var message = new FrameConfirmationMessage
        {
            Frame = frame,
            Commands = commands
        };
        
        foreach (var player in _players)
        {
            player.Send(message);
        }
    }
}
```

---

## ═══════════════════════════════════════════════════════════
## 常见问题
## ═══════════════════════════════════════════════════════════

### Q1: 如果玩家网络延迟很高怎么办？

**A**: 服务器需要等待最慢的玩家。

**优化方案**：
1. **设置超时**：如果玩家超过N帧未响应，踢出或使用上一帧命令
2. **预测输入**：假设玩家会继续上一个操作
3. **动态帧率**：根据网络状况调整逻辑帧率

### Q2: 为什么不直接同步游戏状态？

**A**: 帧同步 vs 状态同步

| 方案 | 网络流量 | CPU | 适用场景 |
|-----|---------|-----|---------|
| 帧同步 | 低（只传输命令） | 高（每个客户端都计算） | RTS、MOBA |
| 状态同步 | 高（传输所有状态） | 低（只有服务器计算） | FPS、MMO |

**帧同步优势**：
- 网络流量极低
- 天然支持回放（录制命令序列）
- 反作弊（服务器可验证）

### Q3: confirmedFrame 和 currentFrame 的区别？

```
confirmedFrame: 服务器说"这些帧可以执行"
currentFrame:   客户端当前执行到哪一帧

示例：
confirmedFrame = 10
currentFrame = 7

含义：服务器已确认到第10帧，客户端执行到第7帧
下一次Update：可以执行 Frame 8
```

### Q4: 如何处理断线重连？

```csharp
void OnReconnect()
{
    // 1. 重置FrameSyncManager
    game.FrameSyncManager.Reset();
    
    // 2. 请求服务器当前状态
    RequestCurrentFrameFromServer();
    
    // 3. 服务器发送快照 + 未执行的命令
    OnReceiveSnapshot(int frame, GameState state, List<ICommand> pendingCommands)
    {
        // 加载快照
        LoadSnapshot(state);
        
        // 快速执行挂起的命令
        foreach (var cmd in pendingCommands)
        {
            game.FrameSyncManager.ConfirmFrame(cmd.ExecuteFrame, [cmd]);
        }
    }
}
```

### Q5: 如何测试锁帧逻辑？

```csharp
[Test]
public void TestFrameSync()
{
    // 创建两个客户端
    var client1 = new Game(GameMode.NetworkClient);
    var client2 = new Game(GameMode.NetworkClient);
    
    client1.Init();
    client2.Init();
    
    // 模拟服务器确认 Frame 0
    var cmd = new MoveCommand(...);
    client1.FrameSyncManager.ConfirmFrame(0, new List<ICommand> { cmd });
    client2.FrameSyncManager.ConfirmFrame(0, new List<ICommand> { cmd });
    
    // 两个客户端都执行
    bool result1 = client1.Update();
    bool result2 = client2.Update();
    
    // 断言：都成功执行
    Assert.IsTrue(result1);
    Assert.IsTrue(result2);
    
    // 断言：状态一致
    Assert.AreEqual(client1.World.Tick, client2.World.Tick);
}
```

---

## ═══════════════════════════════════════════════════════════
## 快速开始
## ═══════════════════════════════════════════════════════════

### 1. Unity场景配置

```
1. 创建空对象 "GameWorld"
2. 添加 GameWorldBridge 组件
3. 设置 Game Mode = NetworkClient
4. 设置 Logic Frame Rate = 20
5. 设置 Local Player Id = 0（玩家1为0，玩家2为1）
```

### 2. 实现网络适配器

```csharp
public class MyNetworkAdapter : MonoBehaviour, INetworkAdapter
{
    public void SendCommandToServer(ICommand command)
    {
        // TODO: 实现你的网络发送逻辑
        Debug.Log($"发送命令到服务器: {command}");
    }
}
```

### 3. 提交命令时传入网络适配器

```csharp
public class PlayerInput : MonoBehaviour
{
    [SerializeField] GameWorldBridge worldBridge;
    [SerializeField] MyNetworkAdapter networkAdapter;
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var cmd = new CreateUnitCommand(...);
            
            // 网络模式需要传入adapter
            worldBridge.Game.SubmitCommand(cmd, networkAdapter);
        }
    }
}
```

### 4. 接收服务器确认

```csharp
public class NetworkReceiver : MonoBehaviour
{
    [SerializeField] GameWorldBridge worldBridge;
    
    // 当收到服务器消息时调用
    void OnReceiveServerMessage(byte[] data)
    {
        // 解析消息
        var message = ParseMessage(data);
        
        // 确认帧
        worldBridge.Game.FrameSyncManager.ConfirmFrame(
            message.Frame,
            message.Commands
        );
    }
}
```

---

## ═══════════════════════════════════════════════════════════
## 调试技巧
## ═══════════════════════════════════════════════════════════

### 1. 显示帧同步状态

```csharp
void OnGUI()
{
    if (_game.Mode == GameMode.NetworkClient && _game.FrameSyncManager != null)
    {
        var status = _game.FrameSyncManager.GetStatusInfo();
        GUI.Label(new Rect(10, 10, 500, 30), status);
    }
}
```

### 2. 模拟网络延迟

```csharp
public class DelayedNetworkAdapter : INetworkAdapter
{
    private Queue<ICommand> _delayedCommands = new Queue<ICommand>();
    private float _delay = 0.5f; // 500ms延迟
    
    public void SendCommandToServer(ICommand command)
    {
        StartCoroutine(SendDelayed(command));
    }
    
    IEnumerator SendDelayed(ICommand command)
    {
        yield return new WaitForSeconds(_delay);
        ActuallySend(command);
    }
}
```

### 3. 记录帧日志

```csharp
void OnFrameExecuted(int frame)
{
    Debug.Log($"[Frame {frame}] Tick={world.Tick}, Entities={entityCount}");
}
```

---

**文档版本**: v1.0  
**最后更新**: 2025-10-23  
**维护者**: ZLockstep团队

