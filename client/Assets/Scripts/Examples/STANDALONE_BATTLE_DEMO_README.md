# 单机战斗Demo使用指南

## 概述

这是一个完整的单机RTS战斗Demo，模仿红警机制，包含：
- 256x256米地图（1米=1格）
- FlowField + RVO导航系统
- 2个敌对阵营（玩家 vs AI）
- 战斗系统（远程攻击，子弹弹道）
- 基地和坦克单位

## Unity场景设置

### 1. 创建场景

1. 创建一个新的Unity场景
2. 在场景中添加一个Plane（地面）
   - Scale设置为 (25.6, 1, 25.6) 来匹配256x256米的地图
   - Position设置为 (128, 0, 128) 让地图居中

### 2. 添加Demo组件

1. 创建空GameObject，命名为"BattleDemo"
2. 添加 `StandaloneBattleDemo` 组件
3. 配置组件：
   - **Logic Frame Rate**: 30（逻辑帧率）
   - **Auto Update**: 勾选（自动更新）
   - **Tank Radius**: 0.5（坦克半径）
   - **Tank Max Speed**: 3（坦克最大速度）
   - **View Root**: 拖入BattleDemo自己
   - **Ground Layer**: 选择Default层

### 3. 准备预制体

在 `Unit Prefabs` 数组中准备以下预制体（可以用简单的几何体代替）：

- **索引0**: 玩家基地（蓝色Cube，Scale: 10,2,10）
- **索引1**: AI基地（红色Cube，Scale: 10,2,10）
- **索引2**: 玩家坦克（蓝色Capsule，Scale: 1,1,1）
- **索引3**: AI坦克（红色Capsule，Scale: 1,1,1）
- **索引4**: 子弹（黄色Sphere，Scale: 0.3,0.3,0.3）

## 操作说明

### 基本操作

- **左键点击**: 在点击位置创建己方坦克（蓝色）
- **右键点击**: 让选中的单位移动到目标位置
- **Q键**: 在鼠标位置创建AI坦克（红色）

### 选择和控制

- **A键**: 选中所有己方单位
- 创建新单位时会自动选中

### 视图控制

- **H键**: 切换生命值条显示
- **R键**: 切换攻击范围显示

### 调试功能

- **Space键**: 切换自动更新（暂停/继续）
- **M键**: 手动更新一帧（需要先暂停）

## 游戏机制

### 初始设置

- 游戏启动时会自动创建：
  - 左下角（20, 20）：玩家基地（蓝色）
  - 右上角（230, 230）：AI基地（红色）

### 导航系统

- 使用FlowField（流场）寻路
- RVO（Reciprocal Velocity Obstacles）避障
- 单位会自动绕过障碍物和建筑

### 战斗系统

- **自动攻击**: 单位会自动搜索并攻击范围内的敌人
- **攻击范围**: 10米
- **攻击伤害**: 30点
- **攻击间隔**: 2秒
- **追踪弹道**: 子弹会追踪移动的目标

### AI系统

- AI单位会自动移动到玩家基地附近
- 每3秒重新评估一次目标
- 战斗由CombatSystem自动处理

## 可视化

### Scene视图中的Gizmos

- **黄色边框**: 地图边界（256x256米）
- **红色障碍物**: 地图边界和建筑占据的格子
- **蓝色半透明立方体**: 玩家建筑
- **红色半透明立方体**: AI建筑
- **绿色线框球**: 选中的单位
- **生命值条**: 显示单位和建筑的生命值
- **攻击范围**: 半透明球形显示攻击范围

### GUI信息

左上角显示：
- 当前帧数
- 玩家单位数量 | AI单位数量
- 建筑数量
- 选中单位数量
- 自动更新状态
- 活跃流场数量

## 测试流程

1. **启动游戏**: 运行场景，会看到地图和两个基地
2. **创建单位**: 左键点击地面创建蓝色坦克
3. **移动单位**: 右键点击让坦克移动
4. **创建敌人**: Q键创建红色AI坦克
5. **观察战斗**: 敌我单位接近时会自动开火
6. **观察AI**: AI坦克会自动移动到玩家基地

## 架构说明

### 核心类

- **BattleGame**: 自定义Game类，管理战斗相关系统
- **StandaloneBattleDemo**: Unity入口，处理输入和可视化
- **SimpleAISystem**: 简单AI，控制敌方单位

### ECS组件

- **CampComponent**: 阵营标识
- **BuildingComponent**: 建筑数据（占据格子）
- **ProjectileComponent**: 弹道数据
- **AttackComponent**: 攻击能力
- **HealthComponent**: 生命值

### ECS系统

- **MovementSystem**: 处理移动
- **CombatSystem**: 处理战斗（目标选择、发射子弹）
- **ProjectileSystem**: 处理弹道（移动、命中、伤害）
- **FlowFieldNavigationSystem**: 流场导航
- **SimpleAISystem**: AI控制

## 性能优化建议

- 如果单位过多导致卡顿，可以：
  - 降低逻辑帧率（如30→20）
  - 减少流场更新频率
  - 限制最大单位数量

## 扩展建议

### 可以添加的功能

1. **更多单位类型**: 步兵、飞机、防御塔
2. **资源系统**: 采集资源、建造单位
3. **多人网络**: 切换到NetworkClient模式
4. **技能系统**: 特殊技能和效果
5. **编队系统**: 单位编队移动
6. **迷雾战争**: 视野和侦察系统

### 修改建议

- 修改 `BattleGame.CreateTank()` 来自定义单位属性
- 修改 `SimpleAISystem.EvaluateAIUnits()` 来改进AI策略
- 修改 `CombatSystem` 来添加新的攻击类型

## 故障排除

### 单位不移动
- 检查是否勾选了Auto Update
- 确保地图初始化成功（查看Console日志）
- 确保点击位置在地图范围内（0-256米）

### 单位无法攻击
- 确保单位有AttackComponent
- 检查攻击范围是否足够
- 确保目标是敌对阵营

### AI不工作
- 确保SimpleAISystem已注册
- 检查是否找到了玩家基地（查看Console日志）
- 确保AI单位有FlowFieldNavigatorComponent

## 文件列表

### 新增文件

#### ECS组件
- `Packages/ZLockstep/Runtime/Simulation/ECS/Components/CampComponent.cs`
- `Packages/ZLockstep/Runtime/Simulation/ECS/Components/BuildingComponent.cs`
- `Packages/ZLockstep/Runtime/Simulation/ECS/Components/ProjectileComponent.cs`

#### ECS系统
- `Packages/ZLockstep/Runtime/Simulation/ECS/Systems/CombatSystem.cs`
- `Packages/ZLockstep/Runtime/Simulation/ECS/Systems/ProjectileSystem.cs`

#### 命令
- `Packages/ZLockstep/Runtime/Sync/Command/Commands/CreateBuildingCommand.cs`

#### Demo类
- `Assets/Scripts/Examples/BattleGame.cs` - 自定义Game类
- `Assets/Scripts/Examples/StandaloneBattleDemo.cs` - Unity入口
- `Assets/Scripts/Examples/SimpleAISystem.cs` - AI系统

## 许可

这个Demo基于ZLockstep框架构建，遵循项目原有许可协议。

