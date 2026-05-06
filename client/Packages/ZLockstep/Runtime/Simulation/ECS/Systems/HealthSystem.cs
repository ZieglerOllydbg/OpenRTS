using ZLockstep.Flow;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Simulation.Events;
using zUnity;

namespace ZLockstep.Simulation.ECS.Systems
{
    /// <summary>
    /// 生命值系统
    /// 统一处理所有实体的生命值检测和死亡标记
    /// 
    /// 职责：
    /// 1. 检测生命值<=0的实体
    /// 2. 添加DeathComponent标记死亡
    /// 3. 发布EntityDiedEvent通知表现层播放死亡动画
    /// 
    /// 执行时机：
    /// - 在造成伤害的系统（CombatSystem、ProjectileSystem）之后
    /// - 在DeathRemovalSystem之前
    /// </summary>
    public class HealthSystem : BaseSystem
    {
        /// <summary>
        /// 尸体保留时间（tick数）
        /// 默认2秒，假设帧率为30fps，则为60个tick
        /// </summary>
        private const long CORPSE_DURATION = 60; // 2秒 * 30帧/秒

        public override void Update()
        {
            ProcessHealthCheck();
        }

        /// <summary>
        /// 检查所有实体的生命值，处理死亡逻辑
        /// </summary>
        private void ProcessHealthCheck()
        {
            var healthEntities = ComponentManager.GetAllEntityIdsWith<HealthComponent>();

            foreach (var entityId in healthEntities)
            {
                var entity = new Entity(entityId);

                // 已经有死亡组件的跳过（避免重复处理）
                if (ComponentManager.HasComponent<DeathComponent>(entity))
                    continue;

                var health = ComponentManager.GetComponent<HealthComponent>(entity);

                // 检查是否死亡
                if (health.CurrentHealth <= zfloat.Zero)
                {
                    // 添加死亡组件
                    var deathComponent = new DeathComponent(World.TimeManager.Tick, CORPSE_DURATION);
                    ComponentManager.AddComponent(entity, deathComponent);

                    // 获取阵营ID（用于事件）
                    int campId = -1;
                    if (ComponentManager.HasComponent<CampComponent>(entity))
                    {
                        var camp = ComponentManager.GetComponent<CampComponent>(entity);
                        campId = camp.CampId;
                    }

                    // 移除RVO（如果有），以防止尸体继续被导航系统处理
                    if (ComponentManager.HasComponent<FlowFieldNavigatorComponent>(entity))
                    {
                        var flowFieldSystem = World.GameInstance.GetNavSystem();
                        flowFieldSystem?.RemoveRVOAgent(entity);
                    }

                    // 发布死亡事件（通知表现层播放死亡动画）
                    var diedEvent = new EntityDiedEvent
                    {
                        EntityId = entity.Id,
                        UnitType = ComponentManager.HasComponent<UnitComponent>(entity)
                            ? ComponentManager.GetComponent<UnitComponent>(entity).UnitType
                            : UnitType.None,
                        Position = ComponentManager.HasComponent<TransformComponent>(entity)
                            ? ComponentManager.GetComponent<TransformComponent>(entity).Position
                            : zVector3.zero,
                    };
                    EventManager.Publish(diedEvent);

                    zUDebug.Log($"[HealthSystem] 实体{entity.Id}死亡，阵营{campId}，将在{CORPSE_DURATION}帧后移除");
                }
            }
        }
    }
}

