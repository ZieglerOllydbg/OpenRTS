namespace ZLockstep.Simulation.ECS.Components
{
    /// <summary>
    /// 阵营组件
    /// 用于区分不同阵营的单位和建筑
    /// </summary>
    public struct CampComponent : IComponent
    {
        /// <summary>
        /// 阵营ID
        /// 0 = 玩家阵营（蓝色）
        /// 1 = AI阵营（红色）
        /// 可扩展更多阵营
        /// </summary>
        public int CampId;

        /// <summary>
        /// 判断是否为敌对阵营
        /// </summary>
        public bool IsEnemy(CampComponent other)
        {
            return CampId != other.CampId;
        }

        /// <summary>
        /// 创建阵营组件
        /// </summary>
        public static CampComponent Create(int campId)
        {
            return new CampComponent
            {
                CampId = campId
            };
        }
    }
}

