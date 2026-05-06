using System;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 采矿组件
    /// 用于跟踪采矿场的采矿进度和关联的矿源
    /// </summary>
    [Serializable]
    public struct MiningComponent : IComponent
    {
        /// <summary>
        /// 关联的矿源实体ID
        /// </summary>
        public int MineEntityId;
        
        /// <summary>
        /// 采集计时器（帧数）
        /// </summary>
        public int MiningTimer;
        
        /// <summary>
        /// 采集周期（帧数）
        /// </summary>
        public int MiningCycleFrames;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mineEntityId">关联的矿源实体ID</param>
        public MiningComponent(int mineEntityId)
        {
            MineEntityId = mineEntityId;
            MiningTimer = 0;
            MiningCycleFrames = 20;
        }

        /// <summary>
        /// 创建采矿组件
        /// </summary>
        /// <param name="mineEntityId">关联的矿源实体ID</param>
        /// <returns>采矿组件</returns>
        public static MiningComponent Create(int mineEntityId)
        {
            return new MiningComponent(mineEntityId);
        }
    }
}