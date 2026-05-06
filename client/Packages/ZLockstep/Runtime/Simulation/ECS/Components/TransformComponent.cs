using zUnity;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 逻辑层的Transform组件（纯数据，使用确定性数学）
    /// 存储实体的位置、旋转、缩放信息
    /// </summary>
    public struct TransformComponent : IComponent
    {
        // 当前帧
        public zVector3 Position;
        public zQuaternion Rotation;
        public zVector3 Scale;

        // 未来帧
        public int FutureTick { get; set; }
        public zVector3 FuturePosition { get; set; }
        public zQuaternion FutureRotation { get; set; }

        // 上一帧
        public zVector3 LastPosition { get; set; }
        public zQuaternion LastRotation { get; set; }

        public static TransformComponent Default => new()
        {
            Position = zVector3.zero,
            Rotation = zQuaternion.xPositive,
            Scale = zVector3.one,

            FutureTick = 0,
            FuturePosition = zVector3.zero,
            FutureRotation = zQuaternion.xPositive,
            LastPosition = zVector3.zero,
            LastRotation = zQuaternion.xPositive,
        };

        public static TransformComponent Create(zVector3 position, zQuaternion rotation, zVector3 scale)
        {
            return new TransformComponent
            {
                Position = position,
                Rotation = rotation,
                Scale = scale,

                FutureTick = 0,
                FuturePosition = position,
                FutureRotation = rotation,
                LastPosition = position,
                LastRotation = rotation,
            };
        }
    }
}

