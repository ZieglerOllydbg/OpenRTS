using System.Collections.Generic;
using System.Linq;
using zUnity;
using ZLockstep.RVO;

namespace ZLockstep.Flow
{
    /// <summary>
    /// 流场信息结构体
    /// 用于调试和监控流场状态的轻量级数据结构
    /// 包含流场的基本信息如ID、目标位置、引用计数、脏标记和最后更新帧
    /// </summary>
    public struct FlowFieldInfo
    {
        public int fieldId;
        public int targetGridX;
        public int targetGridY;
        public int referenceCount;
        public bool isDirty;
        public int lastUpdateFrame;
    }

    /// <summary>
    /// 流场管理器类
    /// 负责流场的创建、缓存、更新和生命周期管理
    /// 
    /// 主要功能包括：
    /// 1. 流场缓存管理：避免重复计算相同目标的流场，提高性能
    /// 2. 流场生命周期管理：通过引用计数自动回收无用流场
    /// 3. 脏区域处理：当地图发生变化时，自动标记并更新受影响的流场
    /// 4. 多源流场支持：支持单目标和多目标流场计算
    /// 5. 流场采样：提供接口获取指定位置的移动方向和代价
    /// 
    /// 缓存策略：
    /// - 单目标流场使用坐标作为键进行缓存
    /// - 多目标流场对目标点排序后生成字符串键进行缓存
    /// - 支持最大缓存数量限制，超出时自动清理无引用的流场
    /// 
    /// 更新机制：
    /// - 支持增量更新：只更新脏区域的流场，避免全量计算
    /// - 每帧更新限制：控制每帧最多更新的流场数量，避免性能波动
    /// </summary>
    public class FlowFieldManager
    {
        private IFlowFieldMap map;
        private Dictionary<long, FlowField> fieldCache;  // key: targetGridX | (targetGridY << 32)
        private Dictionary<string, FlowField> multiFieldCache; // key: "x_y|x_y|..." 排序后
        private Dictionary<int, FlowField> fieldById;
        private Queue<FlowField> dirtyFields;
        private HashSet<int> dirtySet;
        private int nextFieldId;
        private int maxCachedFields;
        private int currentFrame;
        private int maxUpdatesPerFrame;
        private int totalRequestCount = 0;  // 新增：记录总请求次数

        public bool NeedUpdateObstacles {get; set;}

        /// <summary>
        /// 构造函数
        /// 初始化流场管理器的内部数据结构
        /// 默认最大缓存流场数量为20个，每帧最多更新2个脏流场
        /// </summary>
        public FlowFieldManager()
        {
            fieldCache = new Dictionary<long, FlowField>();
            multiFieldCache = new Dictionary<string, FlowField>();
            fieldById = new Dictionary<int, FlowField>();
            dirtyFields = new Queue<FlowField>();
            dirtySet = new HashSet<int>();
            nextFieldId = 0;
            maxCachedFields = 20;
            maxUpdatesPerFrame = 2;
            currentFrame = 0;
        }

        /// <summary>
        /// 初始化流场管理器
        /// 设置地图接口和最大缓存流场数量
        /// </summary>
        /// <param name="mapInterface">地图接口，提供地图信息和坐标转换功能</param>
        /// <param name="maxFields">最大缓存流场数量，默认为20</param>
        public void Initialize(IFlowFieldMap mapInterface, int maxFields = 20)
        {
            map = mapInterface;
            maxCachedFields = maxFields;
        }
        
        /// <summary>
        /// 获取地图接口
        /// </summary>
        /// <returns>地图接口</returns>
        public IFlowFieldMap GetMap()
        {
            return map;
        }

        /// <summary>
        /// 请求到指定世界坐标的流场
        /// 将世界坐标转换为网格坐标后请求流场
        /// </summary>
        /// <param name="targetWorldPos">目标世界坐标</param>
        /// <returns>流场 ID，用于后续采样操作</returns>
        public int RequestFlowField(zVector2 targetWorldPos)
        {
            map.WorldToFlow(targetWorldPos, out int gridX, out int gridY);
            return RequestFlowFieldToGrid(gridX, gridY);
        }

        /// <summary>
        /// 请求多源（多个世界坐标目标）流场
        /// 适用于多个单位需要汇聚到同一区域的情况
        /// 自动去重并排序目标点以生成稳定的缓存键
        /// </summary>
        /// <param name="targetWorldPositions">目标世界坐标列表</param>
        /// <returns>流场 ID，用于后续采样操作</returns>
        public int RequestFlowFieldMultiWorld(List<zVector2> targetWorldPositions)
        {
            List<(int x, int y)> cells = new List<(int x, int y)>();
            HashSet<long> seen = new HashSet<long>();
            foreach (var wp in targetWorldPositions)
            {
                map.WorldToFlow(wp, out int gx, out int gy);
                long k = ((long)gx) | (((long)gy) << 32);
                if (seen.Add(k))
                {
                    cells.Add((gx, gy));
                }
            }
            return RequestFlowFieldMulti(cells);
        }

        /// <summary>
        /// 请求多源（多个格子目标）流场
        /// 直接使用网格坐标创建多目标流场
        /// </summary>
        /// <param name="targetCells">目标网格坐标列表</param>
        /// <returns>流场 ID，用于后续采样操作</returns>
        public int RequestFlowFieldMulti(List<(int x, int y)> targetCells)
        {
            totalRequestCount++;  // 新增：增加请求计数
            if (targetCells == null || targetCells.Count == 0)
                return -1;

            // 去重并排序 (y,x) 稳定 key
            var unique = targetCells.Distinct().OrderBy(t => t.y).ThenBy(t => t.x).ToList();
            string key = string.Join("|", unique.Select(t => $"{t.x}_{t.y}"));

            if (multiFieldCache.TryGetValue(key, out FlowField fieldHit))
            {
                fieldHit.referenceCount++;
                return fieldHit.fieldId;
            }

            // 检查缓存是否已满
            if (fieldCache.Count + multiFieldCache.Count >= maxCachedFields)
            {
                CleanupUnusedFields();
            }

            // 创建并计算
            FlowField field = new FlowField(nextFieldId++, map.GetFlowGridWidth(), map.GetFlowGridHeight());
            field.referenceCount = 1;
            field.isDirty = true;
            field.isMulti = true;

            FlowFieldCalculator.CalculateMulti(field, map, unique);
            field.lastUpdateFrame = currentFrame;

            multiFieldCache[key] = field;
            fieldById[field.fieldId] = field;
            return field.fieldId;
        }

        /// <summary>
        /// 请求到指定格子的流场
        /// 创建或获取到指定网格坐标的流场
        /// </summary>
        /// <param name="targetGridX">目标网格 X 坐标</param>
        /// <param name="targetGridY">目标网格 Y 坐标</param>
        /// <returns>流场 ID，用于后续采样操作</returns>
        public int RequestFlowFieldToGrid(int targetGridX, int targetGridY)
        {
            totalRequestCount++;  // 新增：增加请求计数
            // 生成缓存 key
            long key = MakeKey(targetGridX, targetGridY);

            // 检查是否已存在
            if (fieldCache.TryGetValue(key, out FlowField field))
            {
                field.referenceCount++;
                return field.fieldId;
            }

            // 检查缓存是否已满
            if (fieldCache.Count >= maxCachedFields)
            {
                CleanupUnusedFields();
            }

            // 创建新流场
            field = new FlowField(nextFieldId++, map.GetWidth(), map.GetHeight());
            field.referenceCount = 1;
            field.isDirty = true;

            // 计算流场
            FlowFieldCalculator.Calculate(field, map, targetGridX, targetGridY);
            field.lastUpdateFrame = currentFrame;

            // 缓存
            fieldCache[key] = field;
            fieldById[field.fieldId] = field;

            return field.fieldId;
        }

        /// <summary>
        /// 释放流场引用
        /// 减少流场的引用计数，当引用计数为0时，流场将在适当时机被清理
        /// </summary>
        /// <param name="fieldId">要释放的流场ID</param>
        public void ReleaseFlowField(int fieldId)
        {
            if (fieldById.TryGetValue(fieldId, out FlowField field))
            {
                field.referenceCount--;
                if (field.referenceCount < 0)
                {
                    field.referenceCount = 0;
                }
            }
        }

        /// <summary>
        /// 采样指定世界位置的移动方向
        /// 根据流场ID获取对应位置的移动方向向量
        /// </summary>
        /// <param name="fieldId">流场ID</param>
        /// <param name="worldPos">世界坐标位置</param>
        /// <returns>归一化的移动方向向量</returns>
        public zVector2 SampleDirection(int fieldId, zVector2 worldPos)
        {
            if (!fieldById.TryGetValue(fieldId, out FlowField field))
            {
                return zVector2.zero;
            }

            map.WorldToFlow(worldPos, out int gridX, out int gridY);

            if (!field.IsValid(gridX, gridY))
            {
                return zVector2.zero;
            }

            int index = field.GetIndex(gridX, gridY);
            return field.directions[index];
        }

        /// <summary>
        /// 采样指定世界位置到目标的估算代价
        /// 获取从指定位置到流场目标的路径代价
        /// </summary>
        /// <param name="fieldId">流场ID</param>
        /// <param name="worldPos">世界坐标位置</param>
        /// <returns>路径代价，无穷大表示不可达</returns>
        public zfloat SampleCost(int fieldId, zVector2 worldPos)
        {
            if (!fieldById.TryGetValue(fieldId, out FlowField field))
            {
                return zfloat.Infinity;
            }

            map.WorldToFlow(worldPos, out int gridX, out int gridY);

            if (!field.IsValid(gridX, gridY))
            {
                return zfloat.Infinity;
            }

            int index = field.GetIndex(gridX, gridY);
            return field.costs[index];
        }

        /// <summary>
        /// 判断指定位置是否已到达目标区域
        /// 通过比较当前位置到目标的代价与阈值判断是否接近目标
        /// </summary>
        /// <param name="fieldId">流场ID</param>
        /// <param name="worldPos">世界坐标位置</param>
        /// <param name="threshold">阈值，代价小于此值认为已到达目标</param>
        /// <returns>是否已到达目标区域</returns>
        public bool IsAtTarget(int fieldId, zVector2 worldPos, zfloat threshold)
        {
            zfloat cost = SampleCost(fieldId, worldPos);
            return cost < threshold;
        }

        /// <summary>
        /// 标记地图区域为脏区域
        /// 当地图发生变化时调用此方法，会自动标记受影响的流场为脏状态
        /// 脏流场将在后续更新中重新计算
        /// </summary>
        /// <param name="minX">区域最小X坐标</param>
        /// <param name="minY">区域最小Y坐标</param>
        /// <param name="maxX">区域最大X坐标</param>
        /// <param name="maxY">区域最大Y坐标</param>
        public void MarkRegionDirty(int minX, int minY, int maxX, int maxY)
        {
            // 将逻辑格子坐标转换为流场格子坐标
            int flowMinX = minX / 2;
            int flowMinY = minY / 2;
            int flowMaxX = (maxX + 1) / 2;
            int flowMaxY = (maxY + 1) / 2;

            foreach (var field in fieldById.Values)
            {
                if (field.referenceCount <= 0)
                    continue;

                if (!field.isMulti)
                {
                    if (field.targetGridX >= flowMinX && field.targetGridX <= flowMaxX &&
                        field.targetGridY >= flowMinY && field.targetGridY <= flowMaxY)
                    {
                        field.isDirty = true;
                        if (dirtySet.Add(field.fieldId))
                        {
                            dirtyFields.Enqueue(field);
                        }
                    }
                }
                else
                {
                    bool intersect = !(field.maxTargetX < flowMinX || field.minTargetX > flowMaxX ||
                                       field.maxTargetY < flowMinY || field.minTargetY > flowMaxY);
                    if (intersect)
                    {
                        field.isDirty = true;
                        if (dirtySet.Add(field.fieldId))
                        {
                            dirtyFields.Enqueue(field);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新所有脏流场
        /// 按照队列顺序更新脏流场，受每帧最大更新数量限制
        /// </summary>
        public void UpdateDirtyFields()
        {
            int updated = 0;
            while (dirtyFields.Count > 0 && updated < maxUpdatesPerFrame)
            {
                FlowField field = dirtyFields.Dequeue();
                
                if (field.isDirty)
                {
                    if (field.isMulti)
                    {
                        List<(int x, int y)> targets = new List<(int x, int y)>();
                        if (field.targetXs != null)
                        {
                            for (int i = 0; i < field.targetXs.Length; i++)
                                targets.Add((field.targetXs[i], field.targetYs[i]));
                        }
                        FlowFieldCalculator.CalculateMulti(field, map, targets);
                    }
                    else
                    {
                        FlowFieldCalculator.Calculate(field, map, field.targetGridX, field.targetGridY);
                    }
                    field.lastUpdateFrame = currentFrame;
                    updated++;
                }
                // 出队后无论是否更新，移除脏标记记录，避免重复
                dirtySet.Remove(field.fieldId);
            }
        }

        /// <summary>
        /// 强制更新指定流场
        /// 立即重新计算指定流场，不受每帧更新数量限制
        /// </summary>
        /// <param name="fieldId">要更新的流场ID</param>
        public void ForceUpdateFlowField(int fieldId)
        {
            if (fieldById.TryGetValue(fieldId, out FlowField field))
            {
                if (field.isMulti)
                {
                    List<(int x, int y)> targets = new List<(int x, int y)>();
                    if (field.targetXs != null)
                    {
                        for (int i = 0; i < field.targetXs.Length; i++)
                            targets.Add((field.targetXs[i], field.targetYs[i]));
                    }
                    FlowFieldCalculator.CalculateMulti(field, map, targets);
                }
                else
                {
                    FlowFieldCalculator.Calculate(field, map, field.targetGridX, field.targetGridY);
                }
                field.lastUpdateFrame = currentFrame;
            }
        }

        /// <summary>
        /// 获取当前活跃的流场数量
        /// 活跃流场指至少被一个引用持有的流场
        /// </summary>
        /// <returns>活跃流场数量</returns>
        public int GetActiveFieldCount()
        {
            return fieldById.Count;
        }

        /// <summary>
        /// 获取所有活跃流场的字典
        /// </summary>
        /// <returns>以流场ID为键，流场对象为值的字典</returns>
        public Dictionary<int, FlowField> GetActiveFields()
        {
            return fieldById;
        }

        /// <summary>
        /// 获取流场信息
        /// 返回指定流场的调试信息
        /// </summary>
        /// <param name="fieldId">流场ID</param>
        /// <returns>流场信息结构体</returns>
        public FlowFieldInfo GetFieldInfo(int fieldId)
        {
            if (fieldById.TryGetValue(fieldId, out FlowField field))
            {
                return new FlowFieldInfo
                {
                    fieldId = field.fieldId,
                    targetGridX = field.targetGridX,
                    targetGridY = field.targetGridY,
                    referenceCount = field.referenceCount,
                    isDirty = field.isDirty,
                    lastUpdateFrame = field.lastUpdateFrame
                };
            }
            return default;
        }

        /// <summary>
        /// 清理无引用的流场
        /// 遍历所有缓存的流场，移除引用计数为0的流场以释放内存
        /// </summary>
        public void CleanupUnusedFields()
        {
            List<long> keysToRemove = new List<long>();
            List<int> idsToRemove = new List<int>();
            List<string> multiKeysToRemove = new List<string>();

            foreach (var kvp in fieldCache)
            {
                if (kvp.Value.referenceCount <= 0)
                {
                    keysToRemove.Add(kvp.Key);
                    idsToRemove.Add(kvp.Value.fieldId);
                }
            }

            foreach (var kvp in multiFieldCache)
            {
                if (kvp.Value.referenceCount <= 0)
                {
                    multiKeysToRemove.Add(kvp.Key);
                    idsToRemove.Add(kvp.Value.fieldId);
                }
            }

            foreach (long key in keysToRemove)
            {
                fieldCache.Remove(key);
            }

            foreach (int id in idsToRemove)
            {
                fieldById.Remove(id);
            }

            foreach (string key in multiKeysToRemove)
            {
                multiFieldCache.Remove(key);
            }
        }

        /// <summary>
        /// 清除所有流场
        /// 重置流场管理器到初始状态，清除所有缓存和引用
        /// </summary>
        public void Clear()
        {
            fieldCache.Clear();
            multiFieldCache.Clear();
            fieldById.Clear();
            dirtyFields.Clear();
            dirtySet.Clear();
        }

        /// <summary>
        /// 每帧更新
        /// 更新当前帧计数并处理脏流场更新
        /// 应该在游戏主循环中每帧调用
        /// </summary>
        public void Tick()
        {
            currentFrame++;
            UpdateDirtyFields();
        }

        /// <summary>
        /// 设置每帧最多更新的流场数量
        /// 控制流场更新对性能的影响
        /// </summary>
        /// <param name="maxUpdates">每帧最多更新的流场数量</param>
        public void SetMaxUpdatesPerFrame(int maxUpdates)
        {
            maxUpdatesPerFrame = maxUpdates;
        }

        /// <summary>
        /// 获取流场总请求次数
        /// 统计所有类型流场请求的累计次数
        /// </summary>
        /// <returns>流场请求总次数</returns>
        public int GetTotalRequestCount()
        {
            return totalRequestCount;
        }

        /// <summary>
        /// 重置流场请求计数器
        /// 用于重新开始统计数据
        /// </summary>
        public void ResetRequestCount()
        {
            totalRequestCount = 0;
        }

        /// <summary>
        /// 生成流场缓存键
        /// 将网格坐标组合成长整型作为单目标流场的缓存键
        /// </summary>
        /// <param name="x">网格 X 坐标</param>
        /// <param name="y">网格 Y 坐标</param>
        /// <returns>缓存键</returns>
        private long MakeKey(int x, int y)
        {
            return ((long)x) | (((long)y) << 32);
        }
    }
}