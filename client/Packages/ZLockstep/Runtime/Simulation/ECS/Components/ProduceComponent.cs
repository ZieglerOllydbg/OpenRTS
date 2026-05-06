using System.Collections.Generic;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 生产组件
    /// 用于具有生产功能的建筑（如工厂）
    /// </summary>
    public struct ProduceComponent : IComponent
    {
        /// <summary>
        /// 支持生产的单位类型列表
        /// </summary>
        public HashSet<UnitType> SupportedUnitTypes;

        /// <summary>
        /// 每种单位类型的生产数量
        /// Key: UnitType, Value: 生产数量 (0-99)
        /// </summary>
        public Dictionary<UnitType, int> ProduceNumbers;

        /// <summary>
        /// 每种单位类型的生产进度
        /// Key: UnitType, Value: 生产进度 (0-100)
        /// </summary>
        public Dictionary<UnitType, int> ProduceProgress;

        /// <summary>
        /// 创建生产组件
        /// </summary>
        /// <param name="supportedUnitTypes">支持生产的单位类型列表</param>
        /// <returns>生产组件实例</returns>
        public static ProduceComponent Create(HashSet<UnitType> supportedUnitTypes)
        {
            var component = new ProduceComponent
            {
                SupportedUnitTypes = supportedUnitTypes ?? new HashSet<UnitType>(),
                ProduceNumbers = new Dictionary<UnitType, int>(),
                ProduceProgress = new Dictionary<UnitType, int>()
            };

            // 初始化所有支持类型的生产数量和进度
            foreach (var unitType in supportedUnitTypes)
            {
                component.ProduceNumbers[unitType] = 0;
                component.ProduceProgress[unitType] = 0;
            }

            return component;
        }
    }
}