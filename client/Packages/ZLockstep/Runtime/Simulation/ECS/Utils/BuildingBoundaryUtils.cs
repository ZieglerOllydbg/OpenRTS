using System.Numerics;
using ZLockstep.Simulation.ECS.Components;
using zUnity;

namespace ZLockstep.Simulation.ECS.Utils
{
    /// <summary>
    /// 建筑边界计算工具类
    /// 提供与建筑边界相关的计算方法
    /// </summary>
    public static class BuildingBoundaryUtils
    {
        /// <summary>
        /// 计算追击者/攻击者到建筑边界的交点
        /// </summary>
        /// <param name="chaserPosition">追击者/攻击者位置</param>
        /// <param name="building">建筑组件</param>
        /// <param name="world">游戏世界引用</param>
        /// <returns>建筑边界上的交点</returns>
        public static zVector2 CalculateBuildingBoundaryPoint(zVector3 chaserPosition, BuildingComponent building, zWorld world)
        {
            // 获取地图管理器
            var mapManager = world.GameInstance.GetMapManager();
            if (mapManager == null)
                return new zVector2(chaserPosition.x, chaserPosition.z); // 出错时返回当前位置
            
            // 获取建筑在世界坐标系中的边界框
            zVector2 buildingCenter = new(building.X, building.Y);

            return buildingCenter;
        }
        
        /// <summary>
        /// 检查建筑是否在指定范围内
        /// </summary>
        /// <param name="chaserPosition">追击者/攻击者位置</param>
        /// <param name="building">建筑组件</param>
        /// <param name="range">检测范围</param>
        /// <param name="world">游戏世界引用</param>
        /// <returns>建筑是否在范围内</returns>
        public static bool IsBuildingInRange(zVector3 chaserPosition, BuildingComponent building, zfloat range, zWorld world)
        {
            // 获取地图管理器
            var mapManager = world.GameInstance.GetMapManager();
            if (mapManager == null)
                return false;
            
            // 获取建筑在世界坐标系中的边界框
            zVector2 buildingCenter = new(building.X, building.Y);
            
            // 计算建筑边界
            zfloat halfWidth = new zfloat(building.Width) / 2;
            zfloat halfHeight = new zfloat(building.Height) / 2;
            
            zVector2 minBound = new(buildingCenter.x - halfWidth, buildingCenter.y - halfHeight);
            zVector2 maxBound = new(buildingCenter.x + halfWidth, buildingCenter.y + halfHeight);
            
            // 计算追击者位置到建筑边界框的最短距离
            zfloat distX = zfloat.Zero;
            zfloat distY = zfloat.Zero;
            
            // X轴方向的距离
            if (chaserPosition.x < minBound.x)
                distX = minBound.x - chaserPosition.x;
            else if (chaserPosition.x > maxBound.x)
                distX = chaserPosition.x - maxBound.x;
                
            // Y轴方向的距离 (注意: 在世界坐标系中Z对应Y)
            if (chaserPosition.z < minBound.y)
                distY = minBound.y - chaserPosition.z;
            else if (chaserPosition.z > maxBound.y)
                distY = chaserPosition.z - maxBound.y;
                
            // 计算到边界框的欧几里得距离
            zfloat distanceSqr = distX * distX + distY * distY;
            zfloat rangeSqr = range * range;
            
            return distanceSqr <= rangeSqr;
        }
        
    }
}