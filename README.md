# OpenRTS

## 简介

OpenRTS 是一个基于 Unity 和 Java 的实时策略（RTS）游戏框架，采用帧同步技术实现多人联机对战。项目使用自定义 ECS 三层架构，核心特点包括：

- **确定性**：通过定点数系统保证所有客户端计算结果完全一致
- **可回放**：完整支持游戏回放功能
- **断线重连**：支持追帧、断线重连、杀端重连等网络异常处理
- **模块化设计**：严格分层，核心逻辑层完全独立于 Unity

## 演示视频

![OpenRTS 演示](docs/OpenRTS.mp4)

## 快速开始

### 启动客户端

1. 使用 Unity 打开 `client` 目录下的项目
2. 打开场景：
   - [OpenRTS.unity](client/Assets/Scenes/OpenRTS.unity) - OpenRTS 主场景
3. 点击运行即可体验本地单机模式

### 启动服务器

服务器位于 `server/ra2server` 目录，基于 Java 11 + Netty + WebSocket 实现。

1. 进入服务器目录：`cd server/ra2server`
2. 启动服务器：运行 [GameStartUp.java](server/ra2server/src/main/java/org/game/ra2/GameStartUp.java)
3. 服务器默认端口：8080
4. 客户端连接到 `ws://localhost:8080` 即可联机对战

## 项目基础框架 ECS

项目采用严格的三层架构设计，各层相互独立，保证核心逻辑的可移植性和确定性：

### 第1层 - 逻辑核心层（Simulation）

- **目录**：[client/Packages/ZLockstep/Runtime/Simulation](client/Packages/ZLockstep/Runtime/Simulation)
- **核心类**：[zWorld.cs](client/Packages/ZLockstep/Runtime/Simulation/zWorld.cs)
- **包含内容**：
  - ECS 系统：EntityManager、ComponentManager、SystemManager
  - 游戏系统：MovementSystem、CombatSystem、ProjectileSystem 等
  - 定点数数学库：zfloat、zVector2、zVector3、zQuaternion 等
  - 流场寻路：FlowFieldSystem、FlowFieldManager
  - RVO 避障系统
- **特点**：完全不依赖 Unity，可在任何 C# 环境运行

### 第2层 - 应用控制层（Sync）

- **目录**：[client/Packages/ZLockstep/Runtime/Sync](client/Packages/ZLockstep/Runtime/Sync)
- **核心类**：[Game.cs](client/Packages/ZLockstep/Runtime/Sync/Game.cs)、[FrameSyncManager.cs](client/Packages/ZLockstep/Runtime/Sync/FrameSyncManager.cs)
- **包含内容**：
  - 帧同步管理
  - 命令系统：CommandManager、CommandBuffer
  - 各种游戏命令：MoveCommand、CreateUnitCommand 等
  - 确定性校验：DeterminismProbe
- **特点**：管理游戏流程，不知道 Unity 的存在

### 第3层 - Unity 桥接层（View）

- **目录**：[client/Packages/ZLockstep/Runtime/View](client/Packages/ZLockstep/Runtime/View)
- **核心类**：[GameWorldBridge.cs](client/Packages/ZLockstep/Runtime/View/GameWorldBridge.cs)
- **包含内容**：
  - 表现系统：PresentationSystem
  - 视图组件：ViewComponent（唯一引用 Unity 的组件）
- **特点**：将逻辑层状态同步到 Unity 渲染层

> 详细架构说明请参考：[ARCHITECTURE.md](client/Packages/ZLockstep/Runtime/ARCHITECTURE.md)

## 定点数基础类介绍

为了保证所有客户端的计算结果完全一致，核心数学库使用定点数实现。

### 核心类型

- **zfloat**：Q16.16 格式定点数实现 ([zfloat.cs](client/Packages/ZLockstep/Runtime/Core/zfloat.cs))
  - 高精度：小数点后保留 16 位
  - 支持基本运算：+、-、*、/、比较等

### 数学库

所有数学运算都使用定点数，包括：

- [zVector2.cs](client/Packages/ZLockstep/Runtime/Core/Math/zVector2.cs) - 2D 向量
- [zVector3.cs](client/Packages/ZLockstep/Runtime/Core/Math/zVector3.cs) - 3D 向量
- [zVector4.cs](client/Packages/ZLockstep/Runtime/Core/Math/zVector4.cs) - 4D 向量
- [zQuaternion.cs](client/Packages/ZLockstep/Runtime/Core/Math/zQuaternion.cs) - 四元数
- [zMatrix4x4.cs](client/Packages/Zlockstep/Runtime/Core/Math/zMatrix4x4.cs) - 4x4 矩阵
- [zMath.cs](client/Packages/ZLockstep/Runtime/Core/Math/zMath.cs) - 数学函数
- [zRandom.cs](client/Packages/ZLockstep/Runtime/Core/Math/zRandom.cs) - 随机数生成器
- [zBounds.cs](client/Packages/ZLockstep/Runtime/Core/Math/zBounds.cs) - 边界
- [zRay.cs](client/Packages/ZLockstep/Runtime/Core/Math/zRay.cs) - 射线

> 定点数 API 文档：[zfloat.API.md](client/Packages/ZLockstep/Runtime/Core/zfloat.API.md)

## 帧同步实现

帧同步是多人联机的核心技术，确保所有客户端在相同的逻辑帧执行相同的操作。

### 核心组件

- **FrameSyncManager**：帧同步管理器 ([FrameSyncManager.cs](client/Packages/ZLockstep/Runtime/Sync/FrameSyncManager.cs))
- **Game**：游戏类，控制游戏流程 ([Game.cs](client/Packages/ZLockstep/Runtime/Sync/Game.cs))
- **CommandManager**：命令管理器，收集和分发玩家命令

### 工作流程

1. 服务器每 20ms（50FPS）向所有客户端发送帧同步消息
2. 客户端收集当前帧的所有玩家命令
3. 命令发送到服务器，服务器聚合后广播给所有客户端
4. 所有客户端执行相同的命令序列，确保状态一致

### 确定性校验

- **DeterminismProbe**：确定性探测器 ([DeterminismProbe.cs](client/Packages/ZLockstep/Runtime/Sync/Determinism/DeterminismProbe.cs))
  - 用于检测帧同步不同步问题
  - 详细说明：[DETERMINISM_PROBE_GUIDE.md](client/Packages/ZLockstep/Runtime/Sync/Determinism/DETERMINISM_PROBE_GUIDE.md)

> 完整帧同步指南：[LOCKSTEP_GUIDE.md](client/Packages/ZLockstep/Runtime/Sync/LOCKSTEP_GUIDE.md)

## 回放

回放系统通过记录所有玩家命令实现，支持完整回放和回放对比功能。

### 核心组件

- **CommandRecorder**：命令录制器 ([CommandRecorder.cs](client/Packages/ZLockstep/Runtime/Sync/Command/CommandRecorder.cs))
  - 记录每一帧的所有命令
- **CommandReader**：命令读取器 ([CommandReader.cs](client/Packages/ZLockstep/Runtime/Sync/Command/CommandReader.cs))
  - 读取录制的命令并重现游戏

### 回放功能

- 完整游戏回放
- 从任意帧开始回放
- 回放对比工具：对比两次回放的结果，检测不同步问题

> 回放对比工具文档：[回放对比工具](docs/tools/replay-diff-tool/replay-diff-tool.md)

## 联机帧同步

联机模式下，网络异常的处理是关键。OpenRTS 提供了完善的网络异常处理机制。

### 追帧

当客户端落后于服务器时，可以加速执行逻辑帧来追赶进度。

- **核心文档**：[CATCHUP_GUIDE.md](client/Packages/ZLockstep/Runtime/Sync/CATCHUP_GUIDE.md)
- **实现**：FrameSyncManager 中的追帧逻辑

### 断线重连

客户端断线后，可以从服务器获取历史帧数据，恢复到断线前的状态。

- **服务器组件**：
  - [HistoryFrameCacheService.java](server/ra2server/src/main/java/org/game/ra2/service/HistoryFrameCacheService.java) - 历史帧缓存服务
  - [HistoryFramePersistenceService.java](server/ra2server/src/main/java/org/game/ra2/service/HistoryFramePersistenceService.java) - 历史帧持久化服务
- **默认配置**：服务器保存最近 30 秒（600 帧）的历史数据

### 杀端重连

支持强制重连，RoomService 提供重连接口。

- **相关服务**：[RoomService.java](server/ra2server/src/main/java/org/game/ra2/service/RoomService.java)

## 服务器实现

服务器基于 Java 11 + Netty + WebSocket，提供匹配、房间管理、帧同步等功能。

### 核心组件

| 组件 | 文件 | 说明 |
|------|------|------|
| 服务器启动 | [GameStartUp.java](server/ra2server/src/main/java/org/game/ra2/GameStartUp.java) | 服务器入口 |
| WebSocket 服务器 | [WebSocketServer.java](server/ra2server/src/main/java/org/game/ra2/netty/WebSocketServer.java) | 处理 WebSocket 连接 |
| 匹配服务 | [MatchService.java](server/ra2server/src/main/java/org/game/ra2/service/MatchService.java) | 玩家匹配 |
| 房间服务 | [RoomService.java](server/ra2server/src/main/java/org/game/ra2/service/RoomService.java) | 房间管理 |
| 房间线程 | [RoomThread.java](server/ra2server/src/main/java/org/game/ra2/thread/RoomThread.java) | 以 20FPS 驱动房间逻辑 |
| 历史帧缓存 | [HistoryFrameCacheService.java](server/ra2server/src/main/java/org/game/ra2/service/HistoryFrameCacheService.java) | 支持断线重连 |

### 通信协议

- 协议文档：[协议.md](server/ra2server/协议.md)
- 数据格式：JSON
- 传输方式：WebSocket

### 服务器结构

详细结构说明：[项目结构.md](server/ra2server/项目结构.md)

## 工具

项目提供多个开发工具，方便开发调试：

| 工具 | 说明 |
|------|------|
| [导入 GLB](docs/tools/import-glb/import-glb.md) | 导入 glTF/GLB 格式模型 |
| [导出 APK](docs/tools/export-apk/export-apk.md) | Android 打包工具 |
| [地图编辑工具 - Terrain Tool](docs/tools/terrain-tool/terrain-tool.md) | 地形编辑工具 |
| [阻挡导出工具](docs/tools/blocker-generator/blocker-generator.md) | 导出阻挡区域 |
| [回放对比工具](docs/tools/replay-diff-tool/replay-diff-tool.md) | 回放不同步检测工具 |

### 默认配置

- **逻辑帧率**：20 FPS
- **服务器端口**：8080
- **网络协议**：WebSocket
- **数据格式**：JSON
- **阵营数量**：支持 1-8 人对战
- **历史帧保存**：游戏进行期间保存在内存中，房间销毁时持久化到磁盘
- **房间销毁延迟**：所有玩家断线后 30 秒销毁房间
