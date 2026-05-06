using System;

namespace ZLockstep.Simulation.ECS
{
    /// <summary>
    /// 系统接口
    /// 所有ECS系统都需要实现此接口
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// 设置该系统所属的世界。
        /// 这个方法会在系统被注册到 SystemManager 时调用。
        /// </summary>
        /// <param name="world">游戏世界</param>
        void SetWorld(zWorld world);

        /// <summary>
        /// 系统的主更新逻辑。
        /// </summary>
        void Update();
        
        /// <summary>
        /// 获取系统执行顺序
        /// 数字越小优先级越高，越早执行
        /// </summary>
        /// <returns>执行顺序编号</returns>
        int GetOrder();
    }
}