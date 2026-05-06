using UnityEngine;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using zUnity;
using ZLockstep.View;
using Game.Examples;
using ZLockstep.Simulation;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;
using ZFrame;
using ZLockstep.RVO;
using Unity.Mathematics;

[RequireComponent(typeof(Ra2Demo))]

/// <summary>
/// 调试可视化类，负责处理Ra2Demo中的OnGUI和OnDrawGizmos调试功能
/// </summary>
public class Ra2DemoDebugger : MonoBehaviour
{
    [Header("可视化设置")]
    [SerializeField] private bool _showUnits = false;          // 显示单位
    [SerializeField] private bool _showRVOAgents = false;      // 显示 RVO 智能体
    [SerializeField] private bool _showGrid = false;           // 显示网格
    [SerializeField] private bool _showObstacles = false;      // 显示障碍物
    [SerializeField] private bool _showFlowField = false;      // 显示流场方向

    private Ra2Demo _demo;
    
    // zTime FPS 计算相关变量
    private float _zTimeAccumulatedTime = 0f;
    private int _zTimeAccumulatedTicks = 0;
    private float _zTimeFps = 0f;

    // 矩形框
    private Rect commonRect = new(200, 450, 600, 500);
    
    // GUI 样式初始化
    private bool _initStyle = false;
    // GUI 样式缓存
    private GUIStyle _labelStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _toggleStyle;

    private float _drawHeight = 0.1f;

    private void Awake()
    {
        _demo = GetComponent<Ra2Demo>();
    }

    /// <summary>
    /// 初始化 GUI 样式
    /// </summary>
    private void InitializeGUIStyles()
    {
        // 初始化标题样式
        _titleStyle = new(GUI.skin.label)
        {
            fontSize = 32
        };
        _titleStyle.normal.textColor = Color.green;
        _titleStyle.fontStyle = FontStyle.Bold;

        // 初始化标签样式
        _labelStyle = new(GUI.skin.label)
        {
            fontSize = 24
        };
        _labelStyle.normal.textColor = Color.black;

        // 初始化开关样式
        _toggleStyle = new(GUI.skin.toggle)
        {
            fontSize = 24
        };
        _toggleStyle.normal.textColor = Color.green;
        _toggleStyle.onNormal.textColor = Color.red;
    }

    private void OnGUI()
    {
        if (!_initStyle)
        {
            _initStyle = true;
            InitializeGUIStyles();
        }
        
        // 如果 Ra2Demo 不存在或游戏尚未准备好，则不显示调试 UI
        if (_demo == null)
            return;

        var game = _demo.GetBattleGame();
        if (game == null)
            return;

        // 根据开关状态决定是否绘制
        int type = _DebugType % 6;
        if (type == 0)
        {
            return;
        }

        // 绘制矩形框
        // GUI.Box(commonRect, "");
        GUILayout.BeginArea(commonRect);

        if (type == 1)
        {
            // 仅在编辑器状态下显示调试 UI
            if (!Application.isEditor)
                return;
            DrawHelpInfo();
        }
        else if (type == 2)
        {
            DrawUnitStatisticsPanel(game);
        }
        else if (type == 3)
        {
            // 仅在编辑器状态下显示调试 UI
            if (!Application.isEditor)
                return;
            DrawDebugUI(game);
        }
        else if (type == 4)
        {
            DrawSimulationInfoPanel(game);
        }
        else if (type == 5)
        {
            DrawPathfindingInfoPanel(game);
        }

        GUILayout.EndArea();
    }

        /// <summary>
    /// 绘制帮助信息面板
    /// </summary>
    private void DrawHelpInfo()
    {
        GUILayout.Label("=== Debug Console ===", _titleStyle);
        GUILayout.Space(10);
        GUILayout.Label("press ` hotkey to switch panel:", _labelStyle);
        GUILayout.Label("  Type 0 - close", _labelStyle);
        GUILayout.Label("  Type 1 - help info", _labelStyle);
        GUILayout.Label("  Type 2 - units stat", _labelStyle);
        GUILayout.Label("  Type 3 - Gizmoz debug switch", _labelStyle);
        GUILayout.Label("  Type 4 - simulation", _labelStyle);
        GUILayout.Label("  Type 5 - pathfinding info", _labelStyle);
        GUILayout.Space(5);
    }

    private void OnDrawGizmos()
    {
        // 只在编辑器模式下或者运行时附加了该脚本的情况下工作
        if (!Application.isPlaying && _demo == null)
            return;

        var game = _demo?.GetBattleGame();

        // 显示所有已创建的单位（从逻辑层读取）
        if (_showUnits)
        {
            DrawUnits(game);
        }
        
        // 绘制RVO agents
        if (_showRVOAgents)
        {
            DrawRVOAgents(game);
        }
        
        // 绘制网格、障碍物和流场
        if (game != null && game.MapManager != null)
        {
            // 1. 绘制网格和障碍物
            if (_showGrid || _showObstacles)
            {
                DrawGridAndObstacles(game);
            }

            // 2. 绘制流场
            if (_showFlowField && game.FlowFieldManager != null)
            {
                DrawFlowFields(game);
            }
        }
    }

    /// <summary>
    /// 绘制所有单位
    /// </summary>
    private void DrawUnits(BattleGame game)
    {
        if (game != null && game.World != null)
        {
            var transforms = game.World.ComponentManager
                .GetAllEntityIdsWith<TransformComponent>();

            foreach (var entityId in transforms)
            {
                var entity = new Entity(entityId);
                var transform = game.World.ComponentManager
                    .GetComponent<TransformComponent>(entity);

                // 绘制单位位置
                Vector3 pos = transform.Position.ToVector3();
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(pos, 0.5f);

                // 如果这是选中的单位，用不同颜色标记
                // 注意：由于访问私有变量限制，这里简化处理
                // 实际应用中可能需要通过其他方式传递选中单位信息

                // 如果有移动命令，绘制目标位置
                if (game.World.ComponentManager
                    .HasComponent<MoveCommandComponent>(entity))
                {
                    var moveCmd = game.World.ComponentManager
                        .GetComponent<MoveCommandComponent>(entity);
                    
                    Vector3 targetPos = moveCmd.TargetPosition.ToVector3();
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(targetPos, 0.3f);
                    Gizmos.DrawLine(pos, targetPos);
                }
            }
        }
    }

    /// <summary>
    /// 绘制RVO agents用于调试
    /// </summary>
    private void DrawRVOAgents(BattleGame game)
    {
        if (game == null)
            return;

        var agentNoList = Simulator.Instance.GetAgentNoList();

        foreach (var agentNo in agentNoList)
        {
            zVector2 v2Pos = Simulator.Instance.getAgentPosition(agentNo);
            // 获取agent位置
            Vector3 pos = new Vector3(v2Pos.x.ToFloat(), _drawHeight, v2Pos.y.ToFloat());
            
            // 绘制agent半径（白色）
            Gizmos.color = Color.red;
            zfloat radius = Simulator.Instance.getAgentRadius(agentNo);
            Gizmos.DrawWireSphere(pos, radius.ToFloat());
            
            // 绘制agent位置（红色球体）
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pos, 0.2f);
            
            // 绘制agent实际速度（蓝色箭头）
            zVector2 v2Vel = Simulator.Instance.getAgentVelocity(agentNo);
            if (zMathf.AbsSq(v2Vel) > new zfloat(0, 10))
            {
                Vector3 velocity = new Vector3(((float)v2Vel.x), _drawHeight, ((float)v2Vel.y));
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(pos, pos + velocity);
                // 绘制箭头头部
                DrawArrowHead(pos + velocity, velocity.normalized, 0.3f);
            }

            // 绘制agent期望速度（绿色箭头）
            zVector2 v2PrefVel = Simulator.Instance.getAgentPrefVelocity(agentNo);
            if (zMathf.AbsSq(v2PrefVel) > new zfloat(0, 10))
            {
                Vector3 prefVelocity = new Vector3((float)v2PrefVel.x, _drawHeight + 0.02f, (float)v2PrefVel.y);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(pos, pos + prefVelocity);
                DrawArrowHead(pos + prefVelocity, prefVelocity.normalized, 0.25f);
            }
            
            // 绘制agent ID
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, $"ID: {agentNo}");
            #endif
        }

        // 绘制导航系统散点
        var points = game.NavSystem.DebugScatterPoints;
        if (points != null)
        {
            // 散点
            Gizmos.color = Color.cyan;
            float r = 0.5f;
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                Vector3 pos = new Vector3((float)p.x, _drawHeight, (float)p.y);
                Gizmos.DrawWireSphere(pos, r);
            }
        }

        // 获取障碍物列表，绘制障碍物
        Gizmos.color = Color.red;
        int obstacleVertexCount = Simulator.Instance.getNumObstacleVertices();
        
        for (int vertexNo = 0; vertexNo < obstacleVertexCount; ++vertexNo)
        {
            zVector2 currentVertex = Simulator.Instance.getObstacleVertex(vertexNo);
            Vector3 currentPos = new Vector3((float)currentVertex.x, _drawHeight, (float)currentVertex.y);
            
            // 获取下一个顶点，绘制连线
            int nextVertexNo = Simulator.Instance.getNextObstacleVertexNo(vertexNo);
            zVector2 nextVertex = Simulator.Instance.getObstacleVertex(nextVertexNo);
            Vector3 nextPos = new Vector3((float)nextVertex.x, _drawHeight, (float)nextVertex.y);
            
            // 绘制顶点
            Gizmos.DrawWireSphere(currentPos, 0.3f);
            
            // 绘制边
            Gizmos.DrawLine(currentPos, nextPos);
        }
    }

    /// <summary>
    /// 绘制箭头头部
    /// </summary>
    /// <param name="position">箭头尖端位置</param>
    /// <param name="direction">箭头方向</param>
    /// <param name="size">箭头大小</param>
    private void DrawArrowHead(Vector3 position, Vector3 direction, float size)
    {
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 30, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 30, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawLine(position, position + right * size);
        Gizmos.DrawLine(position, position + left * size);
    }

    /// <summary>
    /// 绘制网格和障碍物
    /// </summary>
    private void DrawGridAndObstacles(BattleGame game)
    {
        if (game == null || game.MapManager == null)
            return;

        int width = game.MapManager.GetWidth();
        int height = game.MapManager.GetHeight();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool walkable = game.MapManager.IsWalkable(x, y);
                Vector3 worldPos = new Vector3(
                    x + 0.5f,
                    _drawHeight,
                    y + 0.5f
                );

                // 绘制障碍物
                if (!walkable && _showObstacles)
                {
                    Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.7f);
                    Gizmos.DrawCube(worldPos, new Vector3(0.95f, _drawHeight, 0.95f));
                }
                // 绘制网格线
                else if (_showGrid)
                {
                    Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                    // 只绘制底部和左边线，避免重复
                    Vector3 bottomLeft = worldPos - new Vector3(0.5f, 0, 0.5f);
                    Vector3 bottomRight = bottomLeft + new Vector3(1, 0, 0);
                    Vector3 topLeft = bottomLeft + new Vector3(0, 0, 1);
                    
                    Gizmos.DrawLine(bottomLeft, bottomRight);
                    Gizmos.DrawLine(bottomLeft, topLeft);
                }
            }
        }

        // 绘制地图边界
        if (_showGrid)
        {
            Gizmos.color = Color.yellow;
            
            Vector3 corner1 = new(0, 0.02f, 0);
            Vector3 corner2 = new(width, 0.02f, 0);
            Vector3 corner3 = new(width, 0.02f, height);
            Vector3 corner4 = new(0, 0.02f, height);
            
            Gizmos.DrawLine(corner1, corner2);
            Gizmos.DrawLine(corner2, corner3);
            Gizmos.DrawLine(corner3, corner4);
            Gizmos.DrawLine(corner4, corner1);
        }
    }

    /// <summary>
    /// 绘制流场方向
    /// </summary>
    private void DrawFlowFields(BattleGame game)
    {
        if (game == null || game.FlowFieldManager == null)
            return;

        // 获取活跃的流场
        var activeFields = game.FlowFieldManager.GetActiveFields();

        if (activeFields == null)
            return;

        int flowFieldDisplayInterval = 1; // 间隔显示，避免太密集

        foreach (var field in activeFields.Values)
        {
            if (field.referenceCount <= 0)
                continue;

            int flowSize = game.MapManager.GetFlowSize();
            zVector2 targetWorldPos = game.MapManager.FlowToWorld(field.targetGridX, field.targetGridY);
            // 绘制目标点
            Vector3 targetPos = new(
                targetWorldPos.x.ToFloat(),
                _drawHeight,
                targetWorldPos.y.ToFloat()
            );
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPos, 1f);
            Gizmos.DrawSphere(targetPos, 0.3f);

            // 绘制流场箭头（间隔显示，避免太密集）
            for (int flowY = 0; flowY < field.height; flowY += flowFieldDisplayInterval)
            {
                for (int flowX = 0; flowX < field.width; flowX += flowFieldDisplayInterval)
                {
                    if (!game.MapManager.IsFlowWalkable(flowX, flowY))
                        continue;

                    int idx = field.GetIndex(flowX, flowY);
                    zVector2 direction = field.directions[idx];
                    
                    if (direction.sqrMagnitude < new zfloat(0, 100)) // 0.01
                        continue;

                    zVector2 pos = game.MapManager.FlowToWorld(flowX, flowY);
                    Vector3 worldPos = new(
                        pos.x.ToFloat(),
                        _drawHeight,
                        pos.y.ToFloat()
                    );

                    Vector3 dir = new((float)direction.x, _drawHeight, (float)direction.y);
                    
                    // 根据到目标的距离设置颜色
                    float cost = (float)field.costs[idx];
                    float maxCost = field.width + field.height;
                    float t = 1f - (cost / maxCost);
                    Gizmos.color = Color.Lerp(Color.red, Color.green, t);
                    
                    // 绘制方向箭头
                    DrawArrow(worldPos, dir * 0.8f);
                }
            }
        }
    }

    /// <summary>
    /// 绘制箭头（辅助方法）
    /// </summary>
    private void DrawArrow(Vector3 start, Vector3 direction)
    {
        if (direction.magnitude < 0.001f)
            return;

        Vector3 end = start + direction;
        Gizmos.DrawLine(start, end);
        
        // 箭头头部
        Vector3 right = Quaternion.Euler(0, 30, 0) * -direction * 0.3f;
        Vector3 left = Quaternion.Euler(0, -30, 0) * -direction * 0.3f;
        
        Gizmos.DrawLine(end, end + right);
        Gizmos.DrawLine(end, end + left);
    }

    private void Update()
    {
        // 处理快捷键输入
        HandleDebugUIHotkey();
    }



    /// <summary>
    /// 处理调试 UI 的快捷键 (` 键)
    /// </summary>
    private void HandleDebugUIHotkey()
    {
        // 只在按键按下的瞬间触发 (从 false 变为 true) 
        if (Keyboard.current.backquoteKey.wasPressedThisFrame)
        {
            _showDebugUI = !_showDebugUI;
            _DebugType += 1;
            zUDebug.Log($"[Ra2DemoDebugger] zUDebug UI 显示状态：{_showDebugUI}");
        }
    }

    private bool _showDebugUI = true; // ` 开关控制
    private int _DebugType = 0;

    public void SetDebugType(int type)
    {
        _DebugType = type;
    }

    /// <summary>
    /// 绘制调试 UI 面板
    /// </summary>
    private void DrawDebugUI(BattleGame game)
    {
        DrawDebugToggles(_toggleStyle);
    }

    /// <summary>
    /// 绘制仿真信息面板（独立面板，type==4 时显示）
    /// </summary>
    private void DrawSimulationInfoPanel(BattleGame game)
    {
        GUILayout.Label("=== simulation info ===", _titleStyle);
        GUILayout.Space(15);

        // 显示 _game.Update() 耗时统计
        GUILayout.Label($"GameUpdate Avg: {_demo.GameUpdateAvgMs:F3} ms", _labelStyle);
        GUILayout.Label($"GameUpdate Max: {_demo.GameUpdateMaxMs:F3} ms", _labelStyle);

        GUILayout.Space(10);

        if (game.World != null && game.World.TimeManager != null)
        {
            DrawSimulationInfo(game, _labelStyle);
        }
    }

    /// <summary>
    /// 绘制单位统计信息面板（独立面板，type==2 时显示）
    /// </summary>
    private void DrawUnitStatisticsPanel(BattleGame game)
    {
        GUILayout.Label("=== Unit statistics info ===", _titleStyle);
        GUILayout.Space(15);
        
        DrawUnitStatistics(game, _labelStyle);
    }

    /// <summary>
    /// 绘制路径查找信息面板（独立面板，type==5 时显示）
    /// </summary>
    private void DrawPathfindingInfoPanel(BattleGame game)
    {
        GUILayout.Label("=== Pathfinding info ===", _titleStyle);
        GUILayout.Space(15);
        
        DrawPathfindingInfo(game, _labelStyle);
    }

    /// <summary>
    /// 绘制调试开关控件
    /// </summary>
    private void DrawDebugToggles(GUIStyle toggleStyle)
    {
        GUILayout.Label("=== Gizmoz Debug Switch ===", _labelStyle);
        GUILayout.Space(15);

        _showUnits = GUILayout.Toggle(_showUnits, "Show Units", toggleStyle);
        _showRVOAgents = GUILayout.Toggle(_showRVOAgents, "Show RVO Agents", toggleStyle);
        
        _showGrid = GUILayout.Toggle(_showGrid, "Show Grids", toggleStyle);
        _showObstacles = GUILayout.Toggle(_showObstacles, "Show obstacles", toggleStyle);
        _showFlowField = GUILayout.Toggle(_showFlowField, "Show flowfields", toggleStyle);
    }

    /// <summary>
    /// 绘制调试信息标签（流场数量、RVO 智能体数量等）
    /// </summary>
    private void DrawPathfindingInfo(BattleGame game, GUIStyle labelStyle)
    {
        GUILayout.Label("=== Pathfinding info ===", labelStyle);
        GUILayout.Space(15);

        // 显示流场数量
        ShowFlowFieldInfo(game, labelStyle);
        
        GUILayout.Space(10);
        // 显示 RVO agents 数量
        int rvoAgentCount = Simulator.Instance.getNumAgents();
        GUILayout.Label($"RVO count: {rvoAgentCount}", labelStyle);
        int obstacleCount = Simulator.Instance.getNumObstacleVertices();
        GUILayout.Label($"obstacle vertices count: {obstacleCount}", labelStyle);
    }

    private void ShowFlowFieldInfo(BattleGame game, GUIStyle labelStyle)
    {
        if (game.FlowFieldManager != null)
        {
            int flowFieldCount = game.FlowFieldManager.GetActiveFieldCount();
            GUILayout.Label($"flowfields count: {flowFieldCount}", labelStyle);
            
            // 显示流场请求次数
            int totalRequestCount = game.FlowFieldManager.GetTotalRequestCount();
            GUILayout.Label($"request flowfield count: {totalRequestCount}", labelStyle);
        }
    }

    /// <summary>
    /// 绘制仿真信息（zTime Tick、时间、DeltaTime、FPS 和帧同步状态等）。
    /// </summary>
    /// <param name="game">当前运行中的战斗实例，用于读取逻辑时间和帧同步管理器。</param>
    /// <param name="labelStyle">仿真信息标签使用的 GUI 样式。</param>
    private void DrawSimulationInfo(BattleGame game, GUIStyle labelStyle)
    {
        TimeManager timeManager = game.World.TimeManager;

        // 计算 zTime 的 FPS
        _zTimeAccumulatedTime += (float)timeManager.DeltaTime;
        _zTimeAccumulatedTicks++;
        
        if (_zTimeAccumulatedTime >= 0.5f) // 每 0.5 秒更新一次 FPS 显示
        {
            _zTimeFps = _zTimeAccumulatedTicks / _zTimeAccumulatedTime;
            _zTimeAccumulatedTime = 0f;
            _zTimeAccumulatedTicks = 0;
        }
        
        GUILayout.Label($"zTime Tick: {timeManager.Tick}", labelStyle);
        GUILayout.Label($"zTime Time: {(float)timeManager.Time:F2}s", labelStyle);
        GUILayout.Label($"zTime DeltaTime: {(float)timeManager.DeltaTime:F3}s", labelStyle);
        GUILayout.Label($"zTime FPS: {_zTimeFps:F1}", labelStyle);

        if (game.FrameSyncManager != null)
        {
            GUILayout.Label($"FrameSync CurrentFrame: {game.FrameSyncManager.CurrentFrame}", labelStyle);
            GUILayout.Label($"FrameSync ConfirmedFrame: {game.FrameSyncManager.ConfirmedFrame}", labelStyle);
        }
    }

    /// <summary>
    /// 绘制单位统计信息（按阵营分组显示每种 Unit 类型的数量、金钱和能量）
    /// </summary>
    private void DrawUnitStatistics(BattleGame game, GUIStyle labelStyle)
    {
        if (game == null || game.World == null)
            return;

        // 统计每个阵营每种 UnitType 的数量
        // 外层字典：PlayerId -> 内层字典
        // 内层字典：UnitType -> 数量
        Dictionary<int, Dictionary<UnitType, int>> campUnitStats = new Dictionary<int, Dictionary<UnitType, int>>();
        
        // 统计每个阵营的金钱和能量（通过 EconomyComponent + CampComponent 获取）
        Dictionary<int, EconomyComponent> campEconomyStats = new Dictionary<int, EconomyComponent>();

        // 1. 先获取所有带 EconomyComponent 的实体（数量较少），再获取对应的 CampComponent
        var economyEntities = game.World.ComponentManager.GetAllEntityIdsWith<EconomyComponent>();
        foreach (var entityId in economyEntities)
        {
            var entity = new Entity(entityId);
            
            // 检查是否有 CampComponent 来确定阵营
            if (!game.World.ComponentManager.HasComponent<CampComponent>(entity))
                continue;
                
            var campComponent = game.World.ComponentManager.GetComponent<CampComponent>(entity);
            int campId = campComponent.CampId;
            var economyComponent = game.World.ComponentManager.GetComponent<EconomyComponent>(entity);
            
            // 如果该阵营还未在字典中，先创建
            if (!campEconomyStats.ContainsKey(campId))
            {
                campEconomyStats[campId] = economyComponent;
            }
        }
        
        // 2. 统计单位数据
        var entities = game.World.ComponentManager.GetAllEntityIdsWith<TransformComponent>();

        foreach (var entityId in entities)
        {
            var entity = new Entity(entityId);
            
            // 检查是否有 UnitComponent
            if (!game.World.ComponentManager.HasComponent<UnitComponent>(entity))
                continue;

            var unitComponent = game.World.ComponentManager.GetComponent<UnitComponent>(entity);
            UnitType unitType = unitComponent.UnitType;
            int playerId = unitComponent.PlayerId;

            if (ConfigManager.Get<ConfUnit>((int)unitType) == null)
                continue;

            // 如果该阵营还未在字典中，先创建
            if (!campUnitStats.ContainsKey(playerId))
            {
                campUnitStats[playerId] = new Dictionary<UnitType, int>();
            }

            // 统计该阵营下该单位类型的数量
            if (campUnitStats[playerId].ContainsKey(unitType))
            {
                campUnitStats[playerId][unitType]++;
            }
            else
            {
                campUnitStats[playerId][unitType] = 1;
            }
        }

        // 绘制统计信息
        GUILayout.Label("=== Units statistics ===", labelStyle);
        GUILayout.Space(15);
        
        // 按阵营遍历显示（只显示有经济数据的阵营）
        foreach (var economyEntry in campEconomyStats.OrderBy(kvp => kvp.Key))
        {
            int playerId = economyEntry.Key;
            var economy = economyEntry.Value;
            
            GUILayout.Label($"[Player {playerId}] Money: {economy.Money} | Power: {economy.Power}", labelStyle);
            
            // 如果该阵营有单位数据，显示单位列表
            if (campUnitStats.ContainsKey(playerId))
            {
                Dictionary<UnitType, int> unitTypes = campUnitStats[playerId];
                
                // 构建单位列表字符串
                List<string> unitList = new List<string>();
                foreach (var unitEntry in unitTypes)
                {
                    ConfUnit confUnit = ConfigManager.Get<ConfUnit>((int)unitEntry.Key);
                    if (confUnit != null)
                    {
                        unitList.Add($"{unitEntry.ToString()}");
                    }
                }
                
                // 显示格式：[1] 反重装步兵:28，坦克:2
                if (unitList.Count > 0)
                {
                    string unitInfo = string.Join(",", unitList);
                    GUILayout.Label($"  {unitInfo}", labelStyle);
                }
            }
            
            GUILayout.Space(5);
        }

        GUILayout.Space(10);
        // 显示总数
        int totalCount = campUnitStats.Values.Sum(dict => dict.Values.Sum());
        GUILayout.Label($"All Units: {totalCount}", labelStyle);
    }
}
