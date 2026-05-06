# 锁步同步管理器设计文档 (Lockstep Manager Design)

## 1. 概述 (Overview)

`LockstepManager` 是整个帧同步框架的“交通警察”和“节拍器”。它位于**表现层 (Presentation Layer, e.g., Unity)** 和 **仿真核心 (Simulation Core)** 之间，扮演着承上启下的关键角色。

它的核心职责是确保**所有客户端在完全相同的逻辑帧（Tick），以完全相同的顺序，执行完全相同的命令**。

## 2. 顶层设计：持有关系与驱动模型

为了保证核心逻辑与Unity引擎的完全解耦，`LockstepManager` 自身以及整个仿真核心都不应依赖任何Unity的对象（如`MonoBehaviour`）。

### 2.1 `Game` 类 (纯 C#)
- 我们将设计一个顶层的、纯 C# 的 `Game` 类，作为整个游戏世界的总容器和启动器。
- `Game` 类负责在其初始化时，创建并**持有** `LockstepManager` 的实例。
- `Game` 类是所有核心系统（`LockstepManager`, `zWorld` 等）的生命周期管理者。

### 2.2 `GameLauncher` (Unity 适配器)
- 在Unity环境中，我们将创建一个非常薄的适配器层，通常是一个名为 `GameLauncher` 的 `MonoBehaviour` 脚本。
- `GameLauncher` 的职责是：
  - 在 `Awake()` 中 `new Game()` 并调用其初始化方法。
  - 在 `Update()` 中，稳定地调用 `Game.Update()` 来驱动整个游戏逻辑循环。
  - 在 `OnDestroy()` 中，调用 `Game.Shutdown()` 来释放资源。
- 这种设计将所有对Unity API的依赖都隔离在了 `GameLauncher` 这一层，确保了我们核心框架的独立性、可移植性和可测试性。

### 2.3 架构分层
```
+--------------------------------+
|      Unity Engine Layer        |
+--------------------------------+
             | (调用 Update)
+--------------------------------+
|    Adapter (GameLauncher.cs)   |
+--------------------------------+
             | (new Game(), game.Update())
+--------------------------------+
|    Game Class (Pure C#)        |  <-- 持有 LockstepManager
+--------------------------------+
             | (new LockstepManager())
+--------------------------------+
|  Lockstep & Simulation Core    |
+--------------------------------+
```

## 3. 核心职责 (Core Responsibilities)

### 3.1 游戏主循环驱动 (Game Loop Driver)
- **控制节拍**: `LockstepManager` 负责驱动 `zWorld` 的 `Update()` 方法。它被 `Game.Update()` 调用，根据固定的时间步长，稳定地推动仿真核心向前演进一帧。
- **追帧逻辑**: 当一个客户端落后于服务器或其他客户端时，`LockstepManager` 需要实现“追帧”逻辑，即在一个物理帧内，连续执行多个逻辑帧 (`zWorld.Update()`)，直到赶上目标进度。
- **等待与缓冲**: 在网络对战中，如果当前帧还没有收到所有玩家的命令，主循环需要适当暂停等待，以避免客户端状态超前。

### 3.2 命令的收集与调度 (Command Collection & Scheduling)
- **统一入口**: 提供一个入口点（例如 `ScheduleCommand(ICommand, int tick)`），让 `Game` 对象可以调用它，从而将来自表现层的命令提交进来。
- **命令缓冲**: 内部维护一个命令缓冲区，通常是一个以“逻辑帧 (Tick)”为键，命令列表为值的字典 (`Dictionary<int, List<ICommand>>`)。
- **延迟执行**: 负责计算命令应该在未来的哪个逻辑帧执行。这个延迟通常是为了补偿网络抖动，确保在执行某一帧的逻辑时，大概率已经收到了所有玩家那一帧的命令。

### 3.3 命令的分发 (Command Dispatching)
- **按帧分发**: 在每个逻辑帧开始时，`LockstepManager` 会从命令缓冲区中取出当前 Tick 所对应的所有命令。
- **排序保证**: 为了进一步确保确定性，在将命令列表交给 `System` 处理之前，需要对这个列表进行排序（例如，按玩家ID排序），以消除因网络消息到达顺序不同而引入的随机性。
- **与System协作**: 将排序后的命令列表传递给仿真核心中的一个或多个 `System` (例如 `CommandProcessingSystem`) 进行最终的执行。

### 3.4 网络协同 (Network Coordination)
- **命令广播**: 本地玩家产生的命令，需要通过 `LockstepManager` 序列化后发送给服务器（或P2P网络中的其他玩家）。
- **命令接收**: 从网络层接收到的其他玩家的命令，也由 `LockstepManager` 反序列化后放入命令缓冲区。
- **帧确认**: （在一些设计中）定期向服务器报告自己已经成功执行到哪一帧，以便服务器判断各个客户端的同步状态。

## 4. 核心工作流程 (Core Workflow)

1.  **初始化 (Initialization)**:
    - `GameLauncher` (Unity) 创建 `Game` 实例并调用 `Game.Init()`。
    - `Game` 内部创建 `LockstepManager` 实例，并调用其初始化方法。
    - `LockstepManager` 内部创建 `zWorld` 实例，并调用 `zWorld.Init()`。

2.  **每物理帧 (Unity `Update`)**:
    - `GameLauncher.Update()` 调用 `Game.Update()`。
    - `Game.Update()` 调用 `LockstepManager.UpdateLoop()`。
    - `LockstepManager` 根据真实时间流逝，计算当前应该执行到哪个逻辑帧 (Tick)。
    - **IF** (当前逻辑帧 < 目标逻辑帧) **THEN** // 需要追帧
        - **LOOP** (从当前帧到目标帧)
            1.  `currentTick = zWorld.Tick`
            2.  从命令缓冲区取出 `_commandBuffer[currentTick]` 的命令列表。
            3.  对命令列表进行确定性排序。
            4.  将排序后的命令交给 `CommandProcessingSystem`（通常是通过一个共享的数据结构）。
            5.  调用 `zWorld.Update()`。
        - **END LOOP**
    - **ELSE** // 正常运行或等待
        - 检查是否已收到所有玩家对 `currentTick` 的输入。
        - **IF** (是) **THEN** -> 执行上述循环一次。
        - **IF** (否) **THEN** -> 等待，不做任何事。

3.  **命令的生命周期 (Lifecycle of a Command)**:
    - **(A) 本地玩家输入**:
        - `InputManager` 捕获输入 -> `new MoveCommand()` -> `LockstepManager.ScheduleCommand(cmd, zWorld.Tick + networkLatencyFrames)` -> 命令进入本地缓冲区 -> **广播给其他玩家**。
    - **(B) 接收网络命令**:
        - `NetworkManager` 收到数据包 -> 反序列化为 `MoveCommand` -> `LockstepManager.ScheduleCommand(cmd, commandTickFromNetwork)` -> 命令进入本地缓冲区。

## 5. 核心API (Core API)

```csharp
// Game.cs (Pure C#)
public class Game
{
    public LockstepManager LockstepManager { get; private set; }
    public void Init();
    public void Update();
    public void Shutdown();
    public void ScheduleCommand(ICommand command, int executionTick);
}

// LockstepManager.cs (Pure C#)
public class LockstepManager
{
    private zWorld _world;
    private Dictionary<int, List<ICommand>> _commandBuffer;

    // (由Game对象调用)
    public void Init( /* zWorld could be passed in */ );
    public void UpdateLoop(); 
    public void Shutdown();
    public void ScheduleCommand(ICommand command, int executionTick);
}
```
