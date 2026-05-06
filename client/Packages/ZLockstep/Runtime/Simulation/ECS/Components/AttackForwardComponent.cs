using zUnity;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 攻击朝向表现组件，仅用于表现层驱动 Forward 子节点。
    /// </summary>
    public struct AttackForwardComponent : IComponent
    {
        public bool HasDirection;
        public zVector2 Direction;
        public int LastAttackTick;

        public static AttackForwardComponent Create(zVector2 direction, int lastAttackTick)
        {
            return new AttackForwardComponent
            {
                HasDirection = true,
                Direction = direction,
                LastAttackTick = lastAttackTick
            };
        }
    }
}
