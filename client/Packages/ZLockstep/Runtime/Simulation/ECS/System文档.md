# ECS System 系统文档

## 概述

ECS（Entity-Component-System）架构中的System（系统）负责处理游戏逻辑。系统通过实现[ISystem](ISystem.cs)接口来定义，并由[SystemManager](SystemManager.cs)进行统一管理和调度。所有系统按照预定义的执行顺序进行更新，确保逻辑的一致性和正确性。

## 核心接口和类

### ISystem 接口

[ISystem.cs](ISystem.cs) 定义了系统的基本接口，所有系统都必须实现此接口。

主要方法：

- `SetWorld(zWorld world)`：设置系统所属的游戏世界，由SystemManager在注册系统时调用。
- `Update()`：系统的主更新逻辑，每帧调用一次。
- `GetOrder()`：获取系统执行顺序，返回值越小优先级越高。

### BaseSystem 基类

[BaseSystem.cs](BaseSystem.cs) 是所有系统的基类，实现了ISystem接口的基本功能。

主要特性：

- 提供对游戏世界各种管理器的快捷访问：
  - `World`：游戏世界引用
  - `EntityManager`：实体管理器
  - `ComponentManager`：组件管理器
  - `EventManager`：事件管理器
  - `TimeManager`：时间管理器
- 提供常用属性：
  - `Tick`：当前逻辑帧
  - `DeltaTime`：逻辑帧间隔时间
- 虚方法：
  - `OnInitialize()`：系统初始化时调用
  - `OnDestroy()`：系统销毁时调用
  - `GetOrder()`：获取系统执行顺序，默认返回SystemOrder.Normal

### PresentationBaseSystem 表现系统基类

[PresentationBaseSystem.cs](PresentationBaseSystem.cs) 是所有表现层系统的基类，继承自BaseSystem。

主要特性：

- 重写了`GetOrder()`方法，返回`SystemOrder.Presentation`，确保表现系统在普通系统之后执行
- 专门用于处理与Unity引擎显示相关的逻辑

### SystemManager 系统管理器

[SystemManager.cs](SystemManager.cs) 负责管理系统生命周期和更新顺序。

主要功能：

- `RegisterSystem(ISystem system)`：注册系统，设置系统所属世界并按执行顺序排序
- `UpdateAll()`：按顺序更新所有系统

系统注册时会根据`GetOrder()`返回值进行排序，确保系统按正确顺序执行。

## 系统执行顺序

[SystemOrder.cs](SystemOrder.cs) 定义了系统执行顺序的枚举：

```csharp
public enum SystemOrder 
{
    /// <summary>
    /// 普通系统（默认）
    /// </summary>
    Normal = 0,
    
    /// <summary>
    /// 表现系统
    /// </summary>
    Presentation = 1
}
```

系统按照枚举值从小到大执行，即Normal(0) -> Presentation(1)。

## 使用示例

创建一个自定义系统：

```csharp
public class MyCustomSystem : BaseSystem
{
    protected override void OnInitialize()
    {
        // 系统初始化逻辑
    }

    public override void Update()
    {
        // 系统主更新逻辑
        // 可以访问EntityManager、ComponentManager等
    }
    
    public override int GetOrder() => (int)SystemOrder.Normal;
}
```

注册系统到SystemManager：

```csharp
systemManager.RegisterSystem(new MyCustomSystem());
```

系统会在每帧调用UpdateAll()时按顺序执行：

```csharp
systemManager.UpdateAll();
```