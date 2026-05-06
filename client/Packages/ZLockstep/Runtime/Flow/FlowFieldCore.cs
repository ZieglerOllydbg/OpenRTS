using System.Collections.Generic;
using zUnity;

namespace ZLockstep.Flow
{
    /// <summary>
    /// 流场数据类
    /// 用于存储和管理流场相关数据，包括每个网格的方向向量和到目标的代价
    /// 支持单目标和多目标流场计算
    /// </summary>
    public class FlowField
    {
        public int fieldId;
        public int targetGridX;
        public int targetGridY;
        public int width;
        public int height;
        
        // 多源支持
        public bool isMulti;
        public int[] targetXs;
        public int[] targetYs;
        public int minTargetX;
        public int minTargetY;
        public int maxTargetX;
        public int maxTargetY;
        
        /// <summary>
        /// 每个格子的方向向量（已归一化）
        /// </summary>
        public zVector2[] directions;
        
        /// <summary>
        /// 每个格子到目标的代价
        /// </summary>
        public zfloat[] costs;
        
        /// <summary>
        /// 引用计数
        /// </summary>
        public int referenceCount;
        
        /// <summary>
        /// 是否需要更新（地图变化时标记为脏）
        /// </summary>
        public bool isDirty;
        
        /// <summary>
        /// 最后更新帧
        /// </summary>
        public int lastUpdateFrame;

        public FlowField(int id, int w, int h)
        {
            fieldId = id;
            width = w;
            height = h;
            directions = new zVector2[w * h];
            costs = new zfloat[w * h];
            referenceCount = 0;
            isDirty = false;
            lastUpdateFrame = 0;
            isMulti = false;
            targetXs = null;
            targetYs = null;
            minTargetX = 0; minTargetY = 0; maxTargetX = 0; maxTargetY = 0;
        }

        public int GetIndex(int x, int y)
        {
            return y * width + x;
        }

        public bool IsValid(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }

    /// <summary>
    /// 流场计算器类
    /// 负责计算流场数据，使用Dijkstra算法计算代价场，然后生成方向场
    /// 提供单目标和多目标流场计算功能
    /// </summary>
    public class FlowFieldCalculator
    {
        /// <summary>
        /// 网格节点结构体
        /// 用于在优先队列中存储网格坐标和对应的移动代价
        /// </summary>
        private struct GridNode
        {
            public int x;
            public int y;
            public zfloat cost;

            public GridNode(int x, int y, zfloat cost)
            {
                this.x = x;
                this.y = y;
                this.cost = cost;
            }
        }

        /// <summary>
        /// 优先队列类
        /// 实现最小堆，用于Dijkstra算法中按代价排序处理网格节点
        /// </summary>
        private class PriorityQueue
        {
            private List<GridNode> nodes = new List<GridNode>();

            public int Count => nodes.Count;

            public void Enqueue(GridNode node)
            {
                nodes.Add(node);
                int i = nodes.Count - 1;
                
                // 向上调整
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (nodes[parent].cost <= nodes[i].cost)
                        break;
                    
                    GridNode temp = nodes[parent];
                    nodes[parent] = nodes[i];
                    nodes[i] = temp;
                    i = parent;
                }
            }

            public GridNode Dequeue()
            {
                GridNode result = nodes[0];
                int last = nodes.Count - 1;
                nodes[0] = nodes[last];
                nodes.RemoveAt(last);

                if (nodes.Count > 0)
                {
                    // 向下调整
                    int i = 0;
                    while (true)
                    {
                        int left = i * 2 + 1;
                        int right = i * 2 + 2;
                        int smallest = i;

                        if (left < nodes.Count && nodes[left].cost < nodes[smallest].cost)
                            smallest = left;
                        if (right < nodes.Count && nodes[right].cost < nodes[smallest].cost)
                            smallest = right;

                        if (smallest == i)
                            break;

                        GridNode temp = nodes[i];
                        nodes[i] = nodes[smallest];
                        nodes[smallest] = temp;
                        i = smallest;
                    }
                }

                return result;
            }
        }

        private static readonly int[] DX = { 0, 1, 0, -1, 1, 1, -1, -1 };
        private static readonly int[] DY = { 1, 0, -1, 0, 1, -1, 1, -1 };
        private static readonly zfloat SQRT2 = new zfloat(1, 4142); // 对角线代价

        /// <summary>
        /// 计算流场
        /// 主要步骤：
        /// 1. 初始化所有网格的代价为无穷大
        /// 2. 使用Dijkstra算法计算从目标点到各点的最短路径代价
        /// 3. 添加墙壁惩罚使路径远离障碍物
        /// 4. 根据代价场生成方向场
        /// </summary>
        /// <param name="field">流场对象</param>
        /// <param name="map">地图接口，提供地形信息</param>
        /// <param name="targetX">目标点X坐标</param>
        /// <param name="targetY">目标点Y坐标</param>
        public static void Calculate(FlowField field, IFlowFieldMap map, int targetX, int targetY)
        {
            field.targetGridX = targetX;
            field.targetGridY = targetY;

            // 1. 初始化代价为无穷大
            for (int i = 0; i < field.costs.Length; i++)
            {
                field.costs[i] = zfloat.Infinity;
            }

            // 2. 使用Dijkstra计算代价场
            CalculateCostField(field, map, targetX, targetY);

            // 3. 添加墙壁惩罚（让单位远离障碍物）
            AddWallPenalty(field, map);

            // 4. 根据代价场生成方向场（使用加权平均，更平滑）
            GenerateDirectionField(field, map);

            field.isDirty = false;
        }

        /// <summary>
        /// 计算多源流场（多个目标格作为起点）
        /// 适用于多个单位需要汇聚到同一区域的情况
        /// </summary>
        /// <param name="field">流场对象</param>
        /// <param name="map">地图接口，提供地形信息</param>
        /// <param name="targets">目标点列表</param>
        public static void CalculateMulti(FlowField field, IFlowFieldMap map, List<(int x, int y)> targets)
        {
            field.isMulti = true;
            if (targets == null || targets.Count == 0)
                return;

            // 记录一个代表性目标（用于兼容调试）
            field.targetGridX = targets[0].x;
            field.targetGridY = targets[0].y;

            // 记录目标集合与包围盒
            field.targetXs = new int[targets.Count];
            field.targetYs = new int[targets.Count];
            field.minTargetX = int.MaxValue;
            field.minTargetY = int.MaxValue;
            field.maxTargetX = int.MinValue;
            field.maxTargetY = int.MinValue;
            for (int i = 0; i < targets.Count; i++)
            {
                field.targetXs[i] = targets[i].x;
                field.targetYs[i] = targets[i].y;
                if (targets[i].x < field.minTargetX) field.minTargetX = targets[i].x;
                if (targets[i].y < field.minTargetY) field.minTargetY = targets[i].y;
                if (targets[i].x > field.maxTargetX) field.maxTargetX = targets[i].x;
                if (targets[i].y > field.maxTargetY) field.maxTargetY = targets[i].y;
            }

            // 1. 初始化代价为无穷大
            for (int i = 0; i < field.costs.Length; i++)
            {
                field.costs[i] = zfloat.Infinity;
            }

            // 2. 多源 Dijkstra
            CalculateCostFieldMulti(field, map, targets);

            // 3. 墙壁惩罚
            AddWallPenalty(field, map);

            // 4. 生成方向
            GenerateDirectionField(field, map);

            field.isDirty = false;
        }

        /// <summary>
        /// 使用Dijkstra算法计算代价场
        /// 从单一目标点开始向外扩散计算到各个网格的最短路径代价
        /// 支持8方向移动，考虑对角线移动成本和穿墙检测
        /// </summary>
        /// <param name="field">流场对象</param>
        /// <param name="map">地图接口</param>
        /// <param name="targetX">目标点X坐标</param>
        /// <param name="targetY">目标点Y坐标</param>
        private static void CalculateCostField(FlowField field, IFlowFieldMap map, int targetX, int targetY)
        {
            if (!field.IsValid(targetX, targetY))
                return;

            PriorityQueue openSet = new PriorityQueue();
            bool[] closedSet = new bool[field.width * field.height];

            // 目标点代价为0
            int targetIndex = field.GetIndex(targetX, targetY);
            field.costs[targetIndex] = zfloat.Zero;
            openSet.Enqueue(new GridNode(targetX, targetY, zfloat.Zero));

            // Dijkstra算法
            while (openSet.Count > 0)
            {
                GridNode current = openSet.Dequeue();
                int currentIndex = field.GetIndex(current.x, current.y);

                if (closedSet[currentIndex])
                    continue;

                closedSet[currentIndex] = true;

                // 检查8个邻居
                for (int i = 0; i < 8; i++)
                {
                    int nx = current.x + DX[i];
                    int ny = current.y + DY[i];

                    if (!field.IsValid(nx, ny))
                        continue;

                    if (!map.IsFlowWalkable(nx, ny))
                        continue;

                    // 禁止对角穿角：对角方向要求相邻两正交格可走
                    if (i >= 4)
                    {
                        int stepX = DX[i];
                        int stepY = DY[i];
                        int adjX1 = current.x + stepX; // 水平相邻
                        int adjY1 = current.y;
                        int adjX2 = current.x;         // 垂直相邻
                        int adjY2 = current.y + stepY;

                        if (!field.IsValid(adjX1, adjY1) || !field.IsValid(adjX2, adjY2))
                            continue;
                        if (!map.IsFlowWalkable(adjX1, adjY1) || !map.IsFlowWalkable(adjX2, adjY2))
                            continue;
                    }

                    int neighborIndex = field.GetIndex(nx, ny);
                    if (closedSet[neighborIndex])
                        continue;

                    // 计算新代价
                    zfloat moveCost = (i < 4) ? zfloat.One : SQRT2; // 直线或对角线
                    zfloat terrainCost = map.GetTerrainCost(nx, ny);
                    zfloat newCost = current.cost + moveCost * terrainCost;

                    // 更新代价
                    if (newCost < field.costs[neighborIndex])
                    {
                        field.costs[neighborIndex] = newCost;
                        openSet.Enqueue(new GridNode(nx, ny, newCost));
                    }
                }
            }
        }

        /// <summary>
        /// 多源 Dijkstra 计算代价场
        /// 从多个目标点同时开始计算到各个网格的最短路径代价
        /// </summary>
        /// <param name="field">流场对象</param>
        /// <param name="map">地图接口</param>
        /// <param name="targets">目标点列表</param>
        private static void CalculateCostFieldMulti(FlowField field, IFlowFieldMap map, List<(int x, int y)> targets)
        {
            PriorityQueue openSet = new PriorityQueue();
            bool[] closedSet = new bool[field.width * field.height];

            // 所有目标点代价为0
            for (int i = 0; i < targets.Count; i++)
            {
                int tx = targets[i].x;
                int ty = targets[i].y;
                if (!field.IsValid(tx, ty))
                    continue;
                int tidx = field.GetIndex(tx, ty);
                field.costs[tidx] = zfloat.Zero;
                openSet.Enqueue(new GridNode(tx, ty, zfloat.Zero));
            }

            while (openSet.Count > 0)
            {
                GridNode current = openSet.Dequeue();
                int currentIndex = field.GetIndex(current.x, current.y);
                if (closedSet[currentIndex])
                    continue;
                closedSet[currentIndex] = true;

                for (int i = 0; i < 8; i++)
                {
                    int nx = current.x + DX[i];
                    int ny = current.y + DY[i];

                    if (!field.IsValid(nx, ny))
                        continue;
                    if (!map.IsFlowWalkable(nx, ny))
                        continue;

                    // 禁止对角穿角
                    if (i >= 4)
                    {
                        int stepX = DX[i];
                        int stepY = DY[i];
                        int adjX1 = current.x + stepX;
                        int adjY1 = current.y;
                        int adjX2 = current.x;
                        int adjY2 = current.y + stepY;
                        if (!field.IsValid(adjX1, adjY1) || !field.IsValid(adjX2, adjY2))
                            continue;
                        if (!map.IsFlowWalkable(adjX1, adjY1) || !map.IsFlowWalkable(adjX2, adjY2))
                            continue;
                    }

                    int neighborIndex = field.GetIndex(nx, ny);
                    if (closedSet[neighborIndex])
                        continue;

                    zfloat moveCost = (i < 4) ? zfloat.One : SQRT2;
                    zfloat terrainCost = map.GetTerrainCost(nx, ny);
                    zfloat newCost = current.cost + moveCost * terrainCost;

                    if (newCost < field.costs[neighborIndex])
                    {
                        field.costs[neighborIndex] = newCost;
                        openSet.Enqueue(new GridNode(nx, ny, newCost));
                    }
                }
            }
        }

        /// <summary>
        /// 添加墙壁惩罚到代价场
        /// 靠近障碍物的格子增加额外代价，让单位自然远离墙壁
        /// 这样可以防止单位过于贴近墙壁行走
        /// </summary>
        /// <param name="field">流场对象</param>
        /// <param name="map">地图接口</param>
        private static void AddWallPenalty(FlowField field, IFlowFieldMap map)
        {
            zfloat wallPenalty = new zfloat(0, 5000); // 每个邻近障碍物增加0.5代价

            for (int y = 0; y < field.height; y++)
            {
                for (int x = 0; x < field.width; x++)
                {
                    if (!map.IsFlowWalkable(x, y))
                        continue;

                    int index = field.GetIndex(x, y);
                    
                    // 如果已经是无限代价（不可达），跳过
                    if (field.costs[index] == zfloat.Infinity)
                        continue;

                    // 检查8个邻居，计算障碍物数量
                    int obstacleCount = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;

                            int nx = x + dx;
                            int ny = y + dy;

                            if (!field.IsValid(nx, ny) || !map.IsFlowWalkable(nx, ny))
                            {
                                obstacleCount++;
                            }
                        }
                    }

                    // 添加惩罚：障碍物越多，惩罚越高
                    if (obstacleCount > 0)
                    {
                        field.costs[index] += wallPenalty * (zfloat)obstacleCount;
                    }
                }
            }
        }

        /// <summary>
        /// 根据代价场生成方向场
        /// 使用梯度下降法计算每个网格的移动方向，使单位能沿着最优路径移动
        /// 当梯度法无法计算有效方向时，回退使用加权平均方向
        /// </summary>
        /// <param name="field">流场对象</param>
        /// <param name="map">地图接口</param>
        private static void GenerateDirectionField(FlowField field, IFlowFieldMap map)
        {
            for (int y = 0; y < field.height; y++)
            {
                for (int x = 0; x < field.width; x++)
                {
                    int index = field.GetIndex(x, y);

                    // 如果不可达，方向为零
                    if (field.costs[index] == zfloat.Infinity)
                    {
                        field.directions[index] = zVector2.zero;
                        continue;
                    }

                    // 如果已经是目标，方向为零
                    if (x == field.targetGridX && y == field.targetGridY)
                    {
                        field.directions[index] = zVector2.zero;
                        continue;
                    }

                    // 先尝试用代价场梯度（中心差分）生成方向
                    zfloat currentCost = field.costs[index];

                    bool hasLeft = field.IsValid(x - 1, y) && field.costs[field.GetIndex(x - 1, y)] != zfloat.Infinity;
                    bool hasRight = field.IsValid(x + 1, y) && field.costs[field.GetIndex(x + 1, y)] != zfloat.Infinity;
                    bool hasDown = field.IsValid(x, y - 1) && field.costs[field.GetIndex(x, y - 1)] != zfloat.Infinity;
                    bool hasUp = field.IsValid(x, y + 1) && field.costs[field.GetIndex(x, y + 1)] != zfloat.Infinity;

                    zVector2 grad = zVector2.zero;
                    if (hasLeft && hasRight)
                    {
                        zfloat cL = field.costs[field.GetIndex(x - 1, y)];
                        zfloat cR = field.costs[field.GetIndex(x + 1, y)];
                        grad.x = (cR - cL) * new zfloat(0, 5000); // *0.5
                    }
                    if (hasDown && hasUp)
                    {
                        zfloat cD = field.costs[field.GetIndex(x, y - 1)];
                        zfloat cU = field.costs[field.GetIndex(x, y + 1)];
                        grad.y = (cU - cD) * new zfloat(0, 5000); // *0.5
                    }

                    if (grad.magnitude > zfloat.Epsilon)
                    {
                        field.directions[index] = (-grad).normalized;
                    }
                    else
                    {
                        // 回退：加权平均方向（原实现）
                        zVector2 avgDirection = zVector2.zero;
                        zfloat totalWeight = zfloat.Zero;

                        for (int i = 0; i < 8; i++)
                        {
                            int nx = x + DX[i];
                            int ny = y + DY[i];

                            if (!field.IsValid(nx, ny))
                                continue;

                            int neighborIndex = field.GetIndex(nx, ny);
                            zfloat neighborCost = field.costs[neighborIndex];

                            if (neighborCost < currentCost)
                            {
                                zfloat costDiff = currentCost - neighborCost;
                                zfloat weight = costDiff * costDiff;

                                zVector2 dirToNeighbor = new zVector2((zfloat)DX[i], (zfloat)DY[i]);
                                avgDirection += dirToNeighbor * weight;
                                totalWeight += weight;
                            }
                        }

                        if (totalWeight > zfloat.Epsilon)
                        {
                            field.directions[index] = (avgDirection / totalWeight).normalized;
                        }
                        else
                        {
                            field.directions[index] = zVector2.zero;
                        }
                    }
                }
            }
        }
    }
}

