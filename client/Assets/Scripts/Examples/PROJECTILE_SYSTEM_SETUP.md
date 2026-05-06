# 弹道系统实现文档

## 概述

弹道系统现已完整实现，包含ECS逻辑层和Unity表现层的完整支持。

## ECS架构

### 弹道Entity组件结构

每个弹道Entity包含以下组件：

1. **TransformComponent** - 空间位置、旋转、缩放
   - Position: 弹道当前3D位置
   - Rotation: 飞行方向
   - Scale: 大小（默认1）

2. **ProjectileComponent** - 弹道逻辑数据
   - SourceEntityId: 发射者Entity ID
   - TargetEntityId: 目标Entity ID
   - Damage: 伤害值
   - Speed: 飞行速度（10米/秒）
   - TargetPosition: 目标位置（追踪型会更新）
   - IsHoming: 是否追踪目标（true）
   - SourceCampId: 发射者阵营

3. **VelocityComponent** - 运动速度向量
   - 根据飞行方向和速度计算

4. **UnitComponent** - 视图标识信息（新增）
   - UnitType: 100（表示弹道类型）
   - PrefabId: 4（弹道预制体ID）
   - PlayerId: 继承发射者阵营
   - MoveSpeed: 飞行速度

5. **CampComponent** - 阵营归属（新增）
   - CampId: 继承发射者阵营
   - 便于统一查询和碰撞检测

6. **ViewComponent** - 表现层引用（自动添加）
   - GameObject: Unity GameObject引用
   - Transform: Unity Transform引用
   - 由PresentationSystem自动添加

## 系统流程

### 1. 创建阶段（CombatSystem）

```
单位发起攻击
  ↓
CombatSystem.FireProjectile()
  ↓
创建弹道Entity
  ↓
添加所有必要组件
  ↓
发布 UnitCreatedEvent
  ↓
日志输出: [CombatSystem] 发射弹道Entity_XXX
```

### 2. 更新阶段（ProjectileSystem）

```
每个逻辑帧:
  ↓
遍历所有 ProjectileComponent
  ↓
更新目标位置（追踪型）
  ↓
计算到目标的方向和距离
  ↓
检查是否命中（距离 <= 0.5米）
  ├─ 是 → 造成伤害 → 销毁弹道
  └─ 否 → 更新位置和朝向 → 继续飞行
```

### 3. 表现阶段（PresentationSystem）

```
接收 UnitCreatedEvent
  ↓
检查是否有预制体（PrefabId=4）
  ├─ 有 → 实例化预制体
  └─ 无 → 创建默认可视化
      ↓
      Sphere（0.3米直径）
      + TrailRenderer（拖尾效果）
      + 发光材质（根据阵营着色）
  ↓
添加 ViewComponent
  ↓
每帧同步Transform
```

## 参数配置

### 弹道参数

| 参数 | 值 | 说明 |
|-----|-----|------|
| 飞行速度 | 10米/秒 | 从CombatSystem.FireProjectile设置 |
| 命中距离 | 0.5米 | ProjectileSystem.HIT_DISTANCE |
| 追踪模式 | true | 追踪型导弹，会更新目标位置 |
| 起始高度 | +0.5米 | 相对于发射者位置 |

### 可视化参数

| 参数 | 值 | 说明 |
|-----|-----|------|
| 弹头大小 | 0.3米 | Sphere直径 |
| 拖尾时长 | 0.5秒 | TrailRenderer.time |
| 拖尾宽度 | 0.2米→0.05米 | 起始→结束 |
| 颜色 | 蓝色/红色 | 根据阵营（0=蓝，1=红）|
| 发光强度 | 2倍 | EmissionColor |

## 测试方法

### 基本测试

1. **创建双方单位**
   - 左键：创建己方坦克
   - Q键：创建AI坦克

2. **触发战斗**
   - 让双方单位靠近（15米内）
   - 等待自动追击系统工作
   - 当距离 < 10米时开始攻击

3. **观察弹道**
   - 看到小球从攻击者飞向目标
   - 带有拖尾效果
   - 蓝色=玩家弹道，红色=AI弹道
   - 飞行约1秒后命中

### 日志验证

控制台应该看到：

```
[CombatSystem] 发射弹道Entity_123: 45 -> 67, 伤害30
[PresentationSystem] 创建了默认弹道可视化: Entity_123
[PresentationSystem] 为Entity_123创建了视图: Unit_123_Type100_P0
[ProjectileSystem] 实体67已死亡并被销毁  // 目标被击杀
```

### 场景视图检查

1. **Hierarchy视图**
   - 查找 `Projectile_XXX` GameObject
   - 子节点 `ProjectileHead`（Sphere）
   - 组件：TrailRenderer

2. **Inspector视图**
   - TrailRenderer有拖尾效果
   - Material是发光材质
   - Transform在不断更新

## 自定义弹道预制体（可选）

如果想使用自己的弹道模型：

1. **创建预制体**
   - 准备弹道模型（导弹、箭矢等）
   - 添加TrailRenderer（可选）
   - 保存为Prefab

2. **配置到Demo**
   - 在 `StandaloneBattleDemo` Inspector
   - 找到 `Unit Prefabs` 数组
   - 在索引 **4** 位置拖入你的弹道预制体

3. **重新测试**
   - PresentationSystem会使用你的预制体
   - 不再创建默认Sphere

## 性能考虑

### 当前性能

- 弹道Entity是轻量级的（6个组件）
- 自动销毁（命中或目标消失时）
- 典型生命周期：1秒左右

### 优化建议

1. **对象池**：如果弹道很多，可以实现对象池
2. **LOD**：远距离弹道降低细节
3. **批处理**：使用相同材质以支持动态批处理

## 扩展功能

### 可以添加的功能

1. **非追踪弹道**
   - 设置 `IsHoming = false`
   - 直线飞行，不更新目标位置

2. **溅射伤害**
   - 在ProjectileSystem.DealDamage中检测范围内敌人
   - 对多个目标造成伤害

3. **命中特效**
   - 监听弹道销毁事件
   - 在命中点播放粒子特效

4. **不同弹道类型**
   - 子弹（快速，直线）
   - 导弹（追踪，较慢）
   - 炮弹（抛物线，范围伤害）

## 故障排查

### 问题1: 看不到弹道

**检查**：
- 控制台有 `[CombatSystem] 发射弹道` 日志吗？
  - 无 → 单位可能距离太远，无法攻击
  - 有 → 继续下一步
- 控制台有 `[PresentationSystem] 创建了默认弹道可视化` 吗？
  - 无 → PresentationSystem可能未启用
  - 有 → 弹道可能飞太快，已经销毁

**解决**：
- 确保单位距离 < 10米
- 检查 Camera 视角能看到弹道路径
- 降低弹道速度（在CombatSystem中修改）

### 问题2: 弹道不命中

**检查**：
- 目标是否移动过快？
- 弹道速度是否足够快？

**解决**：
- 增加弹道速度
- 或增加命中距离阈值

### 问题3: 弹道穿透目标

**检查**：
- ProjectileSystem的命中距离可能太小

**解决**：
- 增大 `HIT_DISTANCE` 值（当前0.5米）

## 总结

弹道系统已完全集成到ECS架构中：
- ✅ 完整的组件结构
- ✅ 逻辑层与表现层分离
- ✅ 自动可视化（带拖尾效果）
- ✅ 追踪目标功能
- ✅ 支持自定义预制体

现在你可以在游戏中看到完整的战斗过程，包括单位追击、攻击、弹道飞行和命中效果！

