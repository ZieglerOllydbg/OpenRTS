using zUnity;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 炮塔组件
    /// 用于有炮塔的单位（坦克、防御塔等）
    /// 炮塔可以独立于车体旋转
    /// </summary>
    public struct TurretComponent : IComponent
    {
        /// <summary>
        /// 炮塔当前旋转角度（相对车体，度数）
        /// 0度表示与车体朝向一致
        /// 正值表示顺时针旋转
        /// </summary>
        public zfloat CurrentTurretAngle;

        /// <summary>
        /// 炮塔期望旋转角度（相对车体，度数）
        /// 由战斗系统设置，指向攻击目标
        /// </summary>
        public zfloat DesiredTurretAngle;

        /// <summary>
        /// 炮塔期望朝向方向（世界坐标，2D归一化）
        /// 由战斗系统设置，用于计算期望角度
        /// </summary>
        public zVector2 DesiredTurretDirection;

        /// <summary>
        /// 炮塔最小旋转角度（相对车体，度数）
        /// 用于限制炮塔旋转范围，如 -180
        /// </summary>
        public zfloat MinTurretAngle;

        /// <summary>
        /// 炮塔最大旋转角度（相对车体，度数）
        /// 用于限制炮塔旋转范围，如 +180
        /// </summary>
        public zfloat MaxTurretAngle;

        /// <summary>
        /// 是否限制炮塔旋转范围
        /// true时，炮塔只能在 [MinTurretAngle, MaxTurretAngle] 范围内旋转
        /// false时，炮塔可以360度旋转
        /// </summary>
        public bool HasRotationLimit;

        /// <summary>
        /// 是否有攻击目标
        /// true时，炮塔会朝向目标旋转
        /// false时，炮塔恢复到与车体一致的方向
        /// </summary>
        public bool HasTarget;

        /// <summary>
        /// 创建默认炮塔组件（360度旋转）
        /// </summary>
        public static TurretComponent CreateDefault()
        {
            return new TurretComponent
            {
                CurrentTurretAngle = zfloat.Zero,
                DesiredTurretAngle = zfloat.Zero,
                DesiredTurretDirection = new zVector2(zfloat.Zero, zfloat.One),
                MinTurretAngle = new zfloat(-180),
                MaxTurretAngle = new zfloat(180),
                HasRotationLimit = false, // 默认不限制
                HasTarget = false
            };
        }

        /// <summary>
        /// 创建有旋转限制的炮塔组件
        /// </summary>
        public static TurretComponent CreateWithLimit(zfloat minAngle, zfloat maxAngle)
        {
            return new TurretComponent
            {
                CurrentTurretAngle = zfloat.Zero,
                DesiredTurretAngle = zfloat.Zero,
                DesiredTurretDirection = new zVector2(zfloat.Zero, zfloat.One),
                MinTurretAngle = minAngle,
                MaxTurretAngle = maxAngle,
                HasRotationLimit = true,
                HasTarget = false
            };
        }
    }
}

