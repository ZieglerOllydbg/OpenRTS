namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 攻击组件
    /// 用于具有攻击能力的单位
    /// </summary>
    public struct AttackComponent : IComponent
    {
        /// <summary>
        /// 最大攻击目标数量
        /// </summary>
        public int MaxTargets;

        /// <summary>
        /// 预警范围（进入此范围锁定目标）
        /// </summary>
        public zfloat WarningRange { get; set; }

        /// <summary>
        /// 攻击范围
        /// </summary>
        public zfloat Range { get; set; }

        /// <summary>
        /// 子弹 ID
        /// </summary>
        public int ConfProjectileID;

        /// <summary>
        /// 攻击间隔（秒）
        /// </summary>
        public zfloat AttackInterval;

        /// <summary>
        /// 距离上次攻击的时间
        /// </summary>
        public zfloat TimeSinceLastAttack;

        /// <summary>
        /// 当前目标实体 ID（-1 表示无目标）
        /// </summary>
        public int TargetEntityId;

        /// <summary>
        /// 能否攻击
        /// </summary>
        public bool CanAttack => TimeSinceLastAttack >= AttackInterval;

        /// <summary>
        /// 是否有目标
        /// </summary>
        public bool HasTarget => TargetEntityId >= 0;

        /// <summary>
        /// 攻击音效
        /// </summary>
        public bool PlayAttackAudio;

        public static AttackComponent CreateDefault()
        {
            return new AttackComponent
            {
                Range = (zfloat)5.0f,
                AttackInterval = (zfloat)1.0f,
                TimeSinceLastAttack = zfloat.Zero,
                TargetEntityId = -1,
                MaxTargets = 1
            };
        }
    }
}