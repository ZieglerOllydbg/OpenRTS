# 流场寻路系统（Flow Field Pathfinding）

基于定点数的流场寻路系统，专为RTS游戏中的大规模单位导航设计。支持ECS架构，与RVO2无缝集成。

## 核心特性

- ✅ **高性能**：多个单位共享同一流场，适合大规模单位
- ✅ **定点数**：所有计算使用zfloat，保证多端同步
- ✅ **ECS架构**：基于组件和系统的设计
- ✅ **动态障碍**：支持建筑建造/摧毁时更新流场
- ✅ **RVO2集成**：流场提供大方向，RVO2处理局部避障
- ✅ **自动缓存**：基于引用计数的智能流场管理

## 系统架构

```
┌─────────────────────────────────────────────┐
│         FlowFieldNavigationSystem           │ ← ECS系统（每帧调用）
│  - 管理所有导航实体                         │
│  - 设置目标、更新导航                       │
└──────────────┬──────────────────────────────┘
               │
       ┌───────┴───────┐
       │               │
┌──────▼──────┐  ┌────▼──────┐
│ FlowField   │  │   RVO2    │
│  Manager    │  │ Simulator │
│  - 创建流场  │  │ - 避障计算 │
│  - 缓存管理  │  │ - 位置更新 │
└──────┬──────┘  └───────────┘
       │
┌──────▼──────┐
│ IFlowField  │
│    Map      │
│ - 地图数据   │
└─────────────┘
```

## 快速开始

### 1. 初始化系统

```csharp
// 在游戏世界初始化时
public void InitializeWorld(zWorld world)
{
    // 1. 创建地图管理器
    SimpleMapManager mapManager = new SimpleMapManager();
    mapManager.Initialize(256, 256, new zfloat(0, 5000)); // 256x256，每格0.5

    // 2. 创建流场管理器（自动使用128x128的流场，每格1.0）
    FlowFieldManager flowFieldManager = new FlowFieldManager();
    flowFieldManager.Initialize(mapManager, maxFields: 20);

    // 3. 创建RVO模拟器
    RVO2Simulator rvoSimulator = new RVO2Simulator();

    // 4. 创建导航系统并注册到World
    FlowFieldNavigationSystem navSystem = new FlowFieldNavigationSystem();
    world.SystemManager.RegisterSystem(navSystem);
    navSystem.InitializeNavigation(flowFieldManager, rvoSimulator);
}
```

### 2. 创建单位

```csharp
// 创建具有导航能力的实体
public Entity CreateNavigableUnit(zWorld world, zVector2 startPos)
{
    // 1. 创建实体
    Entity entity = world.EntityManager.CreateEntity();

    // 2. 添加Transform组件
    var transform = TransformComponent.Default;
    transform.Position = new zVector3(startPos.x, zfloat.Zero, startPos.y);
    world.ComponentManager.AddComponent(entity, transform);

    // 3. 添加导航能力
    var navSystem = world.SystemManager.GetSystem<FlowFieldNavigationSystem>();
    navSystem.AddNavigator(entity, 
        radius: new zfloat(0, 3000),      // 0.3
        maxSpeed: new zfloat(2)           // 2.0
    );

    return entity;
}
```

### 3. 设置目标

```csharp
// 获取导航系统
var navSystem = world.SystemManager.GetSystem<FlowFieldNavigationSystem>();

// 单个单位
zVector2 targetPos = new zVector2(new zfloat(200), new zfloat(200));
navSystem.SetMoveTarget(entity, targetPos);

// 多个单位（常见于RTS选中操作）
List<Entity> selectedUnits = GetSelectedUnits();
navSystem.SetMultipleTargets(selectedUnits, targetPos);
```

### 4. 每帧更新

```csharp
// 游戏循环
void FixedUpdate()
{
    // World.Update() 会自动调用所有系统的 Update()
    // 包括 FlowFieldNavigationSystem
    world.Update();
}
```

### 5. 地图变化

```csharp
// 建造建筑
void OnBuildingPlaced(int x, int y, int width, int height)
{
    // 1. 更新地图数据
    mapManager.SetWalkableRect(x, y, x + width, y + height, false);
    
    // 2. 标记流场为脏（会在下一帧自动更新）
    flowFieldManager.MarkRegionDirty(x, y, x + width, y + height);
}

// 摧毁建筑
void OnBuildingDestroyed(int x, int y, int width, int height)
{
    mapManager.SetWalkableRect(x, y, x + width, y + height, true);
    flowFieldManager.MarkRegionDirty(x, y, x + width, y + height);
}
```

## ECS组件说明

### FlowFieldNavigatorComponent（核心组件）

```csharp
public struct FlowFieldNavigatorComponent : IComponent
{
    public int CurrentFlowFieldId;    // 当前使用的流场ID
    public int RvoAgentId;            // RVO智能体ID
    public zfloat MaxSpeed;           // 最大速度
    public zfloat ArrivalRadius;      // 到达半径
    public zfloat SlowDownRadius;     // 减速半径
    public bool HasReachedTarget;     // 是否已到达
    public bool IsEnabled;            // 是否启用
    public zfloat Radius;             // 单位半径
}
```

挂载在需要导航的实体上。使用项目的 `IComponent` 接口。

### MoveTargetComponent

```csharp
public struct MoveTargetComponent : IComponent
{
    public zVector2 TargetPosition;  // 目标位置
    public bool HasTarget;           // 是否有目标
}
```

### 使用现有组件

系统使用现有的 ECS 组件：
- `TransformComponent` - 位置、旋转、缩放
- `VelocityComponent` - 速度（可选）

## 核心API

### FlowFieldManager

| 方法 | 说明 |
|------|------|
| `Initialize(map, maxFields)` | 初始化管理器 |
| `RequestFlowField(targetPos)` | 请求流场（自动复用） |
| `ReleaseFlowField(fieldId)` | 释放流场（引用计数-1） |
| `SampleDirection(fieldId, pos)` | 查询移动方向 |
| `SampleCost(fieldId, pos)` | 查询到目标的代价 |
| `IsAtTarget(fieldId, pos, threshold)` | 判断是否到达目标 |
| `MarkRegionDirty(minX, minY, maxX, maxY)` | 标记区域为脏 |
| `UpdateDirtyFields()` | 更新所有脏流场 |

### FlowFieldNavigationSystem

| 方法 | 说明 |
|------|------|
| `InitializeNavigation(ffMgr, rvoSim)` | 初始化导航系统 |
| `AddNavigator(entity, radius, maxSpeed)` | 为实体添加导航能力 |
| `SetMoveTarget(entity, target)` | 设置移动目标 |
| `ClearMoveTarget(entity)` | 清除移动目标 |
| `SetMultipleTargets(entities, target)` | 批量设置目标 |
| `RemoveNavigator(entity)` | 移除导航能力 |
| `Update()` | 每帧更新（由World自动调用） |

### IFlowFieldMap（需要实现）

| 方法 | 说明 |
|------|------|
| `GetWidth()` | 获取流场宽度 |
| `GetHeight()` | 获取流场高度 |
| `GetGridSize()` | 获取格子大小 |
| `IsWalkable(x, y)` | 判断是否可行走 |
| `GetTerrainCost(x, y)` | 获取地形代价 |
| `WorldToGrid(worldPos, out x, out y)` | 世界坐标转格子 |
| `GridToWorld(x, y)` | 格子转世界坐标 |

## 性能优化

### 地图精度建议

对于256×256的RTS地图：

```
逻辑地图：256×256 格子，每格 0.5 单位
- 用于碰撞检测
- 建筑占用

流场地图：128×128 格子，每格 1.0 单位
- 用于寻路导航
- 计算量是逻辑地图的 1/4
```

### 流场更新策略

```csharp
// 控制每帧最多更新的流场数量（防止卡顿）
flowFieldManager.SetMaxUpdatesPerFrame(2);

// 脏流场会在接下来的帧中逐步更新
// 不会在建造建筑的瞬间卡顿
```

### 缓存策略

- **自动复用**：相同目标格子的请求会复用流场
- **引用计数**：无引用的流场会被清理
- **LRU机制**：超过上限时，移除最久未使用的流场

## 常见场景

### 场景1：编队移动

```csharp
// 10个单位移动到同一目标
List<Entity> squad = new List<Entity>();
for (int i = 0; i < 10; i++)
{
    var unit = navSystem.CreateEntity(i, startPos[i], config);
    squad.Add(unit);
}

// 设置相同目标（只创建1个流场）
navSystem.SetMultipleTargets(squad, targetPos);
```

**结果**：只创建1个流场，被10个单位共享。

### 场景2：多目标导航

```csharp
// 4组单位，每组5个，移动到4个不同目标
for (int group = 0; group < 4; group++)
{
    List<Entity> units = CreateGroup(group);
    navSystem.SetMultipleTargets(units, targets[group]);
}
```

**结果**：创建4个流场，每个被5个单位共享。

### 场景3：动态建筑

```csharp
// 单位正在移动
navSystem.SetEntityTarget(unit, targetPos);

// 玩家建造建筑
BuildBuilding(x, y, width, height);
// 内部会调用：
// - mapManager.SetWalkableRect(...)
// - flowFieldManager.MarkRegionDirty(...)

// 流场会在接下来的帧中自动更新
// 单位会自动绕过新建筑
```

## 与RVO2的集成

流场系统与RVO2完美配合：

```
流场（Flow Field）      →  提供大方向（全局路径）
         ↓
RVO2（局部避障）        →  处理单位间避让（局部避障）
         ↓
最终速度和位置          →  平滑自然的移动
```

**工作流程**：
1. 流场计算到目标的方向向量
2. 设置为RVO的期望速度
3. RVO计算考虑避障的实际速度
4. 更新单位位置

## 调试技巧

### 查看流场信息

```csharp
FlowFieldInfo info = flowFieldManager.GetFieldInfo(fieldId);
Debug.Log($"流场 {info.fieldId}:");
Debug.Log($"  目标: ({info.targetGridX}, {info.targetGridY})");
Debug.Log($"  引用数: {info.referenceCount}");
Debug.Log($"  是否脏: {info.isDirty}");
```

### 可视化流场

```csharp
// 绘制流场方向（调试时）
for (int y = 0; y < height; y++)
{
    for (int x = 0; x < width; x++)
    {
        zVector2 worldPos = map.GridToWorld(x, y);
        zVector2 direction = field.directions[field.GetIndex(x, y)];
        // DrawArrow(worldPos, direction);
    }
}
```

## 注意事项

1. **定点数精度**：所有计算使用zfloat，保证同步
2. **地图比例**：流场格子应该是逻辑格子的2倍大小
3. **更新频率**：使用`SetMaxUpdatesPerFrame`防止卡顿
4. **引用管理**：不再使用的流场要调用`ReleaseFlowField`
5. **局部性**：流场适合全局导航，RVO2处理局部避障

## 完整示例

参考 `FlowFieldExample.cs` 查看完整的使用示例，包括：
- 编队移动
- 多目标导航
- 动态障碍物
- 完整游戏循环

## 下一步

1. 实现自己的 `IFlowFieldMap` 接口
2. 在游戏中集成 `FlowFieldNavigationSystem`
3. 根据地图特点调整格子大小和缓存策略
4. 配合RVO2实现平滑的单位移动

## 参考资料

- Elijah Emerson, "Crowd Pathfinding and Steering Using Flow Field Tiles"
- Daniel Brewer, "Toward More Realistic Pathfinding"

