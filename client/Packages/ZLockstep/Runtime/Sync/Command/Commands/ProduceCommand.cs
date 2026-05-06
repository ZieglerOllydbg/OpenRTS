using zUnity;
using ZLockstep.Simulation;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using System.Text;
using ZLockstep.Simulation.Events;

namespace ZLockstep.Sync.Command.Commands
{
    /// <summary>
    /// 生产命令
    /// 用于增加或减少特定单位类型的生产数量
    /// </summary>
    [CommandType(CommandTypes.Produce)]
    public class ProduceCommand : BaseCommand
    {
        /// <summary>
        /// 建筑实体ID
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// 单位类型
        /// </summary>
        public UnitType UnitType { get; set; }

        /// <summary>
        /// 变化值 (+1 或 -1)
        /// </summary>
        public int ChangeValue { get; set; }

        public ProduceCommand(int campId, int entityId, UnitType unitType, int changeValue)
            : base(campId)
        {
            EntityId = entityId;
            UnitType = unitType;
            ChangeValue = changeValue;
        }

        public override void Execute(zWorld world)
        {
            var entity = new Entity(EntityId);

            // 阵营是否一致
            if (!world.ComponentManager.HasComponent<CampComponent>(entity))
            {
                UnityEngine.Debug.LogWarning($"[ProduceCommand] 实体 {EntityId} 不具有阵营组件");
                return;
            }

            var campComponent = world.ComponentManager.GetComponent<CampComponent>(entity);
            if (campComponent.CampId != CampId)
            {
                UnityEngine.Debug.LogWarning($"[ProduceCommand] 玩家 {CampId} 尝试控制不属于自己的建筑 {EntityId}");
                return;
            }

            // 检查实体是否存在且具有ProduceComponent组件
            if (!world.ComponentManager.HasComponent<ProduceComponent>(entity))
            {
                UnityEngine.Debug.LogWarning($"[ProduceCommand] 实体 {EntityId} 不具有 ProduceComponent 组件");
                return;
            }

            // 获取当前生产组件状态
            var produceComponent = world.ComponentManager.GetComponent<ProduceComponent>(entity);
            int currentNumber = produceComponent.ProduceNumbers.ContainsKey(UnitType) ? 
                produceComponent.ProduceNumbers[UnitType] : 0;
            int newNumber = currentNumber + ChangeValue;
            
            // 限制在0-99范围内
            newNumber = UnityEngine.Mathf.Clamp(newNumber, 0, 99);
            
            // 检查是否是增加生产命令（ChangeValue > 0）
            if (ChangeValue > 0)
            {
                // 检查金币是否足够
                if (!CheckAndDeductProductionCost(world, campComponent.CampId, ChangeValue))
                {
                    zUDebug.Log($"[ProduceCommand] 阵营 {campComponent.CampId} 金币不足，无法生产单位 {UnitType}");
                    return;
                }
            }
            // 如果是减少生产命令（ChangeValue < 0）并且生产数量确实减少了
            else if (ChangeValue < 0 && newNumber < currentNumber)
            {
                // 返还金币
                RefundProductionCost(world, campComponent.CampId, -ChangeValue);
            }

            // 检查单位类型是否支持生产
            if (!produceComponent.SupportedUnitTypes.Contains(UnitType))
            {
                zUDebug.LogWarning($"[ProduceCommand] 实体 {EntityId} 不支持生产单位类型 {UnitType}");
                // 如果不支持生产，且之前扣除了金币，需要返还金币
                if (ChangeValue > 0)
                {
                    RefundProductionCost(world, campComponent.CampId, ChangeValue);
                }
                return;
            }

            // 更新生产数量
            if (produceComponent.ProduceNumbers.ContainsKey(UnitType))
            {
                produceComponent.ProduceNumbers[UnitType] = newNumber;
                
                // 更新组件
                world.ComponentManager.AddComponent(entity, produceComponent);
                
                zUDebug.Log($"[ProduceCommand] 实体 {EntityId} 的单位类型 {UnitType} 生产数量更新为 {newNumber}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[ProduceCommand] 实体 {EntityId} 的生产组件中未找到单位类型 {UnitType}");
                // 如果未找到单位类型，且之前扣除了金币，需要返还金币
                if (ChangeValue > 0)
                {
                    RefundProductionCost(world, campComponent.CampId, ChangeValue);
                }
                return;
            }
        }

        /// <summary>
        /// 检查并扣除生产单位所需的成本
        /// </summary>
        /// <param name="world">游戏世界实例</param>
        /// <param name="campId">阵营 ID</param>
        /// <param name="quantity">生产数量</param>
        /// <returns>是否有足够的资源</returns>
        private bool CheckAndDeductProductionCost(zWorld world, int campId, int quantity)
        {
            // 获取玩家的经济组件
            var (economyComponent, economyEntity) = world.ComponentManager.GetComponentWithCondition<EconomyComponent>(
                e => world.ComponentManager.HasComponent<CampComponent>(e) && 
                world.ComponentManager.GetComponent<CampComponent>(e).CampId == campId);
            
            if (economyEntity.Id == -1)
            {
                UnityEngine.Debug.LogWarning($"[ProduceCommand] 未找到阵营 {campId} 的经济组件");
                return false;
            }
            
            // 根据单位类型确定所需资源
            ConfUnit confUnit = ConfigManager.Get<ConfUnit>((int)UnitType);
            if (confUnit == null)
            {
                zUDebug.LogError($"[ProduceCommand] 创建单位时无法获取单位配置信息。ID:{confUnit}");
                return false;
            }

            int costMoney = confUnit.CostMoney * quantity;
            
            // 检查是否有足够的资源
            if (economyComponent.Money < costMoney)
            {
                zUDebug.Log($"[ProduceCommand] 金币不足。需要：{costMoney}, 当前：{economyComponent.Money}");
                return false;
            }
            
            // 扣除资源
            int oldMoney = economyComponent.Money;
            economyComponent.Money -= costMoney;
            int newMoney = economyComponent.Money;
            
            // 触发金币变化事件
            world.EventManager.Publish(new MoneyChangedEvent
            {
                CampId = campId,
                OldMoney = oldMoney,
                NewMoney = newMoney,
                Reason = $"生产单位消耗金币 x{quantity}"
            });
            
            // 更新经济组件
            world.ComponentManager.AddComponent(economyEntity, economyComponent);
            
            zUDebug.Log($"[ProduceCommand] 扣除资源成功。花费金币：{costMoney} (单价：{confUnit.CostMoney} x {quantity})。剩余金币：{economyComponent.Money}");
            return true;
        }

        /// <summary>
        /// 返还生产单位所需的成本
        /// </summary>
        /// <param name="world">游戏世界实例</param>
        /// <param name="campId">阵营 ID</param>
        /// <param name="quantity">返还数量</param>
        private void RefundProductionCost(zWorld world, int campId, int quantity)
        {
            // 获取玩家的经济组件
            var (economyComponent, economyEntity) = world.ComponentManager.GetComponentWithCondition<EconomyComponent>(
                e => world.ComponentManager.HasComponent<CampComponent>(e) && 
                world.ComponentManager.GetComponent<CampComponent>(e).CampId == campId);
            
            if (economyEntity.Id == -1)
            {
                UnityEngine.Debug.LogWarning($"[ProduceCommand] 未找到阵营 {campId} 的经济组件");
                return;
            }
            
            // 根据单位类型确定返还资源
            ConfUnit confUnit = ConfigManager.Get<ConfUnit>((int)UnitType);
            if (confUnit == null)
            {
                zUDebug.LogError($"[ProduceCommand] 创建单位时无法获取单位配置信息。ID:{confUnit}");
                return;
            }

            int refundMoney = confUnit.CostMoney * quantity;
            
            // 返还资源
            int oldMoney = economyComponent.Money;
            economyComponent.Money += refundMoney;
            int newMoney = economyComponent.Money;
            
            // 触发金币变化事件
            world.EventManager.Publish(new MoneyChangedEvent
            {
                CampId = campId,
                OldMoney = oldMoney,
                NewMoney = newMoney,
                Reason = $"取消生产单位返还金币 x{quantity}"
            });
            
            // 更新经济组件
            world.ComponentManager.AddComponent(economyEntity, economyComponent);
            
            zUDebug.Log($"[ProduceCommand] 返还资源成功。返还金币：{refundMoney} (单价：{confUnit.CostMoney} x {quantity})。剩余金币：{economyComponent.Money}");
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[ProduceCommand] 玩家{CampId} 修改实体{EntityId}的单位类型{UnitType}生产数量，变化值{ChangeValue}");
            return sb.ToString();
        }
    }
}