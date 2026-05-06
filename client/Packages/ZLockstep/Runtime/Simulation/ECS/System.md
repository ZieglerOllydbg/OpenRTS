# ECS 系统架构改进方案

## 当前架构问题

当前 [SystemManager](file:///C:/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/SystemManager.cs#L8-L57) 使用两个独立的列表来管理不同类型的系统：
1. [_systems](file:///C:/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/SystemManager.cs#L10-L10) - 存储普通系统
2. [_presentationSystems](file:///C:/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/SystemManager.cs#L11-L11) - 存储表现系统（Presentation systems）

这种方式存在以下问题：
- 系统分类固化，难以扩展更多类型的系统
- 系统更新顺序固定，不够灵活
- 增加了 [SystemManager](file:///C:/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/SystemManager.cs#L8-L57) 的复杂性

## 改进方案

### 1. 统一系统存储

移除 [_presentationSystems](file:///C:/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/SystemManager.cs#L11-L11) 列表，只使用 [_systems](file:///C:/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/SystemManager.cs#L10-L10) 列表来存储所有类型的系统。

### 2. 系统排序机制

引入基于优先级的系统排序机制：

1. 在 [ISystem](file:///C:/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/ISystem.cs#L6-L19) 接口中添加 `GetOrder()` 方法，返回排序编号
2. 创建 [SystemOrder](file:///C:/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/SystemOrder.cs#L3-L17) 枚举，定义不同类型的系统优先级：
   - Normal = 0 （普通系统）
   - Presentation = 1 （表现系统）
3. 在注册系统后，根据排序编号进行排序

### 3. 实现细节

#### SystemOrder 枚举
```csharp
public enum SystemOrder 
{
    Normal = 0,
    Presentation = 1
}
```

#### ISystem 接口修改
```csharp
public interface ISystem
{
    void SetWorld(zWorld world);
    void Update();
    int GetOrder(); // 新增：返回系统排序编号
}
```

#### BaseSystem 类修改
```csharp
public abstract class BaseSystem : ISystem
{
    // ... 现有代码 ...
    
    public virtual int GetOrder() => (int)SystemOrder.Normal;
}
```

#### PresentationBaseSystem 类修改
```csharp
public abstract class PresentationBaseSystem : BaseSystem
{
    public override int GetOrder() => (int)SystemOrder.Presentation;
}
```

#### SystemManager 类修改
```csharp
public class SystemManager
{
    private readonly List<ISystem> _systems = new List<ISystem>();
    private readonly zWorld _world;
    
    // ... 现有代码 ...
    
    public void RegisterSystem(ISystem system)
    {
        system.SetWorld(_world);
        _systems.Add(system);
        // 根据排序编号重新排序
        _systems.Sort((a, b) => a.GetOrder().CompareTo(b.GetOrder()));
    }
    
    public void UpdateAll()
    {
        // 由于已排序，直接按顺序更新所有系统
        foreach (var system in _systems)
        {
            system.Update();
        }
    }
}
```

## 方案优势

1. **简化架构**：只需要维护一个系统列表
2. **增强扩展性**：易于添加更多类型的系统并指定执行顺序
3. **提高可读性**：通过枚举定义排序类型，使代码更具可读性
4. **保持兼容性**：现有系统只需少量修改即可适配新架构
5. **灵活排序**：未来可以轻松调整系统执行顺序

## 实施步骤

1. 创建 [SystemOrder](file:///C:/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/SystemOrder.cs#L3-L17) 枚举
2. 在 [ISystem](file:///C:/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/ISystem.cs#L6-L19) 接口中添加 `GetOrder()` 方法
3. 修改 [BaseSystem](file:///C:/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/BaseSystem.cs#L6-L73) 和 [PresentationBaseSystem](file:///C:/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/PresentationBaseSystem.cs#L7-L9) 类实现 `GetOrder()` 方法
4. 修改 [SystemManager](file:///C:/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/SystemManager.cs#L8-L57) 类，移除 [_presentationSystems](file:///C:/MyZiegler/github/ra2/client/Packages/ZLockstep/Runtime/Simulation/ECS/SystemManager.cs#L11-L11) 列表，实现统一排序机制
5. 测试系统运行确保功能正常