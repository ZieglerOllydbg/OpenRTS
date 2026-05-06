# ZLockstep - 帧同步（RTS）游戏框架设计

## ═══════════════════════════════════════════════════════════
## 三层架构设计（奥卡姆剃刀原则）
## ═══════════════════════════════════════════════════════════

### 核心理念：严格分层，职责单一

```
┌───────────────────────────────────────────────────────┐
│  【第3层：Unity桥接层】GameWorldBridge                 │
│  职责：Unity平台适配                                  │
│  - MonoBehaviour生命周期                              │
│  - Unity资源管理（预制体、Transform）                │
│  - Inspector配置界面                                  │
│  - 不知道游戏逻辑细节                                 │
└──────────────────┬────────────────────────────────────┘
                   │ 持有并驱动
                   ▼
┌───────────────────────────────────────────────────────┐
│  【第2层：应用控制层】Game                             │
│  职责：游戏生命周期管理                               │
│  - Init/Update/Shutdown                               │
│  - 创建唯一的zWorld                                   │
│  - 纯C#，可在Unity/服务器/测试运行                    │
│  - 不知道Unity，不知道表现                            │
└──────────────────┬────────────────────────────────────┘
                   │ 创建并驱动
                   ▼
┌───────────────────────────────────────────────────────┐
│  【第1层：游戏逻辑核心】zWorld                         │
│  职责：游戏状态容器                                   │
│  - ECS架构（Entity/Component/System）                 │
│  - 确定性计算（zfloat/zVector3）                      │
│  - 命令系统（Command → ECS → Event）                  │
│  - 不知道平台，不知道如何被驱动                       │
└───────────────────────────────────────────────────────┘
```

### 数据流向

```
输入 → GameWorldBridge → Game → zWorld → CommandManager
                                          ↓
                                     CommandExecute
                                          ↓
                                    ECS Components
                                          ↓
                                   System Process
                                          ↓
                                   EventManager
                                          ↓
                             PresentationSystem
                                          ↓
                                Unity GameObject
```

### 关键特性
- ✅ **严格分层**：每层只知道下层，不知道上层
- ✅ **唯一zWorld**：整个游戏只有一个逻辑世界实例
- ✅ **跨平台核心**：Game和zWorld可在任何C#环境运行
- ✅ **确定性保证**：逻辑层使用zfloat/zVector3
- ✅ **易于测试**：可纯C#单元测试，无需Unity

---

## 详细分层说明

### 【第1层】zWorld - 游戏逻辑核心

**文件位置**：`Simulation/zWorld.cs`

**职责**：
- 游戏状态的容器和管理者
- 管理ECS架构的所有管理器
- 纯逻辑计算，确定性保证
- 不关心平台、不关心如何被驱动

**对外接口**：
```csharp
var world = new zWorld();
world.Init(frameRate: 20);
world.Update();  // 推进一帧
world.Shutdown();
```

**包含的管理器**：
- `EntityManager` - 实体管理
- `ComponentManager` - 组件管理
- `SystemManager` - 系统管理
- `EventManager` - 事件管理
- `TimeManager` - 时间管理
- `CommandManager` - 命令管理

---

### 【第2层】Game - 应用控制层

**文件位置**：`Sync/Game.cs`

**职责**：
- 应用生命周期管理（Init/Update/Shutdown）
- 创建并持有唯一的zWorld实例
- 注册游戏系统
- 纯C#实现，跨平台

**使用场景**：
- Unity客户端：通过GameWorldBridge调用
- 服务器：直接在Main()中使用
- 单元测试：直接实例化

**对外接口**：
```csharp
var game = new Game(frameRate: 20, playerId: 0);
game.Init();
game.Update();  // 每帧调用
game.SubmitCommand(cmd);
game.Shutdown();
```

---

### 【第3层】GameWorldBridge - Unity桥接层

**文件位置**：`View/GameWorldBridge.cs`

**职责**：
- 连接Unity和纯C# Game
- MonoBehaviour生命周期适配
- Unity资源管理（预制体等）
- Inspector配置界面

**使用方式**：
1. 在Unity场景创建空对象
2. 添加GameWorldBridge组件
3. 配置unitPrefabs数组
4. 运行游戏

**对外接口**：
```csharp
// 在其他脚本中使用
worldBridge.SubmitCommand(cmd);
var world = worldBridge.LogicWorld;
```

---

## 一、 核心引擎层 (Core Engine Layer)

### 1. 核心库 (Core Library)
- **确定性数学库 (Deterministic Math Library)**
  - 定点数 (Fixed-Point Numbers)
  - 向量、矩阵、四元数 (Vector, Matrix, Quaternion)
  - 几何与碰撞体 (Geometry & Colliders - Bounds, Ray, Sphere)
- **确定性辅助工具 (Deterministic Utilities)**
  - 随机数 (zRandom)
  - 容器 (Containers)
  - 序列化 (Serialization for replays and save/load)

### 2. 仿真核心 (Simulation Core)
- **`World`**: 游戏世界，管理所有实体和系统 (Game world, manages all entities and systems)
- **`ECS`**: 实体-组件-系统架构 (Entity-Component-System Architecture)
  - **核心组件 (Core Components)**:
    - `TransformComponent`: 位置、旋转、缩放（纯逻辑数据）
    - `VelocityComponent`: 速度
    - `UnitComponent`: 单位属性
    - `HealthComponent`: 生命值
  - **系统 (Systems)**:
    - `BaseSystem`: 系统基类
    - `MovementSystem`: 移动系统
    - `CombatSystem`: 战斗系统
    - `PresentationSystem`: 表现同步系统
- **`Time`**: 确定性时间管理器 (Deterministic Time Manager)
- **`Event System`**: 事件系统 (Event System)

### 3. 物理与寻路 (Physics & Pathfinding)
- **确定性物理 (Deterministic Physics)**
  - 碰撞检测 (Collision Detection)
  - 空间划分 (Spatial Partitioning - e.g., Quadtree/Grid)
- **寻路 (Pathfinding)**
  - A* 算法 (A* Algorithm)
  - 地图与路径数据 (Map & Path Data)

### 4. AI 框架 (AI Framework)
- 行为树 (Behavior Tree)

### 5. 命令系统 (Command System)
- **CommandManager**: 命令管理器，负责命令的排序和执行
- **Command**: 所有游戏操作都通过Command驱动
- **CommandBuffer**: 命令缓冲区
- 详细文档：`Sync/Command/README_COMMAND_SYSTEM.md`

### 6. 表现层 (Presentation Layer)
- **PresentationSystem**: 表现同步系统
  - 监听逻辑事件，创建Unity视图
  - 同步Transform、动画等
  - 支持插值平滑
- **ViewComponent**: Unity GameObject绑定组件
- **MathConversion**: 确定性数学类型与Unity类型转换


## 二、 游戏逻辑层 (Game Logic Layer)

### 1. 命令系统 (Command System)
- **玩家输入转换 (Player Input to Command)**
- **命令定义 (Command Definitions)**: 移动、攻击、建造等 (Move, Attack, Build, etc.)

### 2. 游戏玩法逻辑 (Gameplay Logic - Implemented with ECS)
- **Components**: `HealthComponent`, `AttackComponent`, `MovementComponent`, etc.
- **Systems**: `MovementSystem`, `AttackSystem`, `BuildSystem`, `ResourceSystem`, etc.

### 3. AI 逻辑 (AI Logic)
- **单位/建筑的具体AI行为 (Specific AI behaviors for units/buildings)**
- **电脑玩家策略 (Computer Player / AI opponent strategies)**

### 4. 回放系统 (Replay System)
- **命令的录制与回放 (Recording and playback of commands)**

---

## 快速开始

### Unity场景设置

1. **创建GameWorld对象**
   ```
   Hierarchy → Create Empty → 命名为 "GameWorld"
   Add Component → GameWorldBridge
   ```

2. **配置GameWorldBridge**
   - Logic Frame Rate: 20
   - Local Player Id: 0
   - View Root: 拖入GameWorld自己
   - Unit Prefabs[1]: 拖入单位预制体

3. **创建测试脚本**
   ```csharp
   using ZLockstep.Sync.Command.Commands;
   
   worldBridge.SubmitCommand(new CreateUnitCommand(
       playerId: 0,
       unitType: 1,
       position: new zVector3(0, 0, 0),
       prefabId: 1
   ));
   ```

4. **运行游戏**
   - 点击Play
   - 单位会在逻辑层创建
   - PresentationSystem会自动创建Unity视图

---

## 架构优势

### 1. 严格分层
- 每层职责单一，互不干扰
- 上层依赖下层，下层不知道上层
- 遵循奥卡姆剃刀原则

### 2. 跨平台
- Game和zWorld完全独立于Unity
- 可在服务器、测试环境运行
- Unity只是一个"皮肤"

### 3. 易于测试
```csharp
// 纯C#单元测试
[Test]
public void TestUnitMovement()
{
    var game = new Game();
    game.Init();
    
    var cmd = new CreateUnitCommand(...);
    game.SubmitCommand(cmd);
    game.Update();
    
    Assert.IsTrue(...);
}
```

### 4. 命令驱动
- 所有操作通过Command
- 支持网络同步（未来）
- 支持回放系统

---
