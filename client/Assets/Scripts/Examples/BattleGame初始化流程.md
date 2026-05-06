# BattleGame 初始化主线是：

1. 外部入口创建 `BattleGame`
   - 单人：`MatchPanel.OnSoloButtonClick()` 创建 `new BattleGame(Standalone, 20, 0, 1)`。
   - 回放：`MatchReplaySubPanel` 创建 `new BattleGame(Replay, 20, 0, playerNumber)`。
   - 联机匹配：`NetworkManager.OnMatchSuccess()` 根据初始状态判断人数后创建 `BattleGame`。

2. 调用 `BattleGame.Init()`
   - 初始化命令映射：`CommandMapper.Initialize()`。
   - 初始化地图和流场：创建 `SimpleMapManager`，初始化为 `128 x 128`、格子大小 `2`；创建 `FlowFieldManager`，最大流场数 `30`，每帧最多更新 `2` 个。
   - 调用 `base.Init()` 进入通用 `Game` 初始化。

3. `Game.Init()` 创建逻辑世界
   - 创建 `zWorld`。
   - 设置 `Time.fixedDeltaTime = 1 / frameRate`。
   - 调用 `World.Init(frameRate)` 初始化核心管理器：`TimeManager`、`ComponentManager`、`EntityManager`、`SystemManager`、`EventManager`、`CommandManager`、`GMManager`、`DeterminismProbe`。
   - 设置 `World.GameInstance = this`。
   - 如果是 `NetworkClient`，创建 `FrameSyncManager`。
   - 调用虚方法 `RegisterSystems()`，实际执行 `BattleGame.RegisterSystems()`。

4. `BattleGame.RegisterSystems()` 注册战斗系统
   注册顺序大致是：
   - `FlowFieldNavigationSystem`，并绑定 `FlowFieldManager`、`MapManager`
   - `AutoChaseSystem`
   - `CombatSystem`
   - `ProjectileSystem`
   - `HealthSystem`
   - `AutoHealSystem`
   - `DeathRemovalSystem`
   - `FirstAISystem`，但回放模式和双人玩法不注册
   - `ProduceSystem`
   - `EconomySystem`
   - `SettlementSystem`
   - `BuildingConstructionSystem`

5. 初始化 Unity 表现层
   外部随后调用 `Ra2Demo.InitializeUnityView()`：
   - 创建 `PresentationSystem`
   - 初始化视图根节点
   - 设置当前 `BattleGame`
   - 把表现系统注册进 `World.SystemManager`

6. 添加全局玩家信息
   外部创建 `GlobalInfoComponent` 并加入 `World.ComponentManager`，用于记录本地阵营 ID。

7. 创世阶段创建初始世界
   当前主流程调用 `BattleGame.CreateWorldByConfig()`：
   - 根据 `PlayerNumber` 决定阵营：
     - 单人：阵营 `1` 和 `9`
     - 双人：阵营 `1` 和 `2`
   - 读取 `ConfInitUnits`
   - 创建初始建筑、单位
   - 基地建筑会额外创建经济实体，添加 `EconomyComponent` 和 `CampComponent`
   - 把创世阶段产生的 `UnitCreatedEvent` 缓存在 `_genesisEntities`

8. 启动记录、探针和事件
   - 单人、联机会调用 `CommandManager.StartRecording(...)` 开始记录回放命令。
   - 单人、联机、回放都会按各自入口启动 `DeterminismProbe`。
   - 最后派发 `SoloGameStartEvent` / `ReplayGameStartEvent` / `MatchedEvent`。

9. 第一帧真正推进时发布创世事件
   `Game.Update()` 内部调用 `ExecuteLogicFrame()`：
   - 先清空上一帧事件。
   - 如果 `_genesisEntities` 还没发布，则发布所有创世实体事件，并调用 `OnWorldInitialized()`。
   - 然后调用 `World.Update()`。
   - `World.Update()` 顺序是：推进时间、执行命令、更新所有系统、记录确定性摘要。

关键文件：
- [BattleGame.cs](c:/MyZiegler/github/SimpleRTS/client/Assets/Scripts/Examples/BattleGame.cs:81)
- [Game.cs](c:/MyZiegler/github/SimpleRTS/client/Packages/ZLockstep/Runtime/Sync/Game.cs:146)
- [zWorld.cs](c:/MyZiegler/github/SimpleRTS/client/Packages/ZLockstep/Runtime/Simulation/zWorld.cs:64)
- [Ra2Demo.cs](c:/MyZiegler/github/SimpleRTS/client/Assets/Scripts/Ra2Demo/Ra2Demo.cs:203)