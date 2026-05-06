using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ZLockstep.View;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Sync.Command;
using ZLockstep.Sync.Command.Commands;
using zUnity;
using Game.Examples;
using ZLockstep.Sync;
using ZLockstep.View.Systems;
using ZFrame;
using System;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using ZLib;
using PostHogUnity;
// using WeChatWASM;
using Unity.Collections;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

/// <summary>
/// 测试脚本：点击地面创建单位（使用Command系统）
/// </summary>
public class Ra2Demo : MonoBehaviour
{
    [Header("游戏世界")]
    [SerializeField] private BattleGame _game;
    [SerializeField] public GameMode Mode;


    [Header("创建设置")]
    [SerializeField] private LayerMask groundLayer = -1; // 地面层


    [Header("Unity 资源")]
    [SerializeField] private Transform viewRoot;
    private PresentationSystem _presentationSystem;
    private bool _startupAssetsPreloadStarted;


    [Header("小地图设置")]
    private MiniMapController miniMapController; // 小地图控制器

    public RTSControl _controls;


    [Header("相机设置")]
    public Camera _mainCamera;
    public GameObject _cameraTarget;

    [SerializeField, Range(-10000, 0)] public float BoundaryMinX = -500f;

    [SerializeField, Range(0, 10000)] public float BoundaryMaxX = 500f;

    [SerializeField, Range(-10000, 0)] public float BoundaryMinZ = -500f;

    [SerializeField, Range(0, 10000)] public float BoundaryMaxZ = 500f;
     
    /// <summary>
    /// 设置最大选择数量
    /// </summary>
    /// <param name="count">最大选择数量，0 表示不限制</param>
    public void SetMaxSelectionCount(int count)
    {
        maxSelectionCount = Mathf.Max(0, count);
        zUDebug.Log($"[Ra2Demo] 最大选择数量已设置为：{maxSelectionCount} (0 表示不限制)");
    }
    
    /// <summary>
    /// 获取当前最大选择数量
    /// </summary>
    /// <returns>最大选择数量，0 表示不限制</returns>
    public int GetMaxSelectionCount()
    {
        return maxSelectionCount;
    }
    
    /// <summary>
    /// 设置单位选择半径
    /// </summary>
    /// <param name="radius">选择半径（米）</param>
    public void SetUnitSelectionRadius(float radius)
    {
        unitSelectionRadius = radius;
        zUDebug.Log($"[Ra2Demo] 单位选择半径已设置为：{radius}m");
    }
    
    /// <summary>
    /// 获取当前单位选择半径
    /// </summary>
    /// <returns>选择半径（米）</returns>
    public float GetUnitSelectionRadius()
    {
        return unitSelectionRadius;
    }

    /**************************************
     * UI 部分
     *************************************/

         // 相机移动相关的常量
    private const float JOYSTICK_CAMERA_MOVE_SPEED = 0.02f;  // 虚拟摇杆相机移动速度
    private const float CAMERA_MOVE_ZONE_WIDTH_RATIO = 0.33f;  // 相机移动区域宽度比例（左侧 1/3 屏幕）
    
    // 选择模式/（相机）移动模式
    public bool IsSelectMode { get; set; } = true;
    
    // 出售模式状态
    private bool isSellMode = false;
    private bool IsUnitMoveDisabledInCurrentMode => Mode == GameMode.Replay;

    // _game.Update() 性能统计
    private readonly Stopwatch _gameUpdateStopwatch = new();
    private readonly System.Collections.Generic.Queue<(float timestamp, float durationMs)> _gameUpdateSamples = new();
    private const float STATS_WINDOW_SECONDS = 1f; // 统计窗口：1秒
    private const int MAX_LOGIC_STEPS_PER_FIXED_TICK = 8;
    private const int CATCH_UP_SMALL_PENDING_THRESHOLD = 100;
    private const int CATCH_UP_MEDIUM_PENDING_THRESHOLD = 300;
    private const int CATCH_UP_LARGE_PENDING_THRESHOLD = 1000;
    private const int CATCH_UP_SMALL_FRAME_BUDGET = 10;
    private const int CATCH_UP_MEDIUM_FRAME_BUDGET = 20;
    private const int CATCH_UP_LARGE_FRAME_BUDGET = 40;
    private const int CATCH_UP_HUGE_FRAME_BUDGET = 80;
    private const int CATCH_UP_MAX_FRAME_BUDGET = 100;
    private const double CATCH_UP_MAX_MILLISECONDS_PER_FIXED_TICK = 50d;
    public float GameUpdateAvgMs { get; private set; } // 最近1秒平均耗时(ms)
    public float GameUpdateMaxMs { get; private set; } // 最近1秒最大耗时(ms)

    // 回放倍速状态
    private readonly float[] replaySpeeds = { 1f, 2f, 4f, 6f, 0.5f };
    private int replaySpeedIndex;
    private float replaySpeedAccumulator;

    [Header("追帧设置")]
    [SerializeField] private bool disablePresentationDuringCatchUp = true;
    private bool _wasInCatchUp;
    private bool _presentationDisabledForCatchUp;
    
    /// <summary>
    /// 设置出售模式
    /// </summary>
    /// <param name="sell">true: 开启出售模式，false: 关闭出售模式</param>
    public void SetSellMode(bool sell)
    {
        isSellMode = sell;
        zUDebug.Log($"[Ra2Demo] 出售模式已{(isSellMode ? "开启" : "关闭")}");
    }
    
    /// <summary>
    /// 获取当前出售模式状态
    /// </summary>
    /// <returns>出售模式状态</returns>
    public bool GetSellMode()
    {
        return isSellMode;
    }

    /// <summary>
    /// 重置回放倍速为默认档位。
    /// </summary>
    public void ResetReplaySpeed()
    {
        replaySpeedIndex = 0;
        replaySpeedAccumulator = 0f;
    }

    /// <summary>
    /// 轮换回放倍速档位（X1/X2/X4/X6/X0.5）。
    /// </summary>
    public void CycleReplaySpeed()
    {
        replaySpeedIndex = (replaySpeedIndex + 1) % replaySpeeds.Length;
    }

    /// <summary>
    /// 获取当前回放倍速。
    /// </summary>
    /// <returns>当前回放倍速值。</returns>
    public float GetReplaySpeed()
    {
        if (replaySpeedIndex < 0 || replaySpeedIndex >= replaySpeeds.Length)
        {
            return 1f;
        }

        return replaySpeeds[replaySpeedIndex];
    }


    private void Awake()
    {
        // 启用 EnhancedTouch 支持（双指缩放需要）
        EnhancedTouchSupport.Enable();

        // FIX ME: 临时解决内存泄漏
        NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;

        // 初始化命令映射
        CommandMapper.Initialize();

        _mainCamera = Camera.main;
        _controls = new RTSControl();

        // 初始化小地图系统
        InitializeMiniMap();
    }

    /// <summary>
    /// 初始化Unity视图层
    /// </summary>
    public void InitializeUnityView()
    {
        if (viewRoot == null)
            viewRoot = transform;

        // 创建表现系统
        _presentationSystem = new PresentationSystem
        {
            EnableSmoothInterpolation = true
        };

        // 初始化并注册表现系统
        _presentationSystem.Initialize(viewRoot);
        _presentationSystem.SetGame(_game);
        _game.World.SystemManager.RegisterSystem(_presentationSystem);

        zUDebug.Log("[Ra2Demo] 视图系统初始化完成");
    }

    /// <summary>
    /// 初始化小地图系统
    /// </summary>
    private void InitializeMiniMap()
    {
        if (miniMapController == null)
        {
            // 尝试查找场景中的小地图控制器
            miniMapController = FindObjectOfType<MiniMapController>();

            // 如果找不到，创建一个新的
            if (miniMapController == null)
            {
                GameObject miniMapObject = new GameObject("MiniMapController");
                miniMapController = miniMapObject.AddComponent<MiniMapController>();
            }
        }
    }

    /// <summary>
    /// 获取小地图渲染纹理
    /// </summary>
    /// <returns>小地图渲染纹理</returns>
    public RenderTexture GetMiniMapTexture()
    {
        return miniMapController.GetMiniMapTexture();
    }

    private Frame frame;

    private void Start()
    {
        PostHog.Setup(
            new PostHogConfig
            {
                ApiKey = "phc_MCpygzl60lEcEwRKwIo2H7D5iVu6vtB7kHrtJCMgvEw",
                Host = "https://us.i.posthog.com",
                LogLevel = PostHogLogLevel.Info, // Set to Warning or Error in production
            }
        );

        // Capture a simple event
        PostHog.Capture("app_started");

        // Capture an event with properties
        PostHog.Capture(
            "level_started",
            new Dictionary<string, object> { { "level_id", 1 }, { "difficulty", "normal" } }
        );

        frame = new Frame();

        NetworkManager.Instance.SetRa2Demo(this);

        DiscoverTools.Discover(typeof(Main).Assembly);
        Frame.DispatchEvent(new Ra2StartUpEvent(this));
    }

    private async UniTask PreloadStartupAssetsAsync()
    {
        try
        {
            await AssetManager.PreloadByLabelAsync<GameObject>("Building");
            await AssetManager.PreloadByLabelAsync<GameObject>("Unit");
            zUDebug.LogInfo("[Ra2Demo] Startup assets preloaded.");
        }
        catch (Exception ex)
        {
            zUDebug.LogError($"[Ra2Demo] Failed to preload startup assets: {ex.Message}");
        }
    }

    private void TickStartupPreload()
    {
        if (_startupAssetsPreloadStarted)
        {
            return;
        }

        _startupAssetsPreloadStarted = true;
        PreloadStartupAssetsAsync().Forget();
    }

    public BattleGame GetBattleGame()
    {
        return _game;
    }

    private void OnEnable()
    {
        _controls.Create.Enable();
        _controls.Create.Press.performed += OnPress;
        _controls.Create.Drag.performed += OnDrag;
        _controls.Create.Release.performed += OnRelease;

        // 启用相机缩放
        _controls.Camera.Enable();
        _controls.Camera.Zoom.performed += OnZoom;
    }

    private void OnDisable()
    {
        _controls.Create.Disable();
        _controls.Create.Press.performed -= OnPress;
        _controls.Create.Drag.performed -= OnDrag;
        _controls.Create.Release.performed -= OnRelease;

        // 禁用相机缩放
        _controls.Camera.Zoom.performed -= OnZoom;
        _controls.Camera.Disable();
    }

    /// <summary>
    /// 鼠标滚轮缩放回调
    /// </summary>
    private void OnZoom(InputAction.CallbackContext context)
    {
        Vector2 scroll = context.ReadValue<Vector2>();
        if (Mathf.Abs(scroll.y) < 0.01f)
            return;

        const float WHEEL_ZOOM_SPEED = 0.001f;
        float delta = -scroll.y * WHEEL_ZOOM_SPEED;
        float newSize = _mainCamera.orthographicSize * (1f + delta);
        _mainCamera.orthographicSize = Mathf.Clamp(newSize, MIN_ORTHOGRAPHIC_SIZE, MAX_ORTHOGRAPHIC_SIZE);
    }

    // 操作状态枚举
    private enum InputActionState
    {
        None,
        PressStarted,      // 按下开始
        Dragging,          // 拖拽中
        ReleasePerformed   // 释放执行
    }

    // 输入操作状态
    private Vector2 pressStartPosition; // 按下时的起始位置
    private Vector2 currentPosition; // 当前位置
    private Vector2 dragStartScreenPos; // 拖拽起始屏幕位置（用于绘制虚线）
    private Vector2 dragCurrentScreenPos; // 拖拽当前屏幕位置（用于绘制虚线）
    
    // 输入事件队列 - 解决同一帧内多个输入事件的顺序处理问题
    private System.Collections.Generic.Queue<InputEvent> inputEventQueue = new System.Collections.Generic.Queue<InputEvent>();
    
    // 输入事件结构
    private struct InputEvent
    {
        public InputActionState State;
        public Vector2 Position;
        public float Timestamp;
    }
    
    // 单位移动模式相关字段
    private bool isUnitMoveMode = false; // 是否处于单位移动模式
    private float unitSelectionRadius = 8f; // 单位选择半径（米）
    private float tapSelectRadius = 3f; // 单击时点击的半径（米）
    
    // 相机移动相关字段
    private Vector3 _targetInitialPosition = Vector3.zero; // 相机初始位置
    private bool isCameraMoving = false; // 是否正在移动相机

    // 双指缩放相关字段
    private bool isPinching = false; // 是否正在双指缩放
    private float lastPinchDistance = 0f; // 上一帧双指距离
    private int pinchCooldownFrames = 0; // 缩放结束后的冷却帧数
    private const int PINCH_COOLDOWN = 1; // 冷却帧数，防止双指切换时误触发单指操作

    // 相机缩放限制
    private const float MIN_ORTHOGRAPHIC_SIZE = 10f;  // 最小缩放
    private const float MAX_ORTHOGRAPHIC_SIZE = 30f; // 最大缩放
    private const float PINCH_DISTANCE_EPSILON = 0.1f;
    private const float PINCH_ZOOM_SPEED = 0.01f;    // 缩放灵敏度
    
    // 选择相关字段
    private readonly List<int> selectedEntityIds = new(); // 多选单位列表
    [Header("选择限制")]
    [Tooltip("最大可选择单位数量，0 表示不限制")]
    [SerializeField] private int maxSelectionCount = 0; // 默认为 0，不限制数量
    
    // 虚线绘制相关字段
    private bool shouldDrawDragLine = false; // 是否应该绘制拖拽虚线
    
    // 选择框资源管理字段
    private GameObject selectionBoxInstance = null; // 选择框资源实例
    private const string SELECTION_BOX_PREFAB_PATH = "SelectCircle"; // 选择框预制体路径（需要根据实际资源调整）
    
    // 淡出效果相关字段
    private bool isFadingOut = false; // 是否正在淡出
    private float fadeOutStartTime = 0f; // 淡出开始时间
    private const float FADE_OUT_DURATION = 0.3f; // 淡出持续时间（秒）
    
    private void OnPress(InputAction.CallbackContext context)
    {
        // 将事件加入队列而非直接设置状态
        var pressEvent = new InputEvent
        {
            State = InputActionState.PressStarted,
            Position = GetCurrentInputPosition(),
            Timestamp = Time.time
        };
        inputEventQueue.Enqueue(pressEvent);

        zUDebug.Log($"[Ra2Demo] OnPress - 添加事件： {pressEvent}");
    }

    private void OnDrag(InputAction.CallbackContext context)
    {
        // 将事件加入队列而非直接设置状态
        inputEventQueue.Enqueue(new InputEvent
        {
            State = InputActionState.Dragging,
            Position = GetCurrentInputPosition(),
            Timestamp = Time.time
        });
    }

    private void OnRelease(InputAction.CallbackContext context)
    {
        var releaseEvent = new InputEvent
        {
            State = InputActionState.ReleasePerformed,
            Position = GetCurrentInputPosition(),
            Timestamp = Time.time
        };
        // 将事件加入队列而非直接设置状态
        inputEventQueue.Enqueue(releaseEvent);

        zUDebug.Log($"[Ra2Demo] OnRelease - 添加事件：{releaseEvent}");
    }

    /// <summary>
    /// 在 Update中处理输入逻辑 - 按队列顺序处理所有事件
    /// </summary>
    private void ProcessInputLogic()
    {
        // 按顺序处理队列中的所有事件
        while (inputEventQueue.Count > 0)
        {
            var inputEvent = inputEventQueue.Dequeue();
            
            // zUDebug.Log($"[Ra2Demo] ProcessInputLogic - 处理事件：{inputEvent.State}, 位置：{inputEvent.Position}");
            
            switch (inputEvent.State)
            {
                case InputActionState.PressStarted:
                    pressStartPosition = inputEvent.Position;
                    currentPosition = inputEvent.Position;
                    dragStartScreenPos = inputEvent.Position;
                    shouldDrawDragLine = false;
                    HandlePressLogic();
                    break;
                    
                case InputActionState.Dragging:
                    currentPosition = inputEvent.Position;
                    dragCurrentScreenPos = inputEvent.Position;
                    shouldDrawDragLine = true;
                    HandleDragLogic();
                    break;
                    
                case InputActionState.ReleasePerformed:
                    currentPosition = inputEvent.Position;
                    shouldDrawDragLine = false;
                    HandleReleaseLogic();
                    break;
            }
        }
    }

    /// <summary>
    /// 处理按下逻辑
    /// </summary>
    private void HandlePressLogic()
    {
        zUDebug.Log($"[Ra2Demo] HandlePressLogic - 尝试处理点击");

        if (EventSystem.current.IsPointerOverGameObject()) {
            // 方法 2：精确获取被点击的 UI GameObject
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue() // 新输入系统获取鼠标位置
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                zUDebug.Log($"[zUDebug] 真正被点击的 UI 对象: {results[0].gameObject.name}");
                // results[0] 是最上层的 UI 元素
            }

            return;
        }

        // 尝试获取点击位置的世界坐标
        Vector3 worldPosition = Vector3.zero;
        bool hasGroundPosition = TryGetGroundPosition(pressStartPosition, out worldPosition);

        // 射线探测是否命中建筑（仅在出售模式下检测）
        if (isSellMode)
        {
            Entity clickedBuildingEntity = GetClickedBuildingEntity(worldPosition);
            if (clickedBuildingEntity.Id != -1)
            {
                // 检查是否包含 LocalPlayerComponent（本地玩家建筑）
                if (_game.World.ComponentManager.HasComponent<LocalPlayerComponent>(clickedBuildingEntity))
                {
                    zUDebug.Log($"[建筑检测 - 本地玩家] 这是本地玩家的建筑，可以出售");
                    
                    // 获取建筑组件以获取建筑类型
                    var buildingComp = _game.World.ComponentManager.GetComponent<BuildingComponent>(clickedBuildingEntity);
                    
                    // 触发确认售卖建筑事件
                    Frame.DispatchEvent(new ConfirmSellBuildingEvent(clickedBuildingEntity.Id));
                }
                else
                {
                    zUDebug.Log($"[建筑检测 - 非本地玩家] 这不是本地玩家的建筑");
                }

                // 检测是否点击了单位，不再继续执行下面逻辑
                return;
            }
        }


        bool hasUnitInRange = false;
        if (hasGroundPosition && _game != null && _game.World != null && IsUnitMoveDisabledInCurrentMode == false)
        {
            var entities = _game.World.ComponentManager
                .GetAllEntityIdsWith<TransformComponent>();

            foreach (var entityId in entities)
            {
                var entity = new Entity(entityId);
                
                // 检查实体是否包含 LocalPlayerComponent（只有本地玩家单位才能被选择）
                if (!_game.World.ComponentManager.HasComponent<LocalPlayerComponent>(entity))
                {
                    continue;
                }

                if (!_game.World.ComponentManager.HasComponent<UnitComponent>(entity))
                {
                    continue; // 跳过非单位实体
                }

                var transform = _game.World.ComponentManager
                    .GetComponent<TransformComponent>(entity);

                // 计算与点击位置的距离
                Vector3 unitWorldPosition = transform.Position.ToVector3();
                float distance = Vector3.Distance(unitWorldPosition, worldPosition);

                if (distance <= tapSelectRadius)
                {
                    hasUnitInRange = true;
                    break;
                }
            }
        }

        if (hasUnitInRange)
        {
            // 清空之前的选择
            ClearAllOutlines();
            
            // 收集所有范围内的单位及其距离
            List<(int entityId, float distance)> unitsInRange = new List<(int, float)>();
            
            var entities = _game.World.ComponentManager
                .GetAllEntityIdsWith<TransformComponent>();

            foreach (var entityId in entities)
            {
                var entity = new Entity(entityId);
                
                // 检查实体是否包含 LocalPlayerComponent（只有本地玩家单位才能被选择）
                if (!_game.World.ComponentManager.HasComponent<LocalPlayerComponent>(entity))
                {
                    continue;
                }

                if (!_game.World.ComponentManager.HasComponent<UnitComponent>(entity))
                {
                    continue; // 跳过非单位实体
                }

                var transform = _game.World.ComponentManager
                    .GetComponent<TransformComponent>(entity);

                // 计算与点击位置的距离
                Vector3 unitWorldPosition = transform.Position.ToVector3();
                float distance = Vector3.Distance(unitWorldPosition, worldPosition);

                if (distance <= unitSelectionRadius)
                {
                    unitsInRange.Add((entityId, distance));
                }
            }
            
            // 按距离排序，选择最近的单位
            unitsInRange.Sort((a, b) => a.distance.CompareTo(b.distance));
            
            // 根据最大选择数量限制，选择最近的单位
            int selectCount = maxSelectionCount > 0 
                ? Mathf.Min(unitsInRange.Count, maxSelectionCount) 
                : unitsInRange.Count;
            
            for (int i = 0; i < selectCount; i++)
            {
                int entityId = unitsInRange[i].entityId;
                
                // 添加到选中列表
                selectedEntityIds.Add(entityId);
                
                // 启用轮廓显示
                EnableOutlineForEntity(entityId);
                
                zUDebug.Log($"[Ra2Demo] 选中单位：EntityId={entityId}, Distance={unitsInRange[i].distance:F2}m");
            }
            
            zUDebug.Log($"[Ra2Demo] 共检测到 {unitsInRange.Count} 个单位在范围内，实际选择 {selectCount} 个单位");
        }

        // 根据是否有单位在范围内决定模式
        if (!IsUnitMoveDisabledInCurrentMode && hasUnitInRange && selectedEntityIds.Count > 0)
        {
            // 进入单位移动模式
            isUnitMoveMode = true;
            isCameraMoving = false;
            
            // 创建并显示选择框资源实例，传入世界坐标位置
            CreateAndShowSelectionBox(worldPosition);
            
            zUDebug.Log($"[Ra2Demo] >>> 进入单位移动模式，点击位置：{worldPosition}, 选中单位数：{selectedEntityIds.Count}");
        }
        else
        {
            // 进入相机移动模式（不再判断 1/4 屏幕范围）
            isUnitMoveMode = false;
            isCameraMoving = true;
            
            // 记录相机初始位置
            _targetInitialPosition = _cameraTarget.transform.position;
            
            zUDebug.Log($"[Ra2Demo] >>> 进入相机移动模式，起始位置：{pressStartPosition}, 相机初始位置：{_targetInitialPosition}");
        }
    }

    /// <summary>
    /// 处理拖拽逻辑
    /// </summary>
    private void HandleDragLogic()
    {
        if (EventSystem.current.IsPointerOverGameObject()) {
            return;
        }

        // 如果是单位移动模式
        if (isUnitMoveMode)
        {
            // 启用虚线绘制标志
            shouldDrawDragLine = true;
            // zUDebug.Log($"[Ra2Demo] HandleDragLogic [单位移动模式] - 忽略拖拽，等待释放");
        }
        // 如果是相机移动模式
        else if (isCameraMoving)
        {
            // 直接移动相机，不需要阈值判断
            MoveCameraByDrag(currentPosition);
        }
    }

    /// <summary>
    /// 处理释放逻辑
    /// </summary>
    private void HandleReleaseLogic()
    {
        zUDebug.Log($"[Ra2Demo] HandleReleaseLogic - StartPos: {pressStartPosition}, EndPos: {currentPosition}");

        // 检查是否之前处于按下状态（即发生了拖拽）
        float totalDragDistance = Vector2.Distance(pressStartPosition, currentPosition);
        
        // 如果是单位移动模式
        if (isUnitMoveMode)
        {
            zUDebug.Log($"[Ra2Demo] >>> 单位移动模式结束 - StartPos: {pressStartPosition}, EndPos: {currentPosition}");
            
            // 隐藏选择框实例
            HideSelectionBox();

            if (IsUnitMoveDisabledInCurrentMode)
            {
                zUDebug.Log("[Ra2Demo] 当前为回放模式，跳过单位移动命令发送");
                ClearAllOutlines();
                isUnitMoveMode = false;
                shouldDrawDragLine = false;
                return;
            }
            
            // 获取目标位置并发送移动命令
            if (TryGetGroundPosition(currentPosition, out Vector3 targetWorldPosition))
            {
                zUDebug.Log($"[Ra2Demo] >>> 目标位置：{targetWorldPosition}");
                
                // 使用当前选中的单位列表发送移动命令
                if (selectedEntityIds.Count > 0)
                {
                    SendMoveCommand(selectedEntityIds, targetWorldPosition);
                }
                else
                {
                    zUDebug.LogWarning("[Ra2Demo] >>> 没有找到可移动的单位");
                }

                String entityIds = "";
                foreach (var entityId in selectedEntityIds)
                {
                    entityIds += entityId + ",";
                }

                // TODO PostHog 记录移动，包括 currentPosition 和 targetWorldPosition
                PostHog.Capture("move_to_target", new Dictionary<string, object>()
                {
                    {"entityIds", entityIds},
                    {"pressStartPosition", pressStartPosition},
                    {"targetPosition", currentPosition},
                });
            }
            
            // 清空选中单位列表
            ClearAllOutlines();
            
            // 重置单位移动模式
            isUnitMoveMode = false;
        }
        // 如果是相机移动模式
        else if (isCameraMoving)
        {
            zUDebug.Log($"[Ra2Demo] >>> 相机移动结束 - StartPos: {pressStartPosition}, EndPos: {currentPosition}, TotalDistance: {totalDragDistance:F2}");
            // 重置相机移动状态
            isCameraMoving = false;
        }
    }

    /// <summary>
    /// 根据拖拽偏移移动相机
    /// </summary>
    /// <param name="currentScreenPos">当前屏幕位置</param>
    private void MoveCameraByDrag(Vector2 currentScreenPos)
    {
        Vector2 screenDelta = currentScreenPos - pressStartPosition;

        // 正交相机：orthographicSize = 视口高度的一半（世界单位）
        // 每像素对应的世界单位 = (视口高度) / (屏幕像素高度)
        float worldUnitsPerPixel = _mainCamera.orthographicSize * 2f / Screen.height;

        Vector3 worldDelta = new Vector3(-screenDelta.x * worldUnitsPerPixel, 0, -screenDelta.y * worldUnitsPerPixel);
        _cameraTarget.transform.position = _targetInitialPosition + worldDelta;
    }

    private Vector2 GetCurrentInputPosition()
    {
        // 优先级获取：先尝试鼠标，再尝试触摸[1,2](@ref)
        if (Mouse.current != null && Mouse.current.position.IsActuated())
        {
            return Mouse.current.position.ReadValue();
        }
        
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.IsActuated())
        {
            var touch = Touchscreen.current.primaryTouch;
            return touch.position.ReadValue();
        }
        
        // 备用方案：如果上述都失败，使用最后已知位置
        return currentPosition;
    }

    /// <summary>
    /// 通过射线检测获取地面点击位置
    /// </summary>
    private bool TryGetGroundPosition(Vector2 screenPosition, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (_mainCamera == null)
        {
            zUDebug.LogWarning("[Test] 找不到主相机！");
            return false;
        }

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

        // 尝试射线检测地面
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            worldPosition = hit.point;
            return true;
        }

        // 如果没有地面碰撞体，使用 Y=0 平面
        if (TryRaycastPlane(ray, Vector3.zero, Vector3.up, out Vector3 planeHit))
        {
            worldPosition = planeHit;
            return true;
        }

        zUDebug.LogWarning("[Test] 无法获取点击位置！请确保有地面碰撞体或使用默认平面。");
        return false;
    }

    /// <summary>
    /// 射线与平面相交检测
    /// </summary>
    private bool TryRaycastPlane(Ray ray, Vector3 planePoint, Vector3 planeNormal, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;

        float denominator = Vector3.Dot(planeNormal, ray.direction);
        if (Mathf.Abs(denominator) < 0.0001f)
            return false; // 射线与平面平行

        float t = Vector3.Dot(planePoint - ray.origin, planeNormal) / denominator;
        if (t < 0)
            return false; // 射线朝反方向

        hitPoint = ray.origin + ray.direction * t;
        return true;
    }

    /// <summary>
    /// LateUpdate - 在每帧的 LateUpdate 中更新血量条位置
    /// </summary>
    private void LateUpdate()
    {
        // 优先检测双指缩放
        if (HandlePinchZoom())
        {
            return;
        }

        // 处理输入逻辑（在 Update中执行，而非输入回调中）
        ProcessInputLogic();
        // 触发 HealthPanel 的 LateUpdate 事件
        Frame.DispatchEvent(new HealthPanelLateUpdateEvent());
        // 主相机跟随目标
        MainCameraFollow();
    }

    private void ResetTouchInputState()
    {
        isPinching = false;
        lastPinchDistance = 0f;
        pinchCooldownFrames = 0;
        isCameraMoving = false;
        isUnitMoveMode = false;
        shouldDrawDragLine = false;
        inputEventQueue.Clear();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            ResetTouchInputState();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            ResetTouchInputState();
        }
    }

    private void MainCameraFollow()
    {
        Vector3 position = _cameraTarget.transform.position;

        float halfHeight = _mainCamera.orthographicSize * 1.41f;
        float halfWidth = _mainCamera.orthographicSize * _mainCamera.aspect;

        if (halfWidth * 2 > BoundaryMaxX - BoundaryMinX)
        {
            halfWidth = (BoundaryMaxX - BoundaryMinX) / 2;
        }
        if (halfHeight * 2 > BoundaryMaxZ - BoundaryMinZ)
        {
            halfHeight = (BoundaryMaxZ - BoundaryMinZ) / 2;
        }

        position.x = Mathf.Clamp(position.x, BoundaryMinX + halfWidth, BoundaryMaxX - halfWidth);
        position.z = Mathf.Clamp(position.z, BoundaryMinZ + halfHeight, BoundaryMaxZ - halfHeight);
        // 更新相机跟随目标位置
        _cameraTarget.transform.position = position;

        // 调整相机高度和z轴偏移
        Vector3 cameraPosition = new(position.x, _mainCamera.transform.position.y, position.z - 50);

        _mainCamera.transform.position = cameraPosition;
    }

    /// <summary>
    /// 固定帧驱动游戏逻辑更新，按当前模式选择普通单步、回放倍速或网络追帧推进方式。
    /// </summary>
    private void FixedUpdate()
    {
        // 每帧开始先处理网络消息
        NetworkManager.Instance.DispatchMessageQueues();

        if (_game == null)
        {
            return;
        }

        // 精确计时 _game.Update()
        _gameUpdateStopwatch.Restart();

        if (_game.IsCatchingUp)
        {
            replaySpeedAccumulator = 0f;
            BeginCatchUpPresentationPause();
            ExecuteCatchUpLogicSteps();
        }
        else
        {
            CompleteCatchUpPresentationSync();
            int logicSteps = 1;

            if (Mode == GameMode.Replay)
            {
                replaySpeedAccumulator += GetReplaySpeed();
                logicSteps = Mathf.FloorToInt(replaySpeedAccumulator);
                if (logicSteps > MAX_LOGIC_STEPS_PER_FIXED_TICK)
                {
                    logicSteps = MAX_LOGIC_STEPS_PER_FIXED_TICK;
                }

                replaySpeedAccumulator -= logicSteps;
            }
            else
            {
                replaySpeedAccumulator = 0f;
            }

            for (int i = 0; i < logicSteps; i++)
            {
                _game.Update();
            }
        }

        if (_wasInCatchUp && !_game.IsCatchingUp)
        {
            CompleteCatchUpPresentationSync();
        }

        _gameUpdateStopwatch.Stop();
        RefreshGameUpdateStats();
    }

    /// <summary>
    /// 表现系统：插值更新
    /// </summary>
    private void Update()
    {
        TickStartupPreload();

        bool isCatchingUp = _game != null && _game.IsCatchingUp;
        if (!isCatchingUp)
        {
            if (_wasInCatchUp)
            {
                CompleteCatchUpPresentationSync();
            }

            // 表现系统：插值更新
            _presentationSystem?.LerpUpdate(Time.deltaTime);
        }

        // 更新淡出效果
        UpdateFadeOut();
    }

    /// <summary>
    /// 执行网络追帧期间的逻辑帧推进，在单个 Unity 固定帧内按上限连续调用游戏逻辑，直到追上确认帧或达到本帧预算。
    /// </summary>
    private void ExecuteCatchUpLogicSteps()
    {
        int pendingFrames = GetPendingCatchUpFrameCount();
        int maxSteps = CalculateCatchUpFrameBudget(pendingFrames);
        int executedFrames = 0;
        Stopwatch catchUpStopwatch = Stopwatch.StartNew();

        while (_game != null && _game.IsCatchingUp && executedFrames < maxSteps)
        {
            _game.Update();
            executedFrames++;

            if (executedFrames > 0 && catchUpStopwatch.Elapsed.TotalMilliseconds >= CATCH_UP_MAX_MILLISECONDS_PER_FIXED_TICK)
            {
                break;
            }
        }

        if (executedFrames > 5)
        {
            int remainingFrames = GetPendingCatchUpFrameCount();
            zUDebug.LogInfo($"[Ra2Demo] 追帧中，本次执行 {executedFrames} 帧，剩余 {remainingFrames} 帧，预算 {maxSteps} 帧，进度: {_game.GetCatchUpProgress():P0}");
        }
    }

    /// <summary>
    /// 根据当前待追帧数计算本次固定帧允许执行的逻辑帧预算，积压越多预算越高，并始终受硬上限保护。
    /// </summary>
    /// <param name="pendingFrames">当前帧同步系统中尚未执行的确认帧数量。</param>
    /// <returns>本次固定帧最多允许执行的追帧逻辑帧数。</returns>
    private int CalculateCatchUpFrameBudget(int pendingFrames)
    {
        int budget;
        if (pendingFrames < CATCH_UP_SMALL_PENDING_THRESHOLD)
        {
            budget = CATCH_UP_SMALL_FRAME_BUDGET;
        }
        else if (pendingFrames < CATCH_UP_MEDIUM_PENDING_THRESHOLD)
        {
            budget = CATCH_UP_MEDIUM_FRAME_BUDGET;
        }
        else if (pendingFrames < CATCH_UP_LARGE_PENDING_THRESHOLD)
        {
            budget = CATCH_UP_LARGE_FRAME_BUDGET;
        }
        else
        {
            budget = CATCH_UP_HUGE_FRAME_BUDGET;
        }

        return Mathf.Clamp(budget, 1, CATCH_UP_MAX_FRAME_BUDGET);
    }

    /// <summary>
    /// 读取网络追帧当前剩余的待执行帧数，供动态预算和日志展示使用。
    /// </summary>
    /// <returns>当前仍需要追赶的确认帧数量；无法读取时返回 0。</returns>
    private int GetPendingCatchUpFrameCount()
    {
        return _game?.FrameSyncManager?.GetPendingFrameCount() ?? 0;
    }

    /// <summary>
    /// 进入追帧表现暂停状态，在追帧期间关闭表现系统更新，避免逻辑快速推进时视图插值产生卡顿或中间态闪烁。
    /// </summary>
    private void BeginCatchUpPresentationPause()
    {
        _wasInCatchUp = true;

        if (!disablePresentationDuringCatchUp || _presentationSystem == null || _presentationDisabledForCatchUp)
        {
            return;
        }

        _presentationSystem.Enabled = false;
        _presentationDisabledForCatchUp = true;
    }

    /// <summary>
    /// 完成追帧后的表现同步，恢复被追帧关闭的表现系统，并将全部实体视图重新对齐到最新逻辑状态。
    /// </summary>
    private void CompleteCatchUpPresentationSync()
    {
        if (!_wasInCatchUp)
        {
            return;
        }

        _wasInCatchUp = false;

        if (_presentationSystem == null)
        {
            _presentationDisabledForCatchUp = false;
            return;
        }

        if (_presentationDisabledForCatchUp)
        {
            _presentationSystem.Enabled = true;
            _presentationDisabledForCatchUp = false;
        }

        _presentationSystem.ResyncAllEntities();
        zUDebug.Log($"[Ra2Demo] 追帧完成，当前帧: {_game?.World?.Tick ?? 0}，表现层已重新同步");
    }

    /// <summary>
    /// 刷新游戏逻辑更新耗时统计，维护最近一秒内的平均耗时和最大耗时，供调试面板观察性能变化。
    /// </summary>
    private void RefreshGameUpdateStats()
    {
        float now = Time.time;
        float durationMs = (float)_gameUpdateStopwatch.Elapsed.TotalMilliseconds;
        _gameUpdateSamples.Enqueue((now, durationMs));

        float cutoffTime = now - STATS_WINDOW_SECONDS;
        float totalMs = 0f;
        float maxMs = 0f;
        int count = 0;

        while (_gameUpdateSamples.Count > 0 && _gameUpdateSamples.Peek().timestamp < cutoffTime)
        {
            _gameUpdateSamples.Dequeue();
        }

        foreach (var sample in _gameUpdateSamples)
        {
            totalMs += sample.durationMs;
            if (sample.durationMs > maxMs)
            {
                maxMs = sample.durationMs;
            }

            count++;
        }

        GameUpdateAvgMs = count > 0 ? totalMs / count : 0f;
        GameUpdateMaxMs = maxMs;
    }

    /// <summary>
    /// 处理双指缩放
    /// </summary>
    /// <returns>true 表示正在缩放，应跳过单指处理</returns>
    private bool HandlePinchZoom()
    {
        // 检查触摸设备是否存在
        if (Touchscreen.current == null)
            return false;

        var touches = Touchscreen.current.touches;

        // 收集当前按下的触摸点（严格按压判定，避免 phase 误判）
        List<TouchControl> activeTouches = new();
        foreach (var touch in touches)
        {
            if (touch.press.isPressed)
            {
                activeTouches.Add(touch);
                if (activeTouches.Count >= 2)
                    break;
            }
        }

        // 不是双指触摸
        if (activeTouches.Count < 2)
        {
            if (isPinching)
            {
                // 不立即重置，启动冷却计数
                isPinching = false;
                lastPinchDistance = 0f;
                pinchCooldownFrames = PINCH_COOLDOWN;
                isCameraMoving = false;
                isUnitMoveMode = false;
                shouldDrawDragLine = false;
                inputEventQueue.Clear();
                zUDebug.Log("[Ra2Demo] 双指缩放结束，进入冷却");
            }

            // 冷却期间仍阻止单指操作，防止误触
            if (pinchCooldownFrames > 0)
            {
                pinchCooldownFrames--;
                return true;
            }

            return false;
        }

        // 双指触摸存在，重置冷却
        pinchCooldownFrames = 0;

        // 计算双指距离
        Vector2 firstPosition = activeTouches[0].position.ReadValue();
        Vector2 secondPosition = activeTouches[1].position.ReadValue();
        float currentDistance = Vector2.Distance(firstPosition, secondPosition);

        // 开始双指缩放
        if (!isPinching)
        {
            isPinching = true;
            lastPinchDistance = currentDistance;
            isCameraMoving = false;
            isUnitMoveMode = false;
            shouldDrawDragLine = false;
            inputEventQueue.Clear();
            zUDebug.Log("[Ra2Demo] 双指缩放开始");
            return true;
        }

        // 计算缩放比例
        bool touchesMoved =
            activeTouches[0].delta.ReadValue().sqrMagnitude > 0f ||
            activeTouches[1].delta.ReadValue().sqrMagnitude > 0f;
        if (!touchesMoved)
        {
            lastPinchDistance = currentDistance;
            return true;
        }

        float distanceDelta = currentDistance - lastPinchDistance;
        if (Mathf.Abs(distanceDelta) <= PINCH_DISTANCE_EPSILON)
        {
            lastPinchDistance = currentDistance;
            return true;
        }

        if (lastPinchDistance > 0 && currentDistance > 0)
        {
            float scaleFactor = lastPinchDistance / currentDistance;
            float newSize = _mainCamera.orthographicSize * scaleFactor * (1f + PINCH_ZOOM_SPEED);
            _mainCamera.orthographicSize = Mathf.Clamp(newSize, MIN_ORTHOGRAPHIC_SIZE, MAX_ORTHOGRAPHIC_SIZE);
        }

        lastPinchDistance = currentDistance;
        return true;
    }

    /// <summary>
    /// 发送移动命令给指定的单位
    /// </summary>
    private void SendMoveCommand(List<int> entityIds, Vector3 targetWorldPosition)
    {
        if (IsUnitMoveDisabledInCurrentMode)
        {
            zUDebug.Log("[Ra2Demo] 当前为回放模式，禁止发送单位移动命令");
            return;
        }

        if (entityIds.Count == 0)
        {
            zUDebug.Log("[Test] 没有要移动的单位");
            return;
        }

        if (_game == null || _game.World == null)
            return;

        // 验证单位是否有效
        List<int> validEntityIds = new List<int>();
        foreach (int entityId in entityIds)
        {
            var entity = new Entity(entityId);
            if (!_game.World.ComponentManager.HasComponent<UnitComponent>(entity))
            {
                zUDebug.Log($"[Test] 单位 {entityId} 已不存在");
                DisableOutlineForEntity(entityId);
                continue;
            }

            // 判断拥有 LocalPlayerComponent 就是当前玩家的单位
            if (!_game.World.ComponentManager.HasComponent<LocalPlayerComponent>(entity))
            {
                zUDebug.Log($"[Test] 单位 {entityId} 不属于当前玩家");
                DisableOutlineForEntity(entityId);
                continue;
            }

            validEntityIds.Add(entityId);
        }

        if (validEntityIds.Count == 0)
        {
            zUDebug.Log("[Test] 没有有效的选中单位");
            return;
        }

        zfloat x = zfloat.FromRaw((long)(targetWorldPosition.x * zfloat.SCALE_10000));
        zfloat z = zfloat.FromRaw((long)(targetWorldPosition.z * zfloat.SCALE_10000));

        // 创建移动命令
        var moveCommand = new EntityMoveCommand(
            campId: 0,
            entityIds: validEntityIds.ToArray(),
            targetPosition: new zVector2(x, z)
        )
        {
            Source = CommandSource.Local
        };

        _game.SubmitCommand(moveCommand);
        zUDebug.Log($"[Test] 发送移动命令：{validEntityIds.Count}个单位 → {targetWorldPosition}");
    }

    private void OnGUI()
    {
        // 绘制拖拽虚线（单位移动模式）
        DrawDragLine();
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void RestartGame()
    {
        // 清空选中单位列表
        ClearAllOutlines(); // 清除所有单位的描边

        // 销毁所有视图对象
        _presentationSystem?.DestroyAllViews();

        // 销毁旧的游戏实例和相关组件
        if (_game != null)
        {
            // 注意：C#中没有显式的销毁方法，我们只需要解除引用
            _game = null;
        }

        zUDebug.Log("[Ra2Demo] 重新开始游戏");
    }

    /// <summary>
    /// 绘制拖拽虚线（单位移动模式下从起点到目标点）
    /// </summary>
    private void DrawDragLine()
    {
        // 只有在单位移动模式且需要绘制虚线时才绘制
        if (!shouldDrawDragLine || !isUnitMoveMode)
        {
            return;
        }

        // 计算线段长度和角度
        Vector2 delta = dragCurrentScreenPos - dragStartScreenPos;
        float length = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        // 保存当前 GUI 颜色和矩阵
        Color oldColor = GUI.color;
        Matrix4x4 oldMatrix = GUI.matrix;

        // 虚线参数
        float dashLength = 10f; // 每段虚线长度
        float gapLength = 5f;   // 虚线间隔
        float totalDashGap = dashLength + gapLength;
        int dashCount = Mathf.FloorToInt(length / totalDashGap);

        // 计算方向向量
        Vector2 direction = delta.normalized;

        // 绘制多段绿色虚线
        for (int i = 0; i < dashCount; i++)
        {
            float startDist = i * totalDashGap;
            
            // 计算当前虚线段的起始位置
            Vector2 startPos = dragStartScreenPos + direction * startDist;
            
            // 计算当前虚线段的结束位置
            Vector2 endPos = startPos + direction * dashLength;

            // 转换为 GUI 坐标（Y 轴翻转）
            float guiStartX = startPos.x;
            float guiStartY = Screen.height - startPos.y;
            float guiEndXLocal = endPos.x;
            float guiEndYLocal = Screen.height - endPos.y;

            // 绘制当前虚线段（使用绿色）
            Color lineColor = new Color(0.0f, 1.0f, 0.0f, 0.8f); // 绿色虚线
            DrawLine(new Vector2(guiStartX, guiStartY), new Vector2(guiEndXLocal, guiEndYLocal), lineColor, 2f);
        }

        // 恢复 GUI 状态
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;

        // 绘制目标点圆圈（绿色）
        float circleRadius = 8f;
        float guiEndX = dragCurrentScreenPos.x;
        float guiEndY = Screen.height - dragCurrentScreenPos.y;
        
        Rect targetCircleRect = new Rect(
            guiEndX - circleRadius, 
            guiEndY - circleRadius, 
            circleRadius * 2, 
            circleRadius * 2
        );
        
        Color circleColor = new Color(0.0f, 1.0f, 0.0f, 0.8f); // 绿色圆圈
        GUI.color = circleColor;
        GUI.DrawTexture(targetCircleRect, Texture2D.whiteTexture);
        
        // 恢复颜色
        GUI.color = oldColor;
    }

    /// <summary>
    /// 绘制两点之间的线段
    /// </summary>
    private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        
        Vector2 delta = end - start;
        float length = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        // 旋转矩形来匹配线的方向
        Matrix4x4 oldMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, start);
        
        Rect rect = new Rect(start.x, start.y, length, thickness);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        
        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    /// <summary>
    /// 移动相机到我方工厂位置
    /// </summary>
    public void MoveCameraToOurFactory()
    {
        if (_game == null || _game.World == null)
            return;

        // 直接查找本地玩家的主基地
        var (mainBaseComponent, entity) = _game.World.ComponentManager.GetComponentWithCondition<MainBaseComponent>(
            e => _game.World.ComponentManager.HasComponent<LocalPlayerComponent>(e));

        // 如果找到了本地玩家的主基地
        if (entity.Id != -1)
        {
            // 确保实体有位置组件
            if (_game.World.ComponentManager.HasComponent<TransformComponent>(entity))
            {
                var transform = _game.World.ComponentManager.GetComponent<TransformComponent>(entity);
                Vector3 factoryPosition = transform.Position.ToVector3();

                // 将相机移动到工厂位置
                _cameraTarget.transform.position = factoryPosition;
                zUDebug.Log($"[Ra2Demo] 相机已移动到我方工厂位置: {factoryPosition}");
                return;
            }
        }

        zUDebug.Log("[Ra2Demo] 未找到我方工厂");
    }

    /// <summary>
    /// 清除所有单位的OutlineComponent
    /// </summary>
    public void ClearAllOutlines()
    {
        if (_game == null || _game.World == null)
            return;

        // 禁用之前选中单位的OutlineComponent
        foreach (int entityId in selectedEntityIds)
        {
            DisableOutlineForEntity(entityId);
        }
        selectedEntityIds.Clear();
    }

    /// <summary>
    /// 获取当前选中的单位ID列表
    /// </summary>
    /// <returns>选中的单位ID列表的副本</returns>
    public List<int> GetSelectedEntityIds()
    {
        return new List<int>(selectedEntityIds);
    }

    /// <summary>
    /// 设置当前选中的单位ID列表
    /// </summary>
    /// <param name="entityIds">要设置为选中状态的单位ID列表</param>
    public void SetSelectedEntityIds(List<int> entityIds)
    {
        // 清除当前选择
        ClearAllOutlines();
        
        // 更新选择列表
        selectedEntityIds.Clear();
        if (entityIds != null)
        {
            selectedEntityIds.AddRange(entityIds);
        }
    }

    /// <summary>
    /// 为指定实体启用OutlineComponent
    /// </summary>
    /// <param name="entityId">实体ID</param>
    /// <returns>如果成功启用轮廓返回true，否则返回false</returns>
    public bool EnableOutlineForEntity(int entityId)
    {
        if (_game == null || _game.World == null)
            return false;

        var entity = new Entity(entityId);
        // 检查实体是否存在（通过检查是否有TransformComponent组件来判断实体是否存在）
        if (!_game.World.ComponentManager.HasComponent<TransformComponent>(entity))
            return false;

        if (_game.World.ComponentManager.HasComponent<ViewComponent>(entity))
        {
            var viewComponent = _game.World.ComponentManager.GetComponent<ViewComponent>(entity);
            if (viewComponent != null && viewComponent.GameObject != null)
            {
                var outlineComponent = viewComponent.GameObject.GetComponent<OutlineComponent>();
                if (outlineComponent != null)
                {
                    outlineComponent.ShowCircle();
                    return true; // 成功启用轮廓
                }
            }
        }
        return false; // 启用轮廓失败
    }

    /// <summary>
    /// 为指定实体禁用 OutlineComponent
    /// </summary>
    /// <param name="entityId">实体 ID</param>
    public void DisableOutlineForEntity(int entityId)
    {
        if (_game == null || _game.World == null)
            return;

        var entity = new Entity(entityId);
        if (_game.World.ComponentManager.HasComponent<ViewComponent>(entity))
        {
            var viewComponent = _game.World.ComponentManager.GetComponent<ViewComponent>(entity);
            if (viewComponent != null && viewComponent.GameObject != null)
            {
                var outlineComponent = viewComponent.GameObject.GetComponent<OutlineComponent>();
                if (outlineComponent != null)
                {
                    // outlineComponent.enabled = false;
                    outlineComponent.HideCircle();
                }
            }
        }
    }

    /// <summary>
    /// 创建并显示选择框资源实例
    /// </summary>
    /// <param name="worldPosition">世界坐标位置（只更新 X 和 Z 轴）</param>
    private async void CreateAndShowSelectionBox(Vector3 worldPosition)
    {
        if (maxSelectionCount > 0)
        {
            // 不创建选择框实例，因为最大选择数量已限制
            return;
        }

        worldPosition.y = 0.1f;

        // 如果已有实例，先隐藏它
        if (selectionBoxInstance != null)
        {
            selectionBoxInstance.transform.position = worldPosition;
            if (unitSelectionRadius == 15)
            {
                selectionBoxInstance.transform.localScale = new Vector3(2f, 0.1f, 2f);
            }
            else
            {
                selectionBoxInstance.transform.localScale = new Vector3(1, 0.1f, 1);
            }
            
            // 恢复透明度为完全不透明
            var renderer = selectionBoxInstance.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Material material = renderer.material;
                Color color = material.color;
                material.color = new Color(color.r, color.g, color.b, 0.5f);
            }
            
            selectionBoxInstance.SetActive(true);
            zUDebug.Log($"[Ra2Demo] 选择框已重新激活并更新位置：{worldPosition}");
        }
        else
        {
            // 实例化选择框预制体
            selectionBoxInstance = await AssetManager.InstantiatePrefabAsync(SELECTION_BOX_PREFAB_PATH);
            
            if (selectionBoxInstance != null)
            {
                selectionBoxInstance.transform.position = worldPosition;
                if (unitSelectionRadius == 15)
                {
                    selectionBoxInstance.transform.localScale = new Vector3(2f, 0.1f, 2f);
                }
                else
                {
                    selectionBoxInstance.transform.localScale = new Vector3(1, 0.1f, 1);
                }
                zUDebug.Log($"[Ra2Demo] 选择框创建成功：{selectionBoxInstance.name}, 位置：{worldPosition}");
            }
            else
            {
                zUDebug.LogWarning($"[Ra2Demo] 选择框创建失败，请检查资源路径：{SELECTION_BOX_PREFAB_PATH}");
                return;
            }
        }

        // 设置淡出定时器：0.5 秒后开始淡出
        fadeOutStartTime = Time.time + 0.5f;
        isFadingOut = true;
    }

    /// <summary>
    /// 淡出选择框（在 Update 中调用）
    /// </summary>
    private void UpdateFadeOut()
    {
        if (!isFadingOut || selectionBoxInstance == null)
            return;

        // 检查是否到了开始淡出的时间
        if (Time.time < fadeOutStartTime)
            return;

        // 获取 Renderer 组件
        var renderer = selectionBoxInstance.GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            float elapsed = Time.time - fadeOutStartTime;
            
            if (elapsed >= FADE_OUT_DURATION)
            {
                zUDebug.Log($"[Ra2Demo] 选择框淡出完成：{elapsed} / {FADE_OUT_DURATION}");
                // 淡出完成，隐藏对象
                HideSelectionBox();
                isFadingOut = false;
                
                // 恢复原始颜色（下次使用时正常显示）
                Material resetMaterial = selectionBoxInstance.GetComponentInChildren<Renderer>()?.material;
                if (resetMaterial != null)
                {
                    Color originalColor = resetMaterial.color;
                    resetMaterial.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
                }
            }
            else
            {
                zUDebug.Log($"[Ra2Demo] 选择框淡出中：{elapsed} / {FADE_OUT_DURATION}");
                // 计算透明度（线性插值）
                float t = elapsed / FADE_OUT_DURATION;
                Material material = renderer.material;
                Color originalColor = material.color;
                
                // 设置新的透明度
                material.color = new Color(
                    originalColor.r,
                    originalColor.g,
                    originalColor.b,
                    Mathf.Lerp(0.5f, 0f, t)
                );
            }
        }
        else
        {
            // 如果没有 Renderer，直接隐藏
            HideSelectionBox();
            isFadingOut = false;
        }
    }

    /// <summary>
    /// 隐藏选择框资源实例
    /// </summary>
    private void HideSelectionBox()
    {
        if (selectionBoxInstance != null)
        {
            selectionBoxInstance.SetActive(false);
            zUDebug.Log("[Ra2Demo] 选择框已隐藏");
        }
    }

    public void SetBattleGame(BattleGame game)
    {
        _game = game;
    }

    private Entity GetClickedBuildingEntity(Vector3 worldPosition)
    {
        if (_game != null && _game.World != null)
        {
            // 遍历所有建筑实体，检测是否有点中的建筑
            var buildingEntityIds = _game.World.ComponentManager.GetAllEntityIdsWith<BuildingComponent>();
            
            foreach (var entityId in buildingEntityIds)
            {
                var entity = new Entity(entityId);
                var transformComp = _game.World.ComponentManager.GetComponent<TransformComponent>(entity);
                var buildingComp = _game.World.ComponentManager.GetComponent<BuildingComponent>(entity);
                
                Vector3 buildingPos = transformComp.Position.ToVector3();
                float distance = Vector3.Distance(buildingPos, worldPosition);
                
                // 判断点击位置是否在建筑范围内（考虑建筑的尺寸）
                // 这里使用一个简单的圆形碰撞检测，半径为 4 米
                if (distance <= 4f)
                {
                    return entity;
                }
            }
        }

        return new Entity(-1);
    }

}
