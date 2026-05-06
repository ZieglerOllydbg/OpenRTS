using System.Collections.Generic;
using zUnity;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Simulation;

namespace ZLockstep.Flow
{
    /// <summary>
    /// 停止格子管理器
    /// 负责基于阵营的格子分配和管理
    ///
    /// 核心规则：
    /// - 格子大小：1米
    /// - 格子坐标：gridX = round(x), gridY = round(y)
    /// - 停止位置：格子中心点 (gridX + 0.5, gridY + 0.5)
    /// - 阵营分配：
    ///   - CampId % 2 == 0 → 停在 (偶x, 偶y)
    ///   - CampId % 2 == 1 → 停在 (奇x, 奇y)
    /// - 分配基准：以目标点为基准找最近可用格子
    /// - 分配顺序：按选中顺序（传入的entityIds顺序）
    /// - 对方格子：不可占用
    /// </summary>
    public class StopGridManager
    {
        #region 常量

        /// <summary>最大螺旋搜索半径</summary>
        private const int MAX_SEARCH_RADIUS = 50;

        /// <summary>格子中心偏移（0.5米）</summary>
        private static readonly zfloat GRID_CENTER_OFFSET = new zfloat(0, 5000);

        #endregion

        #region 字段

        /// <summary>地图接口</summary>
        private IFlowFieldMap map;

        /// <summary>格子占用数据：key = 格子坐标编码，value = 占用的实体ID</summary>
        private Dictionary<long, int> gridOccupancy;

        /// <summary>实体到格子的映射：key = 实体ID，value = 格子坐标编码</summary>
        private Dictionary<int, long> entityToGrid;

        #endregion

        #region 初始化

        /// <summary>
        /// 构造函数
        /// </summary>
        public StopGridManager()
        {
            gridOccupancy = new Dictionary<long, int>();
            entityToGrid = new Dictionary<int, long>();
        }

        /// <summary>
        /// 设置地图接口
        /// </summary>
        public void Initialize(IFlowFieldMap mapInterface)
        {
            map = mapInterface;
        }

        #endregion

        #region 公开方法 - 占用管理

        /// <summary>
        /// 清空所有格子占用
        /// 每帧开始时调用
        /// </summary>
        public void ClearAllOccupancy()
        {
            gridOccupancy.Clear();
            entityToGrid.Clear();
        }

        /// <summary>
        /// 每帧更新所有单位的格子占用
        /// 静止单位使用当前位置，移动单位使用目标位置
        /// </summary>
        public void UpdateOccupancy(zWorld world)
        {
            // 清空所有占用
            ClearAllOccupancy();

            if (map == null)
                return;

            var cm = world.ComponentManager;

            // 遍历所有单位
            var unitEntities = cm.GetAllEntityIdsWith<UnitComponent>();

            foreach (var entityId in unitEntities)
            {
                var entity = new Entity(entityId);

                // 必须有 Transform 和 Camp 组件
                if (!cm.HasComponent<TransformComponent>(entity) ||
                    !cm.HasComponent<CampComponent>(entity))
                    continue;

                var transform = cm.GetComponent<TransformComponent>(entity);
                var camp = cm.GetComponent<CampComponent>(entity);

                zVector2 position;

                // 判断单位是否正在移动
                if (cm.HasComponent<MoveTargetComponent>(entity))
                {
                    var navigator = cm.GetComponent<FlowFieldNavigatorComponent>(entity);

                    if (navigator.HasReachedTarget)
                    {
                        // 已到达目标，使用当前位置（静止）
                        position = new zVector2(transform.Position.x, transform.Position.z);
                    }
                    else
                    {
                        // 正在移动，使用目标位置
                        var target = cm.GetComponent<MoveTargetComponent>(entity);
                        position = target.TargetPosition;
                    }
                }
                else if (cm.HasComponent<MoveCommandComponent>(entity))
                {
                    // 正在移动（旧移动系统），使用目标位置
                    var moveCmd = cm.GetComponent<MoveCommandComponent>(entity);
                    position = new zVector2(moveCmd.TargetPosition.x, moveCmd.TargetPosition.z);
                }
                else
                {
                    // 静止单位，使用当前位置
                    position = new zVector2(transform.Position.x, transform.Position.z);
                }

                // 计算格子坐标（四舍五入）
                int gridX = (int)zMathf.Round(position.x);
                int gridY = (int)zMathf.Round(position.y);

                // 检查格子是否属于该阵营
                if (!IsGridValidForCamp(gridX, gridY, camp.CampId))
                {
                    // 单位不在自己阵营的格子上，寻找最近的可用格子
                    var availableGrid = FindNearestAvailableGrid(gridX, gridY, camp.CampId);
                    if (availableGrid != null)
                    {
                        gridX = availableGrid.Value.x;
                        gridY = availableGrid.Value.y;
                    }
                    else
                    {
                        // 没有可用格子，跳过
                        continue;
                    }
                }

                // 检查格子是否已被占用
                if (IsGridOccupied(gridX, gridY))
                {
                    // 格子已被占用，跳过
                    continue;
                }

                // 检查格子是否可行走
                if (!map.IsWalkable(gridX, gridY))
                {
                    // 格子不可行走，跳过
                    continue;
                }

                // 更新占用数据
                long gridKey = EncodeGridKey(gridX, gridY);
                gridOccupancy[gridKey] = entityId;
                entityToGrid[entityId] = gridKey;
            }
        }

        #endregion

        #region 公开方法 - 格子分配

        /// <summary>
        /// 为指定单位分配格子
        /// 以目标点为基准，螺旋搜索最近的可用格子
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="campId">阵营ID</param>
        /// <param name="targetPos">目标位置</param>
        /// <returns>分配的格子中心点位置</returns>
        public zVector2 AssignGrid(int entityId, int campId, zVector2 targetPos)
        {
            if (map == null)
            {
                zUDebug.LogWarning("[StopGridManager] 地图接口未初始化");
                return targetPos;
            }

            // 计算目标格子坐标（四舍五入）
            int targetGridX = (int)zMathf.Round(targetPos.x);
            int targetGridY = (int)zMathf.Round(targetPos.y);

            // 如果目标格子已经属于该阵营且可用，直接使用
            if (IsGridValidForCamp(targetGridX, targetGridY, campId) &&
                !IsGridOccupied(targetGridX, targetGridY) &&
                map.IsWalkable(targetGridX, targetGridY))
            {
                // 更新占用数据
                long gridKey2 = EncodeGridKey(targetGridX, targetGridY);
                gridOccupancy[gridKey2] = entityId;
                entityToGrid[entityId] = gridKey2;

                zUDebug.Log($"[StopGridManager] 实体{entityId}分配到格子({targetGridX},{targetGridY})");

                // 返回格子中心点
                return GetGridCenter(targetGridX, targetGridY);
            }

            // 螺旋搜索最近的可用格子
            var availableGrid = FindNearestAvailableGrid(targetGridX, targetGridY, campId);

            if (availableGrid == null)
            {
                // 没有可用格子，返回原目标位置
                zUDebug.LogWarning($"[StopGridManager] 无法为实体{entityId}找到可用格子");
                return targetPos;
            }

            // 更新占用数据
            int gridX = availableGrid.Value.x;
            int gridY = availableGrid.Value.y;
            long gridKey = EncodeGridKey(gridX, gridY);

            gridOccupancy[gridKey] = entityId;
            entityToGrid[entityId] = gridKey;

            zUDebug.Log($"[StopGridManager] 实体{entityId}分配到格子({gridX},{gridY})，目标({targetGridX},{targetGridY})");

            // 返回格子中心点
            return GetGridCenter(gridX, gridY);
        }

        /// <summary>
        /// 批量分配格子
        /// 按传入的 entityIds 顺序依次分配
        /// </summary>
        /// <param name="entityIds">实体ID列表</param>
        /// <param name="campIds">阵营ID列表</param>
        /// <param name="baseTargetPos">基准目标位置</param>
        /// <returns>分配的格子中心点位置列表</returns>
        public List<zVector2> AssignGrids(List<int> entityIds, List<int> campIds, zVector2 baseTargetPos)
        {
            List<zVector2> result = new List<zVector2>();

            if (entityIds == null || entityIds.Count == 0)
                return result;

            if (campIds == null || campIds.Count != entityIds.Count)
            {
                zUDebug.LogWarning("[StopGridManager] 阵营ID列表与实体ID列表长度不一致");
                return result;
            }

            for (int i = 0; i < entityIds.Count; i++)
            {
                int entityId = entityIds[i];
                int campId = campIds[i];

                // 为每个单位分配格子（以基准目标位置为参考）
                zVector2 assignedPos = AssignGrid(entityId, campId, baseTargetPos);
                result.Add(assignedPos);
            }

            zUDebug.Log($"[StopGridManager] 批量分配{result.Count}个格子");

            return result;
        }

        /// <summary>
        /// 释放指定单位的格子占用
        /// </summary>
        public void ReleaseGrid(int entityId)
        {
            if (entityToGrid.TryGetValue(entityId, out long gridKey))
            {
                gridOccupancy.Remove(gridKey);
                entityToGrid.Remove(entityId);
            }
        }

        #endregion

        #region 公开方法 - 查询

        /// <summary>
        /// 检查格子是否被占用
        /// </summary>
        public bool IsGridOccupied(int gridX, int gridY)
        {
            long key = EncodeGridKey(gridX, gridY);
            return gridOccupancy.ContainsKey(key);
        }

        /// <summary>
        /// 检查格子是否属于指定阵营
        /// CampId % 2 == 0 → 停在 (偶x, 偶y)
        /// CampId % 2 == 1 → 停在 (奇x, 奇y)
        /// </summary>
        public bool IsGridValidForCamp(int gridX, int gridY, int campId)
        {
            if (campId % 2 == 0)
            {
                // 偶数阵营：(偶x, 偶y)
                return gridX % 2 == 0 && gridY % 2 == 0;
            }
            else
            {
                // 奇数阵营：(奇x, 奇y)
                return gridX % 2 == 1 && gridY % 2 == 1;
            }
        }

        /// <summary>
        /// 获取格子中心点位置
        /// </summary>
        public zVector2 GetGridCenter(int gridX, int gridY)
        {
            return new zVector2(
                new zfloat(gridX) + GRID_CENTER_OFFSET,
                new zfloat(gridY) + GRID_CENTER_OFFSET
            );
        }

        /// <summary>
        /// 获取指定格子占用的实体ID
        /// </summary>
        public int GetOccupyingEntity(int gridX, int gridY)
        {
            long key = EncodeGridKey(gridX, gridY);
            if (gridOccupancy.TryGetValue(key, out int entityId))
            {
                return entityId;
            }
            return -1;
        }

        /// <summary>
        /// 获取指定实体占用的格子坐标
        /// </summary>
        public (int x, int y)? GetEntityGrid(int entityId)
        {
            if (entityToGrid.TryGetValue(entityId, out long key))
            {
                DecodeGridKey(key, out int gridX, out int gridY);
                return (gridX, gridY);
            }
            return null;
        }

        /// <summary>
        /// 获取当前格子占用数量
        /// </summary>
        public int GetOccupancyCount()
        {
            return gridOccupancy.Count;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 编码格子坐标为 long 型 key
        /// 复用 FlowFieldManager.MakeKey 的编码方式
        /// </summary>
        private long EncodeGridKey(int gridX, int gridY)
        {
            return ((long)gridX) | (((long)gridY) << 32);
        }

        /// <summary>
        /// 从 key 解码格子坐标
        /// </summary>
        private void DecodeGridKey(long key, out int gridX, out int gridY)
        {
            gridX = (int)(key & 0xFFFFFFFF);
            gridY = (int)((key >> 32) & 0xFFFFFFFF);
        }

        /// <summary>
        /// 螺旋搜索找最近的可用格子
        /// 从中心开始，按环状向外扩展搜索
        /// </summary>
        private (int x, int y)? FindNearestAvailableGrid(int centerX, int centerY, int campId)
        {
            // 特殊处理：检查中心点
            if (IsGridValidForCamp(centerX, centerY, campId) &&
                !IsGridOccupied(centerX, centerY) &&
                map.IsWalkable(centerX, centerY))
            {
                return (centerX, centerY);
            }

            // 螺旋搜索
            for (int radius = 1; radius <= MAX_SEARCH_RADIUS; radius++)
            {
                // 遍历当前环的所有格子
                // 上边：从左到右
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int gridX = centerX + dx;
                    int gridY = centerY + radius;

                    if (CheckAndReturnGrid(gridX, gridY, campId))
                        return (gridX, gridY);
                }

                // 下边：从左到右
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int gridX = centerX + dx;
                    int gridY = centerY - radius;

                    if (CheckAndReturnGrid(gridX, gridY, campId))
                        return (gridX, gridY);
                }

                // 左边：从下到上（跳过角点，已在上边和下边处理）
                for (int dy = -radius + 1; dy <= radius - 1; dy++)
                {
                    int gridX = centerX - radius;
                    int gridY = centerY + dy;

                    if (CheckAndReturnGrid(gridX, gridY, campId))
                        return (gridX, gridY);
                }

                // 右边：从下到上（跳过角点）
                for (int dy = -radius + 1; dy <= radius - 1; dy++)
                {
                    int gridX = centerX + radius;
                    int gridY = centerY + dy;

                    if (CheckAndReturnGrid(gridX, gridY, campId))
                        return (gridX, gridY);
                }
            }

            return null; // 没有找到可用格子
        }

        /// <summary>
        /// 检查格子是否可用，如果可用则返回true
        /// </summary>
        private bool CheckAndReturnGrid(int gridX, int gridY, int campId)
        {
            // 检查格子是否属于该阵营
            if (!IsGridValidForCamp(gridX, gridY, campId))
                return false;

            // 检查格子是否可行走
            if (!map.IsWalkable(gridX, gridY))
                return false;

            // 检查格子是否未被占用
            if (IsGridOccupied(gridX, gridY))
                return false;

            return true;
        }

        #endregion
    }
}