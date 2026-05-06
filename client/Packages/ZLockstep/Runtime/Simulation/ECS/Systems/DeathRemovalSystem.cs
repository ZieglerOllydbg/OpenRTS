using System.Collections.Generic;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Simulation.Events;
using ZLockstep.View;
using ZLockstep.Flow;

namespace ZLockstep.Simulation.ECS.Systems
{
    /// <summary>
    /// 死亡移除系统
    /// 负责在尸体保留时间到期后移除死亡实体
    /// </summary>
    public class DeathRemovalSystem : BaseSystem
    {
        public override void Update()
        {
            int currentTick = World.TimeManager.Tick;
            
            // 查找所有带有死亡组件的实体
            var entities = ComponentManager.GetAllEntityIdsWith<DeathComponent>();
            
            // 处理每个死亡实体
            foreach (int entityId in entities)
            {
                var entity = new Entity(entityId);
                var death = ComponentManager.GetComponent<DeathComponent>(entity);

                // 检查是否应该移除尸体；检查是否存在可视化组件
                if (death.ShouldRemove(currentTick) && !HasViewComponent(entity))
                {
                    // 如果实体有导航组件，则先移除导航能力
                    if (ComponentManager.HasComponent<FlowFieldNavigatorComponent>(entity))
                    {
                        var flowFieldSystem = World.GameInstance.GetNavSystem();
                        flowFieldSystem?.RemoveNavigator(entity);
                    }

                    // 移除建筑的阻挡
                    RemoveBuildingObstruction(entity);
                    
                    // 销毁实体
                    World.EntityManager.DestroyEntity(entity);
                    // zUDebug.Log($"[DeathRemovalSystem] 实体{entityId}尸体保留时间到期，已移除");
                }
            }
        }

        private bool HasViewComponent(Entity entity)
        {
            return ComponentManager.HasComponent<ViewComponent>(entity);
        }

        /// <summary>
        /// 移除建筑的阻挡（将建筑占据的格子标记为可行走）
        /// </summary>
        private void RemoveBuildingObstruction(Entity entity)
        {
            // 检查实体是否是建筑
            if (!ComponentManager.HasComponent<BuildingComponent>(entity))
            {
                return;
            }

            var building = ComponentManager.GetComponent<BuildingComponent>(entity);
            
            // 获取 MapManager
            var mapManager = World.GameInstance?.GetMapManager();
            if (mapManager == null)
            {
                zUDebug.LogWarning($"[DeathRemovalSystem] 无法获取 MapManager，无法移除建筑阻挡");
                return;
            }

            // 计算建筑占据的区域范围
            int minX = building.X - building.Width / 2;
            int maxX = building.X + building.Width / 2;
            int minY = building.Y - building.Height / 2;
            int maxY = building.Y + building.Height / 2;

            // 将建筑占据的区域标记为可行走
            mapManager.SetWalkableRect(minX, minY, maxX, maxY, true);
            
            // 标记流场为脏（需要重新计算）
            var flowFieldManager = World.GameInstance?.GetFlowFieldManager();
            if (flowFieldManager != null)
            {
                flowFieldManager.MarkRegionDirty(minX, minY, maxX, maxY);
                flowFieldManager.NeedUpdateObstacles = true;
            }

            // zUDebug.Log($"[DeathRemovalSystem] 已移除建筑{entity.Id}的阻挡区域：[{minX},{minY}] 到 [{maxX},{maxY}]");
        }
    }
}