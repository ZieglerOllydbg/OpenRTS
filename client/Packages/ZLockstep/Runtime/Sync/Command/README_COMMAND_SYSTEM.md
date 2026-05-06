# Command系统使用指南

## 概述

Command系统是帧同步游戏的核心，所有游戏操作都通过Command来驱动，确保：
- ✅ **确定性**：相同的命令序列产生相同的结果
- ✅ **可回放**：记录命令即可回放游戏
- ✅ **网络同步**：命令在指定帧执行，保证多端一致
- ✅ **统一接口**：本地、AI、网络使用相同的命令系统

## 核心架构

```
┌────────────┐     ┌────────────┐     ┌────────────┐
│ 本地输入   │     │    AI      │     │  网络消息  │
└─────┬──────┘     └─────┬──────┘     └─────┬──────┘
      │                  │                  │
      │                  │                  │
      ▼                  ▼                  ▼
┌──────────────────────────────────────────────┐
│           CommandManager                      │
│  - SubmitCommand()                           │
│  - ExecuteFrame()                            │
│  - 按帧号排序和执行命令                       │
└───────────────┬──────────────────────────────┘
                │
                ▼
┌───────────────────────────────────────────────┐
│           游戏逻辑层 (zWorld)                  │
│  - ECS组件更新                                │
│  - System执行                                 │
│  - 确定性计算                                 │
└───────────────────────────────────────────────┘
```

## 快速开始

### 1. 创建命令

```csharp
using ZLockstep.Sync.Command.Commands;
using zUnity;

// 创建单位命令
var createCommand = new CreateUnitCommand(
    playerId: 0,
    unitType: 1, // 1=动员兵
    position: new zVector3(0, 0, 0),
    prefabId: 1
)
{
    Source = CommandSource.Local
};
```

### 2. 提交命令

```csharp
// 通过GameWorldBridge提交
worldBridge.SubmitCommand(createCommand);

// 或直接提交到CommandManager
world.CommandManager.SubmitCommand(createCommand);
```

### 3. 命令自动执行

命令会在逻辑帧更新时自动执行：
```csharp
// zWorld.Update() 中会调用
CommandManager.ExecuteFrame();  // 执行当前帧的所有命令
```

## 内置命令

### CreateUnitCommand - 创建单位

```csharp
var cmd = new CreateUnitCommand(
    playerId: 0,
    unitType: 2, // 2=坦克
    position: new zVector3(10, 0, 5),
    prefabId: 2  // 对应GameWorldBridge.unitPrefabs[2]
);
```

### MoveCommand - 移动单位

```csharp
var cmd = new MoveCommand(
    playerId: 0,
    entityIds: new[] { 1, 2, 3 }, // 要移动的实体ID数组
    targetPosition: new zVector3(20, 0, 20),
    stopDistance: (zfloat)0.5f
);
```

## 命令来源

```csharp
public enum CommandSource
{
    Local = 0,   // 本地玩家输入
    AI = 1,      // AI决策
    Network = 2, // 网络同步
    Replay = 3   // 回放
}
```

每个命令都会标记来源，便于调试和统计。

## 高级用法

### 延迟执行（用于网络同步）

```csharp
var cmd = new MoveCommand(...);
cmd.ExecuteFrame = currentFrame + 3; // 3帧后执行
worldBridge.SubmitCommand(cmd);
```

CommandManager会自动在第`ExecuteFrame`帧执行该命令。

### 使用CommandBuffer

```csharp
// 创建缓冲区
var buffer = new CommandBuffer(world.CommandManager);

// 添加多个命令
buffer.Add(createCommand1);
buffer.Add(createCommand2);
buffer.Add(moveCommand);

// 一次性提交所有命令
buffer.Submit();
```

### 启用命令历史（用于回放）

```csharp
// 在初始化时启用
world.CommandManager.RecordHistory = true;

// 游戏结束后获取历史
var history = world.CommandManager.GetCommandHistory();

// 回放时重新提交这些命令
foreach (var cmd in history)
{
    world.CommandManager.SubmitCommand(cmd);
}
```

## 自定义命令

### 1. 创建命令类

```csharp
using ZLockstep.Sync.Command;
using ZLockstep.Simulation;

public class AttackCommand : BaseCommand
{
    public override int CommandType => CommandTypes.Attack;

    public int AttackerEntityId { get; set; }
    public int TargetEntityId { get; set; }

    public AttackCommand(int playerId, int attackerId, int targetId)
        : base(playerId)
    {
        AttackerEntityId = attackerId;
        TargetEntityId = targetId;
    }

    public override void Execute(zWorld world)
    {
        var attacker = new Entity(AttackerEntityId);
        var target = new Entity(TargetEntityId);

        // 实现攻击逻辑
        if (world.ComponentManager.HasComponent<AttackComponent>(attacker))
        {
            var attackComp = world.ComponentManager.GetComponent<AttackComponent>(attacker);
            attackComp.TargetEntityId = TargetEntityId;
            world.ComponentManager.AddComponent(attacker, attackComp);
        }
    }
}
```

### 2. 注册命令类型

在`CommandTypes.cs`中添加：
```csharp
public const int Attack = 3;
```

### 3. 使用自定义命令

```csharp
var attackCmd = new AttackCommand(
    playerId: 0,
    attackerId: 1,
    targetId: 5
);
worldBridge.SubmitCommand(attackCmd);
```

## AI示例

```csharp
public class SimpleAI : MonoBehaviour
{
    [SerializeField] private GameWorldBridge worldBridge;
    [SerializeField] private int aiPlayerId = 1;

    void Update()
    {
        if (ShouldCreateUnit())
        {
            var cmd = new CreateUnitCommand(
                aiPlayerId, 2, GetRandomPosition(), 2
            ) { Source = CommandSource.AI };
            
            worldBridge.SubmitCommand(cmd);
        }
    }
}
```

## 网络同步示例

```csharp
public class NetworkSync
{
    private GameWorldBridge _worldBridge;
    private int _localPlayerId;

    // 发送本地命令到网络
    public void SendCommand(ICommand cmd)
    {
        // 1. 计算执行帧（补偿网络延迟）
        int currentFrame = _worldBridge.LogicWorld.Tick;
        cmd.ExecuteFrame = currentFrame + 3; // 假设3帧延迟

        // 2. 序列化
        byte[] data = cmd.Serialize();

        // 3. 发送到网络
        NetworkClient.Send(data);

        // 4. 本地也提交（保证一致性）
        _worldBridge.SubmitCommand(cmd);
    }

    // 接收网络命令
    public void OnReceiveCommand(byte[] data)
    {
        // 1. 反序列化
        var cmd = DeserializeCommand(data);

        // 2. 提交到CommandManager
        // 会在cmd.ExecuteFrame帧自动执行
        _worldBridge.SubmitCommand(cmd);
    }
}
```

## 最佳实践

### ✅ 推荐做法

1. **所有游戏操作都通过Command**
   ```csharp
   // ✅ 好
   worldBridge.SubmitCommand(new MoveCommand(...));
   
   // ❌ 坏：直接修改组件
   ComponentManager.AddComponent(entity, new MoveCommandComponent(...));
   ```

2. **Command只包含输入数据，不包含状态**
   ```csharp
   // ✅ 好
   public class MoveCommand {
       public zVector3 TargetPosition;
   }
   
   // ❌ 坏：包含运行时状态
   public class MoveCommand {
       public float ElapsedTime; // 运行时状态，不应该在这里
   }
   ```

3. **网络同步使用延迟执行**
   ```csharp
   cmd.ExecuteFrame = currentFrame + networkDelayFrames;
   ```

4. **启用历史记录用于调试和回放**
   ```csharp
   #if UNITY_EDITOR
   world.CommandManager.RecordHistory = true;
   #endif
   ```

### ❌ 避免的错误

1. **不要在Command中访问Unity组件**
   ```csharp
   // ❌ 错误
   public override void Execute(zWorld world)
   {
       transform.position = ...; // Command在逻辑层，不能访问Unity
   }
   ```

2. **不要在Command中使用随机数**
   ```csharp
   // ❌ 错误
   var randomPos = Random.Range(-10, 10); // 不确定性
   
   // ✅ 正确
   var randomPos = world.Random.Range(-10, 10); // 使用确定性随机
   ```

3. **不要跳过CommandManager直接修改状态**
   ```csharp
   // ❌ 错误
   ComponentManager.AddComponent(...); // 绕过命令系统
   
   // ✅ 正确
   SubmitCommand(new CreateUnitCommand(...)); // 通过命令系统
   ```

## 调试技巧

### 1. 查看命令执行情况

```csharp
zUDebug.Log($"待执行命令数: {world.CommandManager.GetPendingCommandCount()}");
```

### 2. 查看命令历史

```csharp
var history = world.CommandManager.GetCommandHistory();
foreach (var cmd in history)
{
    zUDebug.Log($"Frame {cmd.ExecuteFrame}: {cmd.GetType().Name} by Player {cmd.PlayerId}");
}
```

### 3. 在编辑器中可视化命令

```csharp
[CustomEditor(typeof(GameWorldBridge))]
public class GameWorldBridgeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        var bridge = target as GameWorldBridge;
        if (bridge.LogicWorld != null)
        {
            int pending = bridge.LogicWorld.CommandManager.GetPendingCommandCount();
            EditorGUILayout.LabelField("Pending Commands", pending.ToString());
        }
    }
}
```

## 性能考虑

1. **批量提交命令**：使用`CommandBuffer`批量提交
2. **避免频繁序列化**：只在网络发送时序列化
3. **历史记录按需启用**：回放和调试时才启用

## 参考示例

- `Assets/Scripts/Test.cs` - 本地输入示例
- `Assets/Scripts/Examples/AICommandExample.cs` - AI示例
- `Assets/Scripts/Examples/NetworkCommandExample.cs` - 网络同步示例

---

更多信息请参考：
- [Architecture文档](../../ARCHITECTURE.md)
- [View使用指南](../../View/README_USAGE.md)

