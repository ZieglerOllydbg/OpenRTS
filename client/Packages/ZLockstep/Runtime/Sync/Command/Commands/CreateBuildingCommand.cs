using zUnity;
using ZLockstep.Simulation;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using System.Text;
using ZLockstep.Simulation.Events;
using ZLockstep.Simulation.ECS.Utils;

namespace ZLockstep.Sync.Command.Commands
{
    /// <summary>
    /// 创建建筑命令
    /// 用于在地图上建造静态建筑（基地、防御塔等）
    /// </summary>
    [CommandType(CommandTypes.BuildStructure)]
    public class CreateBuildingCommand : BaseCommand
    {
        /// <summary>
        /// 配置表ID
        /// </summary>
        public int ConfBuildingID { get; set; }

        /// <summary>
        /// 建筑类型
        /// 0=基地, 1=防御塔等
        /// </summary>
        public BuildingType BuildingType { get; set; }

        /// <summary>
        /// 建筑位置（世界坐标）
        /// </summary>
        public zVector3 Position { get; set; }


        public CreateBuildingCommand(int confID, int campId, zVector3 position)
            : base(campId)
        {
            ConfBuildingID = confID;
            var confBuilding = ConfigManager.Get<ConfBuilding>(ConfBuildingID);
            BuildingType = confBuilding != null? (BuildingType)confBuilding.Type : BuildingType.None;
            zUDebug.Log($"[EntityCreationManager] 创建建筑类型{BuildingType} " +
                $"在位置{Position}");
            Position = position;
        }

        public override void Execute(zWorld world)
        {
            if (BuildingType != BuildingType.Base)
            {
                // 主基地之外的建筑都需要有主基地才能建造
                var (mainBaseComponent, entity) = world.ComponentManager.GetComponentWithCondition<MainBaseComponent>(
                    e => {
                        if (!world.ComponentManager.HasComponent<CampComponent>(e)) 
                            return false;
                        var campComponent = world.ComponentManager.GetComponent<CampComponent>(e);
                        return campComponent.CampId == CampId;
                    });
                
                if (entity.Id == -1)
                {
                    world.EventManager.Publish(new MessageEvent("先建造主基地才能建造其他建筑"));
                    return;
                }
            }
            
            // 阻挡判断
            var mapManager = world.GameInstance.GetMapManager();
            if (!BuildingPlacementUtils.CheckBuildingPlacement(ConfBuildingID, Position, mapManager))
            {
                zUDebug.Log($"[CreateBuildingCommand] 阵营{CampId} 建造建筑类型{BuildingType} 在位置{Position} 失败：位置被阻挡或超出边界");
                world.EventManager.Publish(new MessageEvent("无法建造建筑，请检查资源是否充足"));
                return;
            }

            // 检查并扣除资源
            if (!CheckAndDeductResources(world))
            {
                zUDebug.Log($"[CreateBuildingCommand] 阵营{CampId} 资源不足，无法建造建筑类型{BuildingType}");
                return;
            }

            // 使用EntityCreationManager创建建筑实体
            var unitCreatedEvent = EntityCreationManager.CreateBuildingEntity(
                world, 
                CampId, 
                Position, 
                ConfBuildingID,
                world.GameInstance.GetMapManager(),
                world.GameInstance.GetFlowFieldManager()
            );

            // 发布事件通知表现层创建视图
            if (unitCreatedEvent.HasValue)
            {
                world.EventManager.Publish(unitCreatedEvent.Value);
            }

            zUDebug.Log($"[CreateBuildingCommand] 阵营{CampId} 建造了建筑类型{BuildingType} " +
                $"在位置{Position}");
        }

        /// <summary>
        /// 检查采矿场是否在矿源附近（10格范围内）
        /// </summary>
        /// <param name="world">游戏世界实例</param>
        /// <param name="position">采矿场位置</param>
        /// <returns>是否在矿源附近</returns>
        private bool CheckMineProximity(zWorld world, zVector3 position)
        {
            // 获取所有矿源实体
            var mineEntities = world.ComponentManager.GetAllEntityIdsWith<MineComponent>();
            
            foreach (var entityId in mineEntities)
            {
                var entity = new Entity(entityId);
                
                // 检查实体是否具有Transform组件
                if (world.ComponentManager.HasComponent<TransformComponent>(entity))
                {
                    var transformComponent = world.ComponentManager.GetComponent<TransformComponent>(entity);
                    zVector3 minePosition = transformComponent.Position;
                    
                    // 计算与矿源的距离
                    zVector3 delta = position - minePosition;
                    zfloat distanceSquared = delta.x * delta.x + delta.z * delta.z; // 只考虑水平距离
                    
                    // 检查是否在10格范围内（距离小于等于10）
                    zfloat maxDistance = zfloat.FromRaw(10 * zfloat.SCALE_10000);
                    if (distanceSquared <= maxDistance * maxDistance)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// 检查并扣除建造建筑所需的资源
        /// </summary>
        /// <param name="world">游戏世界实例</param>
        /// <returns>是否有足够的资源</returns>
        private bool CheckAndDeductResources(zWorld world)
        {
            // 获取玩家的经济组件
            var (economyComponent, economyEntity) = world.ComponentManager.GetComponentWithCondition<EconomyComponent>(
                e => world.ComponentManager.HasComponent<CampComponent>(e) && 
                world.ComponentManager.GetComponent<CampComponent>(e).CampId == CampId);
            
            if (economyEntity.Id == -1)
            {
                UnityEngine.Debug.LogWarning($"[CreateBuildingCommand] 未找到阵营 {CampId} 的经济组件");
                return false;
            }

            var confBuilding = ConfigManager.Get<ConfBuilding>(ConfBuildingID);
            if (confBuilding == null)
            {
                zUDebug.LogError($"[CreateBuildingCommand] 未找到建筑配置信息 {ConfBuildingID}");
                return false;
            }
            
            // 根据建筑类型确定所需资源
            int costMoney = confBuilding.CostMoney;
            int costPower = confBuilding.CostPower;
            
            // 检查是否有足够的资源
            if (costMoney > 0 && economyComponent.Money < costMoney)
            {
                zUDebug.Log($"[CreateBuildingCommand] 金币不足。需要: {costMoney}, 当前: {economyComponent.Money}");

                world.EventManager.Publish(new MessageEvent
                {
                    Message = "金币不足！"
                });
                return false;
            }

            if (costPower > 0 && economyComponent.Power < costPower)
            {
                zUDebug.Log($"[CreateBuildingCommand] 能量不足。需要: {costPower}, 当前: {economyComponent.Power}");
                world.EventManager.Publish(new MessageEvent
                {
                    Message = "能量不足！"
                });
                return false;
            }
            
            // 扣除资源
            int oldMoney = economyComponent.Money;
            economyComponent.Money -= costMoney;
            int newMoney = economyComponent.Money;
            
            // 触发金币变化事件
            world.EventManager.Publish(new MoneyChangedEvent
            {
                CampId = CampId,
                OldMoney = oldMoney,
                NewMoney = newMoney,
                Reason = "建造建筑消耗金币"
            });
            
            if (costPower > 0)
            {
                int oldPower = economyComponent.Power;
                economyComponent.Power -= costPower; // 扣除能量供应
                int newPower = economyComponent.Power;
                
                // 触发能量变化事件
                world.EventManager.Publish(new PowerChangedEvent
                {
                    CampId = CampId,
                    OldPower = oldPower,
                    NewPower = newPower,
                    Reason = "建造建筑消耗能量"
                });
                
                zUDebug.Log($"[CreateBuildingCommand] 扣除能量成功。消耗能量: {costPower}。剩余能量: {economyComponent.Power}");
            }
            
            // 更新经济组件
            world.ComponentManager.AddComponent(economyEntity, economyComponent);
            
            zUDebug.Log($"[CreateBuildingCommand] 扣除资源成功。花费金币: {costMoney}, 获得能量: {costPower}。剩余金币: {economyComponent.Money}, 总能量: {economyComponent.Power}");
            return true;
        }

        public override string ToString()
        {
            return $"[CreateBuildingCommand] 阵营{CampId} 建造建筑类型{BuildingType} " +
                $"在位置{Position}（格子坐标）";
        }
    }
}