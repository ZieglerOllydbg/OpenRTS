using zUnity;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Simulation.Events;
using ZLockstep.Flow;
using System.Collections.Generic;
using ZLockstep.Simulation.ECS.Utils;
using Utils;
using ZLockstep.RVO;

namespace ZLockstep.Simulation.ECS
{
    /// <summary>
    /// 建筑类型枚举
    /// </summary>
    public enum BuildingType
    {
        None = 0,
        Base = 1,       // 基地
        Mine = 2,       // 矿
        Smelter = 3,    // 油井
        PowerPlant = 4, // 电厂
        Barracks = 5,   // 整备营区
        LightFactory = 6, // 轻工厂
        HeavyFactory = 7,   // 重工厂
        Tower = 8,      // 防御塔
    }

    /// <summary>
    /// 单位类型枚举
    /// </summary>
    public enum UnitType
    {
        None = 0,           // 无效
        Infantry = 1,       // 动员兵
        badgerTank = 2,     // 獾式战车
        grizzlyTank = 3,    // 重装坦克
        
        Harvester = 4,    // 矿车
        Projectile = 100,   // 弹丸
    }

    /// <summary>
    /// 实体创建管理器
    /// 负责统一管理游戏中各种实体（建筑、单位等）的创建逻辑
    /// </summary>
    public class EntityCreationManager
    {
        /// <summary>
        /// 创建建筑实体（创世阶段使用）
        /// </summary>
        /// <param name="world">游戏世界实例</param>
        /// <param name="campId">玩家ID</param>
        /// <param name="buildingType">建筑类型</param>
        /// <param name="position">位置</param>
        /// <param name="confBuildingID">建筑配置表ID</param>
        /// <param name="mapManager">地图管理器（可选）</param>
        /// <param name="flowFieldManager">流场管理器（可选）</param>
        /// <returns>创建的实体事件</returns>
        public static UnitCreatedEvent? CreateBuildingEntity(
            zWorld world, 
            int campId, 
            zVector3 position, 
            int confBuildingID,
            IFlowFieldMap mapManager = null,
            FlowFieldManager flowFieldManager = null)
        {
            var confBuilding = ConfigManager.Get<ConfBuilding>(confBuildingID);
            if (confBuilding == null)
            {
                zUDebug.LogError($"[EntityCreationManager] 创建建筑实体时无法获取建筑配置信息。ID:{confBuildingID}");
                return null;
            }

            BuildingType buildingType = (BuildingType)confBuilding.Type;

            if (buildingType == BuildingType.Mine)
            {
                return null;
            }

            int width = confBuilding.Size;
            int height = confBuilding.Size;

            // 1. 创建建筑实体
            var entity = world.EntityManager.CreateEntity();

            // 使用confBuilding.Rotation(0-360)作为建筑的初始旋转
            zfloat rotationY = new zfloat(confBuilding.Rotation);
            zQuaternion rotation = zQuaternion.Euler(zfloat.Zero, rotationY, zfloat.Zero);

            // 2. 添加Transform组件
            TransformComponent transform = TransformComponent.Create(position, rotation, zVector3.one * zfloat.FromRaw(confBuilding.Scale));
            world.ComponentManager.AddComponent(entity, transform);

            int x = (int)position.x;
            int y = (int)position.z;

            // 4. 添加建筑组件
            var buildingComponent = BuildingComponent.Create((int)buildingType, x, y, width, height);
            world.ComponentManager.AddComponent(entity, buildingComponent);

            // 5. 添加阵营组件
            var campComponent = CampComponent.Create(campId);
            world.ComponentManager.AddComponent(entity, campComponent);

            // 6. 添加生命值组件
            var healthComponent = CreateBuildingHealthComponent(confBuildingID);
            if (healthComponent.HasValue)
            {
                world.ComponentManager.AddComponent(entity, healthComponent.Value);
            }
            
            // 7. 如果建筑有攻击能力（如防御塔），添加攻击组件
            var attackComponent = CreateBuildingAttackComponent(confBuildingID);
            if (attackComponent.HasValue)
            {
                world.ComponentManager.AddComponent(entity, attackComponent.Value);
            }

            // 8. 更新地图：将建筑占据的格子标记为不可行走
            if (mapManager != null)
            {
                // 计算建筑占据的世界坐标边界
                int minX = x - width / 2;
                int minY = y - height / 2;
                int maxX = x + width / 2;
                int maxY = y + height / 2;

                mapManager.SetWalkableRect(minX, minY, maxX, maxY, false);

                // 创建矩形障碍物顶点（逆时针顺序）
                List<zVector2> buildingVertices = new List<zVector2>
                {
                    new(minX, minY),  // 左下
                    new(maxX, minY),   // 右下
                    new(maxX, maxY),   // 右上
                    new(minX, maxY)    // 左上
                };

                // RVO 添加建筑阻挡障碍物
                Simulator.Instance.addObstacle(buildingVertices);
                Simulator.Instance.processObstacles();

                // 9. 标记流场为脏（需要重新计算）
                if (flowFieldManager != null)
                {
                    flowFieldManager.MarkRegionDirty(minX, minY, maxX, maxY);
                }
            }



            // 判断是本地玩家，添加本地玩家组件
            if (world.ComponentManager.HasGlobalComponent<GlobalInfoComponent>())
            {
                var globalInfoComponent = world.ComponentManager.GetGlobalComponent<GlobalInfoComponent>();
                if (globalInfoComponent.LocalPlayerCampId == campId)
                {
                    world.ComponentManager.AddComponent(entity, new LocalPlayerComponent());
                }
            }

            // 10. 如果是基地建筑，添加主基地组件
            if (buildingType == BuildingType.Base) // 基地建筑
            {
                world.ComponentManager.AddComponent(entity, new MainBaseComponent());
            }

            if (confBuilding.CreateUnitIDList.Length > 0)
            {
                // 支持生产动员兵和坦克
                var supportedUnitTypes = new HashSet<UnitType>(); // 2=獾式战车, 2=重装坦克

                int[] unitIDs = StringUtils.ParseIntegers(confBuilding.CreateUnitIDList);
                foreach (var unitID in unitIDs)
                {
                    if (unitID <= 0) 
                        continue;

                    var unitType = (UnitType)unitID;
                    supportedUnitTypes.Add(unitType);
                }

                if (supportedUnitTypes.Count > 0)
                {
                    var produceComponent = ProduceComponent.Create(supportedUnitTypes);
                    world.ComponentManager.AddComponent(entity, produceComponent);
                }
            }

            // 12. 如果是矿源，添加矿源组件
            if (buildingType == BuildingType.Mine) // 矿源
            {
                // 添加矿源组件，初始资源20000
                var mineComponent = MineComponent.CreateDefault();
                world.ComponentManager.AddComponent(entity, mineComponent);
            }
            // 13. 如果是采矿场，添加采矿组件
            else if (buildingType == BuildingType.Smelter) // 采矿场
            {
                // 添加采矿组件，关联到最近的矿源
                var miningComponent = MiningComponent.Create(0);
                world.ComponentManager.AddComponent(entity, miningComponent);
            }

            // 14. 添加建造组件（所有建筑都需要建造时间）
            var constructionTime = GetConstructionTime(confBuildingID);
            if (constructionTime > zfloat.Zero)
            {
                var buildingConstructionComponent = BuildingConstructionComponent.Create(constructionTime);
                world.ComponentManager.AddComponent(entity, buildingConstructionComponent);
            }

            // 15. 创建事件对象并返回
            var unitCreatedEvent = new UnitCreatedEvent
            {
                EntityId = entity.Id,
                UnitType = (int)buildingType,
                ConfBuildingID = confBuildingID,
                Position = position,
                Rotation = rotation,
                PlayerId = campId
            };

            return unitCreatedEvent;
        }

        /// <summary>
        /// 创建单位实体（创世阶段使用）
        /// </summary>
        /// <param name="world">游戏世界实例</param>
        /// <param name="campId">玩家ID</param>
        /// <param name="unitType">单位类型</param>
        /// <param name="position">位置</param>
        /// <param name="prefabId">预制体ID</param>
        /// <returns>创建的实体事件</returns>
        public static UnitCreatedEvent? CreateUnitEntity(
            zWorld world,
            int campId,
            UnitType unitType,
            zVector3 position,
            int prefabId)
        {
            int confUnitID = (int)unitType;
            ConfUnit confUnit = ConfigManager.Get<ConfUnit>(confUnitID);
            if (confUnit == null)
            {
                zUDebug.LogError($"[EntityCreationManager] 创建单位时无法获取单位配置信息。ID:{confUnitID}");
                return null;
            }

            // 根据单位类型确定单位参数
            zfloat radius = zfloat.FromRaw(confUnit.Radius);
            zfloat maxSpeed = zfloat.FromRaw(confUnit.Speed);
            zfloat scale = zfloat.FromRaw(confUnit.Scale);

            // 1. 创建单位实体
            var entity = world.EntityManager.CreateEntity();

            // 2. 添加Transform组件
            TransformComponent transformComponent = TransformComponent.Create(position, zQuaternion.xPositive, zVector3.one * scale);

            world.ComponentManager.AddComponent(entity, transformComponent);
            
            // 3. 添加阵营组件
            var camp = CampComponent.Create(campId);
            world.ComponentManager.AddComponent(entity, camp);

            // 4. 添加Unit组件（根据类型）
            var unitComponent =  new UnitComponent
            {
                UnitType = unitType,
                MoveSpeed = (zfloat)confUnit.Speed,
                RotateSpeed = (zfloat)180.0f,
                PlayerId = campId,
                SelectionRadius = radius,
                IsSelectable = true,
                PrefabId = prefabId
            };
            world.ComponentManager.AddComponent(entity, unitComponent);

            // 5. 添加Health组件
            var healthComponent = new HealthComponent((zfloat)confUnit.Hp);
            world.ComponentManager.AddComponent(entity, healthComponent);

            // 6. 如果单位有攻击能力，添加Attack组件
            var attackComponent = new AttackComponent
            {
                ConfProjectileID = confUnit.ProjectileID,
                Range = new zfloat(confUnit.AtkRange),
                WarningRange = new zfloat(confUnit.WarningRange),
                AttackInterval = zfloat.FromRaw(confUnit.AtkInterval),
                MaxTargets = confUnit.AtkCount,
                TimeSinceLastAttack = zfloat.Zero,
                TargetEntityId = -1
            };
            zUDebug.Log($"[EntityCreationManager] AttackInterval={attackComponent.AttackInterval}");
            world.ComponentManager.AddComponent(entity, attackComponent);

            // 7. 添加速度组件
            world.ComponentManager.AddComponent(entity, new VelocityComponent(zVector3.zero));

            // 8. 添加导航能力
            // 注意：NavSystem需要在外部添加，因为我们可能没有NavSystem的引用
            world.GameInstance.GetNavSystem().AddNavigator(entity, radius, maxSpeed);

            // 9. 添加旋转相关组件
            // 根据单位类型添加不同的载具类型组件
            VehicleTypeComponent vehicleType = new VehicleTypeComponent
            {
                Type = VehicleTypeComponent.VehicleType.Infantry,
                BodyRotationSpeed = new zfloat(360),  // 360度/秒，快速转向
                InPlaceRotationThreshold = new zfloat(180), // 180度，基本不需要原地转向
                HasTurret = false,
                TurretRotationSpeed = zfloat.Zero
            };
            world.ComponentManager.AddComponent(entity, vehicleType);

            // 添加旋转状态组件
            var rotationState = RotationStateComponent.Create();
            world.ComponentManager.AddComponent(entity, rotationState);

            // 如果有炮塔，添加炮塔组件
            if (vehicleType.HasTurret)
            {
                var turret = TurretComponent.CreateDefault();
                world.ComponentManager.AddComponent(entity, turret);
            }

            // 判断是本地玩家，添加本地玩家组件
            if (world.ComponentManager.HasGlobalComponent<GlobalInfoComponent>())
            {
                var globalInfoComponent = world.ComponentManager.GetGlobalComponent<GlobalInfoComponent>();
                if (globalInfoComponent.LocalPlayerCampId == campId)
                {
                    world.ComponentManager.AddComponent(entity, new LocalPlayerComponent());
                }
            }

            // 10. 创建事件对象并返回
            var unitCreatedEvent = new UnitCreatedEvent
            {
                EntityId = entity.Id,
                UnitType = (int)unitType,
                Position = position,
                Rotation = zQuaternion.xPositive,
                PlayerId = campId,
                ConfUnitID = confUnitID,
            };

            zUDebug.Log($"[EntityCreationManager] 玩家{campId} 创建了单位类型{unitType} 在位置{position}，Entity ID: {entity.Id}");
            
            return unitCreatedEvent;
        }

        #region 建筑辅助方法

        private static HealthComponent? CreateBuildingHealthComponent(int confBuildingID)
        {
            var confBuilding = ConfigManager.Get<ConfBuilding>(confBuildingID);
            if (confBuilding == null)
            {
                zUDebug.LogError($"[EntityCreationManager] 创建建筑时无法获取建筑配置信息。ID:{confBuildingID}");
                return null;
            }

            if (confBuilding.Hp > 0)
            {
                return new HealthComponent((zfloat)confBuilding.Hp);
            }
            else
            {
                return null;
            }
        }

        private static AttackComponent? CreateBuildingAttackComponent(int confBuildingID)
        {
            var confBuilding = ConfigManager.Get<ConfBuilding>(confBuildingID);
            if (confBuilding == null)
            {
                zUDebug.LogError($"[EntityCreationManager] 创建建筑时无法获取建筑配置信息。ID:{confBuildingID}");
                return null;
            }

            if (confBuilding.CanAttack > 0)
            {
                return new AttackComponent
                {
                    MaxTargets = confBuilding.AtkCount,
                    ConfProjectileID = confBuilding.ProjectileID,
                    Range = new zfloat(confBuilding.AtkRange),
                    WarningRange = new zfloat(confBuilding.AtkRange),
                    AttackInterval = zfloat.FromRaw(confBuilding.AtkInterval),
                    TimeSinceLastAttack = zfloat.Zero,
                    TargetEntityId = -1
                };
            }

            return null;
        }

        #endregion

        #region 采矿辅助方法
        
        /// <summary>
        /// 查找距离指定位置最近的矿源
        /// </summary>
        /// <param name="world">游戏世界实例</param>
        /// <param name="position">当前位置</param>
        /// <returns>最近的矿源实体ID，如果没找到则返回-1</returns>
        private static int FindNearestMine(zWorld world, zVector3 position)
        {
            int nearestMineEntityId = -1;
            zfloat minDistanceSquared = zfloat.Infinity;
            
            // 获取所有具有矿源组件的实体
            var mineEntities = world.ComponentManager.GetAllEntityIdsWith<MineComponent>();
            
            foreach (var entityId in mineEntities)
            {
                var entity = new Entity(entityId);
                
                // 检查实体是否具有Transform组件
                if (world.ComponentManager.HasComponent<TransformComponent>(entity))
                {
                    var transformComponent = world.ComponentManager.GetComponent<TransformComponent>(entity);
                    zVector3 minePosition = transformComponent.Position;
                    
                    // 计算距离的平方（避免开方运算）
                    zVector3 delta = position - minePosition;
                    zfloat distanceSquared = delta.x * delta.x + delta.z * delta.z; // 只考虑水平距离
                    
                    // 检查是否是最近的
                    if (distanceSquared < minDistanceSquared)
                    {
                        minDistanceSquared = distanceSquared;
                        nearestMineEntityId = entityId;
                    }
                }
            }
            
            return nearestMineEntityId;
        }
        
        /// <summary>
        /// 为采矿场分配关联的矿源
        /// </summary>
        /// <param name="world">游戏世界实例</param>
        /// <param name="smelterEntity">采矿场实体</param>
        /// <param name="mineEntityId">矿源实体ID</param>
        public static void AssignMineToSmelter(zWorld world, Entity smelterEntity, int mineEntityId)
        {
            // 添加采矿组件，关联到指定的矿源
            var miningComponent = MiningComponent.Create(mineEntityId);
            world.ComponentManager.AddComponent(smelterEntity, miningComponent);
            
            zUDebug.Log($"[EntityCreationManager] 为采矿场实体 {smelterEntity.Id} 分配了矿源实体 {mineEntityId}");
        }
        
        #endregion

        #region 建造辅助方法
        
        /// <summary>
        /// 获取建筑的建造时间
        /// </summary>
        /// <param name="buildingType">建筑类型</param>
        /// <returns>建造时间（秒）</returns>
        private static zfloat GetConstructionTime(int confBuildingID)
        {
            var confBuilding = ConfigManager.Get<ConfBuilding>(confBuildingID);
            if (confBuilding == null)
            {
                zUDebug.LogError($"[BuildingPlacementUtils] 获取建筑配置信息失败。ID:{confBuildingID}");
                return zfloat.Zero;
            }

            return (zfloat)confBuilding.ConstructionTime;
        }
        
        #endregion
    }
}
