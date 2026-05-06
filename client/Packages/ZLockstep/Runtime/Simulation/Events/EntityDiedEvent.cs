namespace ZLockstep.Simulation.Events
{
    /// <summary>
    /// 实体死亡事件
    /// 当实体生命值降为0时发布，用于通知表现层播放死亡动画
    /// 注意：这个事件不代表实体被销毁，只是标记死亡状态
    /// </summary>
    public struct EntityDiedEvent : IEvent
    {
        /// <summary>
        /// 死亡实体的ID
        /// </summary>
        public int EntityId;

        /// <summary>
        /// 死亡实体类型，供表现层区分处理逻辑。
        /// </summary>
        public ECS.UnitType UnitType;

        /// <summary>
        /// 实体死亡时记录的世界坐标。
        /// </summary>
        public zUnity.zVector3 Position;

        /// <summary>
        /// 子弹配置ID，仅对Projectile有效
        /// </summary>
        public int ConfProjectileID;
    }
}
