namespace ZLockstep.Simulation.ECS
{
    /// <summary>
    /// 表现系统基类
    /// 所有表现层系统都应该继承此类
    /// </summary>
    public abstract class PresentationBaseSystem : BaseSystem
    {
        /// <summary>
        /// 表现系统具有更高的执行顺序编号（较低优先级）
        /// 确保在普通系统更新后再执行
        /// </summary>
        /// <returns>表现系统执行顺序</returns>
        public override int GetOrder() => (int)SystemOrder.Presentation;
    }
}