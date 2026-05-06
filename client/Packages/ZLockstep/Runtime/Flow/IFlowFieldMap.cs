using zUnity;

namespace ZLockstep.Flow
{
    /// <summary>
    /// 流场地图接口
    /// 提供地图数据给流场系统使用
    /// </summary>
    public interface IFlowFieldMap
    {
        /// <summary>
        /// 获取地图宽度
        /// </summary>
        int GetWidth();

        /// <summary>
        /// 获取地图高度
        /// </summary>
        int GetHeight();

        /// <summary>
        /// 获取流场宽度
        /// </summary>
        int GetFlowGridWidth();

        /// <summary>
        /// 获取流场高度
        /// </summary>
        int GetFlowGridHeight();

        /// <summary>
        /// 获取每个流场格子的尺寸
        /// </summary>
        int GetFlowSize();

        /// <summary>
        /// 判断指定格子是否可行走
        /// </summary>
        bool IsWalkable(int gridX, int gridY);

        /// <summary>
        /// 判断指定流场格子是否可行走
        /// </summary>
        public bool IsFlowWalkable(int flowX, int flowY);

        /// <summary>
        /// 获取地形代价（可选）
        /// 返回值：1.0 = 正常地形，2.0 = 慢速地形（如泥地），0 = 不可通行
        /// </summary>
        zfloat GetTerrainCost(int gridX, int gridY);

        /// <summary>
        /// 世界坐标转换为格子坐标
        /// </summary>
        void WorldToFlow(zVector2 worldPos, out int gridX, out int gridY);

        /// <summary>
        /// 格子坐标转换为世界坐标（格子中心）
        /// </summary>
        zVector2 FlowToWorld(int gridX, int gridY);
        
        /// <summary>
        /// 设置指定格子的可行走性
        /// </summary>
        void SetWalkable(int x, int y, bool walkable);
        
        /// <summary>
        /// 设置矩形区域的可行走性
        /// </summary>
        void SetWalkableRect(int minX, int minY, int maxX, int maxY, bool walkable);
        
        /// <summary>
        /// 设置圆形区域的可行走性
        /// </summary>
        void SetWalkableCircle(int centerX, int centerY, int radius, bool walkable);
    }
}