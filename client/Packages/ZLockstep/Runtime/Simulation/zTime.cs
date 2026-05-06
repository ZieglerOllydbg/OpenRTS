namespace ZLockstep.Simulation
{
    /// <summary>
    /// 确定性时间管理器
    /// 为整个仿真提供一个确定性的、独立于真实世界时间的时钟。
    /// </summary>
    public class TimeManager
    {
        /// <summary>
        /// 固定的每帧时间间隔（秒）
        /// </summary>
        public zfloat DeltaTime { get; private set; }

        /// <summary>
        /// 从开始到现在的总时间（秒）
        /// </summary>
        public zfloat Time { get; private set; }

        /// <summary>
        /// 从开始到现在的总帧数
        /// </summary>
        public int Tick { get; private set; }

        /// <summary>
        /// 初始化时间管理器
        /// </summary>
        /// <param name="frameRate">逻辑帧率 (e.g., 20)</param>
        public void Init(int frameRate)
        {
            // 基于帧率计算出每帧的时间步长
            // 例如 frameRate = 20, DeltaTime = 1 / 20
            if (frameRate > 0)
            {
                DeltaTime = zfloat.One / new zfloat(frameRate);
            }
            else
            {
                // 提供一个默认值以避免除以零
                DeltaTime = new zfloat(0, 500); // Default to ~20 FPS
            }

            Time = zfloat.Zero;
            Tick = 0;
        }

        /// <summary>
        /// 驱动时间向前推进一帧
        /// </summary>
        public void Advance()
        {
            Time += DeltaTime;
            Tick++;
        }

        /// <summary>
        /// 设置当前帧号（用于锁帧同步）
        /// 在锁帧模式下，应该由 FrameSyncManager 控制 Tick
        /// </summary>
        public void SetTick(int tick)
        {
            Tick = tick;
            Time = DeltaTime * new zfloat(tick);
        }
    }
}
