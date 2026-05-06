# 流场系统集成指南

本指南帮助您将流场系统集成到基于ECS的RTS游戏中。

## 系统集成步骤

### 第一步：实现地图接口

根据您的游戏地图系统，实现 `IFlowFieldMap` 接口：

```csharp
public class YourMapManager : IFlowFieldMap
{
    // 您的地图数据
    private bool[] walkableGrid;
    private int width, height;
    private zfloat gridSize;
    
    public int GetWidth() => width;
    public int GetHeight() => height;
    public zfloat GetGridSize() => gridSize;
    
    public bool IsWalkable(int gridX, int gridY)
    {
        // 根据您的地图数据返回是否可行走
        // 注意：流场格子可能比逻辑格子大（如2倍）
        return walkableGrid[gridY * width + gridX];
    }
    
    public zfloat GetTerrainCost(int gridX, int gridY)
    {
        // 返回地形代价
        // 1.0 = 正常, 2.0 = 慢速（泥地）, 0 = 不可通行
        return zfloat.One;
    }
    
    public void WorldToGrid(zVector2 worldPos, out int gridX, out int gridY)
    {
        gridX = (int)(worldPos.x / gridSize);
        gridY = (int)(worldPos.y / gridSize);
    }
    
    public zVector2 GridToWorld(int gridX, int gridY)
    {
        return new zVector2(
            (zfloat)gridX * gridSize + gridSize * zfloat.Half,
            (zfloat)gridY * gridSize + gridSize * zfloat.Half
        );
    }
}
```

### 第二步：初始化系统

在游戏初始化时创建流场系统：

```csharp
public class GameInitializer
{
    public void Initialize(zWorld world)
    {
        // 1. 地图
        var mapManager = new YourMapManager();
        mapManager.Initialize(/* 您的参数 */);
        
        // 2. 流场管理器
        var flowFieldManager = new FlowFieldManager();
        flowFieldManager.Initialize(mapManager, maxFields: 20);
        flowFieldManager.SetMaxUpdatesPerFrame(2); // 防止卡顿
        
        // 3. RVO模拟器
        var rvoSimulator = new RVO2Simulator();
        
        // 4. 创建并注册导航系统到World
        var navSystem = new FlowFieldNavigationSystem();
        world.SystemManager.RegisterSystem(navSystem);
        navSystem.InitializeNavigation(flowFieldManager, rvoSimulator);
    }
}
```

### 第三步：为实体添加导航能力

流场系统使用项目现有的ECS组件：

```csharp
public class UnitSpawner
{
    public Entity SpawnNavigableUnit(zWorld world, zVector2 spawnPos)
    {
        // 1. 创建实体
        Entity entity = world.EntityManager.CreateEntity();
        
        // 2. 添加Transform组件（使用项目现有的）
        var transform = TransformComponent.Default;
        transform.Position = new zVector3(spawnPos.x, zfloat.Zero, spawnPos.y);
        world.ComponentManager.AddComponent(entity, transform);
        
        // 3. 添加Velocity组件（可选，用于动画等）
        world.ComponentManager.AddComponent(entity, new VelocityComponent(zVector3.zero));
        
        // 4. 添加流场导航能力
        var navSystem = world.SystemManager.GetSystem<FlowFieldNavigationSystem>();
        navSystem.AddNavigator(entity, 
            radius: new zfloat(0, 3000),  // 0.3 碰撞半径
            maxSpeed: new zfloat(2)       // 2.0 最大速度
        );
        
        return entity;
    }
}
```

**组件说明**：
- `TransformComponent` - 项目现有组件，存储位置
- `VelocityComponent` - 项目现有组件，存储速度（可选）
- `FlowFieldNavigatorComponent` - 新增组件，流场导航数据
- `MoveTargetComponent` - 新增组件，移动目标

### 第四步：处理玩家移动命令

```csharp
public class PlayerInputHandler
{
    private zWorld world;
    
    public void OnMoveCommand(List<Entity> selectedUnits, zVector2 targetPos)
    {
        // 获取导航系统
        var navSystem = world.SystemManager.GetSystem<FlowFieldNavigationSystem>();
        
        // 批量设置目标（自动共享流场）
        navSystem.SetMultipleTargets(selectedUnits, targetPos);
        
        // 播放移动音效等
        PlayMoveSound();
    }
}
```

### 第五步：建筑系统集成

```csharp
public class BuildingSystem
{
    private YourMapManager mapManager;
    private FlowFieldManager flowFieldManager;
    
    public void PlaceBuilding(Building building)
    {
        // 1. 更新地图数据
        mapManager.SetBuildingOccupation(
            building.gridX, building.gridY,
            building.width, building.height,
            occupied: true
        );
        
        // 2. 通知流场系统
        flowFieldManager.MarkRegionDirty(
            building.gridX, building.gridY,
            building.gridX + building.width,
            building.gridY + building.height
        );
        
        // 流场会在接下来的帧中自动更新
    }
    
    public void DestroyBuilding(Building building)
    {
        // 1. 更新地图
        mapManager.SetBuildingOccupation(
            building.gridX, building.gridY,
            building.width, building.height,
            occupied: false
        );
        
        // 2. 通知流场系统
        flowFieldManager.MarkRegionDirty(
            building.gridX, building.gridY,
            building.gridX + building.width,
            building.gridY + building.height
        );
    }
}
```

### 第六步：游戏循环

```csharp
public class GameLoop
{
    private zWorld world;
    
    public void FixedUpdate()
    {
        // 1. 处理游戏逻辑（玩家输入、AI等）
        ProcessGameLogic();
        
        // 2. 更新游戏世界（自动调用所有System的Update，包括FlowFieldNavigationSystem）
        world.Update();
        
        // 3. 同步到表现层
        SyncToView();
    }
}
```

## 高级集成

### 与A*路径规划集成

对于超长距离导航，可以结合A*：

```csharp
public class HybridNavigator
{
    private AStarPathfinder astar;
    private FlowFieldManager flowFieldMgr;
    
    public void SetLongDistanceTarget(Entity unit, zVector2 targetPos)
    {
        // 1. 使用A*计算全局路径
        List<zVector2> waypoints = astar.FindPath(unit.position, targetPos);
        
        // 2. 流场导航到下一个航点
        if (waypoints.Count > 0)
        {
            int fieldId = flowFieldMgr.RequestFlowField(waypoints[0]);
            unit.flowFieldId = fieldId;
            unit.waypoints = waypoints;
        }
    }
    
    public void Update(Entity unit)
    {
        // 到达当前航点，切换到下一个
        if (IsAtWaypoint(unit))
        {
            flowFieldMgr.ReleaseFlowField(unit.flowFieldId);
            unit.waypoints.RemoveAt(0);
            
            if (unit.waypoints.Count > 0)
            {
                unit.flowFieldId = flowFieldMgr.RequestFlowField(unit.waypoints[0]);
            }
        }
    }
}
```

### 性能优化：空间分区

对于大量单位，使用空间分区优化RVO：

```csharp
public class OptimizedNavSystem
{
    private SpatialHash spatialHash;
    
    public void Update(zfloat deltaTime)
    {
        // 1. 更新流场
        flowFieldMgr.Tick();
        
        // 2. 更新空间分区
        spatialHash.Clear();
        foreach (var entity in entities)
        {
            spatialHash.Insert(entity.position, entity);
        }
        
        // 3. 只对附近的单位执行RVO
        foreach (var entity in entities)
        {
            var neighbors = spatialHash.Query(entity.position, searchRadius);
            // 只添加附近的单位到RVO
        }
        
        // 4. RVO计算
        rvoSimulator.DoStep(deltaTime);
    }
}
```

### 调试工具集成

```csharp
public class NavigationDebugger
{
    public void DrawDebugInfo(Camera camera)
    {
        // 1. 绘制流场
        if (showFlowField)
        {
            DrawFlowField(selectedFlowFieldId);
        }
        
        // 2. 绘制单位速度
        foreach (var entity in navSystem.GetAllEntities())
        {
            zVector2 vel = rvoSim.GetAgentVelocity(entity.navigator.rvoAgentId);
            DrawArrow(entity.position, vel, Color.blue);
        }
        
        // 3. 绘制目标
        foreach (var entity in navSystem.GetAllEntities())
        {
            if (entity.target.hasTarget)
            {
                DrawLine(entity.position, entity.target.targetPosition, Color.green);
            }
        }
    }
    
    private void DrawFlowField(int fieldId)
    {
        var info = flowFieldMgr.GetFieldInfo(fieldId);
        // 获取流场数据并绘制方向箭头
    }
}
```

## 常见问题

### Q1: 如何适配现有的单位系统？

**A**: 不需要修改现有单位代码，只需：
1. 在单位上添加 `FlowFieldNavigatorComponent`
2. 在移动指令中调用 `navSystem.SetEntityTarget()`
3. 在游戏循环中调用 `navSystem.Update()`

### Q2: 流场系统如何与现有的ECS集成？

**A**: 有两种方式：
- **方案A**：使用提供的 `FlowFieldNavigationSystem`（推荐）
- **方案B**：参考系统代码，自己实现一个适配您ECS的系统

### Q3: 如何处理多层地图？

**A**: 为每一层创建独立的地图管理器和流场管理器：

```csharp
Dictionary<int, FlowFieldManager> flowFieldsByLayer;

int GetFlowField(int layer, zVector2 targetPos)
{
    return flowFieldsByLayer[layer].RequestFlowField(targetPos);
}
```

### Q4: 性能如何？能支持多少单位？

**A**: 
- 128×128流场：计算约5-10ms
- 100个单位共享流场：每帧<1ms
- 推荐：200+单位使用流场，配合RVO避障

### Q5: 如何处理动态单位作为障碍？

**A**: RVO2会自动处理单位间避障，不需要将单位加入地图。
流场处理静态障碍（建筑），RVO2处理动态障碍（单位）。

## 检查清单

集成完成后，检查以下事项：

- [ ] 实现了 `IFlowFieldMap` 接口
- [ ] 流场格子大小是逻辑格子的2倍
- [ ] 初始化了所有系统（地图、流场、RVO、导航）
- [ ] 单位创建时注册到导航系统
- [ ] 移动指令调用 `SetEntityTarget()`
- [ ] 游戏循环调用 `navSystem.Update()`
- [ ] 建筑系统调用 `MarkRegionDirty()`
- [ ] 单位销毁时清理导航组件
- [ ] 设置了合理的 `maxUpdatesPerFrame`
- [ ] 测试了大量单位的性能

## 下一步

1. 运行 `FlowFieldExample.RunCompleteExample()` 验证系统工作
2. 在您的游戏中集成最简单的场景
3. 逐步添加更多功能（建筑、多目标等）
4. 根据实际性能调整参数
5. 添加调试可视化工具

祝您集成顺利！如有问题，请参考示例代码或文档。

