using System;
using System.Collections.Generic;

namespace ZLockstep.Simulation.ECS
{
    /// <summary>
    /// 系统管理器
    /// 负责管理系统生命周期和更新顺序
    /// </summary>
    public class SystemManager
    {
        private readonly List<ISystem> _systems = new List<ISystem>();
        private readonly zWorld _world;

        public SystemManager(zWorld world)
        {
            _world = world;
        }

        /// <summary>
        /// 注册一个新的系统。
        /// 注册的顺序将决定其更新的顺序。
        /// </summary>
        /// <param name="system">要注册的系统</param>
        public void RegisterSystem(ISystem system)
        {
            system.SetWorld(_world);
            _systems.Add(system);
            // 根据排序编号重新排序
            _systems.Sort((a, b) => a.GetOrder().CompareTo(b.GetOrder()));
        }

        /// <summary>
        /// 按照注册顺序和系统类型，更新所有系统。
        /// 系统会根据GetOrder返回值按升序执行
        /// </summary>
        public void UpdateAll()
        {
            // 由于已排序，直接按顺序更新所有系统
            foreach (var system in _systems)
            {
                system.Update();
            }
        }
    }
}