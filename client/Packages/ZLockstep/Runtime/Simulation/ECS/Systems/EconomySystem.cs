using System.Collections.Generic;
using ZLockstep.Simulation.ECS.Components;
using zUnity;
using ZLockstep.Simulation.Events; // 添加事件命名空间

namespace ZLockstep.Simulation.ECS.Systems
{
    /// <summary>
    /// 经济系统
    /// 处理游戏中的经济逻辑，包括采矿、资源增长等
    /// </summary>
    public class EconomySystem : BaseSystem
    {
        public override int GetOrder()
        {
            // 经济系统应该在生产系统之后执行
            return (int)SystemOrder.Produce + 1;
        }

        public override void Update()
        {
            ProcessMining();
            ProcessPower(); // 添加能量处理
        }

        /// <summary>
        /// 处理采矿逻辑
        /// </summary>
        private void ProcessMining()
        {
            // 获取所有具有采矿组件的实体
            var miningEntities = ComponentManager.GetAllEntityIdsWith<MiningComponent>();
            
            foreach (var entityId in miningEntities)
            {
                var miningEntity = new Entity(entityId);
                var miningComponent = ComponentManager.GetComponent<MiningComponent>(miningEntity);
                
                // 检查建筑是否已完成建造
                if (ComponentManager.HasComponent<BuildingConstructionComponent>(miningEntity))
                {
                    // 建造未完成，不能采矿
                    continue;
                }
                
                // 增加采矿计时器
                miningComponent.MiningTimer++;
                
                // 检查是否到达采矿周期
                if (miningComponent.MiningTimer >= miningComponent.MiningCycleFrames)
                {
                    // 重置计时器
                    miningComponent.MiningTimer = 0;
                    
                    // 获取采矿场的阵营
                    if (ComponentManager.HasComponent<CampComponent>(miningEntity))
                    {
                        var campComponent = ComponentManager.GetComponent<CampComponent>(miningEntity);
                        int campId = campComponent.CampId;

                        ConfCamp confCamp = ConfigManager.Get<ConfCamp>(campId);
                        int addMoney = confCamp?.AddMoneyPerSecond ?? 0;
                        
                        // 增加玩家金币
                        AddResourceToPlayer(campId, addMoney);
                    }
                }

                // 更新采矿组件的计时器
                ComponentManager.AddComponent(miningEntity, miningComponent);
            }
        }
        
        /// <summary>
        /// 处理能量系统逻辑
        /// </summary>
        private void ProcessPower()
        {
            // 获取所有阵营
            var campEntities = ComponentManager.GetAllEntityIdsWith<CampComponent>();
            var campIds = new HashSet<int>();
            
            foreach (var entityId in campEntities)
            {
                var entity = new Entity(entityId);
                var campComponent = ComponentManager.GetComponent<CampComponent>(entity);
                campIds.Add(campComponent.CampId);
            }
            
            // 为每个阵营计算能量
            foreach (int campId in campIds)
            {
                UpdateCampPower(campId);
            }
        }
        
        /// <summary>
        /// 更新指定阵营的能量
        /// </summary>
        /// <param name="campId">阵营ID</param>
        private void UpdateCampPower(int campId)
        {
            // 获取该阵营的经济组件
            var (economyComponent, economyEntity) = ComponentManager.GetComponentWithCondition<EconomyComponent>(
                e => ComponentManager.HasComponent<CampComponent>(e) && 
                     ComponentManager.GetComponent<CampComponent>(e).CampId == campId);
            
            if (economyEntity.Id == -1)
            {
                return;
            }

            ConfCamp confCamp = ConfigManager.Get<ConfCamp>(campId);
            if (confCamp == null)
            {
                zUDebug.LogError("未找到阵营配置.  campId: " + campId);
                return;
            }

            IEnumerable<(BuildingComponent, Entity)> enumBuildingComponents = ComponentManager.GetComponentsWithCondition<BuildingComponent>(
                e => ComponentManager.HasComponent<CampComponent>(e) 
                    && ComponentManager.GetComponent<CampComponent>(e).CampId == campId);
            
            int producePower = 0;
            int costPower = 0;
            foreach (var (buildingComponent, buildingEntity) in enumBuildingComponents)
            {
                ConfBuilding confBuilding = ConfigManager.Get<ConfBuilding>(buildingComponent.BuildingType);
                if (confBuilding == null)
                {
                    zUDebug.LogError("未找到建筑配置.  BuildingType: " + buildingComponent.BuildingType);
                    continue;
                }

                costPower += confBuilding.CostPower;

                // 建筑完毕才能产生能量
                if (!ComponentManager.HasComponent<BuildingConstructionComponent>(buildingEntity))
                    producePower += confBuilding.ProducePower;
            }

            int totalPower = economyComponent.GMPower + confCamp.InitPower + producePower - costPower;
            
            // 检查能量是否发生变化，如果变化则触发事件
            if (economyComponent.Power != totalPower)
            {
                int oldPower = economyComponent.Power;
                // 更新能量值
                economyComponent.Power = totalPower;
                
                // 触发能量变化事件
                World.EventManager.Publish(new PowerChangedEvent
                {
                    CampId = campId,
                    OldPower = oldPower,
                    NewPower = totalPower,
                    Reason = "建筑变更导致能量变化"
                });
                
                ComponentManager.AddComponent(economyEntity, economyComponent);

                zUDebug.Log($"[EconomySystem] 阵营 {campId} 的能量已更新。消耗能量: {costPower}。生产能量: {producePower}。剩余能量: {economyComponent.Power}, 更新前能量: {oldPower}");
            }
        }
        
        /// <summary>
        /// 为指定玩家增加资源
        /// </summary>
        /// <param name="campId">玩家ID</param>
        /// <param name="amount">资源数量</param>
        private void AddResourceToPlayer(int campId, int amount)
        {
            // 查找玩家的经济组件
            var (economyComponent, entity) = ComponentManager.GetComponentWithCondition<EconomyComponent>(
                e => ComponentManager.HasComponent<CampComponent>(e) && 
                     ComponentManager.GetComponent<CampComponent>(e).CampId == campId);
            
            if (entity.Id != -1)
            {
                // 找到玩家的经济组件，增加金币
                int oldMoney = economyComponent.Money;
                economyComponent.Money += amount;
                int newMoney = economyComponent.Money;
                
                // 触发金币变化事件
                World.EventManager.Publish(new MoneyChangedEvent
                {
                    CampId = campId,
                    OldMoney = oldMoney,
                    NewMoney = newMoney,
                    Reason = "采矿收入"
                });
                
                ComponentManager.AddComponent(entity, economyComponent);
            }
        }
    }
}