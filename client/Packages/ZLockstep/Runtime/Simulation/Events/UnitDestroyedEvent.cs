using zUnity;

namespace ZLockstep.Simulation.Events
{
    /// <summary>
    /// 单位销毁事件
    /// 逻辑层单位销毁后，通知表现层销毁视图
    /// </summary>
    public struct UnitDestroyedEvent : IEvent
    {
        /// <summary>
        /// 实体ID
        /// </summary>
        public int EntityId;
    }
}