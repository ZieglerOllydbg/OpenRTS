using zUnity;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 速度组件
    /// 用于移动系统计算位置变化
    /// </summary>
    public struct VelocityComponent : IComponent
    {
        public zVector3 Value;

        public VelocityComponent(zVector3 value)
        {
            Value = value;
        }

        /// <summary>
        /// 当前速度的平方大小（避免开方计算）
        /// </summary>
        public zfloat SqrMagnitude => Value.sqrMagnitude;

        /// <summary>
        /// 是否在移动
        /// </summary>
        public bool IsMoving => SqrMagnitude > zfloat.Zero;
    }
}

