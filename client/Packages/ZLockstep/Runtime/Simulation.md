# 仿真核心设计文档 (Simulation Core Design)

## 1. 概述 (Overview)

仿真核心是整个帧同步框架的心脏，负责驱动游戏世界状态的确定性演进。它由以下几个关键模块组成：
- **World (游戏世界)**
- **ECS (实体-组件-系统)**
- **Time (时间管理器)**
- **Event System (事件系统)**

---

## 2. World (游戏世界)

### 2.1 职责 (Responsibilities)
`World` 是所有游戏状态的顶层容器和管理者。它是仿真循环的入口点。
- **状态容器**: 持有并管理所有的 `Systems`、`Entities` 和 `Components`。
- **生命周期管理**: 控制整个仿真过程的初始化 (`Init`)、每帧更新 (`Update`) 和销毁 (`Shutdown`)。
- **系统调度**: 按照预先设定的确定性顺序，在每帧调用所有 `System` 的更新逻辑。
- **服务定位器**: 作为中心枢纽，提供对其他核心模块（如 `TimeManager`, `EventManager`）的访问。

### 2.2 核心API (Core API)
```csharp
public class zWorld
{
    // 管理器引用
    public EntityManager EntityManager { get; private set; }
    public SystemManager SystemManager { get; private set; }
    public EventManager EventManager { get; private set; }
    public TimeManager TimeManager { get; private set; }

    // 当前帧数
    public int Tick { get; private set; }

    // 初始化世界
    public void Init();

    // 驱动世界前进一帧
    public void Update();

    // 销毁世界
    public void Shutdown();
}
```

---

## 3. ECS (实体-组件-系统)

ECS是数据驱动设计的核心，它将游戏对象拆分为数据（Component）、身份（Entity）和逻辑（System），以实现高度解耦和可维护性。

### 3.1 Entity (实体)
- **定义**: 一个简单的整数ID，用于唯一标识一个游戏对象。它本身不包含任何数据或逻辑。
- **管理者**: `EntityManager` 负责实体的创建、销毁和ID复用，以确保高效和稳定。

### 3.2 Component (组件)
- **定义**: 纯数据容器，通常是 `struct` 或简单的 `class`。**组件不应包含任何游戏逻辑**。
- **示例**: `PositionComponent`, `HealthComponent`, `VelocityComponent`。
- **存储**: 组件数据由 `ComponentManager` (或直接由 `EntityManager`) 统一管理，通常存储在紧凑的数组中，以利用CPU缓存，提高访问速度。

### 3.3 System (系统)
- **定义**: 包含了所有的游戏逻辑。`System` 负责处理具有特定组件组合的实体。
- **职责**:
  - 定义其关心的组件查询（例如：所有同时拥有 `PositionComponent` 和 `VelocityComponent` 的实体）。
  - 在 `Update` 方法中，遍历所有符合条件的实体，并更新它们的组件数据。
- **执行顺序**: 所有 `System` 的执行顺序必须是固定的、确定性的，由 `SystemManager` 统一调度。这对于保证帧同步至关重要。
- **示例**: `MovementSystem` 遍历实体，读取其 `VelocityComponent`，然后更新其 `PositionComponent`。

### 3.4 SystemManager (系统管理器)
`SystemManager` 扮演着所有 `System` 的“**调度中心**”角色，其核心职责如下：
- **管理System生命周期**: 负责注册、存储和管理游戏世界中所有的 `System` 实例。
- **保证确定性执行顺序**: 这是它在帧同步框架中**最重要**的职责。`SystemManager` 维护一个固定的、有序的 `System` 列表。在每帧更新时，它会严格按照这个预设顺序依次调用每个 `System` 的 `Update` 方法。这确保了无论在哪台客户端上，游戏逻辑的执行顺序都是完全一致的，从而避免状态不一致（desync）。
- **提供统一更新入口**: `zWorld` 无需关心具体的 `System` 有哪些，只需在自己的 `Update` 循环中调用 `SystemManager` 的一个总更新方法（如 `UpdateAll()`），即可驱动所有逻辑系统的运行，实现了 `World` 与具体业务逻辑的解耦。

---

## 4. Time (时间管理器)

### 4.1 职责 (Responsibilities)
`