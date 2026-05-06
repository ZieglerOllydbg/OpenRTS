# 流场系统 - ECS集成说明

## ✅ 已适配您的ECS框架

流场系统已完全集成到项目现有的ECS架构中，不再使用自定义的Entity和组件系统。

## 核心修改

### 1. 使用现有ECS组件

| 现有组件 | 用途 |
|---------|------|
| `Entity` (struct) | 实体标识符 |
| `TransformComponent` | 存储位置（Position.x, Position.z用于2D导航） |
| `VelocityComponent` | 存储速度（可选） |
| `IComponent` | 组件接口 |

### 2. 新增流场组件

符合 `IComponent` 接口的新组件：

```csharp
// FlowFieldNavigatorComponent - 导航数据
public struct FlowFieldNavigatorComponent : IComponent
{
    public int CurrentFlowFieldId;
    public int RvoAgentId;
    public zfloat MaxSpeed;
    public zfloat Radius;
    // ...
}

// MoveTargetComponent - 移动目标
public struct MoveTargetComponent : IComponent
{
    public zVector2 TargetPosition;
    public bool HasTarget;
}
```

### 3. 系统继承BaseSystem

```csharp
public class FlowFieldNavigationSystem : BaseSystem
{
    // 使用 World.ComponentManager 访问组件
    // 使用 World.EntityManager 管理实体
    // 自动注册到 World.SystemManager
}
```

## 快速集成

### 步骤1：注册系统

```csharp
// 在游戏初始化时
var navSystem = new FlowFieldNavigationSystem();
world.SystemManager.RegisterSystem(navSystem);
navSystem.InitializeNavigation(flowFieldManager, rvoSimulator);
```

### 步骤2：创建可导航实体

```csharp
// 创建实体
Entity entity = world.EntityManager.CreateEntity();

// 添加Transform（现有组件）
var transform = TransformComponent.Default;
transform.Position = new zVector3(x, zfloat.Zero, z);
world.ComponentManager.AddComponent(entity, transform);

// 添加导航能力
var navSystem = world.SystemManager.GetSystem<FlowFieldNavigationSystem>();
navSystem.AddNavigator(entity, radius, maxSpeed);
```

### 步骤3：设置移动目标

```csharp
var navSystem = world.SystemManager.GetSystem<FlowFieldNavigationSystem>();
navSystem.SetMoveTarget(entity, targetPosition);
```

### 步骤4：自动更新

```csharp
// 游戏循环中
world.Update(); // 自动调用所有System.Update()，包括FlowFieldNavigationSystem
```

## 组件访问

系统通过 `ComponentManager` 访问组件：

```csharp
// 获取所有有导航组件的实体
var entities = ComponentManager.GetAllEntityIdsWith<FlowFieldNavigatorComponent>();

// 遍历处理
foreach (var entityId in entities)
{
    Entity entity = new Entity(entityId);
    var navigator = ComponentManager.GetComponent<FlowFieldNavigatorComponent>(entity);
    var transform = ComponentManager.GetComponent<TransformComponent>(entity);
    // 处理逻辑...
}
```

## 与现有系统协同

流场系统会自动：
1. 从 `TransformComponent` 读取位置
2. 计算导航速度
3. 通过RVO2避障
4. 更新位置回 `TransformComponent`
5. 更新速度到 `VelocityComponent`（如果有）

## 文档

- **快速开始**: `FLOW_FIELD_README.md`
- **集成指南**: `INTEGRATION_GUIDE.md`
- **使用示例**: `FlowFieldExample.cs`

## 检查清单

- [x] 移除自定义Entity类
- [x] 组件符合IComponent接口
- [x] 系统继承BaseSystem
- [x] 使用World.ComponentManager
- [x] 使用World.EntityManager
- [x] 集成TransformComponent
- [x] 集成VelocityComponent
- [x] 示例代码更新
- [x] 文档更新

## 与之前版本的差异

| 旧版本 | 新版本 |
|-------|-------|
| 自定义 Entity 类 | 使用项目的 Entity struct |
| 自定义组件类 | 实现 IComponent 接口的 struct |
| 独立的导航系统 | 继承 BaseSystem |
| PositionComponent | TransformComponent |
| 独立Update调用 | World.Update()自动调用 |
| 手动实体管理 | 使用World.EntityManager |

## 完全兼容

✅ 与项目现有ECS完全兼容  
✅ 使用现有的Entity和Component系统  
✅ 遵循项目的System注册模式  
✅ 无需修改现有代码  
✅ 可与其他System协同工作

