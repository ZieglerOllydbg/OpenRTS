using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Simulation.ECS.Utils;
using zUnity;

namespace ZLockstep.Simulation.ECS.Systems
{
    /// <summary>
    /// 自动回血系统
    /// 负责处理实体的自动生命恢复逻辑
    /// 
    /// 机制：
    /// 1. 检测实体最后掉血时间
    /// 2. 当超过 5 秒未受到攻击时，开始自动回血
    /// 3. 每秒恢复最大生命值的 5%
    /// 
    /// 执行时机：
    /// - 在 HealthSystem 之后执行，确保死亡判定优先
    /// - 在每帧更新中检查并应用回血
    /// </summary>
    public class AutoHealSystem : BaseSystem
    {
        /// <summary>
        /// 获取系统执行顺序
        /// 自动回血系统在普通系统之后执行，确保在死亡检测之后
        /// </summary>
        /// <returns>执行顺序编号</returns>
        public override int GetOrder() => (int)SystemOrder.Normal + 1;

        public override void Update()
        {
            ProcessAutoHeal();
        }

        /// <summary>
        /// 处理所有实体的自动回血逻辑
        /// </summary>
        private void ProcessAutoHeal()
        {
            var healthEntities = ComponentManager.GetAllEntityIdsWith<HealthComponent>();

            foreach (var entityId in healthEntities)
            {
                var entity = new Entity(entityId);

                // 获取生命值组件
                var health = ComponentManager.GetComponent<HealthComponent>(entity);

                // 如果已经满血，跳过
                if (health.CurrentHealth >= health.MaxHealth)
                    continue;

                // 如果从未受过伤害 (LastDamageTick == -1)，跳过
                if (health.LastDamageTick < 0)
                    continue;

                // 计算距离最后掉血经过的时间
                int ticksSinceDamage = Tick - health.LastDamageTick;

                // 从 ConfCamp 配置中获取冷却时间
                int healDelayTicks = GetHealDelayTicks(entity);

                // 如果冷却时间为 0，表示禁用自动回血，跳过
                if (healDelayTicks == 0)
                    continue;

                // 检查是否超过冷却时间
                if (ticksSinceDamage < healDelayTicks)
                    continue;

                // 获取配置中的回血比例，建筑是 ConfBuilding，单位是 ConfUnit
                zfloat healRate = GetAutoHealRate(entity);
                
                // 如果配置的回血比例为 0 或无效，跳过
                if (healRate <= zfloat.Zero)
                    continue;

                // 计算本帧应恢复的生命值
                // 配置的 AutoHeal 是万分比，需要转换为小数（除以 10000）
                // 每秒恢复 healRate，即每帧恢复 healRate * DeltaTime
                zfloat healAmount = health.MaxHealth * healRate * DeltaTime;

                // 应用治疗，确保不超过最大生命值
                health.CurrentHealth += healAmount;
                if (health.CurrentHealth > health.MaxHealth)
                {
                    health.CurrentHealth = health.MaxHealth;
                }

                // 写回组件
                ComponentManager.AddComponent(entity, health);

                // zUDebug.Log($"[AutoHealSystem] 实体{entityId} 自动回血 {healAmount:F2}, 当前生命 {health.CurrentHealth:F2}/{health.MaxHealth:F2}");
            }
        }

        /// <summary>
        /// 从 ConfCamp 配置中获取自动回血的冷却时间
        /// </summary>
        /// <param name="entity">目标实体</param>
        /// <returns>冷却时间 (tick 数)，0 表示禁用自动回血</returns>
        private int GetHealDelayTicks(Entity entity)
        {
            // 获取实体的阵营组件
            if (ComponentManager.HasComponent<CampComponent>(entity))
            {
                var camp = ComponentManager.GetComponent<CampComponent>(entity);
                var confCamp = ConfigManager.Get<ConfCamp>(camp.CampId);
                
                if (confCamp != null)
                {
                    return confCamp.HealDelayTick;
                }
            }

            // 默认返回 0，表示禁用自动回血
            return 0;
        }

        /// <summary>
        /// 从配置中获取实体的自动回血比例（万分比转换为小数）
        /// </summary>
        /// <param name="entity">目标实体</param>
        /// <returns>回血比例（如 0.0005 表示每秒回血万分之五）</returns>
        private zfloat GetAutoHealRate(Entity entity)
        {
            // 检查是否是建筑
            if (ComponentManager.HasComponent<BuildingComponent>(entity))
            {
                var building = ComponentManager.GetComponent<BuildingComponent>(entity);
                var confBuilding = ConfigManager.Get<ConfBuilding>(building.BuildingType);
                
                if (confBuilding != null && confBuilding.AutoHeal > 0)
                {
                    // AutoHeal 是万分比，转换为小数
                    return zfloat.FromRaw(confBuilding.AutoHeal);
                }
            }
            // 检查是否是单位
            else if (ComponentManager.HasComponent<UnitComponent>(entity))
            {
                var unit = ComponentManager.GetComponent<UnitComponent>(entity);
                var confUnit = ConfigManager.Get<ConfUnit>((int)unit.UnitType);
                
                if (confUnit != null && confUnit.AutoHeal > 0)
                {
                    // AutoHeal 是万分比，转换为小数
                    return zfloat.FromRaw(confUnit.AutoHeal);
                }
            }

            // 默认返回 0，表示无自动回血
            return zfloat.Zero;
        }
    }
}