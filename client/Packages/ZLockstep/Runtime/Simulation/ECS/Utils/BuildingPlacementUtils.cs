using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Flow;
using zUnity;
using ZLockstep.Simulation.ECS;
using zUnityEngine = UnityEngine;

namespace ZLockstep.Simulation.ECS.Utils
{
    /// <summary>
    /// 建筑放置工具类
    /// 提供与建筑放置相关的计算和验证方法
    /// </summary>
    public static class BuildingPlacementUtils
    {
        /// <summary>
        /// 根据建筑类型获取建筑的宽度和高度
        /// </summary>
        /// <param name="buildingType">建筑类型</param>
        /// <param name="width">输出宽度</param>
        /// <param name="height">输出高度</param>
        public static void GetBuildingDimensions(BuildingType buildingType, out int width, out int height)
        {
            switch (buildingType)
            {
                case BuildingType.Base: // 基地
                    width = 5;
                    height = 5;
                    break;
                case BuildingType.Mine: // 矿
                    width = 0;
                    height = 0;
                    break;
                case BuildingType.Smelter: // 冶金厂
                    width = 5;
                    height = 5;
                    break;
                case BuildingType.PowerPlant: // 电厂
                    width = 5;
                    height = 5;
                    break;
                case BuildingType.LightFactory: // 坦克工厂
                    width = 5;
                    height = 5;
                    break;
                default:
                    width = 5;
                    height = 5;
                    break;
            }
        }

        /// <summary>
        /// 检查建筑放置位置是否有效（无阻挡且在地图范围内）
        /// </summary>
        /// <param name="buildingType">建筑类型</param>
        /// <param name="position">建筑位置（世界坐标）</param>
        /// <param name="mapManager">地图管理器</param>
        /// <returns>是否可以放置建筑</returns>
        public static bool CheckBuildingPlacement(int confBuildingID, zVector3 position, IFlowFieldMap mapManager)
        {
            if (mapManager == null)
            {
                zUnityEngine.Debug.LogWarning("[BuildingPlacementUtils] 无法获取地图管理器");
                return false;
            }

            var confBuilding = ConfigManager.Get<ConfBuilding>(confBuildingID);
            if (confBuilding == null)
            {
                zUnityEngine.Debug.LogError($"[BuildingPlacementUtils] 无法获取建筑配置信息。ID:{confBuildingID}");
                return false;
            }

            int width = confBuilding.Size;
            int height = confBuilding.Size;
            
            // 计算建筑占据的矩形区域（以建筑中心为基准）
            int halfWidth = width / 2;
            int halfHeight = height / 2;
            
            // 将世界坐标转换为格子坐标
            int posX = (int)position.x;
            int posY = (int)position.z;

            // 计算建筑占据的格子范围
            int minX = posX - halfWidth;
            int minY = posY - halfHeight;
            int maxX = posX + halfWidth;
            int maxY = posY + halfHeight;
            
            // 检查建筑是否在地图边界内
            if (minX < 0 || minY < 0 || maxX >= mapManager.GetWidth() || maxY >= mapManager.GetHeight())
            {
                zUDebug.Log($"[BuildingPlacementUtils] 建筑位置超出地图边界。位置:({posX},{posY}), 尺寸:{width}x{height}, 地图尺寸:{mapManager.GetWidth()}x{mapManager.GetHeight()}");
                return false;
            }
            
            // 检查建筑占据的所有格子是否都可以行走（无阻挡）
            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    if (!mapManager.IsWalkable(x, y))
                    {
                        zUDebug.Log($"[BuildingPlacementUtils] 建筑位置存在阻挡。阻挡格子:({x},{y})");
                        return false;
                    }
                }
            }
            
            return true;
        }

        /// <summary>
        /// 检查建筑是否在主城限制区域内（主城+-20范围）
        /// </summary>
        /// <param name="world">游戏世界实例</param>
        /// <param name="position">建筑位置</param>
        /// <param name="buildingType">建筑类型</param>
        /// <param name="campId">阵营ID</param>
        /// <returns>是否在限制区域内</returns>
        public static bool CheckBuildableArea(zWorld world, zVector3 position, int campId)
        {
            // 查找指定阵营的基地
            zVector2 mainBasePos = zVector2.zero;
            bool foundMainBase = false;

            // 使用GetComponentWithCondition优化查找指定阵营的主基地
            var (mainBaseComponent, entity) = world.ComponentManager.GetComponentWithCondition<MainBaseComponent>(
                e => {
                    if (!world.ComponentManager.HasComponent<CampComponent>(e)) 
                        return false;
                    var campComponent = world.ComponentManager.GetComponent<CampComponent>(e);
                    return campComponent.CampId == campId;
                });
            
            if (entity.Id != -1)
            {
                var transform = world.ComponentManager.GetComponent<ZLockstep.Simulation.ECS.Components.TransformComponent>(entity);
                mainBasePos = new zVector2(transform.Position.x, transform.Position.z);
                foundMainBase = true;
            }

            // 如果没有找到主基地，不允许建造
            if (!foundMainBase)
                return false;

            // 计算距离
            zfloat dx = zMathf.Abs(position.x - mainBasePos.x);
            zfloat dy = zMathf.Abs(position.z - mainBasePos.y);
            
            // 主城+-20范围
            int range = 20;
            zfloat rangeZ = zfloat.FromRaw(range * zfloat.SCALE_10000);
            
            return dx <= rangeZ && dy <= rangeZ;
        }
    }
}