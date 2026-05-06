using zUnity;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 旋转状态组件
    /// 存储单位的旋转状态和期望朝向
    /// </summary>
    public struct RotationStateComponent : IComponent
    {
        /// <summary>
        /// 期望朝向方向（2D，归一化）
        /// 由移动系统或导航系统设置
        /// </summary>
        public zVector2 DesiredDirection;

        /// <summary>
        /// 当前朝向方向（2D，归一化）
        /// 从Transform.Rotation提取的当前朝向
        /// </summary>
        public zVector2 CurrentDirection;

        /// <summary>
        /// 是否正在原地转向
        /// true时，移动系统会停止位置更新，只进行旋转
        /// </summary>
        public bool IsInPlaceRotating;

        /// <summary>
        /// 是否启用旋转控制
        /// false时，跳过旋转系统的处理
        /// </summary>
        public bool IsEnabled;

        /// <summary>
        /// 上一帧的旋转角度（用于调试）
        /// </summary>
        public zfloat LastRotationAngle;

        /// <summary>
        /// 创建默认旋转状态
        /// </summary>
        public static RotationStateComponent Create()
        {
            return new RotationStateComponent
            {
                DesiredDirection = new zVector2(zfloat.Zero, zfloat.One), // 默认朝向北
                CurrentDirection = new zVector2(zfloat.Zero, zfloat.One),
                IsInPlaceRotating = false,
                IsEnabled = true,
                LastRotationAngle = zfloat.Zero
            };
        }

        /// <summary>
        /// 创建指定朝向的旋转状态
        /// </summary>
        public static RotationStateComponent Create(zVector2 initialDirection)
        {
            zVector2 normalized = initialDirection.magnitude > zfloat.Epsilon 
                ? initialDirection.normalized 
                : new zVector2(zfloat.Zero, zfloat.One);

            return new RotationStateComponent
            {
                DesiredDirection = normalized,
                CurrentDirection = normalized,
                IsInPlaceRotating = false,
                IsEnabled = true,
                LastRotationAngle = zfloat.Zero
            };
        }
    }
}

