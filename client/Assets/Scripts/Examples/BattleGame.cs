using ZLockstep.Sync;
using ZLockstep.Simulation;
using ZLockstep.Simulation.ECS.Systems;
using ZLockstep.Sync.Command.Commands;
using ZLockstep.Flow;
using ZLockstep.RVO;
using zUnity;
using ZLockstep.Simulation.ECS;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using ZLockstep.Simulation.Events;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Sync.Command;
using ZLockstep.Simulation.ECS.Systems.AI;

namespace Game.Examples
{
    /// <summary>
    /// 战斗游戏类
    /// 继承自Game，添加战斗相关的系统和管理器
    /// </summary>
    public class BattleGame : ZLockstep.Sync.Game
    {
        /// <summary>
        /// 地图管理器（256x256，每格1米）
        /// </summary>
        public SimpleMapManager MapManager { get; private set; }

        /// <summary>
        /// 流场管理器
        /// </summary>
        public FlowFieldManager FlowFieldManager { get; private set; }

        /// <summary>
        /// 导航系统
        /// </summary>
        public FlowFieldNavigationSystem NavSystem { get; private set; }

        /// <summary>
        /// 当前玩法人数，用于决定创世阵营和是否注册单人 AI 系统。
        /// </summary>
        public int PlayerNumber { get; private set; }

        /// <summary>
        /// 创建战斗游戏实例，并记录玩法人数以便初始化系统和创世数据。
        /// </summary>
        /// <param name="mode">游戏运行模式。</param>
        /// <param name="frameRate">逻辑帧帧率。</param>
        /// <param name="localPlayerId">本地玩家 ID。</param>
        /// <param name="playerNumber">玩法人数，1 表示单人对 AI，2 表示双人对战。</param>
        public BattleGame(GameMode mode = GameMode.Standalone, int frameRate = 20, int localPlayerId = 0, int playerNumber = 1)
            : base(mode, frameRate, localPlayerId)
        {
            PlayerNumber = NormalizePlayerNumber(playerNumber);
        }

        /// <summary>
        /// 获取导航系统（重写基类方法）
        /// </summary>
        /// <returns>导航系统实例</returns>
        public override FlowFieldNavigationSystem GetNavSystem()
        {
            return NavSystem; // 返回BattleGame的NavSystem
        }

        public override IFlowFieldMap GetMapManager()
        {
            return MapManager; // 返回BattleGame的NavSystem
        }

        public override FlowFieldManager GetFlowFieldManager()
        {
            return FlowFieldManager; // 返回BattleGame的NavSystem
        }

        /// <summary>
        /// 初始化战斗游戏
        /// </summary>
        public new void Init()
        {
            // 初始化命令映射
            CommandMapper.Initialize();
            // 初始化地图和导航系统
            InitializeMapAndNavigation();
            // 先调用基类初始化
            base.Init();

            zUDebug.Log("[BattleGame] 战斗游戏初始化完成");
        }

        /// <summary>
        /// 初始化地图和导航系统
        /// </summary>
        private void InitializeMapAndNavigation()
        {
            // 1. 创建256x256地图，每格1.0米
            MapManager = new SimpleMapManager();
            MapManager.Initialize(128, 128, 2);
            zUDebug.Log("[BattleGame] 地图初始化: 256x256格，每格1.0米");

            // 2. 设置地图边界为障碍物
            // SetupMapBorders();

            // 3. 创建流场管理器
            FlowFieldManager = new FlowFieldManager();
            FlowFieldManager.Initialize(MapManager, maxFields: 30);
            FlowFieldManager.SetMaxUpdatesPerFrame(2);
            zUDebug.Log("[BattleGame] 流场管理器初始化完成");
        }

        /// <summary>
        /// 注册游戏系统
        /// </summary>
        protected override void RegisterSystems()
        {
            //  创建并注册导航系统(去掉MovementSystem基础移动系统)
            NavSystem = new FlowFieldNavigationSystem();
            World.SystemManager.RegisterSystem(NavSystem);
            NavSystem.InitializeNavigation(FlowFieldManager, MapManager);
            zUDebug.Log("[BattleGame] 导航系统注册完成");

            // 注册自动追击系统（在战斗系统之前，确保单位能追到敌人）
            var autoChaseSystem = new AutoChaseSystem();
            World.SystemManager.RegisterSystem(autoChaseSystem);
            autoChaseSystem.Initialize(NavSystem);

            // 注册战斗系统
            World.SystemManager.RegisterSystem(new CombatSystem());

            // 注册旋转系统（在战斗系统之后，处理车体和炮塔旋转）
            // World.SystemManager.RegisterSystem(new RotationSystem());
            zUDebug.Log("[BattleGame] 旋转系统注册完成");

            World.SystemManager.RegisterSystem(new ProjectileSystem());

            // 注册生命值系统（在 ProjectileSystem 之后，DeathRemovalSystem 之前）
            World.SystemManager.RegisterSystem(new HealthSystem());
            zUDebug.Log("[BattleGame] 生命值系统注册完成");

            // 注册自动回血系统（在 HealthSystem 之后，处理受伤后的自动回血）
            World.SystemManager.RegisterSystem(new AutoHealSystem());
            zUDebug.Log("[BattleGame] 自动回血系统注册完成");

            // 注册销毁系统
            World.SystemManager.RegisterSystem(new DeathRemovalSystem());

            // 注册AI系统（回放模式和双人玩法不注册）
            if (Mode != GameMode.Replay && PlayerNumber != 2)
            {
                // AISystem = new SimpleAISystem();
                FirstAISystem AISystem = new FirstAISystem();
                World.SystemManager.RegisterSystem(AISystem);
                AISystem.Initialize(NavSystem);
            }
            else
            {
                zUDebug.Log($"[BattleGame] 当前模式或玩法不需要 AI，跳过AI系统注册。Mode:{Mode}, PlayerNumber:{PlayerNumber}");
            }

            // 注册生产系统
            World.SystemManager.RegisterSystem(new ProduceSystem());
            // 注册经济系统
            World.SystemManager.RegisterSystem(new EconomySystem());

            // 注册结算系统 (回放模式不注册，避免回放结束后结算系统发出错误的游戏结束事件)
            if (Mode != GameMode.Replay)
            {
                World.SystemManager.RegisterSystem(new SettlementSystem());
            }

            World.SystemManager.RegisterSystem(new BuildingConstructionSystem());

            zUDebug.Log("[BattleGame] 游戏系统注册完成");
        }

        /// <summary>
        /// 按当前玩法人数读取初始配置并创建战斗世界，单人创建 1、9 阵营，双人创建 1、2 阵营。
        /// </summary>
        public void CreateWorldByConfig()
        {
            List<int> campIDs = GetCampIdsByPlayerNumber(PlayerNumber);
            List<ConfInitUnits> confs = ConfigManager.GetAll<ConfInitUnits>();

            // 创建一个临时列表来存储创世阶段创建的实体信息
            var createdEntities = new List<UnitCreatedEvent>();
            foreach (var conf in confs)
            {
                if (conf.Enabled == 0) continue;

                if (!campIDs.Contains(conf.Camp)) continue;

                var confPos = StringToVector3Converter.StringToZVector3(conf.Position);

                if (conf.Type == 1)
                {
                    var confBuilding = ConfigManager.Get<ConfBuilding>(conf.ConfID);
                    if (confBuilding == null)
                    {
                        zUDebug.LogError($"[BattleGame] 创建建筑时无法获取建筑配置信息。ID:{conf.ConfID}");
                        continue;
                    }

                    // 建筑
                    var entEvent = EntityCreationManager.CreateBuildingEntity(World, conf.Camp,
                        new zVector3(confPos.x, confPos.y, confPos.z),
                        confBuildingID: conf.ConfID, mapManager:MapManager, flowFieldManager:FlowFieldManager);
                    if (entEvent.HasValue)
                    {
                        createdEntities.Add(entEvent.Value);
                    }

                    if (confBuilding.Type == (int)BuildingType.Base)
                    {
                        ConfCamp confCamp = ConfigManager.Get<ConfCamp>(conf.Camp);
                        int initMoney = confCamp?.InitMoney ?? 0;
                        int initPower = confCamp?.InitPower ?? 0;

                        // 添加经济
                        var ecoEntity = World.EntityManager.CreateEntity();
                        World.ComponentManager.AddComponent(ecoEntity, EconomyComponent.Create(initMoney, initPower));
                        World.ComponentManager.AddComponent(ecoEntity, CampComponent.Create(conf.Camp));
                    }
                } else if (conf.Type == 2)
                {
                    // 单位
                    var entEvent = EntityCreationManager.CreateUnitEntity(World, conf.Camp, (UnitType)conf.ConfID,
                        new zVector3(confPos.x, confPos.y, confPos.z),
                        prefabId: conf.PrefabId);
                    if (entEvent.HasValue)
                    {
                        createdEntities.Add(entEvent.Value);
                    }
                }
            }

            // 将创世阶段创建的实体信息存储起来，以便在第一帧时发布事件
            _genesisEntities = createdEntities;
            
            zUDebug.Log($"[BattleGame] 通过配置数据进行创世阶段，游戏世界已初始化，共创建 {createdEntities.Count} 个实体");
        }

        private int NormalizePlayerNumber(int playerNumber)
        {
            if (playerNumber == 1 || playerNumber == 2)
            {
                return playerNumber;
            }

            zUDebug.LogWarning($"[BattleGame] 不支持的玩法人数:{playerNumber}，已按单人玩法处理。");
            return 1;
        }

        private List<int> GetCampIdsByPlayerNumber(int playerNumber)
        {
            if (playerNumber == 2)
            {
                return new List<int> { 1, 2 };
            }

            return new List<int> { 1, 9 };
        }

        /// <summary>
        /// 根据匹配成功协议初始化游戏世界（创世阶段）
        /// </summary>
        /// <param name="initialState">初始状态数据</param>
        public new void InitializeWorldFromMatchData(JObject initialState)
        {
            if (World == null)
            {
                zUDebug.LogError("[BattleGame] 世界未初始化，无法执行创世阶段");
                return;
            }

            zUDebug.Log($"[BattleGame] 开始创世阶段，初始化游戏世界状态");

            // 创建一个临时列表来存储创世阶段创建的实体信息
            var createdEntities = new List<UnitCreatedEvent>();

            // 解析initialState数据并创建初始单位和建筑
            foreach (var playerData in initialState)
            {
                // 处理中立建筑
                if (playerData.Key == "neutral")
                {
                    var neutralObj = (JObject)playerData.Value;
                    
                    // 创建中立建筑
                    var neutralBuildings = (JArray)neutralObj["buildings"];
                    if (neutralBuildings != null)
                    {
                        foreach (JObject building in neutralBuildings)
                        {
                            string id = building["id"]?.ToString();
                            string type = building["type"]?.ToString();
                            float x = building["x"]?.ToObject<float>() ?? 0;
                            float y = building["y"]?.ToObject<float>() ?? 0;

                            // 根据类型创建建筑实体 (中立建筑使用特殊playerId，比如-1)
                            var entityEvent = EntityCreationManager.CreateBuildingEntity(World, -1,
                            new zVector3((zfloat)x, zfloat.Zero, (zfloat)y),
                            confBuildingID:2, mapManager:MapManager, flowFieldManager:FlowFieldManager);
                            if (entityEvent.HasValue)
                            {
                                createdEntities.Add(entityEvent.Value);
                            }
                        }
                    }
                    continue; // 处理完中立建筑后继续下一个
                }

                string campIdStr = playerData.Key;
                int campId = int.Parse(campIdStr);
                
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
                        var entityEvent = EntityCreationManager.CreateBuildingEntity(World, campId,
                        new zVector3((zfloat)x, zfloat.Zero, (zfloat)y),
                        confBuildingID: 1, mapManager:MapManager, flowFieldManager:FlowFieldManager);
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
                        
                        // 使用EntityCreationManager创建单位
                        int prefabId = 6; // 默认预制体ID
                        var unitEvent = EntityCreationManager.CreateUnitEntity(World, campId, UnitType.grizzlyTank, 
                            new zVector3((zfloat)x, zfloat.Zero, (zfloat)y), prefabId);
                        if (unitEvent.HasValue)
                        {
                            createdEntities.Add(unitEvent.Value);
                        }
                    }
                }

                ConfCamp confCamp = ConfigManager.Get<ConfCamp>(campId);
                int initPower = confCamp?.InitPower ?? 10;
                
                // 添加经济组件（每个玩家一个全局实体）
                var money = playerObj["money"]?.ToObject<int>() ?? 2200; // 默认2200金币
                var power = playerObj["power"]?.ToObject<int>() ?? initPower;
                var economyEntity = World.EntityManager.CreateEntity();
                World.ComponentManager.AddComponent(economyEntity, EconomyComponent.Create(money, power));
                World.ComponentManager.AddComponent(economyEntity, CampComponent.Create(campId));
            }

            // 将创世阶段创建的实体信息存储起来，以便在第一帧时发布事件
            _genesisEntities = createdEntities;
            
            zUDebug.Log($"[BattleGame] 创世阶段完成，游戏世界已初始化，共创建 {createdEntities.Count} 个实体");
        }

        /// <summary>
        /// 关闭游戏
        /// </summary>
        public new void Shutdown()
        {
            base.Shutdown();
            zUDebug.Log("[BattleGame] 战斗游戏已关闭");
        }
    }
}

