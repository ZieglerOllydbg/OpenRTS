using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using zUnity;
using ZLockstep.RVO;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.View;
using System;
using ZLockstep.Simulation;

namespace ZLockstep.Flow
{
    /// <summary>
    /// 流场导航系统
    /// 处理所有使用流场寻路的实体
    /// 继承自BaseSystem，集成到现有ECS框架
    /// 
    /// 该系统结合了流场寻路(Flow Field Pathfinding)和RVO避障算法(Reciprocal Velocity Obstacles)
    /// 实现高效的群体寻路和避障功能，特别适用于RTS游戏中大量单位的移动控制
    /// 主要功能包括：
    /// 1. 流场生成与管理
    /// 2. RVO避障代理管理
    /// 3. 单位移动控制与同步
    /// 4. 散点目标分配（适用于编队分散）
    /// 5. 卡住检测与处理
    /// 6. 到达目标检测
    /// </summary>
    public class FlowFieldNavigationSystem : BaseSystem
    {
        private const string ConvexBlockerResourcePath = "Data/Map/ConvexBlockerMap";

        private FlowFieldManager flowFieldManager;
        private IFlowFieldMap map;
        private readonly List<List<zVector2>> staticSceneObstaclePolygons = new List<List<zVector2>>();

        /// <summary>
        /// 停止格子管理器
        /// 负责基于阵营的格子分配和管理
        /// </summary>
        private StopGridManager stopGridManager;

        /// <summary>
        /// 到达距离阈值：当单位与目标距离小于此值时，认为已到达目标
        /// 可在 Unity Inspector 中配置，针对不同单位类型调整
        /// </summary>
        private float arrivalDistanceThreshold = 0.5f;
    
        // RVO 更新频率控制常量
        private const int RVO_UPDATE_INTERVAL = 5;           // 每 5 帧更新一次
        private const int MAX_CATCH_UP_FRAMES = 10;          // 追帧时最多补 10 帧
        
        // RVO 更新状态追踪
        private int _lastRvoUpdateTick = -1;

        // Debug: 最近一次散点集合
        private readonly List<zVector2> debugScatterPoints = new List<zVector2>();
        public IReadOnlyList<zVector2> DebugScatterPoints => debugScatterPoints;
        private zVector2 debugScatterCenter = zVector2.zero;
        private zfloat debugScatterRadius = zfloat.Zero;
        public zVector2 DebugScatterCenter => debugScatterCenter;
        public zfloat DebugScatterRadius => debugScatterRadius;

        protected override void OnInitialize()
        {
            m_random = new zRandom(0L); // 使用固定种子，确保可复现的随机行为

            // 系统初始化
            Simulator.Instance.Clear();
            Simulator.Instance.setTimeStep((zfloat)(0.05f * RVO_UPDATE_INTERVAL));
            Simulator.Instance.setAgentDefaults((zfloat)10.0f, 10, (zfloat)2.0f, (zfloat)2.0f, (zfloat)1.0f, (zfloat)6.0f, zVector2.zero);
        }

        /// <summary>
        /// 初始化流场导航系统
        /// 在系统注册到World后调用
        /// </summary>
        /// <param name="ffMgr">流场管理器</param>
        /// <param name="rvoSim">RVO避障模拟器</param>
        /// <param name="gameMap">游戏地图接口</param>
        public void InitializeNavigation(FlowFieldManager ffMgr, IFlowFieldMap gameMap)
        {
            flowFieldManager = ffMgr;
            map = gameMap;
            LoadStaticSceneObstacles();

            // 初始化停止格子管理器
            stopGridManager = new StopGridManager();
            stopGridManager.Initialize(gameMap);

            flowFieldManager.NeedUpdateObstacles = true;
        }

        /// <summary>
        /// 获取停止格子管理器
        /// </summary>
        public StopGridManager GetStopGridManager()
        {
            return stopGridManager;
        }

        /// <summary>
        /// 更新场景障碍物
        /// </summary>
        public void UpdateObstacles()
        {
            Simulator.Instance.ClearObstacles();

            // 场景边界
            int width = map.GetWidth();
            int height = map.GetHeight();

            // 创建场景边界障碍物（逆时针顺序）
            // 边界向外扩展半个格子，确保单位不会走到地图边缘
            float half = 0.5f;
            float minX = -half;
            float minY = -half;
            float maxX = width - half;
            float maxY = height - half;

            // 环境边界，使用顺时针顺序
            List<zVector2> boundaryVertices = new List<zVector2>
            {
                new(zfloat.FromFloat(minX), zfloat.FromFloat(minY)),  // 左下
                new(zfloat.FromFloat(minX), zfloat.FromFloat(maxY)),   // 左上
                new(zfloat.FromFloat(maxX), zfloat.FromFloat(maxY)),  // 右上
                new(zfloat.FromFloat(maxX), zfloat.FromFloat(minY)),  // 右下
            };

            Simulator.Instance.addObstacle(boundaryVertices);

            for (int i = 0; i < staticSceneObstaclePolygons.Count; i++)
            {
                List<zVector2> polygon = staticSceneObstaclePolygons[i];
                if (polygon.Count >= 3)
                {
                    Simulator.Instance.addObstacle(polygon);
                }
            }

            // 获取所有建筑，添加建筑障碍物
            var buildingEntities = ComponentManager.GetAllEntityIdsWith<BuildingComponent>();
            foreach (var entityId in buildingEntities)
            {
                Entity entity = new Entity(entityId);
                var building = ComponentManager.GetComponent<BuildingComponent>(entity);
                
                // 计算建筑物的世界坐标边界
                float minWorldX = building.X - building.Width / 2;
                float minWorldY = building.Y - building.Height / 2;
                float maxWorldX = building.X + building.Width / 2;
                float maxWorldY = building.Y + building.Height / 2;

                // 创建建筑物障碍物（逆时针顺序）
                List<zVector2> buildingVertices = new List<zVector2>
                {
                    new(zfloat.FromFloat(minWorldX), zfloat.FromFloat(minWorldY)),  // 左下
                    new(zfloat.FromFloat(maxWorldX), zfloat.FromFloat(minWorldY)),  // 右下
                    new(zfloat.FromFloat(maxWorldX), zfloat.FromFloat(maxWorldY)),  // 右上
                    new(zfloat.FromFloat(minWorldX), zfloat.FromFloat(maxWorldY))   // 左上
                };

                Simulator.Instance.addObstacle(buildingVertices);
            }

            // add in awake
            Simulator.Instance.processObstacles();
        }

        /// <summary>
        /// 添加导航能力
        /// </summary>
        /// <param name="entity">实体</param>
        /// <param name="radius">半径</param>
        /// <param name="maxSpeed">最大速度</param>
        private void LoadStaticSceneObstacles()
        {
            staticSceneObstaclePolygons.Clear();

            TextAsset textAsset = Resources.Load<TextAsset>(ConvexBlockerResourcePath);
            if (textAsset == null)
            {
                UnityEngine.Debug.LogWarning($"[FlowFieldNavigationSystem] 未找到凸多边形阻挡文件: Resources/{ConvexBlockerResourcePath}.txt");
                return;
            }

            string[] lines = textAsset.text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("count=", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                List<zVector2> polygon = ParseObstaclePolygon(line);
                if (polygon.Count >= 3)
                {
                    staticSceneObstaclePolygons.Add(polygon);
                }
            }

            UnityEngine.Debug.Log($"[FlowFieldNavigationSystem] 已加载场景凸多边形阻挡: {staticSceneObstaclePolygons.Count}");
        }

        private List<zVector2> ParseObstaclePolygon(string line)
        {
            List<zVector2> polygon = new List<zVector2>();
            string[] pointTokens = line.Split(';');

            for (int i = 0; i < pointTokens.Length; i++)
            {
                string pointToken = pointTokens[i].Trim();
                if (string.IsNullOrEmpty(pointToken))
                {
                    continue;
                }

                string[] xy = pointToken.Split(',');
                if (xy.Length != 2)
                {
                    continue;
                }

                if (!float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
                {
                    continue;
                }

                if (!float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                {
                    continue;
                }

                polygon.Add(new zVector2(zfloat.FromFloat(x), zfloat.FromFloat(y)));
            }

            return polygon;
        }

        public void AddNavigator(Entity entity, zfloat radius, zfloat maxSpeed)
        {
            // 获取实体位置
            var transform = ComponentManager.GetComponent<TransformComponent>(entity);
            zVector2 pos2D = new(transform.Position.x, transform.Position.z);

            // 添加导航组件
            var navigator = FlowFieldNavigatorComponent.Create(radius, maxSpeed);
            
            int sid = Simulator.Instance.addAgent(pos2D);
            Simulator.Instance.setAgentRadius(sid, radius);
            Simulator.Instance.setAgentMaxSpeed(sid, maxSpeed);
            navigator.RvoAgentId = sid;

            ComponentManager.AddComponent(entity, navigator);
        }

        /// <summary>
        /// 设置实体的移动目标
        /// 主要功能：
        /// 1. 释放实体当前的流场资源
        /// 2. 请求新的流场数据
        /// 3. 更新导航状态（重置卡住检测等）
        /// 4. 设置目标位置组件
        /// </summary>
        /// <param name="entity">需要设置目标的实体</param>
        /// <param name="targetPos">目标位置</param>
        public void SetMoveTarget(Entity entity, zVector2 targetPos, bool userInput = false, bool useFlowfield = false)
        {
            // 判断单位组件是否存在
            if (!ComponentManager.HasComponent<UnitComponent>(entity))
            {
                return;
            }

            // 不能是炮弹
            if (ComponentManager.HasComponent<ProjectileComponent>(entity))
            {
                return;
            }

            if (!ComponentManager.HasComponent<FlowFieldNavigatorComponent>(entity))
            {
                zUDebug.LogError($"[SetMoveTarget] Entity {entity.Id} does not have FlowFieldNavigatorComponent");
                return;
            }

            if (!ComponentManager.HasComponent<TransformComponent>(entity))
            {
                zUDebug.LogError($"Entity {entity.Id} does not have TransformComponent");
                return;
            }

            var navigator = ComponentManager.GetComponent<FlowFieldNavigatorComponent>(entity);
            var transform = ComponentManager.GetComponent<TransformComponent>(entity);

            // 释放旧流场
            if (navigator.CurrentFlowFieldId >= 0)
            {
                flowFieldManager.ReleaseFlowField(navigator.CurrentFlowFieldId);
            }

            // 将目标位置对齐到网格中心
            map.WorldToFlow(targetPos, out int targetGridX, out int targetGridY);
            zVector2 alignedTargetPos = map.FlowToWorld(targetGridX, targetGridY);

            // 用户输入，使用流畅寻路
            if (useFlowfield)
            {
                navigator.CurrentFlowFieldId = flowFieldManager.RequestFlowField(alignedTargetPos);
            }
            navigator.HasReachedTarget = false;
            // 重置卡住状态
            navigator.StuckFrames = 0;
            navigator.LastPosition = new zVector2(transform.Position.x, transform.Position.z);
            navigator.NearSlowFrames = 0;
            // 重置减速状态
            navigator.IsSlowingDown = false;
            navigator.SlowDownStartTick = 0;
            navigator.SpeedBeforeSlowDown = zfloat.Zero;

            // 更新导航组件
            ComponentManager.AddComponent(entity, navigator);

            // 添加或更新目标组件
            var target = MoveTargetComponent.Create(alignedTargetPos);
            target.UserInput = userInput;
            ComponentManager.AddComponent(entity, target);

            debugScatterPoints.Clear();
            debugScatterCenter = alignedTargetPos;
        }

        /// <summary>
        /// 批量设置多个实体的目标（常见于RTS选中操作）
        /// 使用格子分配逻辑让单位整齐停止
        /// </summary>
        /// <param name="entities">需要设置目标的实体列表</param>
        /// <param name="targetPos">目标位置</param>
        /// <param name="campId">阵营ID，用于格子分配</param>
        /// <param name="userInput">是否为用户输入</param>
        public void SetMultipleTargets(List<Entity> entities, zVector2 targetPos, int campId, bool userInput = false)
        {
            if (entities == null || entities.Count == 0)
                return;

            // 使用 StopGridManager 分配格子
            List<zVector2> assignedPositions = null;
            if (stopGridManager != null)
            {
                // 构建实体ID列表和阵营ID列表
                List<int> entityIds = new List<int>();
                List<int> campIds = new List<int>();
                foreach (var entity in entities)
                {
                    entityIds.Add(entity.Id);
                    campIds.Add(campId); // 所有单位使用同一个阵营ID
                }

                assignedPositions = stopGridManager.AssignGrids(entityIds, campIds, targetPos);
            }

            // 为每个实体设置目标
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                zVector2 finalTarget = targetPos;

                // 使用分配后的格子位置
                if (assignedPositions != null && i < assignedPositions.Count)
                {
                    finalTarget = assignedPositions[i];
                }

                SetMoveTarget(entity, finalTarget, userInput, true);
            }
        }

        /// <summary>
        /// 为多个单位设置散点目标，复用同一个多源流场
        /// 主要功能：
        /// 1. 计算编队中单位的最大半径，确定分散间距
        /// 2. 生成方形候选散点分布（默认）
        /// 3. 将候选点投影到可行走区域并去重
        /// 4. 请求共享的多源流场（一个流场服务多个目标点）
        /// 5. 使用贪心算法为每个单位分配最近的散点
        /// 6. 为每个单位设置个人目标点和共享流场ID
        /// </summary>
        /// <param name="entities">需要设置目标的实体列表</param>
        /// <param name="groupCenter">编队中心位置</param>
        public void SetScatterTargets(List<Entity> entities, zVector2 groupCenter, bool userInput = false)
        {
            if (entities == null || entities.Count == 0)
                return;

            // 将编队中心位置对齐到网格中心
            map.WorldToFlow(groupCenter, out int centerGridX, out int centerGridY);
            zVector2 alignedGroupCenter = map.FlowToWorld(centerGridX, centerGridY);

            // 统计最大半径，确定最小间距
            zfloat maxRadius = zfloat.Zero;
            foreach (var e in entities)
            {
                var nav = ComponentManager.GetComponent<FlowFieldNavigatorComponent>(e);
                if (nav.Radius > maxRadius) maxRadius = nav.Radius;
            }
            // 基础间距：约 4x 半径，并至少为 2 个网格尺寸，保证明显分散
            zfloat spacing = maxRadius * new zfloat(2);
            zfloat minSpacing = new zfloat(2);
            if (spacing < minSpacing) spacing = minSpacing;

            // 生成候选散点（方形格，默认）
            // 计算所需的行列数：rows*cols >= N
            int pointsPerSide = System.Math.Max(6, (int)zMathf.Sqrt((zfloat)entities.Count) + 1);
            var candidates = GenerateSquareLattice(alignedGroupCenter, spacing, pointsPerSide, pointsPerSide, entities.Count * 6);

            // 投影到最近可走格并去重
            List<zVector2> scatterPoints = new List<zVector2>();
            HashSet<long> seen = new HashSet<long>();
            int attempts = 0;
            while (scatterPoints.Count < entities.Count && attempts < 3)
            {
                foreach (var p in candidates)
                {
                    zVector2 q = ProjectToWalkable(p, 12 + attempts * 6);
                    map.WorldToFlow(q, out int gx, out int gy);
                    long k = ((long)gx) | (((long)gy) << 32);
                    if (seen.Add(k))
                    {
                        scatterPoints.Add(q);
                        if (scatterPoints.Count >= entities.Count) break;
                    }
                }
                if (scatterPoints.Count < entities.Count)
                {
                    pointsPerSide += 2;
                    candidates = GenerateSquareLattice(alignedGroupCenter, spacing, pointsPerSide, pointsPerSide, entities.Count * 8);
                    attempts++;
                }
            }
            if (scatterPoints.Count == 0)
                return;

            // 请求共享多源流场
            int fieldId = flowFieldManager.RequestFlowFieldMultiWorld(scatterPoints);
            if (fieldId < 0)
                return;

            // 使用保持相对位置的分配算法
            var assigned = PositionPreservingAssign(entities, scatterPoints, alignedGroupCenter);

            // 记录 Debug 散点
            debugScatterPoints.Clear();
            debugScatterPoints.AddRange(scatterPoints);
            debugScatterCenter = alignedGroupCenter;
            // 半径取最大距离
            zfloat maxR = zfloat.Zero;
            foreach (var p in scatterPoints)
            {
                zfloat d = (p - alignedGroupCenter).magnitude;
                if (d > maxR) maxR = d;
            }
            debugScatterRadius = maxR;

            foreach (var e in entities)
            {
                var navigator = ComponentManager.GetComponent<FlowFieldNavigatorComponent>(e);
                // 释放旧流场
                if (navigator.CurrentFlowFieldId >= 0)
                {
                    flowFieldManager.ReleaseFlowField(navigator.CurrentFlowFieldId);
                }
                navigator.CurrentFlowFieldId = fieldId;
                navigator.HasReachedTarget = false;
                navigator.StuckFrames = 0;
                // 放宽到达半径，减少相互挤动
                zfloat minArrival = navigator.Radius * new zfloat(1, 5000); // 1.5x radius
                if (navigator.ArrivalRadius < minArrival)
                {
                    navigator.ArrivalRadius = minArrival;
                }
                navigator.NearSlowFrames = 0;
                // 重置减速状态
                navigator.IsSlowingDown = false;
                navigator.SlowDownStartTick = 0;
                navigator.SpeedBeforeSlowDown = zfloat.Zero;

                // 更新组件
                ComponentManager.AddComponent(e, navigator);

                // 设置个人目标
                var target = MoveTargetComponent.Create(assigned[e]);
                target.UserInput = userInput;
                ComponentManager.AddComponent(e, target);
            }
        }

       
        /// <summary>
        /// 生成方形分布的候选点
        /// 主要功能：
        /// 1. 在正方形区域内生成均匀分布的候选点集
        /// 2. 根据指定的行列数和最大数量限制生成点
        /// 3. 按距离中心点的远近对生成的点进行排序
        /// </summary>
        /// <param name="center">中心点位置</param>
        /// <param name="spacing">点之间的间隔距离</param>
        /// <param name="rows">生成的行数</param>
        /// <param name="cols">生成的列数</param>
        /// <param name="maxCount">生成点的最大数量</param>
        /// <returns>按距离排序的候选点列表</returns>
        private List<zVector2> GenerateSquareLattice(zVector2 center, zfloat spacing, int rows, int cols, int maxCount)
        {
            List<zVector2> pts = new List<zVector2>();
            
            // 计算起始点，使中心对齐
            zfloat startX = center.x - (cols - 1) * spacing * new zfloat(0, 5000);
            zfloat startY = center.y - (rows - 1) * spacing * new zfloat(0, 5000);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    zVector2 p = new zVector2(
                        startX + col * spacing,
                        startY + row * spacing
                    );
                    // 将点对齐到网格中心
                    map.WorldToFlow(p, out int gridX, out int gridY);
                    zVector2 alignedPoint = map.FlowToWorld(gridX, gridY);
                    pts.Add(alignedPoint);
                    
                    if (pts.Count >= maxCount)
                        goto END;
                }
            }
            
        END:
            // 按距中心排序
            pts.Sort((p1, p2) =>
            {
                zfloat d1 = (p1 - center).sqrMagnitude;
                zfloat d2 = (p2 - center).sqrMagnitude;
                if (d1 < d2) return -1;
                if (d1 > d2) return 1;
                return 0;
            });
            return pts;
        }

        private zVector2 ProjectToWalkable(zVector2 pos, int maxRing)
        {
            map.WorldToFlow(pos, out int gx, out int gy);
            if (map.IsWalkable(gx, gy))
                return map.FlowToWorld(gx, gy);

            for (int r = 1; r <= maxRing; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    int y = gy + dy;
                    int x1 = gx - r;
                    int x2 = gx + r;
                    if (map.IsWalkable(x1, y)) return map.FlowToWorld(x1, y);
                    if (map.IsWalkable(x2, y)) return map.FlowToWorld(x2, y);
                }
                for (int dx = -r + 1; dx <= r - 1; dx++)
                {
                    int x = gx + dx;
                    int y1 = gy - r;
                    int y2 = gy + r;
                    if (map.IsWalkable(x, y1)) return map.FlowToWorld(x, y1);
                    if (map.IsWalkable(x, y2)) return map.FlowToWorld(x, y2);
                }
            }
            return pos;
        }

        /// <summary>
        /// 贪心分配算法：为每个实体分配最近的散点
        /// 主要功能：
        /// 1. 遍历所有实体，为每个实体寻找最近的可用散点
        /// 2. 使用贪心策略，优先为每个实体分配距离最近的点
        /// 3. 处理点数不足的情况，复用最近的点
        /// </summary>
        /// <param name="entities">需要分配目标点的实体列表</param>
        /// <param name="points">可用的目标点列表</param>
        /// <returns>实体与目标点的映射关系</returns>
        private Dictionary<Entity, zVector2> GreedyAssign(List<Entity> entities, List<zVector2> points)
        {
            Dictionary<Entity, zVector2> result = new Dictionary<Entity, zVector2>();
            bool[] used = new bool[points.Count];
            foreach (var e in entities)
            {
                var t = ComponentManager.GetComponent<TransformComponent>(e);
                zVector2 p = new zVector2(t.Position.x, t.Position.z);
                int best = -1;
                zfloat bestDist = zfloat.Infinity;
                for (int i = 0; i < points.Count; i++)
                {
                    if (used[i]) continue;
                    zfloat d = (points[i] - p).sqrMagnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = i;
                    }
                }
                if (best >= 0)
                {
                    used[best] = true;
                    result[e] = points[best];
                }
            }
            // 兜底：若点不足，复用最近的
            for (int i = 0; i < entities.Count; i++)
            {
                if (!result.ContainsKey(entities[i]))
                {
                    result[entities[i]] = points[0];
                }
            }
            return result;
        }

        /// <summary>
        /// 保持相对位置的分配算法：根据实体间的相对位置关系，分配散点以保持编队形状
        /// 主要功能：
        /// 1. 计算实体的相对位置关系（相对于编队中心）
        /// 2. 计算散点的相对位置关系（相对于散点中心）
        /// 3. 按照实体与散点的相对位置匹配程度进行分配
        /// </summary>
        /// <param name="entities">需要分配目标点的实体列表</param>
        /// <param name="points">可用的目标点列表</param>
        /// <param name="groupCenter">编队中心位置</param>
        /// <returns>实体与目标点的映射关系</returns>
        private Dictionary<Entity, zVector2> PositionPreservingAssign(List<Entity> entities, List<zVector2> points, zVector2 groupCenter)
        {
            Dictionary<Entity, zVector2> result = new Dictionary<Entity, zVector2>();
            
            // 获取实体相对于编队中心的位置
            List<zVector2> entityOffsets = new List<zVector2>();
            foreach (var e in entities)
            {
                var t = ComponentManager.GetComponent<TransformComponent>(e);
                zVector2 p = new zVector2(t.Position.x, t.Position.z);
                entityOffsets.Add(p - groupCenter);
            }
            
            // 标记已使用的散点
            bool[] used = new bool[points.Count];
            
            // 为每个实体分配散点
            for (int i = 0; i < entities.Count; i++)
            {
                zVector2 entityOffset = entityOffsets[i];
                int bestPoint = -1;
                zfloat bestScore = zfloat.Infinity;
                
                // 寻找最适合的散点
                for (int j = 0; j < points.Count; j++)
                {
                    if (used[j]) continue;
                    
                    zVector2 pointOffset = points[j] - groupCenter;
                    // 计算相对位置差异（考虑距离和角度）
                    zfloat distanceDiff = zMathf.Abs(entityOffset.magnitude - pointOffset.magnitude);
                    zfloat angleDiff = zMathf.Abs(zMathf.Atan2(entityOffset.y, entityOffset.x) - zMathf.Atan2(pointOffset.y, pointOffset.x));
                    
                    // 角度差需要处理周期性
                    if (angleDiff > zMathf.PI)
                        angleDiff = zMathf.PI * 2 - angleDiff;
                    
                    // 综合评分（距离差异权重较大）
                    zfloat score = distanceDiff * new zfloat(2) + angleDiff;
                    
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestPoint = j;
                    }
                }
                
                // 分配最佳散点
                if (bestPoint >= 0)
                {
                    used[bestPoint] = true;
                    result[entities[i]] = points[bestPoint];
                }
            }
            
            // 兜底：若点不足，为未分配的实体分配最近的可用点
            for (int i = 0; i < entities.Count; i++)
            {
                if (!result.ContainsKey(entities[i]))
                {
                    zVector2 entityPos = new zVector2(
                        ComponentManager.GetComponent<TransformComponent>(entities[i]).Position.x,
                        ComponentManager.GetComponent<TransformComponent>(entities[i]).Position.z
                    );
                    
                    int nearestPoint = -1;
                    zfloat nearestDistance = zfloat.Infinity;
                    
                    for (int j = 0; j < points.Count; j++)
                    {
                        if (used[j]) continue;
                        
                        zfloat distance = (points[j] - entityPos).sqrMagnitude;
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestPoint = j;
                        }
                    }
                    
                    if (nearestPoint >= 0)
                    {
                        used[nearestPoint] = true;
                        result[entities[i]] = points[nearestPoint];
                    }
                    else
                    {
                        // 如果所有点都被使用，分配第一个点
                        result[entities[i]] = points[0];
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// 清除实体的移动目标
        /// 主要功能：
        /// 1. 释放实体当前占用的流场资源
        /// 2. 重置导航状态（流场ID和到达标志）
        /// 3. 移除目标组件
        /// 4. 停止RVO智能体的移动
        /// </summary>
        public void ClearMoveTarget(Entity entity, bool reachTarget = false)
        {
            var navigator = ComponentManager.GetComponent<FlowFieldNavigatorComponent>(entity);
            
            if (navigator.CurrentFlowFieldId >= 0)
            {
                flowFieldManager.ReleaseFlowField(navigator.CurrentFlowFieldId);
                navigator.CurrentFlowFieldId = -1;
                navigator.HasReachedTarget = reachTarget;
                // 重置减速状态
                navigator.IsSlowingDown = false;
                navigator.SlowDownStartTick = 0;
                navigator.SpeedBeforeSlowDown = zfloat.Zero;

                ComponentManager.AddComponent(entity, navigator);
            }

            // 移除目标组件
            ComponentManager.RemoveComponent<MoveTargetComponent>(entity);

            // 停止RVO智能体
            if (navigator.RvoAgentId >= 0)
            {
                Simulator.Instance.setAgentPrefVelocity(navigator.RvoAgentId, zVector2.zero);
            }

            zUDebug.Log($"Clear move target for entity {entity}");
        }

        /// <summary>
        /// 每帧更新逻辑，包含 RVO 障碍物更新和 RVO 避障计算
        /// 主要功能：
        /// 1. 更新 RVO 障碍物（仅在 NeedUpdateObstacles 为 true 时）
        /// 2. 执行 RVO 避障计算（每 3 帧一次，支持追帧，最多补 10 帧）
        /// 3. 监控 RVO Agent 邻居数量，检测是否达到上限
        /// </summary>
        public override void Update()
        {
            // 每帧更新格子占用（必须在最开始）
            if (stopGridManager != null && World != null)
            {
                stopGridManager.UpdateOccupancy(World);
            }

            // RVO 避障计算：每 3 帧更新一次，支持追帧场景（最多补 10 帧）
            // 使用 Tick 差值判断，确保即使发生追帧也能正确触发
            int nowTick = TimeManager.Tick;
            if (_lastRvoUpdateTick == -1)
            {
                // 首次调用，立即执行
                _lastRvoUpdateTick = nowTick;
            }
            else
            {
                // 计算距离上次更新的帧数差
                int deltaTick = nowTick - _lastRvoUpdateTick;
                
                // 限制最大追帧数量，避免性能问题
                int framesToCatchUp = Math.Min(deltaTick, MAX_CATCH_UP_FRAMES);
                
                // 如果累积了多帧未更新，按每 5 帧的频率补上所有应执行的更新（最多 10 帧）
                while (framesToCatchUp >= RVO_UPDATE_INTERVAL)
                {
                    _lastRvoUpdateTick += RVO_UPDATE_INTERVAL;
                    UpdateRVO();
                    framesToCatchUp -= RVO_UPDATE_INTERVAL;
                }
            }

            // 遍历所有带有 TransformComponent 的实体
            var transformEntities = ComponentManager.GetAllEntityIdsWith<TransformComponent>();
            foreach (var entityId in transformEntities)
            {
                var entity = new Entity(entityId);
                var transform = ComponentManager.GetComponent<TransformComponent>(entity);
                
                if (transform.LastPosition == transform.FuturePosition)
                {
                    continue;
                }

                // 检查是否有有效的 FutureTick
                if (transform.FutureTick < 5)
                {
                    // 计算插值因子 t = 当前 Tick / (FutureTick * 5)
                    transform.FutureTick += 1;
                    
                    zfloat t = new zfloat(transform.FutureTick) / new zfloat(5);
                    t = zMathf.Clamp01(t);
                    
                    // 使用 Lerp 插值计算当前位置（在 LastPosition 和 FuturePosition 之间）
                    transform.Position = zVector3.Lerp(transform.LastPosition, transform.FuturePosition, t);

                    // 使用 Lerp 插值计算当前旋转
                    transform.Rotation = zQuaternion.Lerp(transform.LastRotation, transform.FutureRotation, t);
                    
                    // 更新组件
                    ComponentManager.AddComponent(entity, transform);

                    // zUDebug.Log($"Interpolated entity {entityId} position: {transform.Position}, LastPosition: {transform.LastPosition}, FuturePosition: {transform.FuturePosition}");
                }
            }
        }

        private void UpdateRVO()
        {
            if (flowFieldManager == null)
                return;

            // 1. 更新流场管理器（处理脏流场）
            flowFieldManager.Tick();

            // 2. 更新障碍物
            if (flowFieldManager.NeedUpdateObstacles)
            {
                flowFieldManager.NeedUpdateObstacles = false;
                UpdateObstacles();
            }

            // 设置期望速度
            var navigatorEntities = ComponentManager.GetAllEntityIdsWith<FlowFieldNavigatorComponent>();
            foreach (var entityId in navigatorEntities)
            {
                CalculateAndSetEntityVelocity(entityId);
            }

            Simulator.Instance.doStep();

            // 4. 同步位置回 Transform 组件
            foreach (var entityId in navigatorEntities)
            {
                SyncEntityPositionAndRotation(entityId);
            }

            zUDebug.Log($"[RVO] Updated RVO at tick {TimeManager.Tick}, global time: {Simulator.Instance.getGlobalTime()}");
        }

        private zRandom m_random;

        // 减速相关常量
        private const int SLOW_DOWN_DURATION_TICKS = 60;  // 3秒 = 90帧 (30 FPS)
        private const float MIN_STOP_SPEED = 0.1f;        // 最小停止速度

        /// <summary>
        /// 计算并设置实体的期望移动速度
        /// 主要功能：
        /// 1. 检查导航器是否启用以及是否有移动目标
        /// 2. 从流场采样获取移动方向
        /// 3. 根据是否在目标格子内采用不同的速度计算策略：
        ///    - 在目标格子内：直接朝向目标点移动
        ///    - 在路径上：沿流场方向移动，并添加微小随机扰动防止卡住
        /// 4. 通过 RVO 系统设置实体的期望速度
        /// 5. 减速逻辑：3-5米开始减速，最多3秒后或速度小于0.1时停止
        /// </summary>
        /// <param name="entityId">实体 ID</param>
        /// <returns>如果成功设置速度返回 true，否则返回 false（未启用或无目标）</returns>
        private void CalculateAndSetEntityVelocity(int entityId)
        {
            Entity entity = new Entity(entityId);

            if (!ComponentManager.HasComponent<FlowFieldNavigatorComponent>(entity))
            {
                zUDebug.LogError($"[CalculateAndSetEntityVelocity] Entity {entityId} does not have FlowFieldNavigatorComponent");
                return;
            }

            var navigator = ComponentManager.GetComponent<FlowFieldNavigatorComponent>(entity);

            // 导航器未启用，无法进行移动计算
            if (!navigator.IsEnabled)
                return;

            // 没有移动目标时，清零速度并退出
            if (!ComponentManager.HasComponent<MoveTargetComponent>(entity))
            {
                // 停止RVO智能体
                if (navigator.RvoAgentId >= 0)
                {
                    Simulator.Instance.setAgentPrefVelocity(navigator.RvoAgentId, zVector2.zero);
                }
                return;
            }

            if (!Simulator.Instance.IsAgentNoExist(navigator.RvoAgentId))
            {
                return;
            }

            var transform = ComponentManager.GetComponent<TransformComponent>(entity);
            zVector2 currentPos = new zVector2(transform.Position.x, transform.Position.z);
            var moveTargetComponent = ComponentManager.GetComponent<MoveTargetComponent>(entity);

            zVector2 targetPos = moveTargetComponent.TargetPosition;
            zfloat distance = zVector2.Distance(currentPos, targetPos);

            // === 减速逻辑 ===
            // 检查是否需要开始减速（距离在3-5米范围内）
            bool shouldSlowDown = distance < navigator.SlowDownRadius && distance >= zfloat.FromFloat(arrivalDistanceThreshold);

            // 计算当前速度倍率
            zfloat speedMultiplier = zfloat.One;

            if (shouldSlowDown || navigator.IsSlowingDown)
            {
                // 如果刚进入减速状态，记录开始时间和速度
                if (!navigator.IsSlowingDown)
                {
                    navigator.IsSlowingDown = true;
                    navigator.SlowDownStartTick = TimeManager.Tick;
                    navigator.SpeedBeforeSlowDown = navigator.MaxSpeed;
                }

                // 计算已经减速的时间（帧数）
                int elapsedTicks = TimeManager.Tick - navigator.SlowDownStartTick;

                // 检查是否超过最大减速时间（3秒）
                if (elapsedTicks >= SLOW_DOWN_DURATION_TICKS)
                {
                    // 超过3秒，强制停止
                    ClearMoveTarget(entity, true);
                    zUDebug.Log($"[RVO] {entityId} 减速超过3秒，强制停止");
                    return;
                }

                // 计算速度衰减：线性插值从1.0到0.1
                // t = elapsedTicks / SLOW_DOWN_DURATION_TICKS (0 -> 1)
                zfloat t = new zfloat(elapsedTicks) / new zfloat(SLOW_DOWN_DURATION_TICKS);
                speedMultiplier = zMathf.Lerp(zfloat.One, new zfloat(0, 1000), t); // 从1.0衰减到0.1

                // 如果速度倍率对应的实际速度小于0.1，直接停止
                zfloat currentSpeed = navigator.MaxSpeed * speedMultiplier;
                if (currentSpeed < zfloat.FromFloat(MIN_STOP_SPEED))
                {
                    ClearMoveTarget(entity, true);
                    zUDebug.Log($"[RVO] {entityId} 速度小于{MIN_STOP_SPEED}，停止");
                    return;
                }

                // 更新组件
                ComponentManager.AddComponent(entity, navigator);
            }

            // 检查是否到达最终目标（距离小于阈值）
            if (distance < zfloat.FromFloat(arrivalDistanceThreshold))
            {
                ClearMoveTarget(entity, true);
                zUDebug.Log($"[RVO] {entityId} 已到达目标, 距离小于阈值，distance: {distance}");
                return;
            }

            // 从流场获取当前方向的移动指引
            zVector2 flowDirection = flowFieldManager.SampleDirection(navigator.CurrentFlowFieldId, currentPos);

            // 添加微小随机扰动，防止多个单位重叠或卡住
            // int angle = m_random.NextInt(0, 360);
            // zfloat dist = m_random.NextZFloat(zfloat.Zero, zfloat.FromFloat(0.001f));
            // zVector2 offsetVel = new zVector2(
            //     dist * zMathf.CosAngle(angle),
            //     dist * zMathf.SinAngle(angle));

            // 计算最终速度（应用减速倍率）
            zfloat finalSpeed = navigator.MaxSpeed * speedMultiplier;

            // 当流场方向为零时，通常是在目标格子内或者没有使用流畅寻路，直接朝目标点移动
            if (flowDirection == zVector2.zero)
            {
                // 直接朝目标移动
                zVector2 goalVector = moveTargetComponent.TargetPosition - Simulator.Instance.getAgentPosition(navigator.RvoAgentId);
                if (zMathf.AbsSq(goalVector) > zfloat.One)
                {
                    goalVector = zMathf.Normalize(goalVector);
                }

                goalVector *= finalSpeed;

                if (moveTargetComponent.LastPrefVelocity == goalVector)
                {
                    return;
                }

                moveTargetComponent.LastPrefVelocity = goalVector;
                ComponentManager.AddComponent(entity, moveTargetComponent);

                // 设置期望速度，用于目标格子内的精确移动
                Simulator.Instance.setAgentPrefVelocity(navigator.RvoAgentId, goalVector);

                zUDebug.Log($"[RVO] {entityId} 移动到目标, distance: {distance}, goalVector: {goalVector}, finalSpeed: {finalSpeed}");
            }
            else
            {
                // 沿流场方向移动
                zVector2 goalVector = flowDirection;
                goalVector = zMathf.Normalize(goalVector);
                // 应用减速后的速度
                goalVector *= finalSpeed;

                if (moveTargetComponent.LastPrefVelocity == goalVector)
                {
                    return;
                }
                moveTargetComponent.LastPrefVelocity = goalVector;
                ComponentManager.AddComponent(entity, moveTargetComponent);

                Simulator.Instance.setAgentPrefVelocity(navigator.RvoAgentId, goalVector);

                zUDebug.Log($"[RVO] {entityId} 沿流场移动, distance: {distance}, goalVector: {goalVector}, finalSpeed: {finalSpeed}");
            }
        }

        /// <summary>
        /// 同步实体的位置和旋转信息
        /// 从 RVO 模拟器获取实体最新的位置和速度方向，并更新到 TransformComponent
        /// </summary>
        /// <param name="entityId">实体 ID</param>
        private void SyncEntityPositionAndRotation(int entityId)
        {
            Entity entity = new Entity(entityId);

            if (!ComponentManager.HasComponent<FlowFieldNavigatorComponent>(entity))
            {
                zUDebug.LogError($"[SyncEntityPositionAndRotation] Entity {entityId} does not have FlowFieldNavigatorComponent");
                return;
            }

            var navigator = ComponentManager.GetComponent<FlowFieldNavigatorComponent>(entity);
            if (navigator.RvoAgentId < 0)
                return;
            
            // 判断agent存在
            if (!Simulator.Instance.IsAgentNoExist(navigator.RvoAgentId))
            {
                return;
            }

            // 从 RVO 模拟器获取位置
            zVector2 newV2Pos = Simulator.Instance.getAgentPosition(navigator.RvoAgentId);

            if (!ComponentManager.HasComponent<TransformComponent>(entity))
            {
                zUDebug.LogError("[RVO] 实体 {entityId} 没有 TransformComponent");
                return;
            }

            // 同步位置和旋转到 TransformComponent（无论是否移动都要同步位置）
            var transform = ComponentManager.GetComponent<TransformComponent>(entity);
            transform.LastPosition = transform.Position;
            transform.LastRotation = transform.Rotation;

            zVector3 newPos = new(newV2Pos.x, zfloat.Zero, newV2Pos.y);
            transform.FuturePosition = newPos;

            zUDebug.Log($"[RVO] 实体 {entityId} 位置更新: {newPos}");

            // 使用当前位置到目标位置的方向更新朝向
            // 如果位置不变，则保持当前朝向不变
            zVector3 moveDelta = newPos - transform.Position;
            moveDelta.y = zfloat.Zero;

            if (moveDelta.sqrMagnitude > zfloat.Zero)
            {
                transform.FutureRotation = zQuaternion.LookRotation(moveDelta.normalized);
            }
            // 位置未发生变化时，保持当前朝向不变

            transform.FutureTick = 0;
            ComponentManager.AddComponent(entity, transform);
        }

        /// <summary>
        /// 移除实体的导航能力
        /// </summary>
        public void RemoveNavigator(Entity entity)
        {
            var navigator = ComponentManager.GetComponent<FlowFieldNavigatorComponent>(entity);

            // 释放流场
            if (navigator.CurrentFlowFieldId >= 0)
            {
                flowFieldManager.ReleaseFlowField(navigator.CurrentFlowFieldId);
                navigator.CurrentFlowFieldId = -1;
                navigator.HasReachedTarget = false;
            }

            // 将 RVO 代理归还池并冻结
            if (navigator.RvoAgentId >= 0)
            {
                Simulator.Instance.delAgent(navigator.RvoAgentId);
                navigator.RvoAgentId = -1;
            }

            // 移除组件
            ComponentManager.RemoveComponent<FlowFieldNavigatorComponent>(entity);
            ComponentManager.RemoveComponent<MoveTargetComponent>(entity);
        }

        // 仅移除RVO
        public void RemoveRVOAgent(Entity entity)
        {
            var navigator = ComponentManager.GetComponent<FlowFieldNavigatorComponent>(entity);
            if (navigator.RvoAgentId < 0)
            {
                return;
            }

            Simulator.Instance.delAgent(navigator.RvoAgentId);
            navigator.RvoAgentId = -1;

            // 更新组件
            ComponentManager.AddComponent(entity, navigator);
        }
            

    }
}
