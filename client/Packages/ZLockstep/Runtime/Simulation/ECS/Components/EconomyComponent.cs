using System;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 经济组件，包含金币和能量资源
    /// </summary>
    [Serializable]
    public struct EconomyComponent : IComponent
    {
        /// <summary>
        /// 金币
        /// </summary>
        public int Money;

        /// <summary>
        /// 能量
        /// </summary>
        public int Power;

        /// <summary>
        /// GM调试增加的能量
        /// </summary>
        public int GMPower;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="money">初始金币</param>
        /// <param name="power">初始能量</param>
        public EconomyComponent(int money, int power)
        {
            Money = money;
            Power = power;
            GMPower = 0;
        }

        /// <summary>
        /// 创建指定金币和能量的经济组件
        /// </summary>
        /// <param name="money">金币</param>
        /// <param name="power">能量</param>
        /// <returns>经济组件</returns>
        public static EconomyComponent Create(int money, int power)
        {
            return new EconomyComponent(money, power);
        }
    }
}