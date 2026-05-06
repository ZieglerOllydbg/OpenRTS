# Command系统设置指南

## 🎯 完整更新说明

你的项目已经完成了从直接操作改为Command驱动的重大升级！

### ✅ 已完成的工作

1. ✅ **Command系统核心架构**
   - `ICommand` 接口
   - `BaseCommand` 基类
   - `CommandManager` 命令管理器
   - `CommandBuffer` 命令缓冲区

2. ✅ **内置命令**
   - `CreateUnitCommand` - 创建单位
   - `MoveCommand` - 移动单位

3. ✅ **集成到游戏世界**
   - `zWorld` 集成 `CommandManager`
   - `GameWorldBridge` 提供命令提交接口
   - `ViewEventListener` 监听逻辑层事件创建视图

4. ✅ **示例代码**
   - `Test.cs` - 本地输入示例（已更新）
   - `AICommandExample.cs` - AI决策示例
   - `NetworkCommandExample.cs` - 网络同步示例

---

## 📝 Unity场景设置（更新版）

### 1. 基础场景设置（同之前）

创建地面、相机等基础对象（参考之前的 SETUP_TEST.md）

### 2. 配置 GameWorldBridge（重要更新）

1. 选中 `GameWorld` 对象
2. 在 Inspector 中找到 `GameWorldBridge` 组件
3. **新增配置项**：
   - **Unit Prefabs**：数组大小设为 10
   - **Index 1**: 拖入 `UnitCube` 预制体（动员兵）
   - **Index 2**: 拖入坦克预制体（如果有）
   - **Index 3**: 拖入矿车预制体（如果有）

   > ⚠️ 注意：数组索引对应单位类型ID！

### 3. 配置 Test 组件（更新）

1. 选中 `TestManager` 对象
2. 找到 `Test` 组件
3. **新的配置项**：
   - `World Bridge`: 拖入 GameWorld 对象
   - `Ground Layer`: Everything
   - `Player Id`: 0
   - **`Unit Type`**: 1 (1=动员兵, 2=坦克, 3=矿车)
   - **`Prefab Id`**: 1 (对应 GameWorldBridge.unitPrefabs[1])

   > 💡 移除了 `Unit Cube Prefab` 字段，现在统一由 GameWorldBridge 管理

---

## 🎮 新的操作方式

### 本地玩家操作

| 操作 | 效果 |
|------|------|
| **左键点击地面** | 创建单位（通过 CreateUnitCommand） |
| **右键点击地面** | 所有己方单位移动到点击位置（通过 MoveCommand） |
| **鼠标拖拽** | 平移相机 |
| **滚轮** | 缩放相机 |

### 代码示例：创建单位

```csharp
// 旧方式（已废弃）
// var entity = worldBridge.EntityFactory.CreateUnit(...);

// 新方式（使用Command）
var cmd = new CreateUnitCommand(
    playerId: 0,
    unitType: 1,
    position: clickPosition.ToZVector3(),
    prefabId: 1
);
worldBridge.SubmitCommand(cmd);
```

### 代码示例：移动单位

```csharp
var cmd = new MoveCommand(
    playerId: 0,
    entityIds: new[] { 1, 2, 3 },
    targetPosition: targetPos.ToZVector3()
);
worldBridge.SubmitCommand(cmd);
```

---

## 🤖 AI玩家设置（可选）

### 1. 添加AI管理器

1. 创建空对象：`AIManager`
2. 添加脚本：`AICommandExample`
3. 配置：
   - `World Bridge`: 拖入 GameWorld
   - `AI Player Id`: 1
   - `AI Think Interval`: 2.0

### 2. 运行测试

- 运行游戏，AI会每2秒自动创建单位并随机移动
- AI创建的单位会标记为 `Player 1`
- Console 会显示 `[AI]` 前缀的日志

---

## 🌐 网络同步准备（高级）

### 网络命令工作流程

```
┌──────────┐               ┌──────────┐
│ 玩家A    │               │ 玩家B    │
└────┬─────┘               └────┬─────┘
     │                          │
     │ 1. 创建命令              │
     │ cmd.ExecuteFrame=Frame+3 │
     │                          │
     │ 2. 本地提交              │
     ├──>CommandManager         │
     │                          │
     │ 3. 发送到网络────────────>│
     │                     4. 接收
     │                     5. 提交到本地
     │                     CommandManager<─┤
     │                          │
     │    6. Frame+3到达         │
     ├──>CommandManager.Execute │
     │                     同时执行
     │                     CommandManager.Execute<─┤
     │                          │
```

### 使用示例

```csharp
// 在 NetworkCommandExample.cs 中
public void SendLocalCommand(ICommand cmd)
{
    // 1. 计算执行帧
    cmd.ExecuteFrame = currentFrame + networkDelay;
    
    // 2. 序列化并发送
    byte[] data = cmd.Serialize();
    NetworkClient.Send(data);
    
    // 3. 本地也提交
    worldBridge.SubmitCommand(cmd);
}
```

---

## 📊 调试和监控

### 1. Console日志

运行游戏时会看到：
```
[CommandManager] Tick 100: 执行了 1 个命令
[Test] 提交创建单位命令: 类型=1, 位置=(5,0,5), 玩家=0
[CreateUnitCommand] 玩家0 创建了单位类型1 在位置(5,0,5)，Entity ID: 0
[ViewEventListener] 为 Entity_0 创建了视图对象
```

### 2. Scene视图可视化

打开 Scene 视图可以看到：
- 🟢 **绿色球体**：点击位置
- 🔵 **青色方框**：单位的逻辑位置
- 🟡 **黄色连线**：单位的移动目标

### 3. Inspector监控

选中 GameWorld 对象可以看到：
- 当前逻辑帧数
- 待执行命令数量
- 已创建实体数量

---

## 🔧 常见问题

### Q1: 点击创建单位，但没有反应？

**检查清单**：
1. `GameWorldBridge.unitPrefabs[prefabId]` 是否已分配？
2. Console 是否有错误信息？
3. `Test.prefabId` 是否对应 `GameWorldBridge.unitPrefabs` 的索引？

### Q2: 单位创建了但看不见？

**可能原因**：
1. 预制体的 Y 坐标不对（应该 > 0）
2. 相机位置或角度不对
3. 检查 Console 是否有 `[ViewEventListener]` 的日志

### Q3: 右键移动不工作？

**检查**：
1. 是否先创建了单位？
2. 单位的 `PlayerId` 是否匹配 `Test.playerId`？
3. Console 是否显示 `[Test] 发送移动命令`？

### Q4: 想改变创建的单位类型？

在 `Test` 组件中修改：
- `Unit Type`: 1=动员兵, 2=坦克, 3=矿车
- `Prefab Id`: 对应 `GameWorldBridge.unitPrefabs` 的索引

---

## 🚀 下一步扩展

### 1. 添加更多命令

```csharp
// 攻击命令
public class AttackCommand : BaseCommand { ... }

// 建造命令
public class BuildCommand : BaseCommand { ... }

// 生产命令
public class ProduceUnitCommand : BaseCommand { ... }
```

### 2. 完善AI

```csharp
// 矿车采集AI
public class HarvesterAI : MonoBehaviour
{
    void Update()
    {
        if (ShouldGather())
            SubmitCommand(new GatherCommand(...));
        
        if (IsFull())
            SubmitCommand(new ReturnCommand(...));
    }
}
```

### 3. 网络集成

使用 Mirror、Photon 或 Netcode for GameObjects：
```csharp
[Command]
void CmdSendGameCommand(byte[] commandData)
{
    // 反序列化
    var cmd = Deserialize(commandData);
    
    // 提交到所有客户端
    RpcReceiveGameCommand(commandData);
}
```

---

## 📚 相关文档

- [Command系统详细文档](../Packages/ZLockstep/Runtime/Sync/Command/README_COMMAND_SYSTEM.md)
- [架构设计文档](../Packages/ZLockstep/Runtime/ARCHITECTURE.md)
- [使用指南](../Packages/ZLockstep/Runtime/View/README_USAGE.md)

---

## 🎉 总结

现在你的项目完全基于Command驱动：
- ✅ **确定性**：所有操作通过Command，保证可复现
- ✅ **可扩展**：支持本地、AI、网络多种输入源
- ✅ **可回放**：记录Command即可回放整局游戏
- ✅ **易调试**：清晰的Command日志，便于追踪问题

开始享受Command系统带来的强大功能吧！🚀

