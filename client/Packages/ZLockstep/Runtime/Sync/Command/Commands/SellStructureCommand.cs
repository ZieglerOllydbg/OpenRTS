using zUnity;
using ZLockstep.Simulation;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using System.Text;
using ZLockstep.Simulation.Events;

namespace ZLockstep.Sync.Command.Commands
{
    /// <summary>
    /// 售卖建筑命令
    /// 根据建筑血量百分比返还金钱
    /// </summary>
    [CommandType(CommandTypes.SellStructure)]
    public class SellStructureCommand : BaseCommand
    {
        /// <summary>
        /// 建筑实体 ID
        /// </summary>
        public int EntityId { get; set; }

        public SellStructureCommand(int campId, int entityId)
            : base(campId)
        {
            EntityId = entityId;
        }

        public override void Execute(zWorld world)
        {
            var entity = new Entity(EntityId);

            // 验证阵营是否一致
            if (!world.ComponentManager.HasComponent<CampComponent>(entity))
            {
                UnityEngine.Debug.LogWarning($"[SellStructureCommand] 实体 {EntityId} 不具有阵营组件");
                return;
            }

            var campComponent = world.ComponentManager.GetComponent<CampComponent>(entity);
            if (campComponent.CampId != CampId)
            {
                UnityEngine.Debug.LogWarning($"[SellStructureCommand] 玩家 {CampId} 尝试售卖不属于自己的建筑 {EntityId}");
                return;
            }

            // 检查建筑是否存在且具有 BuildingComponent 组件
            if (!world.ComponentManager.HasComponent<BuildingComponent>(entity))
            {
                UnityEngine.Debug.LogWarning($"[SellStructureCommand] 实体 {EntityId} 不具有 BuildingComponent 组件");
                return;
            }

            // 获取建筑配置
            var buildingComponent = world.ComponentManager.GetComponent<BuildingComponent>(entity);
            ConfBuilding confBuilding = ConfigManager.Get<ConfBuilding>(buildingComponent.BuildingType);
            if (confBuilding == null)
            {
                UnityEngine.Debug.LogError($"[SellStructureCommand] 找不到建筑配置：BuildingType={buildingComponent.BuildingType}");
                return;
            }

            // 计算返还金额（基于血量百分比）
            int refundAmount = CalculateRefundAmount(world, entity, confBuilding);
            
            if (refundAmount <= 0)
            {
                zUDebug.LogWarning($"[SellStructureCommand] 返还金额为 0，无法售卖");
                return;
            }

            // 返还金币
            RefundToPlayer(world, campComponent.CampId, refundAmount);

            // 销毁建筑实体
            DestroyBuilding(world, entity, confBuilding);

            zUDebug.Log($"[SellStructureCommand] 成功售卖建筑：{confBuilding.Name}, 返还金额：{refundAmount}");
        }

        /// <summary>
        /// 计算基于血量的返还金额
        /// </summary>
        private int CalculateRefundAmount(zWorld world, Entity entity, ConfBuilding confBuilding)
        {
            // 默认返还比例为 80% (使用 zfloat 定点数)
            zfloat baseRefundRate = zfloat.FromRaw(8000); // 0.8 = 8000/10000
            
            // 检查是否有 HealthComponent，如果有则根据血量百分比计算
            if (world.ComponentManager.HasComponent<HealthComponent>(entity))
            {
                var healthComponent = world.ComponentManager.GetComponent<HealthComponent>(entity);
                
                // 获取血量百分比 (已经是 zfloat 类型，范围 0~1)
                zfloat healthPercent = healthComponent.HealthPercent;
                
                // 返还金额 = 原价 * 基础返还比例 * 血量百分比
                // 注意：需要将结果转换为 int
                zfloat refundAmountZ = confBuilding.CostMoney * baseRefundRate * healthPercent;
                int refundAmount = (int)refundAmountZ;
                
                zUDebug.Log($"[SellStructureCommand] 建筑血量百分比：{(float)healthPercent:P0}, 原价：{confBuilding.CostMoney}, 返还：{refundAmount}");
                
                return refundAmount;
            }
            else
            {
                // 如果没有血量组件，按满血计算
                zfloat refundAmountZ = confBuilding.CostMoney * baseRefundRate;
                int refundAmount = (int)refundAmountZ;
                zUDebug.Log($"[SellStructureCommand] 无血量组件，按满血计算，返还：{refundAmount}");
                return refundAmount;
            }
        }

        /// <summary>
        /// 返还金币给玩家
        /// </summary>
        private void RefundToPlayer(zWorld world, int campId, int amount)
        {
            // 获取玩家的经济组件
            var (economyComponent, economyEntity) = world.ComponentManager.GetComponentWithCondition<EconomyComponent>(
                e => world.ComponentManager.HasComponent<CampComponent>(e) && 
                world.ComponentManager.GetComponent<CampComponent>(e).CampId == campId);
            
            if (economyEntity.Id == -1)
            {
                UnityEngine.Debug.LogWarning($"[SellStructureCommand] 未找到阵营 {campId} 的经济组件");
                return;
            }
            
            // 返还金币
            int oldMoney = economyComponent.Money;
            economyComponent.Money += amount;
            int newMoney = economyComponent.Money;
            
            // 触发金币变化事件
            world.EventManager.Publish(new MoneyChangedEvent
            {
                CampId = campId,
                OldMoney = oldMoney,
                NewMoney = newMoney,
                Reason = $"售卖建筑返还金币"
            });
            
            // 更新经济组件
            world.ComponentManager.AddComponent(economyEntity, economyComponent);
            
            zUDebug.Log($"[SellStructureCommand] 返还金币成功。返还：{amount}, 剩余金币：{economyComponent.Money}");
        }

        /// <summary>
        /// 销毁建筑实体
        /// </summary>
        private void DestroyBuilding(zWorld world, Entity entity, ConfBuilding confBuilding)
        {
            // 添加死亡组件
            var deathComponent = new DeathComponent(world.TimeManager.Tick, 0);
            world.ComponentManager.AddComponent(entity, deathComponent);

            // 发布死亡事件（通知表现层播放死亡动画）
            world.EventManager.Publish(new EntityDiedEvent
            {
                EntityId = entity.Id,
                UnitType = UnitType.None,
                Position = world.ComponentManager.HasComponent<TransformComponent>(entity)
                    ? world.ComponentManager.GetComponent<TransformComponent>(entity).Position
                    : zUnity.zVector3.zero,
            });
            
            zUDebug.Log($"[SellStructureCommand] 建筑已销毁：{confBuilding.Name}, EntityId: {entity.Id}");
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[SellStructureCommand] 玩家{CampId} 售卖建筑实体{EntityId}");
            return sb.ToString();
        }
    }
}
