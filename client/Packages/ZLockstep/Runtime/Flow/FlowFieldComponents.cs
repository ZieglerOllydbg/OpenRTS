using zUnity;
using ZLockstep.Simulation.ECS;

namespace ZLockstep.Flow
{
    /// <summary>
    /// 流场导航组件
    /// 挂载在需要使用流场寻路的实体上
    /// </summary>
    public struct FlowFieldNavigatorComponent : IComponent
    {
        /// <summary>
        /// 当前使用的流场ID（-1表示没有目标）
        /// </summary>
        public int CurrentFlowFieldId;

        /// <summary>
        /// RVO智能体ID（用于避障）
        /// </summary>
        public int RvoAgentId;

        /// <summary>
        /// 最大移动速度
        /// </summary>
        public zfloat MaxSpeed;

        /// <summary>
        /// 到达目标的阈值距离
        /// </summary>
        public zfloat ArrivalRadius;

        /// <summary>
        /// 减速开始的距离
        /// </summary>
        public zfloat SlowDownRadius;

        /// <summary>
        /// 是否已到达目标
        /// </summary>
        public bool HasReachedTarget;

        /// <summary>
        /// 是否启用流场导航
        /// </summary>
        public bool IsEnabled;

        /// <summary>
        /// 单位半径（用于RVO）
        /// </summary>
        public zfloat Radius;

        /// <summary>
        /// 卡住检测：上次位置
        /// </summary>
        public zVector2 LastPosition;

        /// <summary>
        /// 卡住检测：停滞时间
        /// </summary>
        public int StuckFrames;

        /// <summary>
        /// 近端稳定判定：低速连续帧计数
        /// </summary>
        public int NearSlowFrames;

        /// <summary>
        /// 是否正在减速
        /// </summary>
        public bool IsSlowingDown;

        /// <summary>
        /// 开始减速时的Tick
        /// </summary>
        public int SlowDownStartTick;

        /// <summary>
        /// 减速前的速度
        /// </summary>
        public zfloat SpeedBeforeSlowDown;

        public static FlowFieldNavigatorComponent Create(zfloat radius, zfloat maxSpeed)
        {
            return new FlowFieldNavigatorComponent
            {
                CurrentFlowFieldId = -1,
                RvoAgentId = -1,
                MaxSpeed = maxSpeed,
                ArrivalRadius = new zfloat(0, 5000), // 0.5
                SlowDownRadius = new zfloat(4), // 3-5米开始减速
                HasReachedTarget = false,
                IsEnabled = true,
                Radius = radius,
                LastPosition = zVector2.zero,
                StuckFrames = 0,
                NearSlowFrames = 0,
                IsSlowingDown = false,
                SlowDownStartTick = 0,
                SpeedBeforeSlowDown = zfloat.Zero
            };
        }
    }

    /// <summary>
    /// 移动目标组件
    /// 存储单位的目标位置
    /// </summary>
    public struct MoveTargetComponent : IComponent
    {
        /// <summary>
        /// 目标位置
        /// </summary>
        public zVector2 TargetPosition;

        /// <summary>
        /// 是否有目标
        /// </summary>
        public bool HasTarget;

        /// <summary>
        /// 是否是用户输入的目标
        /// </summary>
        public bool UserInput;

        /// <summary>
        /// 上次预设速度
        /// </summary>
        public zVector2 LastPrefVelocity {get; set;}

        public static MoveTargetComponent Create(zVector2 targetPos)
        {
            return new MoveTargetComponent
            {
                TargetPosition = targetPos,
                HasTarget = true,
                UserInput = false,
                LastPrefVelocity = zVector2.zero,
            };
        }
    }
}
