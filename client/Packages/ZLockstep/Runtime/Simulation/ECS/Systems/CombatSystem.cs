using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Simulation.Events;
using ZLockstep.Simulation.ECS.Utils;
using zUnity;
using System;
using System.Collections.Generic;
using System.Linq;
using Utils;
using ZLockstep.Flow;

namespace ZLockstep.Simulation.ECS.Systems
{
    /// <summary>
    /// 战斗系统
    /// 处理单位和建筑的战斗逻辑
    /// 1. 自动搜索攻击范围内的敌方单位
    /// 2. 更新攻击冷却
    /// 3. 发射弹道（创建 Projectile Entity）
    /// </summary>
    public class CombatSystem : BaseSystem
    {
        private static readonly zfloat PROJECTILE_MIN_ARC_HEIGHT = zfloat.Half;
        private static readonly zfloat PROJECTILE_MAX_ARC_HEIGHT = (zfloat)5.0f;
        private static readonly zfloat PROJECTILE_ARC_HEIGHT_FACTOR = (zfloat)0.5f;

        /// <summary>
        /// 目标查找最小间隔时间（秒）
        /// </summary>
        private static readonly zfloat TARGET_SEARCH_INTERVAL = (zfloat)1.0f;

        /// <summary>
        /// 搜索邻居的最大半径（基于最大攻击范围的估计值）
        /// </summary>
        private const float MAX_SEARCH_RADIUS = 20f;

        public override void Update()
        {
            // 每帧重建空间索引（简单但有效的方式）
            RebuildSpatialIndex();
            
            // 处理所有有攻击能力的实体
            ProcessAttacks();
        }

        /// <summary>
        /// 重建空间索引
        /// </summary>
        private void RebuildSpatialIndex()
        {
            SpatialIndex.Instance.Clear();
            
            // 获取所有有 Transform 和 Camp 组件的实体
            var entities = ComponentManager.GetAllEntityIdsWith<TransformComponent>();
            
            foreach (var entityId in entities)
            {
                var entity = new Entity(entityId);
                
                // 确保有 Camp 组件
                if (!ComponentManager.HasComponent<CampComponent>(entity))
                    continue;
                
                var transform = ComponentManager.GetComponent<TransformComponent>(entity);
                var camp = ComponentManager.GetComponent<CampComponent>(entity);
                
                // 添加到空间索引
                SpatialIndex.Instance.Add(entityId, transform.Position, camp.CampId);
            }
            
            // 重建 KD-tree
            SpatialIndex.Instance.Rebuild();
        }

        /// <summary>
        /// 处理攻击逻辑
        /// </summary>
        private void ProcessAttacks()
        {
            var attackerEntities = ComponentManager.GetAllEntityIdsWith<AttackComponent>();

            foreach (var entityId in attackerEntities)
            {
                var entity = new Entity(entityId);

                // 检查攻击者是否还活着（只检查 DeathComponent，不检查生命值）
                if (ComponentManager.HasComponent<DeathComponent>(entity))
                {
                    continue; // 死亡单位不能攻击
                }

                // 正在建筑的碉堡等建筑单位也不能攻击
                if (ComponentManager.HasComponent<BuildingConstructionComponent>(entity))
                {
                    continue; // acting building cannot attack
                }

                // 必须有 Transform 和 Camp 组件
                if (!ComponentManager.HasComponent<TransformComponent>(entity) ||
                    !ComponentManager.HasComponent<CampComponent>(entity) ||
                    !ComponentManager.HasComponent<AttackComponent>(entity)
                    )
                    continue;
                
                var transform = ComponentManager.GetComponent<TransformComponent>(entity);
                var camp = ComponentManager.GetComponent<CampComponent>(entity);
                var attack = ComponentManager.GetComponent<AttackComponent>(entity);

                // 更新攻击冷却
                attack.TimeSinceLastAttack += DeltaTime;

                // 检查是否可以攻击
                if (!attack.CanAttack)
                {
                    // 写回攻击组件
                    ComponentManager.AddComponent(entity, attack);
                    continue;
                }

                // 统一使用多目标攻击逻辑（即使 MaxTargets=1 也走多目标流程）
                AttackMultiTarget(entity);
            }
        }

        private void AttackMultiTarget(Entity entity)
        {
            // 检查攻击者是否还活着（只检查 DeathComponent，不检查生命值）
            if (ComponentManager.HasComponent<DeathComponent>(entity))
            {
                return; // 死亡单位不能攻击
            }

            // 必须有 Transform 和 Camp 组件
            if (!ComponentManager.HasComponent<TransformComponent>(entity) ||
                !ComponentManager.HasComponent<CampComponent>(entity) ||
                !ComponentManager.HasComponent<AttackComponent>(entity)
                )
                return;

            var transform = ComponentManager.GetComponent<TransformComponent>(entity);
            var camp = ComponentManager.GetComponent<CampComponent>(entity);
            var attack = ComponentManager.GetComponent<AttackComponent>(entity);

            // 更新攻击冷却
            attack.TimeSinceLastAttack += DeltaTime;
            
            // 每次都搜索多个目标
            List<int> targetIds = FindMultipleTargets(entity, camp, transform.Position, attack.WarningRange, attack.MaxTargets);
            
            if (targetIds.Count == 0)
            {
                // 没有目标，清空炮塔状态
                if (ComponentManager.HasComponent<TurretComponent>(entity))
                {
                    var turret = ComponentManager.GetComponent<TurretComponent>(entity);
                    turret.HasTarget = false;
                    ComponentManager.AddComponent(entity, turret);
                }
                
                ComponentManager.AddComponent(entity, attack);
                return;
            }
            
            // 如果有目标，使用第一个目标作为主目标（用于炮塔朝向等）
            int mainTargetId = targetIds[0];
            attack.TargetEntityId = mainTargetId;
            
            Entity mainTarget = new Entity(mainTargetId);
            
            if (!ComponentManager.HasComponent<TransformComponent>(mainTarget))
            {
                attack.TargetEntityId = -1;
                
                // 清空炮塔目标
                if (ComponentManager.HasComponent<TurretComponent>(entity))
                {
                    var turret = ComponentManager.GetComponent<TurretComponent>(entity);
                    turret.HasTarget = false;
                    ComponentManager.AddComponent(entity, turret);
                }
                
                ComponentManager.AddComponent(entity, attack);
                return;
            }
            
            // 如果当前目标是建筑，则检查附近是否有非建筑目标可以优先攻击
            if (ComponentManager.HasComponent<BuildingComponent>(mainTarget))
            {
                int priorityTargetId = FindNearestNonBuildingEnemyKDTree(entity, camp, transform.Position, attack.WarningRange);
                if (priorityTargetId >= 0)
                {
                    // 存在可优先攻击的非建筑目标，切换目标
                    attack.TargetEntityId = priorityTargetId;
                    mainTarget = new Entity(attack.TargetEntityId);
                }
            }
            
            var mainTargetTransform = ComponentManager.GetComponent<TransformComponent>(mainTarget);
            
            // 计算到主目标的距离（用于判断是否在攻击范围内）
            zfloat distanceSqr;
            if (ComponentManager.HasComponent<BuildingComponent>(mainTarget))
            {
                var building = ComponentManager.GetComponent<BuildingComponent>(mainTarget);
                zVector2 boundaryPoint = BuildingBoundaryUtils.CalculateBuildingBoundaryPoint(transform.Position, building, World);
                distanceSqr = (new zVector3(boundaryPoint.x, transform.Position.y, boundaryPoint.y) - transform.Position).sqrMagnitude;
            }
            else
            {
                distanceSqr = (mainTargetTransform.Position - transform.Position).sqrMagnitude;
            }
            
            zfloat rangeSqr = attack.Range * attack.Range;
            
            // 如果超出范围，清空目标
            if (distanceSqr > rangeSqr)
            {
                attack.TargetEntityId = -1;
                
                // 清空炮塔目标
                if (ComponentManager.HasComponent<TurretComponent>(entity))
                {
                    var turret = ComponentManager.GetComponent<TurretComponent>(entity);
                    turret.HasTarget = false;
                    ComponentManager.AddComponent(entity, turret);
                }
                
                ComponentManager.AddComponent(entity, attack);
                return;
            }
            
            // 在范围内，设置炮塔朝向主目标
            if (ComponentManager.HasComponent<TurretComponent>(entity))
            {
                var turret = ComponentManager.GetComponent<TurretComponent>(entity);
                turret.HasTarget = true;
                
                // 计算朝向主目标的方向（2D）
                zVector3 toTarget;
                if (ComponentManager.HasComponent<BuildingComponent>(mainTarget))
                {
                    // 如果目标是建筑，朝向建筑边界点
                    var building = ComponentManager.GetComponent<BuildingComponent>(mainTarget);
                    zVector2 boundaryPoint = BuildingBoundaryUtils.CalculateBuildingBoundaryPoint(transform.Position, building, World);
                    toTarget = new zVector3(boundaryPoint.x, transform.Position.y, boundaryPoint.y) - transform.Position;
                }
                else
                {
                    // 普通目标
                    toTarget = mainTargetTransform.Position - transform.Position;
                }
                
                zVector2 toTarget2D = new zVector2(toTarget.x, toTarget.z);
                if (toTarget2D.magnitude > zfloat.Epsilon)
                {
                    turret.DesiredTurretDirection = toTarget2D.normalized;
                }
                
                ComponentManager.AddComponent(entity, turret);
            }
            
            // 如果冷却完成，对所有目标发射攻击
            if (attack.CanAttack)
            {
                // 计算攻击方向
                zVector3 toMainTarget = CalculateTargetDirection(mainTarget, transform, mainTargetTransform);
                zVector2 attackForwardDirection = new zVector2(toMainTarget.x, toMainTarget.z);
                if (attackForwardDirection.magnitude > zfloat.Epsilon)
                {
                    ComponentManager.AddComponent(entity, AttackForwardComponent.Create(attackForwardDirection.normalized, Tick));
                }

                // 播放音效标记
                attack.PlayAttackAudio = true;
                zVector3 projectileSourcePos = GetProjectileSourcePosition(entity, transform);
                
                // 对每个目标发射弹道
                foreach (var targetId in targetIds)
                {
                    Entity target = new Entity(targetId);
                    
                    if (!ComponentManager.HasComponent<TransformComponent>(target))
                        continue;
                    
                    var targetTransform = ComponentManager.GetComponent<TransformComponent>(target);
                    
                    // 确定攻击目标点
                    zVector3 targetPos;
                    if (ComponentManager.HasComponent<BuildingComponent>(target))
                    {
                        // 如果目标是建筑，攻击建筑边界点
                        var building = ComponentManager.GetComponent<BuildingComponent>(target);
                        zVector2 boundaryPoint = BuildingBoundaryUtils.CalculateBuildingBoundaryPoint(transform.Position, building, World);
                        targetPos = new zVector3(boundaryPoint.x, transform.Position.y, boundaryPoint.y);
                    }
                    else
                    {
                        // 普通目标
                        targetPos = targetTransform.Position;
                    }
                    
                    FireProjectile(entity, target, projectileSourcePos, targetPos, camp.CampId, attack.ConfProjectileID);
                }
                
                attack.TimeSinceLastAttack = zfloat.Zero;
            }
            
            // 写回攻击组件
            ComponentManager.AddComponent(entity, attack);
        }

        private zVector3 CalculateTargetDirection(Entity target, TransformComponent transform, TransformComponent targetTransform)
        {
            if (ComponentManager.HasComponent<BuildingComponent>(target))
            {
                var building = ComponentManager.GetComponent<BuildingComponent>(target);
                zVector2 boundaryPoint = BuildingBoundaryUtils.CalculateBuildingBoundaryPoint(transform.Position, building, World);
                return new zVector3(boundaryPoint.x, transform.Position.y, boundaryPoint.y) - transform.Position;
            }

            return targetTransform.Position - transform.Position;
        }

        /// <summary>
        /// 使用 KD-tree 搜索多个敌方单位
        /// </summary>
        /// <param name="self">自己</param>
        /// <param name="selfCamp">自己的阵营</param>
        /// <param name="position">位置</param>
        /// <param name="range">范围</param>
        /// <param name="maxTargets">最大目标数量</param>
        /// <returns>目标实体 ID 列表</returns>
        private List<int> FindMultipleTargets(Entity self, CampComponent selfCamp, zVector3 position, zfloat range, int maxTargets)
        {
            // 使用 KD-tree 进行空间邻近查询，带数量限制和谓词筛选
            float searchRadius = Math.Min((float)range, MAX_SEARCH_RADIUS);
            zfloat rangeSqr = range * range;
            
            var neighbors = SpatialIndex.Instance.RadialSearch(
                position, 
                searchRadius, 
                -1, 
                neighbor => IsValidTarget(neighbor, self.Id, selfCamp, position, range, rangeSqr)
            );
            
            neighbors.Sort((left, right) =>
            {
                zfloat leftDistance = CalculateTargetDistanceSqr(new Entity(left.EntityId), position);
                zfloat rightDistance = CalculateTargetDistanceSqr(new Entity(right.EntityId), position);

                if (leftDistance < rightDistance) return -1;
                if (leftDistance > rightDistance) return 1;
                return 0;
            });

            return neighbors
                .Take(maxTargets)
                .Select(neighbor => neighbor.EntityId)
                .ToList();
        }

        /// <summary>
        /// 判断目标是否有效（在范围内、是敌人、有必要的组件等）
        /// </summary>
        private bool IsValidTarget(SpatialEntry neighbor, int selfEntityId, CampComponent selfCamp, zVector3 position, zfloat range, zfloat rangeSqr)
        {
            // 跳过自己
            if (neighbor.EntityId == selfEntityId)
                return false;

            // 检查是否为敌人
            var neighborCamp = new CampComponent { CampId = neighbor.CampId };
            if (!selfCamp.IsEnemy(neighborCamp))
                return false;

            Entity otherEntity = new Entity(neighbor.EntityId);

            // 必须有 Transform 和 Health 组件
            if (!ComponentManager.HasComponent<TransformComponent>(otherEntity) ||
                !ComponentManager.HasComponent<HealthComponent>(otherEntity))
                return false;

            // 检查是否还活着（只检查 DeathComponent）
            if (ComponentManager.HasComponent<DeathComponent>(otherEntity))
                return false;

            // 计算距离
            var otherTransform = ComponentManager.GetComponent<TransformComponent>(otherEntity);
            zfloat distSqr;
            
            // 如果目标是建筑，计算到建筑边界的距离
            if (ComponentManager.HasComponent<BuildingComponent>(otherEntity))
            {
                var building = ComponentManager.GetComponent<BuildingComponent>(otherEntity);
                if (!BuildingBoundaryUtils.IsBuildingInRange(position, building, range, World))
                    return false; // 建筑不在范围内
                
                zVector2 boundaryPoint = BuildingBoundaryUtils.CalculateBuildingBoundaryPoint(position, building, World);
                distSqr = (new zVector3(boundaryPoint.x, position.y, boundaryPoint.y) - position).sqrMagnitude;
            }
            else
            {
                distSqr = (otherTransform.Position - position).sqrMagnitude;
            }

            // 检查是否在范围内
            return distSqr <= rangeSqr;
        }


        /// <summary>
        /// 使用 KD-tree 搜索最近的非建筑敌方单位
        /// </summary>
        // Reuse the same effective distance rule for target selection and buildings.
        private zfloat CalculateTargetDistanceSqr(Entity target, zVector3 position)
        {
            var otherTransform = ComponentManager.GetComponent<TransformComponent>(target);

            if (ComponentManager.HasComponent<BuildingComponent>(target))
            {
                var building = ComponentManager.GetComponent<BuildingComponent>(target);
                zVector2 boundaryPoint = BuildingBoundaryUtils.CalculateBuildingBoundaryPoint(position, building, World);
                return (new zVector3(boundaryPoint.x, position.y, boundaryPoint.y) - position).sqrMagnitude;
            }

            return (otherTransform.Position - position).sqrMagnitude;
        }

        /// <summary>
        /// 浣跨敤 KD-tree 鎼滅储鏈€杩戠殑闈炲缓绛戞晫鏂瑰崟浣?
        /// </summary>
        private int FindNearestNonBuildingEnemyKDTree(Entity self, CampComponent selfCamp, zVector3 position, zfloat range)
        {
            int nearestEnemyId = -1;
            zfloat minDistSqr = range * range;

            // 使用 KD-tree 进行空间邻近查询
            float searchRadius = Math.Min((float)range, MAX_SEARCH_RADIUS);
            var neighbors = SpatialIndex.Instance.RadialSearch(position, searchRadius);

            foreach (var neighbor in neighbors)
            {
                // 跳过自己
                if (neighbor.EntityId == self.Id)
                    continue;

                // 检查是否为敌人
                var neighborCamp = new CampComponent { CampId = neighbor.CampId };
                if (!selfCamp.IsEnemy(neighborCamp))
                    continue;

                Entity otherEntity = new Entity(neighbor.EntityId);

                // 必须有 Transform 和 Health 组件
                if (!ComponentManager.HasComponent<TransformComponent>(otherEntity) ||
                    !ComponentManager.HasComponent<HealthComponent>(otherEntity))
                    continue;

                // 检查是否还活着（只检查 DeathComponent）
                if (ComponentManager.HasComponent<DeathComponent>(otherEntity))
                    continue;

                // 优先攻击非建筑目标，跳过建筑目标
                if (ComponentManager.HasComponent<BuildingComponent>(otherEntity))
                    continue;

                // 计算距离
                var otherTransform = ComponentManager.GetComponent<TransformComponent>(otherEntity);
                zfloat distSqr = (otherTransform.Position - position).sqrMagnitude;

                // 找到最近的敌人
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    nearestEnemyId = neighbor.EntityId;
                }
            }

            return nearestEnemyId;
        }

        /// <summary>
        /// 发射弹道
        /// </summary>
        private void FireProjectile(Entity source, Entity target, zVector3 sourcePos, zVector3 targetPos, int sourceCampId, int confProjectileId)
        {
            ConfProjectile confProjectile = ConfigManager.Get<ConfProjectile>(confProjectileId);
            if (confProjectile == null)
            {
                zUDebug.LogError($"[EntityCreationManager] 创建弹道实体时无法获取弹道配置信息。ID:{confProjectileId}");
                return;
            }

            // 创建弹道实体
            var projectile = World.EntityManager.CreateEntity();

            // 添加Transform（从源位置开始）
            zVector3 birthPosition = sourcePos + new zVector3(zfloat.Zero, new zfloat(0, 5000), zfloat.Zero);
            bool isHoming = confProjectile.IsHoming == 1;
            bool enableArc = confProjectile.EnableARC == 1;
            zfloat arcHeight = CalculateProjectileArcHeight(birthPosition, targetPos, isHoming, enableArc);

            TransformComponent projectileTransform = TransformComponent.Create(birthPosition, zQuaternion.identity, zVector3.one * zfloat.FromRaw(confProjectile.Scale));
            ComponentManager.AddComponent(projectile, projectileTransform);

            // 添加弹道组件
            // 注意：HitDistance、DamageRadius、MaxDamageTargets、ShareDamage 需要在 Excel Projectile 配置表中添加
            // 字段格式为万分比（10000=1米），使用 zfloat.CreateFloat 转换
            var projComponent = ProjectileComponent.Create(
                sourceEntityId: source.Id,
                targetEntityId: target.Id,
                targetPosition: targetPos,
                startPosition: birthPosition,
                confProjectileId: confProjectileId,
                sourceCampId: sourceCampId,
                damage: (zfloat)confProjectile.Damage,
                speed: (zfloat)confProjectile.Speed,
                isHoming: isHoming, // 追踪型导弹
                enableArc: enableArc,
                arcHeight: arcHeight,
                hitDistance: zfloat.FromRaw(confProjectile.HitDistance), // 命中距离（万分比）
                damageRadius: zfloat.FromRaw(confProjectile.DamageRadius), // 伤害半径（万分比）
                maxDamageTargets: confProjectile.MaxDamageTargets, // 最大伤害目标数
                shareDamage: confProjectile.ShareDamage == 1 // 均摊伤害
            );
            ComponentManager.AddComponent(projectile, projComponent);

            // 添加速度组件（初始速度朝向目标）
            zVector3 direction = (targetPos - sourcePos).normalized;
            // direction不能为zero
            if (direction.IsZero())
                direction = zVector3.forward;

            ComponentManager.AddComponent(projectile, new VelocityComponent(direction * projComponent.Speed));

            // 添加UnitComponent（提供视图标识信息）
            ComponentManager.AddComponent(projectile, new UnitComponent
            {
                UnitType = UnitType.Projectile, // 100=弹道类型
                PrefabId = 9,   // 弹道预制体ID
                PlayerId = sourceCampId,
                MoveSpeed = projComponent.Speed
            });

            // 添加Camp组件（继承发射者阵营，便于统一查询和碰撞检测）
            ComponentManager.AddComponent(projectile, CampComponent.Create(sourceCampId));

            // 发布事件通知表现层创建弹道视图
            World.EventManager.Publish(new UnitCreatedEvent
            {
                EntityId = projectile.Id,
                UnitType = (int)UnitType.Projectile, // 100表示弹道类型
                ConfProjectileID = confProjectileId,
                Position = sourcePos,
                PlayerId = sourceCampId,
                PrefabId = -1 // 弹道预制体ID
            });

            // zUDebug.Log($"[CombatSystem] 发射弹道Entity_{projectile.Id}: {source.Id} -> {target.Id}");
        }

        private zVector3 GetProjectileSourcePosition(Entity source, TransformComponent transform)
        {
            string offsetText = null;

            if (ComponentManager.HasComponent<BuildingComponent>(source))
            {
                BuildingComponent building = ComponentManager.GetComponent<BuildingComponent>(source);
                ConfBuilding confBuilding = ConfigManager.Get<ConfBuilding>(building.BuildingType);
                offsetText = confBuilding?.ProjectileSpawnOffset;
            }
            else if (ComponentManager.HasComponent<UnitComponent>(source))
            {
                UnitComponent unit = ComponentManager.GetComponent<UnitComponent>(source);
                ConfUnit confUnit = ConfigManager.Get<ConfUnit>((int)unit.UnitType);
                offsetText = confUnit?.ProjectileSpawnOffset;
            }

            if (string.IsNullOrWhiteSpace(offsetText))
                return transform.Position;

            zVector3 localOffset = ParseProjectileSpawnOffset(offsetText);
            if (localOffset.IsZero())
                return transform.Position;

            return transform.Position + transform.Rotation * localOffset;
        }

        private zVector3 ParseProjectileSpawnOffset(string offsetText)
        {
            var positions = StringUtils.ParsePositions(offsetText);
            if (positions.Length == 0)
                return zVector3.zero;

            var offset = positions[0];

            return new zVector3(
                zfloat.FromRaw((long)offset.x),
                zfloat.FromRaw((long)offset.y),
                zfloat.FromRaw((long)offset.z));
        }

        private zfloat CalculateProjectileArcHeight(zVector3 startPosition, zVector3 targetPosition, bool isHoming, bool enableArc)
        {
            if (isHoming || !enableArc)
                return zfloat.Zero;

            zVector3 horizontalOffset = targetPosition - startPosition;
            horizontalOffset.y = zfloat.Zero;

            if (horizontalOffset.sqrMagnitude <= zfloat.Zero)
                return zfloat.Zero;

            zfloat arcHeight = horizontalOffset.magnitude * PROJECTILE_ARC_HEIGHT_FACTOR;
            return zMathf.Clamp(arcHeight, PROJECTILE_MIN_ARC_HEIGHT, PROJECTILE_MAX_ARC_HEIGHT);
        }
    }



    
}
