using zUnity;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 移动命令组件
    /// 当单位接收到移动命令时添加此组件
    /// </summary>
    public struct MoveCommandComponent : IComponent
    {
        /// <summary>
        /// 目标位置
        /// </summary>
        public zVector3 TargetPosition;

        /// <summary>
        /// 是否到达目标
        /// </summary>
        public bool HasReached;

        /// <summary>
        /// 停止距离（到达此距离即视为到达）
        /// </summary>
        public zfloat StopDistance;

        public MoveCommandComponent(zVector3 target, zfloat stopDistance = default)
        {
            TargetPosition = target;
            HasReached = false;
            StopDistance = stopDistance > zfloat.Zero ? stopDistance : (zfloat)0.1f;
        }
    }
}

