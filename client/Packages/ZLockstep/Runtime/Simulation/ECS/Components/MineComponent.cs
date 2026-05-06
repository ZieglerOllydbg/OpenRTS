using System;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 矿源组件
    /// 包含矿源的资源信息
    /// </summary>
    [Serializable]
    public struct MineComponent : IComponent
    {
        /// <summary>
        /// 矿源可用资源量
        /// </summary>
        public int ResourceAmount;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="resourceAmount">初始资源量</param>
        public MineComponent(int resourceAmount)
        {
            ResourceAmount = resourceAmount;
        }

        /// <summary>
        /// 创建默认矿源组件（20000资源）
        /// </summary>
        /// <returns>矿源组件</returns>
        public static MineComponent CreateDefault()
        {
            return new MineComponent(20000);
        }

        /// <summary>
        /// 创建指定资源量的矿源组件
        /// </summary>
        /// <param name="resourceAmount">资源量</param>
        /// <returns>矿源组件</returns>
        public static MineComponent Create(int resourceAmount)
        {
            return new MineComponent(resourceAmount);
        }
    }
}