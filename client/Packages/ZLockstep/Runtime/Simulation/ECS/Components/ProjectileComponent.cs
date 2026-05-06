using zUnity;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 弹道组件
    /// 用于子弹、炮弹等飞行物
    /// </summary>
    public struct ProjectileComponent : IComponent
    {
        /// <summary>
        /// 发射者Entity ID
        /// </summary>
        public int SourceEntityId;

        /// <summary>
        /// 目标Entity ID（-1表示无目标）
        /// </summary>
        public int TargetEntityId;

        /// <summary>
        /// 伤害值
        /// </summary>
        public zfloat Damage;

        private zfloat _speed;

        /// <summary>
        /// 飞行速度（米/秒）
        /// Setter可设置任意值，Getter最大返回15
        /// </summary>
        public zfloat Speed
        {
            get => _speed > (zfloat)15.0f ? (zfloat)15.0f : _speed;
            set => _speed = value;
        }

        /// <summary>
        /// 目标位置（用于追踪）
        /// </summary>
        public zVector3 TargetPosition;

        /// <summary>
        /// 发射位置，用于计算抛物线轨迹
        /// </summary>
        public zVector3 StartPosition;

        /// <summary>
        /// 抛物线弧顶高度
        /// </summary>
        public zfloat ArcHeight;

        /// <summary>
        /// 是否启用抛物线飞行
        /// </summary>
        public bool EnableArc;

        /// <summary>
        /// 是否追踪目标（true=追踪型导弹，false=直线飞行）
        /// </summary>
        public bool IsHoming;

        /// <summary>
        /// 子弹配置ID
        /// </summary>
        public int ConfProjectileId;

        /// <summary>
        /// 发射者的阵营ID（用于判断敌我）
        /// </summary>
        public int SourceCampId;

        /// <summary>
        /// 命中距离阈值（米）
        /// </summary>
        public zfloat HitDistance;

        /// <summary>
        /// 伤害半径（米）
        /// 大于 0 表示是 AOE 弹道，命中时对范围内所有敌人造成伤害
        /// 等于 0 表示是单体弹道，只对目标造成伤害
        /// </summary>
        public zfloat DamageRadius;

        /// <summary>
        /// 最大伤害目标数量
        /// AOE 弹道命中时最多能伤害的目标数量
        /// -1 表示无限制
        /// </summary>
        public int MaxDamageTargets;

        /// <summary>
        /// 伤害分配模式
        /// true = 均摊模式：总伤害平均分配给所有目标
        /// false = 全额模式：每个目标受到全额伤害
        /// </summary>
        public bool ShareDamage;

        /// <summary>
        /// 创建弹道组件
        /// </summary>
        public static ProjectileComponent Create(
            int sourceEntityId,
            int targetEntityId,
            zfloat damage,
            zfloat speed,
            zVector3 targetPosition,
            zVector3 startPosition,
            bool isHoming,
            int confProjectileId,
            int sourceCampId,
            bool enableArc = false,
            zfloat arcHeight = default,
            zfloat hitDistance = default,
            zfloat damageRadius = default,
            int maxDamageTargets = 1,
            bool shareDamage = false)
        {
            if (hitDistance <= zfloat.Zero)
                hitDistance = zfloat.Half;

            return new ProjectileComponent
            {
                SourceEntityId = sourceEntityId,
                TargetEntityId = targetEntityId,
                Damage = damage,
                Speed = speed,
                TargetPosition = targetPosition,
                StartPosition = startPosition,
                ArcHeight = arcHeight,
                EnableArc = enableArc,
                IsHoming = isHoming,
                ConfProjectileId = confProjectileId,
                SourceCampId = sourceCampId,
                HitDistance = hitDistance,
                DamageRadius = damageRadius,
                MaxDamageTargets = maxDamageTargets,
                ShareDamage = shareDamage
            };
        }
    }
}
