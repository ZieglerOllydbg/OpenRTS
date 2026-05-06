using ZLockstep.Simulation.ECS.Components;
using zUnity;

namespace ZLockstep.Simulation.ECS.Systems
{
    /// <summary>
    /// 建筑建造系统
    /// 处理建筑的建造进度
    /// </summary>
    public class BuildingConstructionSystem : BaseSystem
    {
        public override int GetOrder()
        {
            // 建造系统应该在大多数其他系统之前执行
            return (int)SystemOrder.BuildingConstruction;
        }

        public override void Update()
        {
            ProcessConstruction();
        }
        
        private void ProcessConstruction()
        {
            // 获取所有正在建造的建筑
            var constructionEntities = ComponentManager.GetAllEntityIdsWith<BuildingConstructionComponent>();
            
            foreach (var entityId in constructionEntities)
            {
                var entity = new Entity(entityId);
                var constructionComponent = ComponentManager.GetComponent<BuildingConstructionComponent>(entity);
                
                // 增加建造时间
                constructionComponent.ElapsedConstructionTime += DeltaTime;
                
                // 检查是否建造完成
                if (constructionComponent.IsConstructed)
                {
                    // 移除建造组件
                    ComponentManager.RemoveComponent<BuildingConstructionComponent>(entity);
                    
                    zUDebug.Log($"[BuildingConstructionSystem] 建筑 {entityId} 建造完成");
                }
                else
                {
                    // 更新建造组件
                    ComponentManager.AddComponent(entity, constructionComponent);
                }
            }
        }
    }
}