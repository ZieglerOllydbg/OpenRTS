using zUnity;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 死亡组件
    /// 用于标记实体已经死亡，并记录死亡时间和尸体保留时间
    /// </summary>
    public struct DeathComponent : IComponent
    {
        /// <summary>
        /// 死亡时的游戏tick
        /// </summary>
        public long DeathTick;
        
        /// <summary>
        /// 尸体保留时间(以tick为单位)
        /// </summary>
        public long CorpseDuration;

        public DeathComponent(long deathTick, long corpseDuration)
        {
            DeathTick = deathTick;
            CorpseDuration = corpseDuration;
        }

        /// <summary>
        /// 检查尸体是否应该被移除
        /// </summary>
        public bool ShouldRemove(long currentTick) => currentTick >= DeathTick + CorpseDuration;
    }
}