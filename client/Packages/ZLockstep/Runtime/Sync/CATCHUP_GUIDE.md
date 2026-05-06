# 追帧（CatchUp）功能使用指南

## 概述

追帧（CatchUp）是帧同步游戏中的重要功能，用于处理**断线重连**场景。当玩家断线后重新连接，客户端需要快速"追赶"到当前游戏进度。

## 工作原理

```
正常游戏：
  客户端: Frame 100 → 101 → 102 → ...
  服务器: Frame 100 → 101 → 102 → ...

掉线（5秒）：
  客户端: Frame 100 (停止)
  服务器: Frame 100 → 101 → ... → 200 (继续推进)

重连后：
  客户端: Frame 100
  服务器: Frame 200
  差距: 100 帧

追帧：
  客户端快速执行 Frame 101-200
  FixedUpdate 中每帧执行 10 个逻辑帧
  10 个 FixedUpdate 后追上
```

## 核心机制

### 禁用渲染 + 追帧后重新同步

追帧期间的性能优化策略：

```
追帧期间（Frame 100-200）：
  1. PresentationSystem.Enabled = false （停止创建GameObject）
  2. 快速执行 100 帧逻辑
  3. 逻辑层正常推进（Entity创建、移动等）
  4. 表现层完全停止

追帧完成后：
  1. PresentationSystem.Enabled = true （重新启用）
  2. 调用 ResyncAllEntities()：
     a) 遍历所有逻辑实体
     b) 为缺失ViewComponent的实体创建GameObject
     c) 强制同步已有ViewComponent的位置
  3. 恢复正常渲染
```

**关键代码**：

```csharp
// GameWorldBridge.OnCatchUpComplete()
private void OnCatchUpComplete()
{
    if (_presentationSystem != null)
    {
        // 1. 重新启用表现系统
        _presentationSystem.Enabled = true;

        // 2. 恢复插值设置
        _presentationSystem.EnableSmoothInterpolation = enableSmoothInterpolation;

        // 3. 重新同步所有实体的视图（最重要！）
        _presentationSystem.ResyncAllEntities();
    }
}
```

## 使用方法

### 1. 配置 GameWorldBridge

```csharp
[Header("追帧设置")]
[SerializeField] private int catchUpFramesPerUpdate = 10; // 每帧执行10个逻辑帧
[SerializeField] private bool disableRenderingDuringCatchUp = true; // 禁用渲染提升性能
```

### 2. 检测断线重连

```csharp
void OnReconnect()
{
    // 1. 获取客户端当前帧
    int clientFrame = worldBridge.LogicWorld.Tick;
    
    // 2. 请求服务器当前帧和历史命令
    RequestCatchUpData(clientFrame);
}
```

### 3. 接收服务器数据

```csharp
void OnReceiveCatchUpData(int serverFrame, Dictionary<int, List<ICommand>> commands)
{
    // commands 格式：
    // {
    //   101: [cmd1, cmd2],
    //   102: [cmd3],
    //   103: [],
    //   ...
    // }
    
    // 开始追帧
    worldBridge.StartCatchUp(commands);
}
```

### 4. 自动追赶

追帧会自动进行，无需额外操作：

```csharp
// GameWorldBridge 的 FixedUpdate 中
if (_game.IsCatchingUp)
{
    // 快速执行多帧
    for (int i = 0; i < catchUpFramesPerUpdate; i++)
    {
        _game.Update(); // 执行一个逻辑帧
    }
}
```

### 5. 监听追帧完成

```csharp
void Update()
{
    if (wasInCatchUp && !worldBridge.Game.IsCatchingUp)
    {
        // 追帧完成
        OnCatchUpComplete();
    }
    
    wasInCatchUp = worldBridge.Game.IsCatchingUp;
}
```

## 完整示例

### 客户端代码

```csharp
using UnityEngine;
using System.Collections.Generic;
using ZLockstep.View;
using ZLockstep.Sync.Command;

public class ReconnectManager : MonoBehaviour
{
    [SerializeField] private GameWorldBridge worldBridge;
    [SerializeField] private NetworkClient networkClient;
    
    private bool _isReconnecting = false;
    
    void OnDisconnected()
    {
        zUDebug.Log("断线了！");
        _isReconnecting = true;
    }
    
    void OnReconnected()
    {
        zUDebug.Log("重连成功，开始追帧");
        
        // 1. 获取当前帧
        int clientFrame = worldBridge.LogicWorld.Tick;
        
        // 2. 请求追帧数据
        networkClient.RequestCatchUpData(clientFrame, OnReceiveCatchUpData);
    }
    
    void OnReceiveCatchUpData(Dictionary<int, List<ICommand>> catchUpCommands)
    {
        zUDebug.Log($"收到追帧数据，共 {catchUpCommands.Count} 帧");
        
        // 3. 开始追帧
        worldBridge.StartCatchUp(catchUpCommands);
        
        _isReconnecting = false;
    }
    
    void Update()
    {
        // 显示追帧进度
        if (worldBridge.Game.IsCatchingUp)
        {
            float progress = worldBridge.Game.GetCatchUpProgress();
            zUDebug.Log($"追帧进度: {progress:P0}");
        }
    }
}
```

### 服务器代码（伪代码）

```csharp
public class LockstepServer
{
    // 保存历史命令
    private Dictionary<int, List<ICommand>> _commandHistory = new();
    private int _maxHistoryFrames = 600; // 保存30秒
    
    void OnClientRequestCatchUp(int playerId, int clientFrame)
    {
        int serverFrame = _currentFrame;
        int missedFrames = serverFrame - clientFrame;
        
        // 检查是否可以追帧
        if (missedFrames > _maxHistoryFrames)
        {
            SendMessage(playerId, new ReconnectFailedMessage
            {
                Reason = "Too many missed frames, please rejoin"
            });
            return;
        }
        
        // 收集历史命令
        var catchUpData = new Dictionary<int, List<ICommand>>();
        for (int frame = clientFrame + 1; frame <= serverFrame; frame++)
        {
            if (_commandHistory.TryGetValue(frame, out var commands))
            {
                catchUpData[frame] = commands;
            }
            else
            {
                // 空帧
                catchUpData[frame] = new List<ICommand>();
            }
        }
        
        // 发送给客户端
        SendMessage(playerId, new CatchUpDataMessage
        {
            StartFrame = clientFrame + 1,
            EndFrame = serverFrame,
            Commands = catchUpData
        });
        
        zUDebug.Log($"发送追帧数据给玩家 {playerId}，范围: {clientFrame + 1} - {serverFrame}");
    }
    
    void OnCommandExecuted(int frame, List<ICommand> commands)
    {
        // 保存到历史
        _commandHistory[frame] = commands;
        
        // 清理过期历史
        int oldestFrame = frame - _maxHistoryFrames;
        if (_commandHistory.ContainsKey(oldestFrame))
        {
            _commandHistory.Remove(oldestFrame);
        }
    }
}
```

## 性能优化

### 1. 调整追帧速度

```csharp
// 根据掉线时长动态调整
int missedFrames = catchUpCommands.Count;

if (missedFrames < 50)
{
    catchUpFramesPerUpdate = 5;  // 慢速追帧
}
else if (missedFrames < 200)
{
    catchUpFramesPerUpdate = 10; // 中速追帧
}
else
{
    catchUpFramesPerUpdate = 20; // 快速追帧
}
```

### 2. 禁用渲染

```csharp
[SerializeField] private bool disableRenderingDuringCatchUp = true;

// 追帧时自动禁用整个表现系统（不创建GameObject，不同步位置）
if (disableRenderingDuringCatchUp)
{
    _presentationSystem.Enabled = false;
}

// 追帧完成后重新启用并同步
_presentationSystem.Enabled = true;
_presentationSystem.ResyncAllEntities(); // 重新创建所有缺失的视图
```

### 3. 进度显示

```csharp
void OnGUI()
{
    if (worldBridge.Game.IsCatchingUp)
    {
        float progress = worldBridge.Game.GetCatchUpProgress();
        int pending = worldBridge.Game.FrameSyncManager.GetPendingFrameCount();
        
        GUILayout.Label($"同步中... {progress:P0}");
        GUILayout.Label($"剩余 {pending} 帧");
    }
}
```

## 注意事项

### 1. 最大掉线时长

```csharp
// 服务器应该设置最大历史保存时长
private int _maxHistoryFrames = 600; // 30秒 @ 20FPS

// 超过此时长，拒绝追帧，要求重新加入
if (missedFrames > _maxHistoryFrames)
{
    return "Please rejoin the game";
}
```

### 2. 内存管理

```csharp
// 服务器：循环缓冲区，自动清理旧命令
void CleanupOldHistory()
{
    int oldestFrame = _currentFrame - _maxHistoryFrames;
    _commandHistory.Remove(oldestFrame);
}
```

### 3. 网络优化

```csharp
// 压缩命令数据
byte[] CompressCommands(Dictionary<int, List<ICommand>> commands)
{
    // 1. 序列化
    // 2. 压缩（如 GZip）
    // 3. 返回字节流
}
```

## 测试

### 使用 CatchUpExample.cs

1. 在场景中添加 `CatchUpExample` 组件
2. 设置 GameWorldBridge 的 GameMode = NetworkClient
3. 运行游戏
4. 按 `C` 键触发追帧测试

```csharp
// 测试代码会：
// 1. 生成 50 帧的模拟命令
// 2. 调用 worldBridge.StartCatchUp()
// 3. 自动快速执行这 50 帧
// 4. 显示进度条
```

### 检查追帧效果

```
追帧前: Frame 100
追帧中: Frame 101, 102, 103... (快速推进)
追帧后: Frame 150
```

## API 参考

### Game

```csharp
// 开始追帧
public void StartCatchUp(Dictionary<int, List<ICommand>> catchUpCommands)

// 是否正在追帧
public bool IsCatchingUp { get; }

// 获取追帧进度 (0-1)
public float GetCatchUpProgress()
```

### FrameSyncManager

```csharp
// 批量确认多帧
public void ConfirmFrames(Dictionary<int, List<ICommand>> frameCommandsMap)

// 是否还有待执行的帧
public bool HasPendingFrames()

// 获取待执行的帧数
public int GetPendingFrameCount()
```

### GameWorldBridge

```csharp
// 开始追帧（对外接口）
public void StartCatchUp(Dictionary<int, List<ICommand>> catchUpCommands)

// 追帧配置
[SerializeField] private int catchUpFramesPerUpdate = 10;
[SerializeField] private bool disableRenderingDuringCatchUp = true;
```

## 故障排查

### 问题：追帧太慢

**解决**：增加 `catchUpFramesPerUpdate`

```csharp
catchUpFramesPerUpdate = 20; // 每帧执行 20 个逻辑帧
```

### 问题：追帧时卡顿

**解决**：启用渲染禁用

```csharp
disableRenderingDuringCatchUp = true;
```

### 问题：追帧后状态不一致

**原因**：可能是命令缺失或顺序错误

**解决**：
1. 检查服务器是否发送了所有帧
2. 检查命令是否按确定性顺序排序
3. 添加哈希校验

```csharp
// 追帧完成后校验
int clientHash = CalculateStateHash();
RequestServerHash(currentFrame, clientHash);
```

### 问题：追帧后GameObject缺失或位置错误

**原因**：追帧期间禁用了渲染，没有创建GameObject

**解决**：`ResyncAllEntities()` 会自动处理

```csharp
// GameWorldBridge.OnCatchUpComplete() 中会自动调用
_presentationSystem.ResyncAllEntities();

// 此方法会：
// 1. 遍历所有逻辑实体
// 2. 为缺失ViewComponent的实体创建GameObject
// 3. 强制同步已有ViewComponent的位置
```

**手动触发重新同步**（通常不需要）：

```csharp
// 如果需要手动重新同步
worldBridge.Game.World.GetSystem<PresentationSystem>().ResyncAllEntities();
```

---

**文档版本**: v1.0  
**最后更新**: 2025-10-23  
**维护者**: ZLockstep团队

