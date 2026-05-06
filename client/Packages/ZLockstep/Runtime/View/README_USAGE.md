# 逻辑视觉分离架构使用指南

## 快速开始

### 1. 在Unity场景中设置

1. 创建一个空GameObject，命名为 `GameWorld`
2. 添加 `GameWorldBridge` 组件
3. 配置参数：
   - **Logic Frame Rate**: 20（逻辑帧率）
   - **Enable Smooth Interpolation**: true（启用平滑插值）
   - **View Root**: 拖入一个空的Transform作为所有实体的父节点

### 2. 创建单位

```csharp
using UnityEngine;
using ZLockstep.View;

public class GameTest : MonoBehaviour
{
    public GameWorldBridge worldBridge;
    public GameObject tankPrefab;

    void Start()
    {
        // 创建一个犀牛坦克
        var entity = worldBridge.EntityFactory.CreateTank(
            position: new zVector3(0, 0, 0),
            playerId: 0,
            prefab: tankPrefab
        );
        
        zUDebug.Log($"创建了实体: {entity.Id}");
    }
}
```

### 3. 发送移动命令

```csharp
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;

// 给单位添加移动命令
var moveCommand = new MoveCommandComponent(
    target: new zVector3(10, 0, 10),
    stopDistance: (zfloat)0.5f
);

worldBridge.LogicWorld.ComponentManager.AddComponent(entity, moveCommand);
```

### 4. 自定义System

```csharp
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;

public class MyCustomSystem : BaseSystem
{
    protected override void OnInitialize()
    {
        zUDebug.Log("系统初始化");
    }

    public override void Update()
    {
        // 遍历所有单位
        var units = ComponentManager.GetAllEntityIdsWith<UnitComponent>();
        
        foreach (var entityId in units)
        {
            var entity = new Entity(entityId);
            var unit = ComponentManager.GetComponent<UnitComponent>(entity);
            
            // 你的逻辑...
        }
    }
}

// 在GameWorldBridge.RegisterSystems()中注册
_logicWorld.SystemManager.RegisterSystem(new MyCustomSystem());
```

## 核心概念

### 逻辑层 vs 表现层

| 特性 | 逻辑层 | 表现层 |
|------|--------|--------|
| 数学类型 | zfloat, zVector3, zQuaternion | float, Vector3, Quaternion |
| 运行环境 | 可独立运行（服务器/测试） | 依赖Unity |
| 确定性 | ✅ 完全确定性 | ❌ 非确定性 |
| 帧率 | 固定（如20FPS） | 可变（60/120FPS） |
| 组件 | TransformComponent, UnitComponent | ViewComponent |

### 数据流向

```
玩家输入 → Command → 逻辑System → 组件数据更新 → PresentationSystem → Unity显示
```

**关键原则**：
- ✅ 逻辑层永远不访问Unity
- ✅ 表现层只读取逻辑层数据
- ❌ 表现层不能修改逻辑层数据

## 常见组件

### TransformComponent（必需）
```csharp
// 位置、旋转、缩放
var transform = new TransformComponent
{
    Position = new zVector3(0, 0, 0),
    Rotation = zQuaternion.identity,
    Scale = zVector3.one
};
```

### UnitComponent
```csharp
// 单位属性
var unit = UnitComponent.CreateTank(playerId: 0);
// 或手动创建
var unit = new UnitComponent
{
    UnitType = 2,
    MoveSpeed = (zfloat)5.0f,
    RotateSpeed = (zfloat)120.0f,
    PlayerId = 0,
    IsSelectable = true
};
```

### HealthComponent
```csharp
// 生命值
var health = new HealthComponent((zfloat)200.0f);
health.TakeDamage((zfloat)50.0f);
if (health.IsAlive)
{
    zUDebug.Log($"剩余血量: {health.HealthPercent * 100}%");
}
```

### VelocityComponent
```csharp
// 速度（通常由MovementSystem自动管理）
var velocity = new VelocityComponent(new zVector3(1, 0, 0) * (zfloat)5.0f);
```

### MoveCommandComponent
```csharp
// 移动命令（添加后MovementSystem会自动处理）
var moveCmd = new MoveCommandComponent(
    target: new zVector3(10, 0, 10),
    stopDistance: (zfloat)0.5f
);
ComponentManager.AddComponent(entity, moveCmd);
```

### AttackComponent
```csharp
// 攻击能力
var attack = new AttackComponent
{
    Damage = (zfloat)30.0f,
    Range = (zfloat)8.0f,
    AttackInterval = (zfloat)2.0f,
    TargetEntityId = targetEntity.Id
};
```

## 完整示例

```csharp
using UnityEngine;
using ZLockstep.View;
using ZLockstep.Simulation.ECS;
using ZLockstep.Simulation.ECS.Components;

public class RTSGameManager : MonoBehaviour
{
    public GameWorldBridge worldBridge;
    public GameObject tankPrefab;

    private Entity playerTank;

    void Start()
    {
        // 创建玩家坦克
        playerTank = worldBridge.EntityFactory.CreateTank(
            position: new zVector3(0, 0, 0),
            playerId: 0,
            prefab: tankPrefab
        );
    }

    void Update()
    {
        // 右键点击地面移动
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // 发送移动命令
                var targetPos = hit.point.ToZVector3();
                var moveCmd = new MoveCommandComponent(targetPos);
                
                worldBridge.LogicWorld.ComponentManager.AddComponent(playerTank, moveCmd);
                
                zUDebug.Log($"移动到: {targetPos}");
            }
        }
    }
}
```

## 调试技巧

### 1. 查看实体组件
```csharp
var entity = new Entity(0);
if (ComponentManager.HasComponent<TransformComponent>(entity))
{
    var transform = ComponentManager.GetComponent<TransformComponent>(entity);
    zUDebug.Log($"位置: {transform.Position}");
}
```

### 2. 遍历所有实体
```csharp
var allUnits = ComponentManager.GetAllEntityIdsWith<UnitComponent>();
zUDebug.Log($"当前单位数量: {allUnits.Count()}");
```

### 3. 在编辑器中可视化
```csharp
void OnDrawGizmos()
{
    if (worldBridge?.LogicWorld == null) return;
    
    var transforms = worldBridge.LogicWorld.ComponentManager
        .GetAllEntityIdsWith<TransformComponent>();
    
    foreach (var entityId in transforms)
    {
        var entity = new Entity(entityId);
        var transform = worldBridge.LogicWorld.ComponentManager
            .GetComponent<TransformComponent>(entity);
        
        Gizmos.DrawWireSphere(transform.Position.ToVector3(), 0.5f);
    }
}
```

## 性能优化建议

1. **ViewComponent按需添加**：服务器端不需要ViewComponent
2. **合理设置逻辑帧率**：20FPS通常足够，不要超过30FPS
3. **使用插值**：启用`EnableSmoothInterpolation`让移动更流畅
4. **批量操作**：在System中批量处理实体，避免单个查询

## 下一步

- [ ] 实现战斗系统（CombatSystem）
- [ ] 实现矿车AI系统（HarvesterAISystem）
- [ ] 实现建筑系统（BuildingSystem）
- [ ] 实现命令队列系统
- [ ] 实现单位选择和框选

---

更多信息请参考：[README.md](../README.md)

