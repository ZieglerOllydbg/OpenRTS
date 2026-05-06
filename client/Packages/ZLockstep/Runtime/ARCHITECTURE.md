# ZLockstep 架构设计（三层架构）

> 遵循奥卡姆剃刀原则：简单的方案是最好的方案

## ═══════════════════════════════════════════════════════════
## 架构总览
## ═══════════════════════════════════════════════════════════

### 设计哲学

1. **严格分层**：每层只知道下层，不知道上层
2. **唯一zWorld**：整个游戏只有一个逻辑世界实例
3. **跨平台核心**：Game和zWorld可在任何C#环境运行
4. **确定性保证**：逻辑层使用zfloat/zVector3

### 三层结构

```
┌───────────────────────────────────────────────────────┐
│  【第3层：Unity桥接层】GameWorldBridge                 │
│  文件：View/GameWorldBridge.cs                        │
│  ─────────────────────────────────────────────────────│
│  职责：                                               │
│  • Unity平台适配（MonoBehaviour生命周期）            │
│  • Unity资源管理（预制体、场景对象）                 │
│  • Inspector配置界面                                 │
│  • 不知道游戏逻辑细节                                │
│  ─────────────────────────────────────────────────────│
│  依赖：持有Game实例                                  │
└──────────────────┬────────────────────────────────────┘
                   │
                   ▼
┌───────────────────────────────────────────────────────┐
│  【第2层：应用控制层】Game                             │
│  文件：Sync/Game.cs                                   │
│  ─────────────────────────────────────────────────────│
│  职责：                                               │
│  • 游戏生命周期管理（Init/Update/Shutdown）          │
│  • 创建并持有唯一的zWorld实例                        │
│  • 注册游戏系统                                      │
│  • 纯C#，跨平台（Unity/服务器/测试）                 │
│  • 不知道Unity，不知道表现                           │
│  ─────────────────────────────────────────────────────│
│  依赖：创建并驱动zWorld                              │
└──────────────────┬────────────────────────────────────┘
                   │
                   ▼
┌───────────────────────────────────────────────────────┐
│  【第1层：游戏逻辑核心】zWorld                         │
│  文件：Simulation/zWorld.cs                           │
│  ─────────────────────────────────────────────────────│
│  职责：                                               │
│  • 游戏状态容器（管理所有ECS数据）                   │
│  • ECS架构（Entity/Component/System）                 │
│  • 确定性计算（zfloat/zVector3）                      │
│  • 命令系统（Command → ECS → Event）                  │
│  • 不知道平台，不知道如何被驱动                       │
│  ─────────────────────────────────────────────────────│
│  依赖：无（完全独立）                                │
└───────────────────────────────────────────────────────┘
```

---

## ═══════════════════════════════════════════════════════════
## 数据流向
## ═══════════════════════════════════════════════════════════

### 完整流程

```
1. 用户输入（鼠标点击）
   ↓
2. GameWorldBridge 捕获输入
   ↓
3. 创建 Command（如 CreateUnitCommand）
   ↓
4. worldBridge.SubmitCommand(cmd)
   ↓
5. Game.SubmitCommand(cmd)
   ↓
6. zWorld.CommandManager.SubmitCommand(cmd)
   ↓
7. Game.Update() → zWorld.Update()
   ↓
8. zWorld.EventManager.Clear() - 清空上一帧事件
   ↓
9. zWorld.CommandManager.ExecuteFrame()
   ├─ cmd.Execute(zWorld)
   ├─ 创建Entity，添加Component
   └─ EventManager.Publish(UnitCreatedEvent)
   ↓
10. zWorld.SystemManager.UpdateAll()
   ├─ MovementSystem.Update() - 处理移动逻辑
   ├─ ...其他逻辑System
   └─ PresentationSystem.Update()
      ├─ ProcessCreationEvents() - 获取UnitCreatedEvent
      ├─ 实例化Unity GameObject
      ├─ 添加ViewComponent
      └─ SyncAllViews() - 同步Transform
   ↓
11. Unity渲染（视觉呈现）
```

### 关键点

1. **事件清空时机**：
   - 在 `Game.Update()` 开始时调用 `EventManager.Clear()`
   - 确保上一帧的事件已被处理

2. **表现同步**：
   - `PresentationSystem` 在同一逻辑帧内处理事件
   - 视图创建发生在 `SystemManager.UpdateAll()` 期间

3. **插值平滑**：
   - `PresentationSystem.LerpUpdate()` 在Unity的 `Update()` 中调用
   - 提供逻辑帧之间的平滑过渡

---

## ═══════════════════════════════════════════════════════════
## 各层详解
## ═══════════════════════════════════════════════════════════

### 【第1层】zWorld - 游戏逻辑核心

**文件位置**：`Simulation/zWorld.cs`

**核心管理器**：

| 管理器 | 职责 |
|-------|------|
| `EntityManager` | 创建/销毁实体 |
| `ComponentManager` | 添加/获取/删除组件 |
| `SystemManager` | 注册/更新所有System |
| `EventManager` | 发布/获取/清空事件 |
| `TimeManager` | 管理逻辑时间和Tick |
| `CommandManager` | 管理命令执行 |

**关键方法**：

```csharp
// 初始化
void Init(int frameRate)

// 推进一帧
void Update()
  1. TimeManager.Advance() - Tick++
  2. CommandManager.ExecuteFrame() - 执行命令
  3. SystemManager.UpdateAll() - 执行系统

// 清理
void Shutdown()
```

**设计原则**：
- ✅ 不知道谁在驱动它（Game/GameWorldBridge/测试）
- ✅ 不知道游戏模式（单机/网络/回放）
- ✅ 不知道平台（Unity/服务器/命令行）

---

### 【第2层】Game - 应用控制层

**文件位置**：`Sync/Game.cs`

**核心职责**：

```csharp
// 构造函数
Game(int frameRate = 20, int localPlayerId = 0)

// 初始化游戏
void Init()
  1. 创建 zWorld
  2. world.Init(frameRate)
  3. RegisterSystems() - 注册游戏系统

// 游戏主循环
void Update()
  1. world.EventManager.Clear() - 清空上一帧事件
  2. world.Update() - 推进逻辑

// 提交命令
void SubmitCommand(ICommand command)

// 关闭游戏
void Shutdown()
```

**使用场景**：

| 场景 | 用法 |
|-----|------|
| Unity客户端 | 通过 `GameWorldBridge` 持有并驱动 |
| 服务器 | 直接在 `Main()` 中创建和驱动 |
| 单元测试 | 直接实例化，无需Unity环境 |

**设计原则**：
- ✅ 只负责"控制"，不负责"呈现"
- ✅ 纯C#实现，不依赖Unity
- ✅ 创建并持有唯一的zWorld

---

### 【第3层】GameWorldBridge - Unity桥接层

**文件位置**：`View/GameWorldBridge.cs`

**核心字段**：

```csharp
[SerializeField] int logicFrameRate = 20;
[SerializeField] int localPlayerId = 0;
[SerializeField] Transform viewRoot;
[SerializeField] GameObject[] unitPrefabs;

bool enableSmoothInterpolation = true;
float interpolationSpeed = 10f;

private Game _game; // 持有Game实例
private PresentationSystem _presentationSystem;
```

**Unity生命周期**：

```csharp
void Awake()
  1. InitializeGame() - 创建Game，Game.Init()
  2. InitializeUnityView() - 创建PresentationSystem

void FixedUpdate()
  _game.Update() - 驱动逻辑更新

void Update()
  _presentationSystem.LerpUpdate() - 平滑插值（可选）

void OnDestroy()
  _game.Shutdown() - 清理资源
```

**对外接口**：

```csharp
// 提交命令
void SubmitCommand(ICommand command)

// 访问逻辑世界
zWorld LogicWorld { get; }
Game Game { get; }
```

**设计原则**：
- ✅ 只负责"桥接"，不负责"逻辑"
- ✅ 持有Game实例，不创建zWorld
- ✅ Unity特定代码只在这一层

---

## ═══════════════════════════════════════════════════════════
## 表现系统（Presentation Layer）
## ═══════════════════════════════════════════════════════════

### PresentationSystem

**文件位置**：`View/Systems/PresentationSystem.cs`

**核心职责**：

```csharp
// 初始化（由GameWorldBridge调用）
void Initialize(Transform viewRoot, Dictionary<int, GameObject> prefabs)

// 主更新（在逻辑帧内执行）
void Update()
  1. ProcessCreationEvents() - 处理UnitCreatedEvent
     ├─ 获取预制体
     ├─ 实例化GameObject
     └─ 添加ViewComponent
  2. SyncAllViews() - 同步Transform、动画

// 平滑插值（在Unity Update中执行）
void LerpUpdate(float deltaTime, float speed)
```

**关键特性**：
- ✅ 在同一逻辑帧内处理事件（确保及时创建）
- ✅ 支持平滑插值（提供更好的视觉体验）
- ✅ 自动同步动画状态（IsMoving, IsAttacking等）

---

### ViewComponent

**文件位置**：`View/ViewComponent.cs`

**核心字段**：

```csharp
GameObject GameObject;           // Unity游戏对象
Transform Transform;             // Transform缓存
Animator Animator;               // 动画控制器
Renderer Renderer;               // 渲染器

// 插值相关
Vector3 LastLogicPosition;
Quaternion LastLogicRotation;
bool EnableInterpolation;
```

**使用说明**：
- 这是唯一引用Unity的组件
- 只在表现层使用，不参与逻辑计算
- 通过 `ViewComponent.Create(GameObject)` 创建

---

## ═══════════════════════════════════════════════════════════
## 命令系统（Command System）
## ═══════════════════════════════════════════════════════════

### 架构

```
ICommand 接口
  ├─ BaseCommand 抽象基类
  │   ├─ CreateUnitCommand - 创建单位
  │   ├─ MoveCommand - 移动命令
  │   └─ ... 其他命令

CommandManager
  ├─ SubmitCommand(cmd) - 提交命令
  ├─ ExecuteFrame() - 执行当前帧命令
  └─ 支持延迟执行（网络同步）
```

### 命令来源

```csharp
enum CommandSource
{
    Local = 0,    // 本地玩家输入
    AI = 1,       // AI决策
    Network = 2,  // 网络同步
    Replay = 3    // 回放系统
}
```

### 使用示例

```csharp
// 创建单位命令
var cmd = new CreateUnitCommand(
    playerId: 0,
    unitType: 1,
    position: new zVector3(0, 0, 0),
    prefabId: 1
) { Source = CommandSource.Local };

// 提交命令
worldBridge.SubmitCommand(cmd);
```

---

## ═══════════════════════════════════════════════════════════
## ECS 核心组件
## ═══════════════════════════════════════════════════════════

### 逻辑组件（纯数据）

| 组件 | 字段 | 用途 |
|-----|------|------|
| `TransformComponent` | Position, Rotation, Scale | 逻辑变换 |
| `VelocityComponent` | Value, IsMoving | 速度 |
| `UnitComponent` | UnitType, PlayerId, MoveSpeed | 单位属性 |
| `HealthComponent` | MaxHealth, CurrentHealth | 生命值 |
| `AttackComponent` | Damage, Range, TargetEntityId | 攻击 |
| `MoveCommandComponent` | TargetPosition, StopDistance | 移动指令 |

### 表现组件（Unity相关）

| 组件 | 字段 | 用途 |
|-----|------|------|
| `ViewComponent` | GameObject, Transform, Animator | 视图绑定 |

---

## ═══════════════════════════════════════════════════════════
## 系统（Systems）
## ═══════════════════════════════════════════════════════════

### 逻辑系统

| 系统 | 职责 |
|-----|------|
| `MovementSystem` | 处理移动命令，更新Transform |
| `CombatSystem` | 处理战斗逻辑（待实现） |
| `AISystem` | AI决策（待实现） |

### 表现系统

| 系统 | 职责 |
|-----|------|
| `PresentationSystem` | 同步逻辑到Unity表现 |

---

## ═══════════════════════════════════════════════════════════
## 快速开始
## ═══════════════════════════════════════════════════════════

### Unity场景设置

1. **创建GameWorld对象**
   ```
   Hierarchy → 右键 → Create Empty
   命名为 "GameWorld"
   Add Component → GameWorldBridge
   ```

2. **配置GameWorldBridge**
   - Logic Frame Rate: `20`
   - Local Player Id: `0`
   - View Root: 拖入 `GameWorld` 自己
   - Unit Prefabs[1]: 拖入单位预制体（如 Cube）

3. **创建测试脚本**
   ```csharp
   using ZLockstep.View;
   using ZLockstep.Sync.Command.Commands;
   using zUnity;
   
   public class MyTest : MonoBehaviour
   {
       [SerializeField] GameWorldBridge worldBridge;
       
       void Update()
       {
           if (Input.GetMouseButtonDown(0))
           {
               var cmd = new CreateUnitCommand(
                   playerId: 0,
                   unitType: 1,
                   position: GetMouseWorldPosition(),
                   prefabId: 1
               );
               worldBridge.SubmitCommand(cmd);
           }
       }
   }
   ```

4. **运行游戏**
   - 点击 Play
   - 点击场景中的地面
   - 单位会在点击位置创建

---

## ═══════════════════════════════════════════════════════════
## 架构优势
## ═══════════════════════════════════════════════════════════

### 1. 严格分层

- ✅ 每层职责单一，互不干扰
- ✅ 上层依赖下层，下层不知道上层
- ✅ 遵循奥卡姆剃刀原则

### 2. 跨平台

```
Unity客户端        服务器            单元测试
    ↓                ↓                  ↓
GameWorldBridge    Main()            TestCase
    ↓                ↓                  ↓
    └────────────→ Game ←─────────────┘
                    ↓
                  zWorld（纯C#，跨平台）
```

### 3. 易于测试

```csharp
[Test]
public void TestUnitMovement()
{
    // 纯C#测试，无需Unity环境
    var game = new Game();
    game.Init();
    
    var cmd = new CreateUnitCommand(...);
    game.SubmitCommand(cmd);
    game.Update();
    
    Assert.IsTrue(...);
}
```

### 4. 命令驱动

- ✅ 所有操作通过Command
- ✅ 支持网络同步（未来）
- ✅ 支持回放系统
- ✅ 支持AI决策

---

## ═══════════════════════════════════════════════════════════
## 常见问题（FAQ）
## ═══════════════════════════════════════════════════════════

### Q: 为什么要三层架构？
A: 
- **第1层 zWorld**：纯逻辑，跨平台，可测试
- **第2层 Game**：生命周期管理，纯C#
- **第3层 GameWorldBridge**：Unity适配，资源管理

### Q: 为什么只有一个zWorld？
A: 
- 游戏状态必须唯一，避免数据不一致
- 多个zWorld会导致同步问题

### Q: 事件什么时候清空？
A: 
- 在 `Game.Update()` 开始时清空上一帧事件
- 确保 `PresentationSystem` 有机会处理

### Q: 如何添加新的System？
A: 
```csharp
// 在 Game.RegisterSystems() 中添加
protected override void RegisterSystems()
{
    base.RegisterSystems();
    World.SystemManager.RegisterSystem(new MyCustomSystem());
}
```

### Q: 如何自定义Command？
A: 
```csharp
public class MyCommand : BaseCommand
{
    public override int CommandType => 100;
    
    public MyCommand(int playerId) : base(playerId) { }
    
    public override void Execute(zWorld world)
    {
        // 你的逻辑
    }
}
```

---

## ═══════════════════════════════════════════════════════════
## 帧同步（Lockstep）实现
## ═══════════════════════════════════════════════════════════

> 详细文档：[Sync/LOCKSTEP_GUIDE.md](Sync/LOCKSTEP_GUIDE.md)

### 核心原理

**帧同步（Lockstep）** = 所有客户端在同一逻辑帧执行相同的命令

```
【关键机制】：客户端必须等待服务器确认才能推进

Frame 0  1  2  3  4  5
客户端A: ✓  ✓  ⏳ ⏳ ✓  ✓  (等待确认)
客户端B: ✓  ✓  ⏳ ⏳ ✓  ✓  (等待确认)
服务器:  ✓确认0 ✓确认1 ✓确认2...
```

### 架构

```
┌─────────────────────────────────────┐
│  GameWorldBridge (Unity)            │
│  FixedUpdate() → Game.Update()      │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  Game (应用层)                      │
│  根据GameMode选择执行方式           │
└──────────────┬──────────────────────┘
               │
       ┌───────┴───────┐
       │ Standalone    │ NetworkClient
       ▼               ▼
ExecuteLogicFrame() FrameSyncManager
                           │
                      ├─ CanAdvanceFrame()
                      ├─ 检查 confirmedFrame
                      ├─ 是 → PrepareNextFrame()
                      └─ 然后 → ExecuteLogicFrame()
```

### 游戏模式

| 模式 | 行为 | 使用场景 |
|-----|------|---------|
| `Standalone` | 每帧都执行 | 单机游戏 |
| `NetworkClient` | 等待服务器确认 | 网络客户端 |
| `NetworkServer` | 每帧都执行 | 服务器权威 |
| `Replay` | 按录制执行 | 回放系统 |

### 使用示例

#### 1. Unity配置

```
GameWorldBridge:
  - Game Mode: NetworkClient
  - Logic Frame Rate: 20
  - Local Player Id: 0
```

#### 2. 提交命令（自动根据模式处理）

```csharp
// 单机模式：直接执行
worldBridge.Game.SubmitCommand(cmd);

// 网络模式：需要传入网络适配器
worldBridge.Game.SubmitCommand(cmd, networkAdapter);
```

#### 3. 接收服务器确认

```csharp
void OnReceiveServerMessage(int frame, List<ICommand> commands)
{
    worldBridge.Game.FrameSyncManager.ConfirmFrame(frame, commands);
}
```

### FrameSyncManager

**职责**：
- 管理帧确认状态（`confirmedFrame`）
- 缓冲未来帧的命令
- 决定是否可以推进

**关键方法**：
```csharp
// 检查是否可以推进（返回false表示等待中）
bool CanAdvanceFrame();

// 准备下一帧（提交命令，不执行world.Update）
int PrepareNextFrame();

// 服务器确认帧（网络层调用）
void ConfirmFrame(int frame, List<ICommand> commands);

// 提交本地命令（发送到服务器）
void SubmitLocalCommand(ICommand cmd, INetworkAdapter adapter);
```

**重要**：`FrameSyncManager` 不直接执行 `world.Update()`，实际执行由 `Game.ExecuteLogicFrame()` 完成，确保所有模式都统一清空事件。

---

## ═══════════════════════════════════════════════════════════
## 未来扩展
## ═══════════════════════════════════════════════════════════

### 网络层（需要实现）

```
┌─────────────────────────────────────┐
│  INetworkAdapter (接口)             │
│  void SendCommandToServer(cmd)      │
└──────────────┬──────────────────────┘
               │
               ├─ Unity Netcode 实现
               ├─ WebSocket 实现
               └─ 自定义协议实现
```

### 回放系统（待实现）

```
CommandManager.RecordHistory = true;
// ... 游戏进行中 ...
var history = CommandManager.GetCommandHistory();
// 保存到文件
// 回放时重新执行命令序列
```

---

**文档版本**: v1.0  
**最后更新**: 2025-10-21  
**维护者**: ZLockstep团队
