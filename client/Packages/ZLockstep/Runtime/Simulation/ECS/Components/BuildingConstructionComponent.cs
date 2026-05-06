using zUnity;

namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 建筑建造组件
    /// 用于跟踪建筑的建造进度
    /// </summary>
    public struct BuildingConstructionComponent : IComponent
    {
        /// <summary>
        /// 总建造时间（秒）
        /// </summary>
        public zfloat TotalConstructionTime;
        
        /// <summary>
        /// 已建造时间（秒）
        /// </summary>
        public zfloat ElapsedConstructionTime;
        
        /// <summary>
        /// 建造是否完成
        /// </summary>
        public bool IsConstructed => ElapsedConstructionTime >= TotalConstructionTime;
        
        public static BuildingConstructionComponent Create(zfloat totalTime)
        {
            return new BuildingConstructionComponent
            {
                TotalConstructionTime = totalTime,
                ElapsedConstructionTime = zfloat.Zero
            };
        }
    }
}