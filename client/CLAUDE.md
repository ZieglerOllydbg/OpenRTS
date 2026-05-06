# 萌宠奇兵 Unity客户端项目结构文档

## 项目基本信息

- **项目名称**: 萌宠奇兵 (PetsWar)
- **产品名称**: 萌宠奇兵
- **游戏类型**: 卡通风格RTS（即时战略）游戏
- **核心玩法**: 剪刀石头布克制系统
- **公司名称**: DefaultCompany
- **Unity版本**: 2021.3.x (基于URP 12.1.15推断)
- **渲染管线**: Universal Render Pipeline (URP)
- **目标平台**: WebGL (微信小游戏)、Android、Standalone

---

## 核心玩法

### 剪刀石头布克制系统

游戏采用剪刀石头布的克制机制作为核心战斗玩法：
- **剪刀克制布** - 攻击型单位克制远程/支援型单位
- **布克制石头** - 远程/支援型单位克制防御型单位  
- **石头克制剪刀** - 防御型单位克制攻击型单位

这种设计让每个单位都有其优势和劣势，鼓励玩家进行战术搭配和策略思考。

---

## 1. 根目录结构

```
client/
├── Assets/               # Unity资源目录 (核心)
├── Packages/             # 自定义包和依赖
├── ProjectSettings/      # Unity项目设置
├── Library/              # Unity缓存 (不提交)
├── Temp/                 # 临时文件
├── Logs/                 # 日志文件
├── UserSettings/         # 用户设置
├── TextToolDatas/        # 文本工具数据
├── .vscode/              # VS Code配置
├── .cursor/              # Cursor编辑器配置
├── client.sln            # Visual Studio解决方案
└── *.csproj              # C#项目文件 (自动生成)
```

---

## 2. Assets 目录结构 (核心资源)

### 2.1 主要目录概览

| 目录 | 用途 |
|------|------|
| `Scripts/` | 主要游戏逻辑代码 |
| `RTSCameraController/` | RTS相机控制系统 |
| `WX-WASM-SDK-V2/` | 微信小游戏WASM SDK |
| `WebGLTemplates/` | WebGL发布模板 |
| `Resources/` | 运行时加载资源 |
| `Scenes/` | Unity场景文件 |
| `Samples/` | 示例和第三方资源 |
| `UniGLTF/` | GLTF模型加载库 |
| `TextMesh Pro/` | 文本渲染资源 |
| `Editor/` | 编辑器扩展脚本 |

### 2.2 Scripts 目录 (游戏逻辑)

```
Assets/Scripts/
├── Main.cs              # 游戏入口
├── Test.cs              # 测试脚本
├── AnimEvent.cs         # 动画事件处理
├── Examples/            # 示例代码
│   ├── AICommandExample.cs
│   ├── NetworkCommandExample.cs
│   ├── DataManagerTest.cs
│   └── ResourceCacheExample.cs
├── JoyStick/            # 虚拟摇杆
│   ├── VirtualJoystick.cs
│   └── VirtualJoystickDevice.cs
├── Modules/             # 功能模块
│   └── DemoModule/
├── NetWork/             # 网络通信
│   ├── NetworkManager.cs
│   ├── WebSocketClient.cs
│   └── WebSocketTest.cs
├── Ra2Demo/             # 游戏核心逻辑
│   ├── Ra2Demo.cs            # 主游戏控制器
│   ├── Ra2DemoDebugger.cs    # 调试器
│   ├── EcsUtils.cs           # ECS工具类
│   ├── MiniMapController.cs  # 小地图控制器
│   ├── LoginWX.cs            # 微信登录
│   ├── PresentationSystem/   # 表现层系统
│   └── UIModule/             # UI模块
└── Utils/               # 工具类
```

### 2.3 游戏核心/UI模块

```
Assets/Scripts/Ra2Demo/UIModule/
├── Ra2Module.cs              # 模块定义
├── Ra2Processor.cs           # 事件处理器
├── MessageUtils.cs           # 消息工具
├── UISound.cs                # UI音效
├── Component/                # UI组件
│   └── ConfirmDialogComponent.cs
├── Event/                    # UI事件定义
│   ├── EconomyEvent.cs
│   ├── GameStartEvent.cs
│   ├── HealthEvent.cs
│   ├── LoginEvent.cs
│   └── ...
└── Panel/                    # UI面板
    ├── HealthPanel.cs
    ├── LoadingPanel.cs
    ├── LoginPanel.cs
    ├── MainPanel.cs
    ├── MatchPanel.cs
    ├── SettlePanel.cs
    └── SubPanel/             # 子面板
        ├── BuildingListItem.cs
        ├── HotKeySubPanel.cs
        ├── MainBuildingSubPanel.cs
        └── ...
```

### 2.4 Resources (运行时资源)

```
Assets/Resources/
├── Prefabs/              # 预制体 (萌宠单位)
├── UI/                   # UI资源
├── Data/Json/            # 数据配置
│   ├── Unit.json
│   └── Camp.json
├── Audio/                # 音频资源
├── Material/             # 材质
├── Images/               # 图片资源
├── FBX/                  # 3D模型
└── Fonts/                # 字体
```

### 2.5 Scenes (场景)

| 场景文件 | 用途 |
|----------|------|
| `Ra2Demo.unity` | 主游戏场景 |
| `Battle.unity` | 战斗场景 |
| `SampleScene.unity` | 示例场景 |
| `UITest.unity` | UI测试场景 |

---

## 3. Packages 目录 (自定义包)

### 3.1 ZFramework (核心框架)

**版本**: 1.1.0

```
Packages/ZFramework/Runtime/
├── Async/        # 异步支持
├── Common/       # 通用工具 (缓存、对象池)
├── Log/          # 日志系统
├── Loom/         # 线程调度
├── MVC/          # MVC框架
├── Services/     # 服务层
├── Tick/         # 定时器
├── UI/           # UI框架
└── WebRequest/   # 网络请求
```

**主要功能**: MVC架构、UI管理、对象池、缓存、异步编程、日志系统

### 3.2 ZLockstep (帧同步引擎)

**版本**: 1.0.0

```
Packages/ZLockstep/Runtime/
├── Core/               # 核心数学库
│   ├── zfloat.cs           # 定点数
│   ├── zVector3.cs         # 定点向量
│   ├── zQuaternion.cs      # 定点四元数
│   ├── Math/               # 数学工具
│   └── PathFinding/        # 寻路算法
├── Simulation/         # 仿真系统 (ECS架构)
│   ├── zWorld.cs           # 游戏世界
│   └── ECS/
│       ├── Components/     # 组件定义
│       └── Systems/        # 系统实现
├── Sync/               # 帧同步
│   ├── Game.cs             # 游戏核心
│   ├── FrameSyncManager.cs
│   └── Command/            # 命令系统
├── Flow/               # 流场寻路
│   ├── FlowFieldCore.cs
│   └── FlowFieldManager.cs
├── RVO/                # RVO避障算法
├── View/               # 视图层
└── WebSocket/          # WebSocket通信
```

**架构特点**:
- 确定性计算: 使用定点数(zfloat)保证跨平台一致性
- ECS架构: Entity-Component-System模式
- 命令系统: 所有操作通过Command执行，支持回放
- 流场寻路: 大规模单位寻路优化
- RVO避障: 实时避障算法

---

## 4. 核心系统架构

```
┌─────────────────────────────────────────────────────────┐
│                    表现层 (Presentation)                │
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐       │
│ │ GameCore.cs │ │ Presentation│ │  UI Module  │       │
│ │             │ │   System    │ │   (Panel)   │       │
│ └─────────────┘ └─────────────┘ └─────────────┘       │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│                   游戏逻辑层 (GameLogic)                │
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐       │
│ │   Game.cs   │ │  FrameSync  │ │   Command   │       │
│ │             │ │   Manager   │ │   System    │       │
│ └─────────────┘ └─────────────┘ └─────────────┘       │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│                  ECS核心层 (Simulation)                 │
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐       │
│ │   zWorld    │ │  Entities   │ │   Systems   │       │
│ │             │ │ Components  │ │             │       │
│ └─────────────┘ └─────────────┘ └─────────────┘       │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│                 数学层 (Deterministic)                  │
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐       │
│ │   zfloat    │ │  zVector3   │ │ zQuaternion │       │
│ └─────────────┘ └─────────────┘ └─────────────┘       │
└─────────────────────────────────────────────────────────┘
```

---

## 5. 主要ECS组件

| 组件 | 功能 |
|------|------|
| `TransformComponent` | 位置、旋转 |
| `HealthComponent` | 生命值 |
| `AttackComponent` | 攻击属性 |
| `UnitComponent` | 单位通用属性 |
| `BuildingComponent` | 建筑属性 |
| `EconomyComponent` | 经济资源 |
| `ProjectileComponent` | 投射物 |
| `VelocityComponent` | 速度 |
| `TurretComponent` | 炮塔 |
| `MiningComponent` | 采矿 |
| `ProduceComponent` | 生产 |

## 6. 主要系统

| 系统 | 功能 |
|------|------|
| `MovementSystem` | 单位移动 |
| `CombatSystem` | 战斗处理 (含克制计算) |
| `HealthSystem` | 生命值管理 |
| `EconomySystem` | 经济系统 |
| `ProduceSystem` | 生产系统 |
| `AutoHealSystem` | 自动回血 |
| `DeathRemovalSystem` | 死亡移除 |
| `RotationSystem` | 旋转控制 |
| `ProjectileSystem` | 投射物系统 |
| `SettlementSystem` | 结算系统 |
| `BuildingConstructionSystem` | 建筑建造 |
| `FirstAISystem` | AI系统 |

---

## 7. 依赖包

### 7.1 Unity官方包

| 包名 | 用途 |
|------|------|
| `com.unity.render-pipelines.universal` | URP渲染管线 |
| `com.unity.inputsystem` | 新输入系统 |
| `com.unity.cinemachine` | 相机系统 |
| `com.unity.textmeshpro` | 文本渲染 |
| `com.unity.postprocessing` | 后处理 |

### 7.2 第三方包

| 包名 | 用途 |
|------|------|
| `com.coplaydev.unity-mcp` | Unity MCP工具 |
| `com.posthog.unity` | PostHog分析 |

---

## 8. 构建配置

### 8.1 目标平台

- **WebGL** (主要目标 - 微信小游戏)
- **Android**
- **Standalone** (Windows/Mac)

### 8.2 WebGL配置

- 模板: `WXTemplate2020`
- WASM支持
- 内存: 256MB
- 压缩格式: gzip

---

## 9. 关键文件路径

### 核心入口
- `Assets/Scripts/Main.cs` - 游戏入口
- `Assets/Scripts/Ra2Demo/Ra2Demo.cs` - 主游戏控制器

### 网络通信
- `Assets/Scripts/NetWork/WebSocketClient.cs`
- `Packages/ZLockstep/Runtime/WebSocket/WebSocket.cs`

### 帧同步核心
- `Packages/ZLockstep/Runtime/Sync/Game.cs`
- `Packages/ZLockstep/Runtime/Sync/FrameSyncManager.cs`

### ECS系统
- `Packages/ZLockstep/Runtime/Simulation/zWorld.cs`
- `Packages/ZLockstep/Runtime/Simulation/ECS/`

### UI系统
- `Assets/Scripts/Ra2Demo/UIModule/`

### 配置数据
- `Assets/Resources/Data/Json/Unit.json`
- `Assets/Resources/Data/Json/Camp.json`

---

## 10. 文档参考

| 文档 | 位置 |
|------|------|
| 帧同步架构说明 | `Packages/ZLockstep/Runtime/ARCHITECTURE.md` |
| 帧同步使用指南 | `Packages/ZLockstep/Runtime/README.md` |
| 命令系统说明 | `Packages/ZLockstep/Runtime/Sync/Command/README_COMMAND_SYSTEM.md` |
| 流场系统说明 | `Packages/ZLockstep/Runtime/Flow/FLOW_FIELD_README.md` |
| ECS系统说明 | `Packages/ZLockstep/Runtime/Simulation/ECS/System.md` |
| 资源缓存说明 | `Packages/ZLockstep/Runtime/View/Cache/RESOURCE_CACHE_README.md` |

---

*文档生成时间: 2026-04-10*
