using System.Collections.Generic;
using ZLockstep.Simulation;
using ZLockstep.Simulation.ECS.Systems;
using ZLockstep.Sync.Command;
using Newtonsoft.Json.Linq;
using ZLockstep.Simulation.ECS.Components;
using zUnity;
using ZLockstep.Simulation.Events;
using ZLockstep.Flow;

namespace ZLockstep.Sync
{
    /// <summary>
    /// 游戏模式枚举
    /// </summary>
    public enum GameMode
    {
        /// <summary>单机模式（无需等待）</summary>
        Standalone,
        
        /// <summary>网络客户端（需要帧同步）</summary>
        NetworkClient,
        
        /// <summary>网络服务器（不需要等待，直接推进）</summary>
        NetworkServer,
        
        /// <summary>回放模式（按录制顺序执行）</summary>
        Replay
    }

    /// <summary>
    /// ═══════════════════════════════════════════════════════════════
    /// 【第2层：应用控制层】Game
    /// ═══════════════════════════════════════════════════════════════
    /// 
    /// 职责定位：
    /// - 应用生命周期管理：Init/Update/Shutdown
    /// - 业务流程编排：决定游戏如何运行（单机/网络/回放）
    /// - 跨平台入口：可在Unity、服务器、测试环境使用
    /// - 唯一zWorld创建者：创建并持有唯一的游戏世界实例
    /// 
    /// 设计原则（奥卡姆剃刀）：
    /// - 只负责"控制"，不负责"呈现"（不管Unity资源）
    /// - 纯C#实现，不依赖Unity
    /// - 可被不同平台的适配器调用（GameWorldBridge/服务器/测试）
    /// 
    /// 层次关系：
    /// - 上层：被 GameWorldBridge(Unity) 或 Main()(服务器) 调用
    /// - 下层：创建并驱动 zWorld
    /// 
    /// 使用场景：
    /// - Unity客户端：通过 GameWorldBridge 调用
    /// - 服务器：直接在 Main() 中使用
    /// - 单元测试：直接实例化测试
    /// 
    /// 使用示例：
    ///   // 单机模式
    ///   var game = new Game(GameMode.Standalone);
    ///   game.Init();
    ///   while(running) {
    ///       game.Update(); // 每帧都执行
    ///   }
    ///   
    ///   // 网络模式
    ///   var game = new Game(GameMode.NetworkClient);
    ///   game.Init();
    ///   while(running) {
    ///       game.Update(); // 只有确认的帧才执行
    ///   }
    /// </summary>
    public class Game
    {
        /// <summary>
        /// 唯一的游戏逻辑世界
        /// </summary>
        public zWorld World { get; private set; }

        /// <summary>
        /// 帧同步管理器（仅网络模式使用）
        /// </summary>
        public FrameSyncManager FrameSyncManager { get; private set; }

        /// <summary>
        /// 游戏模式
        /// </summary>
        public GameMode Mode { get; private set; }

        /// <summary>
        /// 是否正在追帧（断线重连后快速追赶）
        /// </summary>
        public bool IsCatchingUp { get; private set; }
        
        /// <summary>
        /// 是否暂停游戏
        /// </summary>
        public bool IsPaused { get; private set; }
        
        /// <summary>
        /// 创世阶段创建的实体事件列表
        /// </summary>
        protected List<UnitCreatedEvent> _genesisEntities = new List<UnitCreatedEvent>();
        
        /// <summary>
        /// 是否已经发布创世阶段实体事件
        /// </summary>
        private bool _hasPublishedGenesisEntities = false;

        private readonly int _frameRate;
        private int _localPlayerId;
        /// <summary>
        /// 本轮追帧期间观察到的最大待执行帧数，用于按真实积压峰值计算追帧进度。
        /// </summary>
        private int _catchUpPeakPendingFrames;

        /// <summary>
        /// 获取导航系统（虚方法，由子类实现）
        /// </summary>
        /// <returns>导航系统实例</returns>
        public virtual FlowFieldNavigationSystem GetNavSystem()
        {
            return null; // 基类返回null，由子类重写
        }

        public virtual IFlowFieldMap GetMapManager()
        {
            return null; // 返回BattleGame的NavSystem
        }

        public virtual FlowFieldManager GetFlowFieldManager()
        {
            return null; // 返回BattleGame的NavSystem
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mode">游戏模式（默认单机）</param>
        /// <param name="frameRate">逻辑帧率（默认20帧/秒）</param>
        /// <param name="localPlayerId">本地玩家ID（默认0）</param>
        public Game(GameMode mode = GameMode.Standalone, int frameRate = 20, int localPlayerId = 0)
        {
            Mode = mode;
            _frameRate = frameRate;
            _localPlayerId = localPlayerId;
        }

        /// <summary>
        /// 初始化游戏
        /// </summary>
        public void Init()
        {
            // 创建唯一的 zWorld
            World = new zWorld();
            // 修改Unity的固定帧率
            UnityEngine.Time.fixedDeltaTime = 1f / _frameRate;
            World.Init(_frameRate);
            World.GameInstance = this; // 设置Game实例引用

            // 根据游戏模式创建帧同步管理器
            if (Mode == GameMode.NetworkClient)
            {
                FrameSyncManager = new FrameSyncManager(World);
                zUDebug.Log($"[Game] 网络客户端模式 - 启用帧同步");
            }

            // 注册游戏系统
            RegisterSystems();

            zUDebug.Log($"[Game] 初始化完成 - 模式:{Mode}, 帧率:{_frameRate}, 玩家ID:{_localPlayerId}");
        }

        /// <summary>
        /// 注册所有游戏系统
        /// 子类可以重写此方法自定义系统
        /// </summary>
        protected virtual void RegisterSystems()
        {

        }

        /// <summary>
        /// 游戏主更新循环
        /// 由外部驱动（Unity的FixedUpdate或服务器的循环）
        /// 
        /// 不同模式的行为：
        /// - Standalone: 每帧都执行
        /// - NetworkClient: 只有服务器确认的帧才执行（可能等待）
        /// - NetworkServer: 每帧都执行（服务器不等待）
        /// - Replay: 按录制顺序执行
        /// </summary>
        /// <returns>是否执行了逻辑更新（网络模式可能返回false表示等待中）</returns>
        public bool Update()
        {
            // 暂停状态下不执行逻辑更新（追帧模式除外）
            if (IsPaused && !IsCatchingUp)
            {
                return false;
            }

            // 追帧模式：优先处理
            if (IsCatchingUp)
            {
                return UpdateCatchUp();
            }

            switch (Mode)
            {
                case GameMode.Standalone:
                case GameMode.NetworkServer:
                case GameMode.Replay:
                    // 单机/服务器/回放模式：直接执行
                    ExecuteLogicFrame();
                    return true;

                case GameMode.NetworkClient:
                    // 网络客户端模式：需要等待服务器确认
                    if (FrameSyncManager == null)
                    {
                        UnityEngine.Debug.LogError("[Game] 网络模式下FrameSyncManager未初始化！");
                        return false;
                    }

                    // 1. 检查是否可以推进
                    if (!FrameSyncManager.CanAdvanceFrame())
                    {
                        return false; // 等待服务器确认
                    }

                    // 2. 准备下一帧的命令（不执行world.Update）
                    int frameNumber = FrameSyncManager.PrepareNextFrame();
                    
                    // 3. 执行逻辑帧（传入目标帧号，确保 Tick 同步）
                    ExecuteLogicFrame(frameNumber);
                    
                    // zUDebug.Log($"[Game] 执行网络帧 {frameNumber}, world.Tick={World.Tick}");
                    return true;

                default:
                    UnityEngine.Debug.LogError($"[Game] 未知的游戏模式: {Mode}");
                    return false;
            }
        }

        /// <summary>
        /// 追帧更新（快速执行，不等待）
        /// </summary>
        /// <returns>是否还在追帧</returns>
        private bool UpdateCatchUp()
        {
            if (FrameSyncManager == null)
            {
                UnityEngine.Debug.LogError("[Game] 追帧模式下FrameSyncManager未初始化！");
                IsCatchingUp = false;
                return false;
            }

            // 检查是否还有待执行的帧
            if (!FrameSyncManager.HasPendingFrames())
            {
                // 追帧完成
                StopCatchUp();
                return false;
            }

            // 执行一帧
            int frameNumber = FrameSyncManager.PrepareNextFrame();
            ExecuteLogicFrame(frameNumber);

            // 继续追帧
            return true;
        }

        /// <summary>
        /// 执行一个逻辑帧（内部方法）
        /// 所有模式都统一使用此方法
        /// </summary>
        /// <param name="targetTick">目标帧号（可选，用于锁帧同步）</param>
        private void ExecuteLogicFrame(int? targetTick = null)
        {
            // 1. 清空上一帧的事件（修复时序问题）
            World.EventManager.Clear();

            // 特殊处理：在第一帧发布创世阶段创建的实体事件
            if (!_hasPublishedGenesisEntities && _genesisEntities.Count > 0)
            {
                zUDebug.Log($"[Game] 发布 {_genesisEntities.Count} 个创世阶段创建的实体事件");
                foreach (var entityEvent in _genesisEntities)
                {
                    World.EventManager.Publish(entityEvent);
                }
                OnWorldInitialized();
                _hasPublishedGenesisEntities = true;
            }

            // 2. 更新唯一的 zWorld
            // - 单机模式：不传 targetTick，自动递增
            // - 网络模式：传入 targetTick，由锁帧系统控制
            World.Update(targetTick);
        }

        /// <summary>
        /// 提交命令（统一入口）
        /// 
        /// 不同模式的行为：
        /// - Standalone: 直接提交到CommandManager（立即执行）
        /// - NetworkClient: 发送到服务器，等待确认后执行
        /// - NetworkServer: 直接提交（服务器权威）
        /// </summary>
        public void SubmitCommand(ICommand command)
        {
            if (command.CampId == 0)
            {
                command.CampId = World.ComponentManager.GetGlobalComponent<GlobalInfoComponent>().LocalPlayerCampId;
            }

            switch (Mode)
            {
                case GameMode.Standalone:
                case GameMode.NetworkServer:
                case GameMode.Replay:
                    // 单机/服务器模式：直接提交
                    World.CommandManager.SubmitCommand(command);
                    break;

                case GameMode.NetworkClient:
                    // 网络客户端模式：发送到服务器
                    if (FrameSyncManager == null)
                    {
                        UnityEngine.Debug.LogError("[Game] 网络模式下FrameSyncManager未初始化！");
                        return;
                    }

                    FrameSyncManager.SubmitLocalCommand(command);
                    break;
            }
        }

        /// <summary>
        /// 开始追帧（断线重连后调用）
        /// </summary>
        /// <param name="catchUpCommands">需要追赶的帧命令（帧号 → 命令列表）</param>
        public void StartCatchUp(Dictionary<int, List<ICommand>> catchUpCommands)
        {
            if (Mode != GameMode.NetworkClient)
            {
                UnityEngine.Debug.LogWarning("[Game] 只有网络客户端模式才能追帧");
                return;
            }

            if (FrameSyncManager == null)
            {
                UnityEngine.Debug.LogError("[Game] 追帧失败：FrameSyncManager 未初始化");
                return;
            }

            if (catchUpCommands == null || catchUpCommands.Count == 0)
            {
                zUDebug.LogWarning("[Game] 没有需要追赶的帧");
                return;
            }

            // 批量确认所有帧
            FrameSyncManager.ConfirmFrames(catchUpCommands);

            // 进入追帧模式
            IsCatchingUp = true;

            int pendingFrames = FrameSyncManager.GetPendingFrameCount();
            BeginCatchUpProgress(pendingFrames);
            zUDebug.Log($"[Game] 开始追帧，当前帧: {World.Tick}, 目标帧: {World.Tick + pendingFrames}, 需追赶: {pendingFrames} 帧");
        }

        /// <summary>
        /// 尝试进入网络追帧状态，用于服务器确认帧已经积压到本地执行帧之后的自动追赶场景。
        /// 此方法只根据 FrameSyncManager 中已确认和已缓存的帧数据切换追帧状态，不负责注入命令或批量确认帧。
        /// </summary>
        /// <returns>成功进入追帧状态时返回 true；当前模式不支持、正在追帧或没有待执行帧时返回 false。</returns>
        public bool TryStartCatchUp()
        {
            if (Mode != GameMode.NetworkClient)
            {
                return false;
            }

            if (FrameSyncManager == null)
            {
                UnityEngine.Debug.LogError("[Game] 追帧失败：FrameSyncManager 未初始化");
                return false;
            }

            if (IsCatchingUp)
            {
                return false;
            }

            int pendingFrames = FrameSyncManager.GetPendingFrameCount();
            if (pendingFrames <= 0)
            {
                return false;
            }

            IsCatchingUp = true;
            BeginCatchUpProgress(pendingFrames);
            zUDebug.Log($"[Game] 自动进入追帧，当前帧: {World.Tick}, 目标帧: {World.Tick + pendingFrames}, 需追赶: {pendingFrames} 帧");
            return true;
        }

        /// <summary>
        /// 初始化本轮追帧的积压峰值，用于后续按追帧期间观察到的最大积压计算进度。
        /// </summary>
        /// <param name="pendingFrames">进入追帧状态时帧同步系统中尚未执行的确认帧数量。</param>
        private void BeginCatchUpProgress(int pendingFrames)
        {
            _catchUpPeakPendingFrames = System.Math.Max(0, pendingFrames);
        }

        /// <summary>
        /// 刷新本轮追帧期间观察到的最大积压帧数，处理追帧已启动后网络继续确认大量帧的场景。
        /// </summary>
        /// <param name="pendingFrames">当前帧同步系统中尚未执行的确认帧数量。</param>
        private void RefreshCatchUpProgressPeak(int pendingFrames)
        {
            if (pendingFrames > _catchUpPeakPendingFrames)
            {
                _catchUpPeakPendingFrames = pendingFrames;
            }
        }

        /// <summary>
        /// 停止追帧（追赶完成）
        /// </summary>
        private void StopCatchUp()
        {
            IsCatchingUp = false;
            _catchUpPeakPendingFrames = 0;
            
            zUDebug.Log($"[Game] 追帧完成，当前帧: {World.Tick}");
            
            // TODO: 通知表现层重新同步所有视图
            // 因为追帧期间可能禁用了渲染
        }

        /// <summary>
        /// 获取追帧进度（0-1）
        /// </summary>
        /// <returns>追帧完成比例，范围为 0 到 1；未追帧时返回 1。</returns>
        public float GetCatchUpProgress()
        {
            if (!IsCatchingUp || FrameSyncManager == null)
                return 1f;

            int pending = FrameSyncManager.GetPendingFrameCount();
            if (pending == 0)
                return 1f;

            RefreshCatchUpProgressPeak(pending);
            if (_catchUpPeakPendingFrames <= 0)
                return 1f;

            float progress = 1f - pending / (float)_catchUpPeakPendingFrames;
            return System.Math.Max(0f, System.Math.Min(1f, progress));
        }

        /// <summary>
        /// 关闭游戏
        /// </summary>
        public void Shutdown()
        {
            World?.Shutdown();
            zUDebug.Log("[Game] 已关闭");
        }
        
        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
            zUDebug.Log("[Game] 游戏已暂停");
        }
        
        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
            zUDebug.Log("[Game] 游戏已恢复");
        }
        
        /// <summary>
        /// 当游戏世界初始化完成时调用（创世阶段完成）
        /// </summary>
        public void OnWorldInitialized()
        {
            zUDebug.Log("[Game] 世界初始化完成，准备开始帧同步");
            // 可以在这里设置一个标志，表示创世阶段已完成，可以开始处理帧逻辑
        }
        
        /// <summary>
        /// 根据匹配成功协议初始化游戏世界（创世阶段）
        /// </summary>
        /// <param name="initialState">初始状态数据</param>
        public void InitializeWorldFromMatchData(JObject initialState)
        {
            if (World == null)
            {
                zUDebug.LogError("[Game] 世界未初始化，无法执行创世阶段");
                return;
            }

            zUDebug.Log($"[Game] 开始创世阶段，初始化游戏世界状态");

            // 创建一个临时列表来存储创世阶段创建的实体信息
            var createdEntities = new List<UnitCreatedEvent>();

            // 解析initialState数据并创建初始单位和建筑
            foreach (var playerData in initialState)
            {
                string playerIdStr = playerData.Key;
                int playerId = int.Parse(playerIdStr);
                
                var playerObj = (JObject)playerData.Value;
                
                // 创建建筑
                var buildings = (JArray)playerObj["buildings"];
                if (buildings != null)
                {
                    foreach (JObject building in buildings)
                    {
                        string id = building["id"]?.ToString();
                        string type = building["type"]?.ToString();
                        float x = building["x"]?.ToObject<float>() ?? 0;
                        float y = building["y"]?.ToObject<float>() ?? 0;
                        
                        // 根据类型创建建筑实体
                        var entityEvent = CreateBuildingEntity(playerId, type, new zVector3((zfloat)x, zfloat.Zero, (zfloat)y));
                        if (entityEvent.HasValue)
                        {
                            createdEntities.Add(entityEvent.Value);
                        }
                    }
                }
                
                // 创建单位
                var units = (JArray)playerObj["units"];
                if (units != null)
                {
                    foreach (JObject unit in units)
                    {
                        string id = unit["id"]?.ToString();
                        string type = unit["type"]?.ToString();
                        float x = unit["x"]?.ToObject<float>() ?? 0;
                        float y = unit["y"]?.ToObject<float>() ?? 0;
                        
                        // 根据类型创建单位实体
                        var entityEvent = CreateUnitEntity(playerId, type, new zVector3((zfloat)x, zfloat.Zero, (zfloat)y));
                        if (entityEvent.HasValue)
                        {
                            createdEntities.Add(entityEvent.Value);
                        }
                    }
                }
            }
            
            // 将创世阶段创建的实体信息存储起来，以便在第一帧时发布事件
            _genesisEntities = createdEntities;
            
            zUDebug.Log($"[Game] 创世阶段完成，游戏世界已初始化，共创建 {createdEntities.Count} 个实体");
        }

        private UnitCreatedEvent? CreateBuildingEntity(int playerId, string type, zVector3 position)
        {
            // 根据建筑类型确定建筑类型ID和尺寸
            int buildingType = 0; // 默认为基地
            int width = 2;
            int height = 2;
            int prefabId = 0;
            
            switch (type)
            {
                case "tankFactory":
                    buildingType = 1; // 假设1为坦克工厂
                    width = 3;
                    height = 3;
                    prefabId = 2; // 假设2为坦克工厂预制体ID
                    break;
                default:
                    buildingType = 0; // 基地
                    width = 2;
                    height = 2;
                    prefabId = 1; // 假设1为基地预制体ID
                    break;
            }

            // 1. 创建建筑实体
            var entity = World.EntityManager.CreateEntity();

            // 2. 添加Transform组件
            TransformComponent transformComponent = TransformComponent.Create(position, zQuaternion.identity, zVector3.one * 3);

            World.ComponentManager.AddComponent(entity, transformComponent);

            // 3. 添加建筑组件
            var buildingComponent = BuildingComponent.Create(
                buildingType, 0, 0, width, height); // 网格坐标暂时设为0，需要实际计算
            World.ComponentManager.AddComponent(entity, buildingComponent);

            // 4. 添加阵营组件
            var campComponent = CampComponent.Create(playerId);
            World.ComponentManager.AddComponent(entity, campComponent);

            // 5. 添加生命值组件
            var healthComponent = CreateBuildingHealthComponent(buildingType);
            World.ComponentManager.AddComponent(entity, healthComponent);

            // 6. 如果建筑有攻击能力（如防御塔），添加攻击组件
            if (CanBuildingAttack(buildingType))
            {
                var attackComponent = CreateBuildingAttackComponent(buildingType);
                World.ComponentManager.AddComponent(entity, attackComponent);
            }

            // 7. 创建但不发布事件，返回事件对象
            var unitCreatedEvent = new UnitCreatedEvent
            {
                EntityId = entity.Id,
                UnitType = buildingType,
                Position = position,
                PlayerId = playerId,
                PrefabId = prefabId
            };

            zUDebug.Log($"[Game] 创建建筑: Player {playerId}, Type {type} (ID: {buildingType}), Position {position}");
            
            // 返回事件对象，而不是直接发布
            return unitCreatedEvent;
        }

        private UnitCreatedEvent? CreateUnitEntity(int playerId, string type, zVector3 position)
        {
            // 根据单位类型确定单位类型ID
            int unitType = 1; // 默认为动员兵
            int prefabId = 1; // 默认预制体ID
            
            switch (type)
            {
                case "tank":
                    unitType = 2; // 坦克
                    prefabId = 2; // 坦克预制体ID
                    break;
                default:
                    unitType = 1; // 动员兵
                    prefabId = 1; // 动员兵预制体ID
                    break;
            }

            // 1. 创建单位实体
            var entity = World.EntityManager.CreateEntity();

            // 2. 添加Transform组件
            TransformComponent transformComponent = TransformComponent.Create(position, zQuaternion.identity, zVector3.one);

            World.ComponentManager.AddComponent(entity, transformComponent);


            // 3. 添加Unit组件（根据类型）
            var unitComponent = CreateUnitComponent(unitType, playerId);
            World.ComponentManager.AddComponent(entity, unitComponent);

            // 4. 添加Health组件
            var healthComponent = CreateUnitHealthComponent(unitType);
            World.ComponentManager.AddComponent(entity, healthComponent);

            // 5. 如果单位有攻击能力，添加Attack组件
            if (CanUnitAttack(unitType))
            {
                var attackComponent = CreateUnitAttackComponent(unitType);
                World.ComponentManager.AddComponent(entity, attackComponent);
            }

            // 6. 创建但不发布事件，返回事件对象
            var unitCreatedEvent = new UnitCreatedEvent
            {
                EntityId = entity.Id,
                UnitType = unitType,
                Position = position,
                PlayerId = playerId,
                PrefabId = prefabId
            };

            zUDebug.Log($"[Game] 创建单位: Player {playerId}, Type {type} (ID: {unitType}), Position {position}");
            
            // 返回事件对象，而不是直接发布
            return unitCreatedEvent;
        }

        #region 单位辅助方法

        private Simulation.ECS.Components.UnitComponent CreateUnitComponent(int unitType, int playerId)
        {
            switch (unitType)
            {
                case 1: // 动员兵
                    return Simulation.ECS.Components.UnitComponent.CreateInfantry(playerId);
                case 2: // 犀牛坦克
                    return Simulation.ECS.Components.UnitComponent.CreateTank(playerId);
                case 3: // 矿车
                    return Simulation.ECS.Components.UnitComponent.CreateHarvester(playerId);
                default:
                    return Simulation.ECS.Components.UnitComponent.CreateInfantry(playerId);
            }
        }

        private Simulation.ECS.Components.HealthComponent CreateUnitHealthComponent(int unitType)
        {
            switch (unitType)
            {
                case 1: // 动员兵
                    return new Simulation.ECS.Components.HealthComponent((zfloat)50.0f);
                case 2: // 犀牛坦克
                    return new Simulation.ECS.Components.HealthComponent((zfloat)200.0f);
                case 3: // 矿车
                    return new Simulation.ECS.Components.HealthComponent((zfloat)150.0f);
                default:
                    return new Simulation.ECS.Components.HealthComponent((zfloat)100.0f);
            }
        }

        private Simulation.ECS.Components.AttackComponent CreateUnitAttackComponent(int unitType)
        {
            switch (unitType)
            {
                case 1: // 动员兵
                    return new Simulation.ECS.Components.AttackComponent
                    {
                        Range = (zfloat)4.0f,
                        AttackInterval = (zfloat)1.0f,
                        TimeSinceLastAttack = zfloat.Zero,
                        TargetEntityId = -1
                    };
                case 2: // 犀牛坦克
                    return new Simulation.ECS.Components.AttackComponent
                    {
                        Range = (zfloat)8.0f,
                        AttackInterval = (zfloat)2.0f,
                        TimeSinceLastAttack = zfloat.Zero,
                        TargetEntityId = -1
                    };
                default:
                    return Simulation.ECS.Components.AttackComponent.CreateDefault();
            }
        }

        private bool CanUnitAttack(int unitType)
        {
            switch (unitType)
            {
                case 1: // 动员兵
                case 2: // 犀牛坦克
                    return true;
                case 3: // 矿车
                    return false;
                default:
                    return false;
            }
        }

        #endregion

        #region 建筑辅助方法

        private Simulation.ECS.Components.HealthComponent CreateBuildingHealthComponent(int buildingType)
        {
            switch (buildingType)
            {
                case 0: // 基地
                    return new Simulation.ECS.Components.HealthComponent((zfloat)1000.0f);
                case 1: // 防御塔/工厂
                    return new Simulation.ECS.Components.HealthComponent((zfloat)500.0f);
                default:
                    return new Simulation.ECS.Components.HealthComponent((zfloat)500.0f);
            }
        }

        private Simulation.ECS.Components.AttackComponent CreateBuildingAttackComponent(int buildingType)
        {
            switch (buildingType)
            {
                case 1: // 防御塔
                    return new Simulation.ECS.Components.AttackComponent
                    {
                        Range = (zfloat)12.0f,
                        AttackInterval = (zfloat)1.5f,
                        TimeSinceLastAttack = zfloat.Zero,
                        TargetEntityId = -1
                    };
                default:
                    return Simulation.ECS.Components.AttackComponent.CreateDefault();
            }
        }

        private bool CanBuildingAttack(int buildingType)
        {
            switch (buildingType)
            {
                case 0: // 基地
                    return false;
                case 1: // 防御塔/工厂
                    return true; // 假设工厂可以攻击
                default:
                    return false;
            }
        }

        #endregion
    }
}
