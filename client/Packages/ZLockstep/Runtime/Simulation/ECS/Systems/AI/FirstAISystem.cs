using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Simulation.ECS.Systems;
using ZLockstep.Flow;
using ZLockstep.Sync.Command.Commands;
using zUnity;
using System.Collections.Generic;

namespace ZLockstep.Simulation.ECS.Systems.AI
{
    /// <summary>
    /// 简单 AI 系统
    /// 控制 AI 阵营（阵营 ID=2）的所有单位
    /// 策略：
    /// 1. 优先追逐附近的玩家单位（15 米侦测范围）
    /// 2. 如果附近没有玩家单位，则移动到玩家基地
    /// 3. 战斗由 CombatSystem 和 AutoChaseSystem 自动处理
    /// 4. 每 30 秒生产 10 个动员兵（反重装步兵）
    /// </summary>
    public class FirstAISystem : BaseSystem
    {
        /// <summary>
        /// AI 阵营 ID
        /// </summary>
        private const int AI_CAMP_ID = 9;

        /// <summary>
        /// 玩家阵营 ID
        /// </summary>
        private const int PLAYER_CAMP_ID = 1;

        /// <summary>
        /// 重新评估目标的间隔（秒）
        /// </summary>
        private zfloat _evaluationInterval = new zfloat(3);

        /// <summary>
        /// 距离上次评估的时间
        /// </summary>
        private zfloat _timeSinceLastEvaluation = zfloat.Zero;

        /// <summary>
        /// 侦测范围（米）
        /// </summary>
        private zfloat _detectionRange = new zfloat(15);

        /// <summary>
        /// 导航系统引用
        /// </summary>
        private FlowFieldNavigationSystem _navSystem;

        /// <summary>
        /// 玩家基地位置（缓存）
        /// </summary>
        private zVector2 _playerBasePosition = zVector2.zero;

        // ==================== 生产相关字段 ====================
        
        /// <summary>
        /// 生产间隔时间（秒）
        /// </summary>
        private readonly zfloat _productionInterval = new zfloat(20);
        
        /// <summary>
        /// 距离上次生产的时间
        /// </summary>
        private zfloat _timeSinceLastProduction = zfloat.Zero;
        
        /// <summary>
        /// 整备营区建筑类型
        /// </summary>
        private const int BARRACKS_BUILDING_TYPE = 5;
        private const int LIGHT_FACTORY_BUILDING_TYPE = 6;
        private const int HEAVY_FACTORY_BUILDING_TYPE = 7;

        /// <summary>
        /// 整备营区配置ID
        /// </summary>
        private const int BARRACKS_CONF_ID = 5;
        private const int LIGHT_FACTORY_CONF_ID = 6;
        private const int HEAVY_FACTORY_CONF_ID = 7;
        
        /// <summary>
        /// 动员兵单位类型
        /// </summary>
        private const UnitType INFANTRY_UNIT_TYPE = UnitType.Infantry;
        private const UnitType BADGER_TANK_UNIT_TYPE = UnitType.badgerTank;
        private const UnitType GRIZZLY_TANK_UNIT_TYPE = UnitType.grizzlyTank;
        
        /// <summary>
        /// 每次生产数量
        /// </summary>
        private const int INFANTRY_PER_PRODUCTION = 10;
        private const int BADGER_TANKS_PER_PRODUCTION = 6;
        private const int GRIZZLY_TANKS_PER_PRODUCTION = 4;

        /// <summary>
        /// AI 待提交的生产命令缓存。
        /// 只保存生产目标和数量，不保存建筑实体 ID，避免建筑销毁后继续使用过期 ID。
        /// </summary>
        private readonly List<PendingProduceRecord> _pendingProduceRecords = new List<PendingProduceRecord>();

        private List<int> offensebarracksIds = new List<int>();

        /// <summary>
        /// 记录 AI 延迟提交的生产需求。
        /// 每条记录只描述哪类建筑需要生产哪类单位，以及还有多少个单位等待逐帧转成命令。
        /// </summary>
        private struct PendingProduceRecord
        {
            /// <summary>
            /// 可生产目标单位的建筑类型。
            /// </summary>
            public int BuildingType;

            /// <summary>
            /// 需要生产的单位类型。
            /// </summary>
            public UnitType UnitType;

            /// <summary>
            /// 尚未提交为生产命令的单位数量。
            /// </summary>
            public int RemainingCount;

            /// <summary>
            /// 创建一条待生产记录。
            /// </summary>
            /// <param name="buildingType">可生产目标单位的建筑类型。</param>
            /// <param name="unitType">需要生产的单位类型。</param>
            /// <param name="remainingCount">尚未提交为生产命令的单位数量。</param>
            public PendingProduceRecord(int buildingType, UnitType unitType, int remainingCount)
            {
                BuildingType = buildingType;
                UnitType = unitType;
                RemainingCount = remainingCount;
            }
        }

        /// <summary>
        /// 初始化AI系统
        /// </summary>
        public void Initialize(FlowFieldNavigationSystem navSystem)
        {
            _navSystem = navSystem;

            // 添加AI组件
            ComponentManager.AddGlobalComponent(new AIComponent(AI_CAMP_ID, new List<AIActionType> { AIActionType.Produce, AIActionType.Attack }));
        }



        public override void Update()
        {
            if (_navSystem == null)
            {
                zUDebug.LogWarning("[SimpleAISystem] 导航系统未初始化");
                return;
            }

            AIComponent aIComponent = ComponentManager.GetGlobalComponent<AIComponent>();
            if (!aIComponent.IsEnabled)
                return;

            // 更新评估计时器
            _timeSinceLastEvaluation += DeltaTime;

            // 定期重新评估 AI 单位的行为
            if (_timeSinceLastEvaluation >= _evaluationInterval)
            {
                // 只有启用了攻击行为才执行
                if (aIComponent.EnabledActions.Contains(AIActionType.Attack))
                {
                    FindPlayerBuilding();
                    EvaluateAIUnits();
                }
                _timeSinceLastEvaluation = zfloat.Zero;
            }
            
            // 更新生产计时器
            _timeSinceLastProduction += DeltaTime;
            
            // 检查是否到达生产时间
            if (_timeSinceLastProduction >= _productionInterval)
            {
                // 只有启用了生产行为才执行
                if (aIComponent.EnabledActions.Contains(AIActionType.Produce))
                {
                    ProduceInfantryPeriodically();
                    EvaluateAIOffense();
                    FindPlayerBuilding();
                    MarchOffenseUnitsToEnemyBase();
                }
                _timeSinceLastProduction = zfloat.Zero;
            }

            if (aIComponent.EnabledActions.Contains(AIActionType.Produce))
            {
                ProcessPendingProduceCommand();
            }
        }

        /// <summary>
        /// 查找玩家基地位置
        /// </summary>
        private void FindPlayerBuilding()
        {
            var buildings = ComponentManager.GetAllEntityIdsWith<BuildingComponent>();

            foreach (var entityId in buildings)
            {
                Entity entity = new Entity(entityId);

                // 检查是否有Camp组件
                if (!ComponentManager.HasComponent<CampComponent>(entity))
                    continue;
                
                var camp = ComponentManager.GetComponent<CampComponent>(entity);
                var building = ComponentManager.GetComponent<BuildingComponent>(entity);

                // 找到玩家的建筑
                if (camp.CampId == PLAYER_CAMP_ID && ComponentManager.HasComponent<HealthComponent>(entity))
                {
                    if (ComponentManager.HasComponent<TransformComponent>(entity))
                    {
                        var transform = ComponentManager.GetComponent<TransformComponent>(entity);
                        _playerBasePosition = new zVector2(transform.Position.x, transform.Position.z);

                        // zUDebug.Log($"[SimpleAISystem] 找到玩家建筑 {building.BuildingType} 位置: {_playerBasePosition}");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 评估所有AI单位的行为
        /// </summary>
        private void EvaluateAIUnits()
        {
            var allEntities = ComponentManager.GetAllEntityIdsWith<CampComponent>();
            int chaseCount = 0;
            int moveToBaseCount = 0;

            foreach (var entityId in allEntities)
            {
                Entity entity = new Entity(entityId);

                var camp = ComponentManager.GetComponent<CampComponent>(entity);

                // 只处理AI阵营的单位
                if (camp.CampId != AI_CAMP_ID)
                    continue;

                // 必须是可移动单位（有FlowFieldNavigatorComponent）
                if (!ComponentManager.HasComponent<FlowFieldNavigatorComponent>(entity))
                    continue;

                // 必须有Transform组件
                if (!ComponentManager.HasComponent<TransformComponent>(entity))
                    continue;

                // 检查是否已有移动目标
                var navigator = ComponentManager.GetComponent<FlowFieldNavigatorComponent>(entity);
                var transform = ComponentManager.GetComponent<TransformComponent>(entity);

                // 如果已到达目标或没有目标，重新评估
                if (navigator.HasReachedTarget || !ComponentManager.HasComponent<MoveTargetComponent>(entity))
                {
                    // 1. 优先搜索附近的玩家单位
                    int nearestPlayerUnitId = FindNearestPlayerUnit(transform.Position);
                    
                    if (nearestPlayerUnitId >= 0)
                    {
                        // 找到玩家单位，追击
                        Entity target = new(nearestPlayerUnitId);
                        var targetTransform = ComponentManager.GetComponent<TransformComponent>(target);
                        zVector2 targetPos = new(targetTransform.Position.x, targetTransform.Position.z);
                        SendAIMoveCommand(entityId, targetPos, useFlowfield: false);
                        chaseCount++;
                    }
                    else
                    {
                        if (offensebarracksIds.Contains(entityId))
                        {
                            // 没有找到玩家单位，移动到玩家基地
                            SendAIMoveCommand(entityId, _playerBasePosition, useFlowfield: true);
                            moveToBaseCount++;
                        }
                    }
                }
            }

            if (chaseCount > 0 || moveToBaseCount > 0)
            {
                // zUDebug.Log($"[SimpleAISystem] 追击玩家单位数量: {chaseCount}，移动到基地单位数量: {moveToBaseCount}");
            }
        }

        /// <summary>
        /// 收集所有 AI 阵营的可移动单位 ID
        /// </summary>
        private void EvaluateAIOffense()
        {
            offensebarracksIds.Clear();
            var allEntities = ComponentManager.GetAllEntityIdsWith<CampComponent>();
            foreach (var entityId in allEntities)
            {
                Entity entity = new Entity(entityId);

                var camp = ComponentManager.GetComponent<CampComponent>(entity);

                // 只处理AI阵营的单位
                if (camp.CampId != AI_CAMP_ID)
                    continue;

                // 必须是可移动单位（有FlowFieldNavigatorComponent）
                if (!ComponentManager.HasComponent<FlowFieldNavigatorComponent>(entity))
                    continue;

                // 必须有Transform组件
                if (!ComponentManager.HasComponent<TransformComponent>(entity))
                    continue;

                offensebarracksIds.Add(entityId);
            }
        }

        private void MarchOffenseUnitsToEnemyBase()
        {
            if (_playerBasePosition == zVector2.zero)
                return;

            foreach (var entityId in offensebarracksIds)
            {
                Entity entity = new Entity(entityId);
                if (!ComponentManager.HasComponent<TransformComponent>(entity))
                    continue;
                if (!ComponentManager.HasComponent<FlowFieldNavigatorComponent>(entity))
                    continue;

                SendAIMoveCommand(entityId, _playerBasePosition, useFlowfield: true);
            }
        }

        /// <summary>
        /// 发送 AI 移动命令。
        /// 通过命令队列统一驱动移动，确保与命令回放/同步链路一致。
        /// </summary>
        /// <param name="entityId">需要移动的实体ID。</param>
        /// <param name="targetPos">目标位置。</param>
        /// <param name="useFlowfield">是否启用流场寻路。</param>
        private void SendAIMoveCommand(int entityId, zVector2 targetPos, bool useFlowfield)
        {
            var game = World.GameInstance;
            if (game == null)
            {
                zUDebug.LogError("[FirstAISystem] 无法获取 Game 实例，无法发送移动命令");
                return;
            }

            var moveCommand = new EntityMoveCommand(
                campId: AI_CAMP_ID,
                entityIds: new[] { entityId },
                targetPosition: targetPos,
                useFlowfield: useFlowfield,
                userInput: false
            )
            {
                Source = ZLockstep.Sync.Command.CommandSource.AI
            };

            game.SubmitCommand(moveCommand);
        }

        /// <summary>
        /// 搜索最近的玩家单位（使用 SpatialIndex）
        /// </summary>
        private int FindNearestPlayerUnit(zVector3 position)
        {
            // 使用 SpatialIndex 进行空间邻近查询
            var neighbors = SpatialIndex.Instance.RadialSearch(
                position, 
                (float)_detectionRange, 
                3, // 不限制数量
                neighbor => IsValidPlayerTarget(neighbor, position)
            );
            
            if (neighbors.Count == 0)
                return -1;
            
            // 找到最近的单位
            int nearestUnitId = -1;
            zfloat minDistSqr = _detectionRange * _detectionRange;
            
            foreach (var neighbor in neighbors)
            {
                Entity otherEntity = new Entity(neighbor.EntityId);
                var otherTransform = ComponentManager.GetComponent<TransformComponent>(otherEntity);
                zfloat distSqr = (otherTransform.Position - position).sqrMagnitude;
                
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    nearestUnitId = neighbor.EntityId;
                }
            }
            
            return nearestUnitId;
        }
        
        /// <summary>
        /// 判断玩家目标是否有效（是玩家阵营、非建筑、有必要的组件、存活）
        /// </summary>
        private bool IsValidPlayerTarget(SpatialEntry neighbor, zVector3 position)
        {
            // 检查是否为玩家阵营
            if (neighbor.CampId != PLAYER_CAMP_ID)
                return false;
            
            Entity otherEntity = new Entity(neighbor.EntityId);
            
            // 跳过建筑，只追单位
            if (ComponentManager.HasComponent<BuildingComponent>(otherEntity))
                return false;
            
            // 必须有 Transform 和 Health 组件
            if (!ComponentManager.HasComponent<TransformComponent>(otherEntity) ||
                !ComponentManager.HasComponent<HealthComponent>(otherEntity))
                return false;
            
            // 检查是否还活着
            var health = ComponentManager.GetComponent<HealthComponent>(otherEntity);
            if (health.CurrentHealth <= zfloat.Zero)
                return false;
            
            return true;
        }

        // ==================== 生产相关方法 ====================

        /// <summary>
        /// 定期生产动员兵（每 30 秒生产 10 个）
        /// </summary>
        private void ProduceInfantryPeriodically()
        {
            var barracks = FindAIBuildingsByType(BARRACKS_BUILDING_TYPE, INFANTRY_UNIT_TYPE);
            var lightFactories = FindAIBuildingsByType(LIGHT_FACTORY_BUILDING_TYPE, BADGER_TANK_UNIT_TYPE);
            var heavyFactories = FindAIBuildingsByType(HEAVY_FACTORY_BUILDING_TYPE, GRIZZLY_TANK_UNIT_TYPE);

            if (barracks.Count == 0)
            {
                SendBuildBuildingCommand(BARRACKS_CONF_ID, "整备营区");
            }
            if (lightFactories.Count == 0)
            {
                SendBuildBuildingCommand(LIGHT_FACTORY_CONF_ID, "轻工厂");
            }
            if (heavyFactories.Count == 0)
            {
                SendBuildBuildingCommand(HEAVY_FACTORY_CONF_ID, "重工厂");
            }

            ProduceUnitsByType(BARRACKS_BUILDING_TYPE, barracks.Count, INFANTRY_PER_PRODUCTION, INFANTRY_UNIT_TYPE);
            ProduceUnitsByType(LIGHT_FACTORY_BUILDING_TYPE, lightFactories.Count, BADGER_TANKS_PER_PRODUCTION, BADGER_TANK_UNIT_TYPE);
            ProduceUnitsByType(HEAVY_FACTORY_BUILDING_TYPE, heavyFactories.Count, GRIZZLY_TANKS_PER_PRODUCTION, GRIZZLY_TANK_UNIT_TYPE);

            zUDebug.Log($"[FirstAISystem] 生产需求已缓存：反重装步兵 {INFANTRY_PER_PRODUCTION}，獾式战车 {BADGER_TANKS_PER_PRODUCTION}，重装坦克 {GRIZZLY_TANKS_PER_PRODUCTION}");
        }

        /// <summary>
        /// 查找 AI 阵营的所有可用整备营区建筑
        /// </summary>
        /// <returns>整备营区实体 ID 列表</returns>
        private List<int> FindAIBuildingsByType(int buildingType, UnitType unitType)
        {
            // 获取所有建筑
            var buildings = ComponentManager.GetAllEntityIdsWith<BuildingComponent>();

            List<int> barracksList = new List<int>();

            foreach (var entityId in buildings)
            {
                Entity entity = new Entity(entityId);

                // 检查是否有 Camp 组件
                if (!ComponentManager.HasComponent<CampComponent>(entity))
                    continue;

                var camp = ComponentManager.GetComponent<CampComponent>(entity);

                // 只检查 AI 阵营
                if (camp.CampId != AI_CAMP_ID)
                    continue;

                var building = ComponentManager.GetComponent<BuildingComponent>(entity);
                if (building.BuildingType != buildingType)
                    continue;

                // 检查是否有 ProduceComponent
                if (!ComponentManager.HasComponent<ProduceComponent>(entity))
                    continue;

                var produce = ComponentManager.GetComponent<ProduceComponent>(entity);
                if (!produce.SupportedUnitTypes.Contains(unitType))
                    continue;

                barracksList.Add(entityId);
            }

            return barracksList;
        }

        /// <summary>
        /// 根据当前可用建筑数量缓存待生产单位。
        /// </summary>
        /// <param name="buildingType">可生产目标单位的建筑类型。</param>
        /// <param name="availableBuildingCount">当前可用建筑数量。</param>
        /// <param name="totalCount">本轮计划加入缓存的生产数量。</param>
        /// <param name="unitType">需要生产的单位类型。</param>
        private void ProduceUnitsByType(int buildingType, int availableBuildingCount, int totalCount, UnitType unitType)
        {
            if (availableBuildingCount == 0 || totalCount <= 0)
                return;

            EnqueueProduceUnits(buildingType, unitType, totalCount);
        }

        /// <summary>
        /// 将生产需求追加到缓存中，相同建筑类型和单位类型会合并数量。
        /// </summary>
        /// <param name="buildingType">可生产目标单位的建筑类型。</param>
        /// <param name="unitType">需要生产的单位类型。</param>
        /// <param name="count">需要追加的生产数量。</param>
        private void EnqueueProduceUnits(int buildingType, UnitType unitType, int count)
        {
            if (count <= 0)
                return;

            for (int i = 0; i < _pendingProduceRecords.Count; i++)
            {
                var record = _pendingProduceRecords[i];
                if (record.BuildingType == buildingType && record.UnitType == unitType)
                {
                    record.RemainingCount += count;
                    _pendingProduceRecords[i] = record;
                    return;
                }
            }

            _pendingProduceRecords.Add(new PendingProduceRecord(buildingType, unitType, count));
        }

        /// <summary>
        /// 每帧最多把一条待生产数量转成生产命令。
        /// 发送前实时查找可用建筑，避免缓存建筑 ID 导致命令指向失效实体。
        /// </summary>
        private void ProcessPendingProduceCommand()
        {
            if (_pendingProduceRecords.Count == 0)
                return;

            var record = _pendingProduceRecords[0];
            if (record.RemainingCount <= 0)
            {
                _pendingProduceRecords.RemoveAt(0);
                return;
            }

            if (!TryGetProduceBuildingId(record.BuildingType, record.UnitType, out int buildingId))
                return;

            SendProduceCommand(buildingId, 1, record.UnitType);

            record.RemainingCount--;
            if (record.RemainingCount <= 0)
            {
                _pendingProduceRecords.RemoveAt(0);
            }
            else
            {
                _pendingProduceRecords[0] = record;
            }
        }

        /// <summary>
        /// 实时查找可生产指定单位的 AI 建筑，并优先选择当前生产队列最短的建筑。
        /// </summary>
        /// <param name="buildingType">可生产目标单位的建筑类型。</param>
        /// <param name="unitType">需要生产的单位类型。</param>
        /// <param name="buildingId">找到的建筑实体 ID。</param>
        /// <returns>找到可用建筑时返回 true，否则返回 false。</returns>
        private bool TryGetProduceBuildingId(int buildingType, UnitType unitType, out int buildingId)
        {
            var buildings = FindAIBuildingsByType(buildingType, unitType);
            buildingId = -1;
            if (buildings.Count == 0)
                return false;

            int bestQueueCount = int.MaxValue;
            for (int i = 0; i < buildings.Count; i++)
            {
                int candidateId = buildings[i];
                int queueCount = GetProduceQueueCount(candidateId, unitType);
                if (buildingId == -1 || queueCount < bestQueueCount)
                {
                    buildingId = candidateId;
                    bestQueueCount = queueCount;
                }
            }

            return buildingId != -1;
        }

        /// <summary>
        /// 获取指定建筑当前排队生产指定单位的数量。
        /// </summary>
        /// <param name="buildingId">需要检查的建筑实体 ID。</param>
        /// <param name="unitType">需要统计的单位类型。</param>
        /// <returns>建筑上对应单位类型的待生产数量，无法读取时返回 0。</returns>
        private int GetProduceQueueCount(int buildingId, UnitType unitType)
        {
            Entity entity = new Entity(buildingId);
            if (!ComponentManager.HasComponent<ProduceComponent>(entity))
                return 0;

            var produce = ComponentManager.GetComponent<ProduceComponent>(entity);
            if (!produce.ProduceNumbers.TryGetValue(unitType, out int count))
                return 0;

            return count;
        }

        /// <summary>
        /// 发送生产命令
        /// </summary>
        /// <param name="barracksId">整备营区实体 ID</param>
        /// <param name="count">生产数量</param>
        private void SendProduceCommand(int barracksId, int count, UnitType unitType)
        {
            // 获取 Game 实例
            var game = World.GameInstance;
            if (game == null)
            {
                zUDebug.LogError("[FirstAISystem] 无法获取 Game 实例，无法发送生产命令");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                // 创建生产命令
                var command = new ZLockstep.Sync.Command.Commands.ProduceCommand(
                    campId: AI_CAMP_ID,
                    entityId: barracksId,
                    unitType: unitType,
                    changeValue: 1
                );

                // 提交命令
                game.SubmitCommand(command);
            }

            zUDebug.Log($"[FirstAISystem] 发送生产命令：建筑{barracksId} 生产{count}个 {unitType}");
        }

        /// <summary>
        /// 发送建造整备营区命令
        /// </summary>
        private void SendBuildBuildingCommand(int confID, string buildingName)
        {
            // 获取 Game 实例
            var game = World.GameInstance;
            if (game == null)
            {
                zUDebug.LogError("[FirstAISystem] 无法获取 Game 实例，无法发送建造命令");
                return;
            }

            // 从 ConfInitUnits 配置表中查找整备营区位置
            string buildPositionStr = null;
            var initUnits = ConfigManager.GetAll<ConfInitUnits>();
            foreach (var unit in initUnits)
            {
                if (unit.Camp == AI_CAMP_ID && unit.ConfID == confID)
                {
                    buildPositionStr = unit.Position;
                    break;
                }
            }

            if (string.IsNullOrEmpty(buildPositionStr))
            {
                zUDebug.LogError($"[FirstAISystem] 未找到阵营 {AI_CAMP_ID} {buildingName}（ConfID={confID}）的位置配置");
                return;
            }

            // 将字符串位置转换为 zVector3
            zVector3 buildPosition = StringToVector3Converter.StringToZVector3(buildPositionStr);

            // 创建建造整备营区命令
            var command = new CreateBuildingCommand(
                confID: confID,
                campId: AI_CAMP_ID,
                position: buildPosition
            );

            // 提交命令
            game.SubmitCommand(command);

            zUDebug.Log($"[FirstAISystem] 发送建造{buildingName}命令：位置 {buildPosition}");
        }
    }
}
