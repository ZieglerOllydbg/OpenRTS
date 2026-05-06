using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Simulation.ECS.Systems;
using ZLockstep.Simulation.ECS.Utils;
using ZLockstep.Flow;
using zUnity;
using ZLockstep.RVO;
using ZLockstep.View;

namespace Game.Examples
{
    /// <summary>
    /// 自动追击系统
    /// 让所有有攻击能力的单位自动追击视野内的敌人
    /// 
    /// 策略：
    /// 1. 侦测范围为 15 米（大于攻击范围 10 米）
    /// 2. 如果正在攻击目标且目标有效，继续追击该目标
    /// 3. 如果没有目标或目标无效，搜索最近的敌人
    /// 4. 只有在没有移动目标或已到达目标时才重新评估
    /// </summary>
    public class AutoChaseSystem : BaseSystem
    {
        /// <summary>
        /// 侦测范围（米）
        /// </summary>
        private zfloat _detectionRange = new zfloat(15);

        /// <summary>
        /// 重新评估目标的间隔（秒）
        /// </summary>
        private zfloat _evaluationInterval = zfloat.FromRaw(5000);

        /// <summary>
        /// 距离上次评估的时间
        /// </summary>
        private zfloat _timeSinceLastEvaluation = zfloat.Zero;

        /// <summary>
        /// 导航系统引用
        /// </summary>
        private FlowFieldNavigationSystem _navSystem;

        /// <summary>
        /// 初始化系统
        /// </summary>
        public void Initialize(FlowFieldNavigationSystem navSystem)
        {
            _navSystem = navSystem;
        }

        public override void Update()
        {
            if (_navSystem == null)
            {
                zUDebug.LogWarning("[AutoChaseSystem] 导航系统未初始化");
                return;
            }

            // 更新评估计时器
            _timeSinceLastEvaluation += DeltaTime;

            // 定期评估所有单位
            if (_timeSinceLastEvaluation >= _evaluationInterval)
            {
                EvaluateAllUnits();
                _timeSinceLastEvaluation = zfloat.Zero;
            }
        }

        /// <summary>
        /// 评估所有有攻击能力的单位
        /// </summary>
        private void EvaluateAllUnits()
        {
            var attackerEntities = ComponentManager.GetAllEntityIdsWith<AttackComponent>();

            foreach (var entityId in attackerEntities)
            {
                Entity entity = new Entity(entityId);

                // 必须有 Transform、Camp 和 FlowFieldNavigator 组件
                if (!ComponentManager.HasComponent<TransformComponent>(entity) ||
                    !ComponentManager.HasComponent<CampComponent>(entity) ||
                    !ComponentManager.HasComponent<FlowFieldNavigatorComponent>(entity))
                    continue;

                var attack = ComponentManager.GetComponent<AttackComponent>(entity);
                var transform = ComponentManager.GetComponent<TransformComponent>(entity);
                var campComponent = ComponentManager.GetComponent<CampComponent>(entity);
                var navigator = ComponentManager.GetComponent<FlowFieldNavigatorComponent>(entity);

                // 1. 检查当前攻击目标是否有效且在侦测范围内
                if (attack.TargetEntityId >= 0 && IsValidChaseTarget(attack.TargetEntityId, campComponent, transform.Position, attack.Range))
                {
                    // 目标有效，继续追击
                    Entity target = new(attack.TargetEntityId);
                    if (ComponentManager.HasComponent<TransformComponent>(target))
                    {
                        // 检查目标是否为建筑，如果是则计算边界点
                        zVector2 targetPos;
                        if (ComponentManager.HasComponent<BuildingComponent>(target))
                        {
                            var building = ComponentManager.GetComponent<BuildingComponent>(target);
                            targetPos = CalculateOptimizedBuildingTargetPoint(transform.Position, building, attack.Range);
                            
                            // 检查当前距离建筑边界点是否超出攻击范围
                            zfloat currentDistance = (targetPos - new zVector2(transform.Position.x, transform.Position.z)).magnitude;
                            
                            // 只有当目标超出攻击范围时才需要移动
                            if (currentDistance > attack.Range)
                            {
                                // 只有在没有移动目标或已到达目标时才设置新目标
                                if (!ComponentManager.HasComponent<MoveTargetComponent>(entity) || navigator.HasReachedTarget)
                                {
                                    _navSystem.SetMoveTarget(entity, targetPos);
                                    // zUDebug.Log($"[AutoChaseSystem] 追击建筑目标 - 距离: {currentDistance}, 攻击范围: {attack.Range}");
                                }
                            }
                            else
                            {
                                // zUDebug.Log($"[AutoChaseSystem] 继续追击 - 目标在攻击范围内({currentDistance} <= {attack.Range})，无需移动");
                                // 目标在攻击范围内，清除移动目标（如果有的话）
                                if (ComponentManager.HasComponent<MoveTargetComponent>(entity))
                                {
                                    if (!ComponentManager.GetComponent<MoveTargetComponent>(entity).UserInput)
                                    {
                                        ComponentManager.RemoveComponent<MoveTargetComponent>(entity);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var targetTransform = ComponentManager.GetComponent<TransformComponent>(target);
                            targetPos = new zVector2(targetTransform.Position.x, targetTransform.Position.z);
                            
                            // 检查当前距离是否超出攻击范围
                            zfloat currentDistance = (targetPos - new zVector2(transform.Position.x, transform.Position.z)).magnitude;
                            
                            // 只有当目标超出攻击范围时才需要移动
                            if (currentDistance > attack.Range)
                            {
                                // 只有在没有移动目标或已到达目标时才设置新目标
                                if (navigator.HasReachedTarget)
                                {
                                    if (ComponentManager.HasComponent<MoveTargetComponent>(entity))
                                    {
                                        MoveTargetComponent moveTargetComponent = ComponentManager.GetComponent<MoveTargetComponent>(entity);
                                        if (!moveTargetComponent.UserInput)
                                        {
                                            _navSystem.SetMoveTarget(entity, targetPos);
                                        }
                                    }
                                    else
                                    {
                                        _navSystem.SetMoveTarget(entity, targetPos);
                                    }
                                }
                            }
                            else if (currentDistance < attack.Range * zfloat.FromFloat(0.5f))
                            {
                                // 目标在攻击范围内，清除移动目标（如果有的话）
                                if (ComponentManager.HasComponent<MoveTargetComponent>(entity))
                                {
                                    if (!ComponentManager.GetComponent<MoveTargetComponent>(entity).UserInput)
                                    {
                                        ComponentManager.RemoveComponent<MoveTargetComponent>(entity);
                                    }
                                }
                            }
                        }
                    }
                    continue;
                }

                // 2. 搜索新的敌人
                // int nearestEnemyId = FindNearestEnemy(entity, camp, transform.Position, _detectionRange);

                var neighbors = SpatialIndex.Instance.RadialSearch(transform.Position, _detectionRange.ToFloat());

                int nearestEnemyId = -1;
                zfloat minDistSqr = _detectionRange * _detectionRange;

                foreach (var neighbor in neighbors)
                {
                    // 跳过自己
                    Entity neighborEntity = new(neighbor.EntityId);
                    if (neighbor.EntityId == entityId)
                        continue;

                    if (!ComponentManager.HasComponent<CampComponent>(neighborEntity))
                        continue;

                    // 检查是否为敌人
                    var neighborCamp = ComponentManager.GetComponent<CampComponent>(neighborEntity);
                    if (!campComponent.IsEnemy(neighborCamp))
                        continue;

                    // 必须有 Transform 和 Health 组件
                    if (!ComponentManager.HasComponent<TransformComponent>(neighborEntity) ||
                        !ComponentManager.HasComponent<HealthComponent>(neighborEntity))
                        continue;

                    // 检查是否还活着
                    var health = ComponentManager.GetComponent<HealthComponent>(neighborEntity);
                    if (health.CurrentHealth <= zfloat.Zero)
                        continue;

                    // 计算距离
                    zfloat distSqr;
                    if (ComponentManager.HasComponent<BuildingComponent>(neighborEntity))
                    {
                        var building = ComponentManager.GetComponent<BuildingComponent>(neighborEntity);
                        if (!BuildingBoundaryUtils.IsBuildingInRange(transform.Position, building, _detectionRange, World))
                            continue; // 建筑不在范围内
                            
                        zVector2 boundaryPoint = BuildingBoundaryUtils.CalculateBuildingBoundaryPoint(transform.Position, building, World);
                        distSqr = (new zVector3(boundaryPoint.x, transform.Position.y, boundaryPoint.y) - transform.Position).sqrMagnitude;
                    }
                    else
                    {
                        var neighborTransform = ComponentManager.GetComponent<TransformComponent>(neighborEntity);
                        distSqr = (neighborTransform.Position - transform.Position).sqrMagnitude;
                    }

                    // 找到最近的敌人
                    if (distSqr < minDistSqr)
                    {
                        minDistSqr = distSqr;
                        nearestEnemyId = neighbor.EntityId;
                    }
                }

                if (nearestEnemyId >= 0)
                {
                    // 找到敌人，追击
                    Entity target = new Entity(nearestEnemyId);
                    if (ComponentManager.HasComponent<TransformComponent>(target))
                    {
                        // 检查目标是否为建筑，如果是则计算边界点
                        zVector2 targetPos;
                        if (ComponentManager.HasComponent<BuildingComponent>(target))
                        {
                            var building = ComponentManager.GetComponent<BuildingComponent>(target);
                            targetPos = CalculateOptimizedBuildingTargetPoint(transform.Position, building, attack.Range);
                        }
                        else
                        {
                            var targetTransform = ComponentManager.GetComponent<TransformComponent>(target);
                            targetPos = new zVector2(targetTransform.Position.x, targetTransform.Position.z);
                        }
                        
                        // 检查目标是否在攻击范围内，如果在范围内则不需要移动
                        zfloat distanceToTarget;
                        if (ComponentManager.HasComponent<BuildingComponent>(target))
                        {
                            var building = ComponentManager.GetComponent<BuildingComponent>(target);
                            zVector2 boundaryPoint = BuildingBoundaryUtils.CalculateBuildingBoundaryPoint(transform.Position, building, World);
                            distanceToTarget = (boundaryPoint - new zVector2(transform.Position.x, transform.Position.z)).magnitude;
                        }
                        else
                        {
                            var targetTransform = ComponentManager.GetComponent<TransformComponent>(target);
                            distanceToTarget = (targetTransform.Position - transform.Position).magnitude;
                        }

                        // 只有当目标超出攻击范围时才需要移动
                        if (distanceToTarget > attack.Range)
                        {
                            // 只有在没有移动目标或已到达目标时才设置新目标
                            if (!ComponentManager.HasComponent<MoveTargetComponent>(entity) || navigator.HasReachedTarget)
                            {
                                _navSystem.SetMoveTarget(entity, targetPos);
                                // zUDebug.Log($"[AutoChaseSystem] 目标距离: {distanceToTarget}, 攻击范围: {attack.Range}, 开始追击");
                            }
                        }
                        else
                        {
                            // zUDebug.Log($"[AutoChaseSystem] 目标在攻击范围内({distanceToTarget} <= {attack.Range})，无需移动");
                            // 目标在攻击范围内，清除移动目标（如果有的话）
                            if (ComponentManager.HasComponent<MoveTargetComponent>(entity))
                            {
                                if (!ComponentManager.GetComponent<MoveTargetComponent>(entity).UserInput)
                                {
                                    ComponentManager.RemoveComponent<MoveTargetComponent>(entity);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 计算优化后的建筑目标点（使用攻击范围 - 0.5米作为安全距离）
        /// </summary>
        /// <param name="chaserPosition">追击者位置</param>
        /// <param name="building">建筑组件</param>
        /// <param name="attackRange">攻击范围</param>
        /// <returns>优化后的目标点</returns>
        private zVector2 CalculateOptimizedBuildingTargetPoint(zVector3 chaserPosition, BuildingComponent building, zfloat attackRange)
        {
            // 获取建筑边界点
            zVector2 boundaryPoint = BuildingBoundaryUtils.CalculateBuildingBoundaryPoint(chaserPosition, building, World);
            
            // 计算从追击者到边界点的方向
            zVector2 toBoundary = boundaryPoint - new zVector2(chaserPosition.x, chaserPosition.z);
            zfloat distanceToBoundary = toBoundary.magnitude;
            
            // 安全距离（攻击范围 - 0.5米）
            zfloat safeDistance = attackRange - new zfloat(2);
            
            // 计算距离边界点安全距离远的位置
            zVector2 direction = toBoundary.normalized;
            return boundaryPoint - direction * safeDistance;
        }

        /// <summary>
        /// 检查追击目标是否有效
        /// </summary>
        private bool IsValidChaseTarget(int targetEntityId, CampComponent selfCamp, zVector3 position, zfloat range)
        {
            Entity target = new Entity(targetEntityId);

            // 检查是否存在
            if (!ComponentManager.HasComponent<TransformComponent>(target))
                return false;

            // 检查是否还活着
            if (ComponentManager.HasComponent<HealthComponent>(target))
            {
                var health = ComponentManager.GetComponent<HealthComponent>(target);
                if (health.CurrentHealth <= zfloat.Zero)
                    return false;
            }

            // 检查是否为敌人
            if (ComponentManager.HasComponent<CampComponent>(target))
            {
                var targetCamp = ComponentManager.GetComponent<CampComponent>(target);
                if (!selfCamp.IsEnemy(targetCamp))
                    return false;
            }
            else
            {
                return false;
            }

            // 检查是否在侦测范围内
            if (ComponentManager.HasComponent<BuildingComponent>(target))
            {
                var building = ComponentManager.GetComponent<BuildingComponent>(target);
                return BuildingBoundaryUtils.IsBuildingInRange(position, building, range, World);
            }
            else
            {
                var targetTransform = ComponentManager.GetComponent<TransformComponent>(target);
                zfloat distSqr = (targetTransform.Position - position).sqrMagnitude;
                zfloat rangeSqr = _detectionRange * _detectionRange;
                return distSqr <= rangeSqr;
            }
        }
    }
}