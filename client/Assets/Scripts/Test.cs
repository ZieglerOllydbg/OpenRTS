using UnityEngine;
using UnityEngine.InputSystem;
using ZLockstep.View;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;
using ZLockstep.Sync.Command;
using ZLockstep.Sync.Command.Commands;
using zUnity;

/// <summary>
/// 测试脚本：点击地面创建单位（使用Command系统）
/// </summary>
public class Test : MonoBehaviour
{
    [Header("游戏世界")]
    [SerializeField] private GameWorldBridge worldBridge;

    [Header("创建设置")]
    [SerializeField] private LayerMask groundLayer = -1; // 地面层
    [SerializeField] private int playerId = 0; // 玩家ID
    [SerializeField] private int unitType = 1; // 单位类型：1=动员兵, 2=坦克, 3=矿车
    [SerializeField] private int prefabId = 1; // 预制体ID（对应unitPrefabs数组索引）

    private RTSControl _controls;
    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _controls = new RTSControl();

        // 如果没有分配 worldBridge，尝试自动查找
        if (worldBridge == null)
        {
            worldBridge = FindObjectOfType<GameWorldBridge>();
            if (worldBridge == null)
            {
                Debug.LogError("[Test] 找不到 GameWorldBridge！请在场景中添加或手动分配。");
            }
        }
    }

    private void OnEnable()
    {
        _controls.Create.Enable();
        _controls.Create.createUnit.performed += OnCreateUnit;
    }

    private void OnDisable()
    {
        _controls.Create.Disable();
        _controls.Create.createUnit.performed -= OnCreateUnit;
    }

    /// <summary>
    /// 响应创建单位的输入（使用Command系统）
    /// </summary>
    private void OnCreateUnit(InputAction.CallbackContext context)
    {
        if (worldBridge == null || worldBridge.LogicWorld == null)
        {
            Debug.LogWarning("[Test] GameWorldBridge 未初始化！");
            return;
        }

        // 获取鼠标/触摸位置
        Vector2 screenPosition = Pointer.current.position.ReadValue();

        // 射线检测获取点击位置
        if (TryGetGroundPosition(screenPosition, out Vector3 worldPosition))
        {
            CreateUnitAtPosition(worldPosition);
        }
    }

    /// <summary>
    /// 通过射线检测获取地面点击位置
    /// </summary>
    private bool TryGetGroundPosition(Vector2 screenPosition, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (_mainCamera == null)
        {
            Debug.LogWarning("[Test] 找不到主相机！");
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

        Debug.LogWarning("[Test] 无法获取点击位置！请确保有地面碰撞体或使用默认平面。");
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
    /// 在指定位置创建单位（通过Command）
    /// </summary>
    private void CreateUnitAtPosition(Vector3 position)
    {
        // 转换为逻辑层坐标
        zVector3 logicPosition = position.ToZVector3();

        // 创建CreateUnitCommand
        var createCommand = new CreateUnitCommand(
            campId: playerId,
            unitType: unitType,
            position: logicPosition,
            prefabId: prefabId
        )
        {
            Source = CommandSource.Local
        };

        // 提交命令到游戏世界
        worldBridge.SubmitCommand(createCommand);

        Debug.Log($"[Test] 提交创建单位命令: 类型={unitType}, 位置={position}, 玩家={playerId}");
    }

    /// <summary>
    /// 测试：右键点击地面让所有单位移动（可选）
    /// </summary>
    private void Update()
    {
        // 右键点击发送移动命令
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            SendMoveCommand();
        }
    }

    /// <summary>
    /// 发送移动命令给所有单位
    /// </summary>
    private void SendMoveCommand()
    {
        if (worldBridge == null || worldBridge.LogicWorld == null)
            return;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        if (!TryGetGroundPosition(screenPosition, out Vector3 worldPosition))
            return;

        // 获取所有属于该玩家的单位
        var allUnits = worldBridge.LogicWorld.ComponentManager
            .GetAllEntityIdsWith<UnitComponent>();

        var playerUnits = new System.Collections.Generic.List<int>();
        foreach (var entityId in allUnits)
        {
            var entity = new Entity(entityId);
            var unit = worldBridge.LogicWorld.ComponentManager.GetComponent<UnitComponent>(entity);
            if (unit.PlayerId == playerId)
            {
                playerUnits.Add(entityId);
            }
        }

        if (playerUnits.Count > 0)
        {
            // 创建移动命令
            var moveCommand = new MoveCommand(
                campId: playerId,
                entityIds: playerUnits.ToArray(),
                targetPosition: worldPosition.ToZVector3()
            )
            {
                Source = CommandSource.Local
            };

            worldBridge.SubmitCommand(moveCommand);
            zUDebug.Log($"[Test] 发送移动命令: {playerUnits.Count}个单位 → {worldPosition}");
        }
    }

    #region 调试辅助

    // 在编辑器中显示点击位置
    private Vector3 _lastClickPosition;
    private float _gizmoDisplayTime = 2f;
    private float _lastClickTime;

    private void OnDrawGizmos()
    {
        // 显示最后点击位置
        if (Time.time - _lastClickTime < _gizmoDisplayTime)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_lastClickPosition, 0.5f);
            Gizmos.DrawLine(_lastClickPosition, _lastClickPosition + Vector3.up * 2f);
        }

        // 显示所有已创建的单位（从逻辑层读取）
        if (worldBridge != null && worldBridge.LogicWorld != null)
        {
            var transforms = worldBridge.LogicWorld.ComponentManager
                .GetAllEntityIdsWith<TransformComponent>();

            foreach (var entityId in transforms)
            {
                var entity = new Entity(entityId);
                var transform = worldBridge.LogicWorld.ComponentManager
                    .GetComponent<TransformComponent>(entity);

                // 绘制单位位置
                Vector3 pos = transform.Position.ToVector3();
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(pos, Vector3.one * 0.8f);

                // 如果有移动命令，绘制目标位置
                if (worldBridge.LogicWorld.ComponentManager
                    .HasComponent<MoveCommandComponent>(entity))
                {
                    var moveCmd = worldBridge.LogicWorld.ComponentManager
                        .GetComponent<MoveCommandComponent>(entity);
                    
                    Vector3 targetPos = moveCmd.TargetPosition.ToVector3();
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(targetPos, 0.3f);
                    Gizmos.DrawLine(pos, targetPos);
                }
            }
        }
    }

    #endregion
}

