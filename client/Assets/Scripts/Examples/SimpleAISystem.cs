using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Simulation.ECS.Systems;
using ZLockstep.Flow;
using zUnity;
using Game.Examples;

namespace Game.Examples
{
    /// <summary>
    /// 简单AI系统
    /// 控制AI阵营（阵营ID=1）的所有单位
    /// 策略：
    /// 1. 优先追逐附近的玩家单位（15米侦测范围）
    /// 2. 如果附近没有玩家单位，则移动到玩家基地
    /// 3. 战斗由CombatSystem和AutoChaseSystem自动处理
    /// </summary>
    public class SimpleAISystem : BaseSystem
    {
        /// <summary>
        /// AI阵营ID
        /// </summary>
        private const int AI_CAMP_ID = 2;

        /// <summary>
        /// 玩家阵营ID
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

        /// <summary>
        /// 是否已找到玩家基地
        /// </summary>
        private bool _hasFoundPlayerBase = false;

        /// <summary>
        /// 初始化AI系统
        /// </summary>
        public void Initialize(FlowFieldNavigationSystem navSystem)
        {
            _navSystem = navSystem;
        }

        public override void Update()
        {
            if (_navSystem == null)
            {
                zUDebug.LogWarning("[SimpleAISystem] 导航系统未初始化");
                return;
            }

            // 更新评估计时器
            _timeSinceLastEvaluation += DeltaTime;

            // 如果还没找到玩家基地，尝试查找
            if (!_hasFoundPlayerBase)
            {
                FindPlayerBase();
            }

            // 定期重新评估AI单位的行为
            if (_timeSinceLastEvaluation >= _evaluationInterval)
            {
                EvaluateAIUnits();
                _timeSinceLastEvaluation = zfloat.Zero;
            }
        }

        /// <summary>
        /// 查找玩家基地位置
        /// </summary>
        private void FindPlayerBase()
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

                // 找到玩家的基地（BuildingType=0）
                if (camp.CampId == PLAYER_CAMP_ID && building.BuildingType == (int)BuildingType.Base)
                {
                    if (ComponentManager.HasComponent<TransformComponent>(entity))
                    {
                        var transform = ComponentManager.GetComponent<TransformComponent>(entity);
                        _playerBasePosition = new zVector2(transform.Position.x, transform.Position.z);
                        _hasFoundPlayerBase = true;

                        zUDebug.Log($"[SimpleAISystem] 找到玩家基地位置: {_playerBasePosition}");
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
            if (!_hasFoundPlayerBase)
                return;

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
                        Entity target = new Entity(nearestPlayerUnitId);
                        var targetTransform = ComponentManager.GetComponent<TransformComponent>(target);
                        zVector2 targetPos = new zVector2(targetTransform.Position.x, targetTransform.Position.z);
                        _navSystem.SetMoveTarget(entity, targetPos);
                        chaseCount++;
                    }
                    else
                    {
                        // 没有找到玩家单位，移动到玩家基地
                        _navSystem.SetMoveTarget(entity, _playerBasePosition);
                        moveToBaseCount++;
                    }
                }
            }

            if (chaseCount > 0 || moveToBaseCount > 0)
            {
                zUDebug.Log($"[SimpleAISystem] 追击玩家单位: {chaseCount}，移动到基地: {moveToBaseCount}");
            }
        }

        /// <summary>
        /// 搜索最近的玩家单位
        /// </summary>
        private int FindNearestPlayerUnit(zVector3 position)
        {
            int nearestUnitId = -1;
            zfloat minDistSqr = _detectionRange * _detectionRange;

            var allEntities = ComponentManager.GetAllEntityIdsWith<CampComponent>();

            foreach (var entityId in allEntities)
            {
                Entity otherEntity = new Entity(entityId);

                // 检查是否为玩家阵营
                var otherCamp = ComponentManager.GetComponent<CampComponent>(otherEntity);
                if (otherCamp.CampId != PLAYER_CAMP_ID)
                    continue;

                // 跳过建筑，只追单位
                if (ComponentManager.HasComponent<BuildingComponent>(otherEntity))
                    continue;

                // 必须有 Transform 和 Health 组件
                if (!ComponentManager.HasComponent<TransformComponent>(otherEntity) ||
                    !ComponentManager.HasComponent<HealthComponent>(otherEntity))
                    continue;

                // 检查是否还活着
                var health = ComponentManager.GetComponent<HealthComponent>(otherEntity);
                if (health.CurrentHealth <= zfloat.Zero)
                    continue;

                // 计算距离
                var otherTransform = ComponentManager.GetComponent<TransformComponent>(otherEntity);
                zfloat distSqr = (otherTransform.Position - position).sqrMagnitude;

                // 找到最近的单位
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    nearestUnitId = entityId;
                }
            }

            return nearestUnitId;
        }
    }
}

