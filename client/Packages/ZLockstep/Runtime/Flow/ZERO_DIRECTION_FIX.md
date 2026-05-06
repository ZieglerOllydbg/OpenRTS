# 流场零方向问题修复

## 🐛 问题描述

在流场寻路中，单位会在**接近目标时卡住不动**，即使还没有真正到达目标点。

### 表现症状
- ✅ 单位能正常移动大部分距离
- ❌ 在距离目标约1个格子范围内突然停止
- ❌ 停止位置距离目标可能还有0.5-1.0单位距离
- ❌ 单位永远不会触发"到达目标"判定

### 根本原因

**流场方向场生成逻辑**（`FlowFieldCore.cs` 第249-254行）：

```csharp
// 如果已经是目标，方向为零
if (x == field.targetGridX && y == field.targetGridY)
{
    field.directions[index] = zVector2.zero;
    continue;
}
```

**问题分析**：

1. 流场以**网格格子**为单位生成方向
2. 当单位进入目标所在的格子时，该格子的方向被设为 `zero`
3. 流场系统原有逻辑会让单位**停止移动**

**示意图**：

```
地图网格（每格1.0单位）：

┌─────┬─────┬─────┐
│  →  │  →  │  ↓  │
├─────┼─────┼─────┤
│  →  │  →  │  ★  │  ← 目标格子，方向=zero
├─────┼─────┼─────┤
│  ↗  │  →  │  ↗  │
└─────┴─────┴─────┘

问题场景：
- 目标点：(10.5, 10.5) - 在格子中心
- 单位位置：(10.0, 10.0) - 刚进入目标格子
- 流场返回：zero
- 距离目标：0.707单位（还有一定距离）
- 到达半径：0.5单位
- 结果：单位停止移动 ❌
```

## ✅ 解决方案

### 核心思路

**当流场方向为零时，不应该直接停止，而应该直接朝目标点移动**

### 修复代码

**文件**：`FlowFieldSystem.cs` - `UpdateEntityNavigation()` 方法

**修复前**：
```csharp
if (flowDirection == zVector2.zero)
{
    // 无法到达目标或已经在目标处
    rvoSimulator.SetAgentPrefVelocity(navigator.RvoAgentId, zVector2.zero);
    return;  // ❌ 直接停止
}
```

**修复后**：
```csharp
if (flowDirection == zVector2.zero)
{
    zVector2 toTarget = target.TargetPosition - currentPos;
    zfloat distSq = toTarget.sqrMagnitude;
    
    // 只有真正到达时才停止
    zfloat arrivalRadiusSq = navigator.ArrivalRadius * navigator.ArrivalRadius;
    if (distSq <= arrivalRadiusSq)
    {
        rvoSimulator.SetAgentPrefVelocity(navigator.RvoAgentId, zVector2.zero);
        return;  // ✅ 真正到达才停止
    }
    
    // 还有距离就继续朝目标移动
    flowDirection = toTarget.normalized;  // ✅ 精确移动到目标点
}
```

### 逻辑流程

```
遇到 zero 方向时：
1. 计算到目标的距离
2. 距离 ≤ 到达半径？
   ├─ 是 → 停止移动（真正到达）
   └─ 否 → 直接朝目标点移动（精确导航）
```

## 🎯 效果对比

### 修复前
```
单位轨迹：
起点 ━━━━→ ━━━━→ ━━━━→ ◉ X  ← 在这里停止
                           ↑
                       距离目标还有0.7单位
                       但流场返回zero
```

### 修复后
```
单位轨迹：
起点 ━━━━→ ━━━━→ ━━━━→ ━━━→ ★  ← 精确到达目标
                              ↑
                         到达半径内才停止
```

## 📊 技术细节

### 为什么会返回 zero？

**FlowFieldCore.cs** 中有3种情况会返回 `zero` 方向：

1. **不可达** (第243-246行)
```csharp
if (field.costs[index] == zfloat.Infinity)
{
    field.directions[index] = zVector2.zero;
}
```

2. **在目标格子** (第249-254行) ⭐ **主要问题**
```csharp
if (x == field.targetGridX && y == field.targetGridY)
{
    field.directions[index] = zVector2.zero;
}
```

3. **没有更低代价的邻居** (第284-287行)
```csharp
if (bestDx == 0 && bestDy == 0)
{
    field.directions[index] = zVector2.zero;
}
```

### 为什么直接朝目标移动是安全的？

1. **距离判定**：只有距离 > 到达半径时才继续移动
2. **已经在目标格子内**：说明该格子是可达的，不会撞墙
3. **RVO避障**：即使直接移动，RVO也会处理与其他单位的碰撞
4. **精确到达**：可以精确到达目标点，而不是格子中心

### 网格分辨率影响

流场网格越大，这个问题越明显：

| 格子大小 | 可能的停止距离 | 影响 |
|---------|---------------|------|
| 0.5单位 | 0.35单位 | 较小 |
| 1.0单位 | 0.7单位 | ⚠️ 明显 |
| 2.0单位 | 1.4单位 | ❌ 严重 |

当前实现：
- 逻辑网格：0.5单位/格
- 流场网格：1.0单位/格 ⚠️

## 🧪 测试建议

### 测试场景1：精确到达
```csharp
// 创建单位
var unit = CreateNavigableUnit(new zVector2(10, 10), radius: 0.3f, maxSpeed: 2.0f);

// 设置目标（在格子边界）
navSystem.SetMoveTarget(unit, new zVector2(20.0f, 20.0f));

// 预期：单位应该精确到达(20.0, 20.0)，而不是在(19.5, 19.5)停下
```

### 测试场景2：格子中心目标
```csharp
// 目标在格子中心
navSystem.SetMoveTarget(unit, new zVector2(20.5f, 20.5f));

// 预期：单位应该到达(20.5, 20.5)附近（误差<0.5单位）
```

### 测试场景3：多单位同目标
```csharp
// 10个单位前往同一目标
for (int i = 0; i < 10; i++)
{
    var unit = CreateUnit(startPos[i]);
    navSystem.SetMoveTarget(unit, new zVector2(50, 50));
}

// 预期：所有单位都应该到达目标附近，不会提前停止
```

### 观察要点

在 TestRVO 可视化中：
- ✅ 单位颜色变绿（到达状态）
- ✅ 单位与目标点的连线很短（< 到达半径）
- ❌ 如果单位变蓝/青但不动 → 说明还有问题

## 📝 相关配置

### 到达半径

**位置**：`FlowFieldComponents.cs`

```csharp
ArrivalRadius = new zfloat(0, 5000)  // 0.5单位
```

**建议**：
- 开阔地形：0.5 单位
- 密集场景：0.3 单位
- 精确定位：0.2 单位

### 格子大小

**位置**：`FlowFieldExample.cs` 或你的初始化代码

```csharp
mapManager.Initialize(
    256, 256,           // 256x256 格子
    new zfloat(0, 5000) // 每格 0.5 单位
);
```

**权衡**：
- 格子越小：精度越高，计算量越大
- 格子越大：计算快，但零方向问题更明显

## 🔍 调试技巧

### 1. 添加调试日志

```csharp
if (flowDirection == zVector2.zero)
{
    zUDebug.Log($"单位 {entity.Id} 遇到零方向：" +
                $"位置={currentPos}, 目标={target.TargetPosition}, " +
                $"距离={(target.TargetPosition - currentPos).magnitude}");
}
```

### 2. 可视化流场

在 TestRVO 中启用"显示流场方向"，观察目标格子周围的箭头。

### 3. 单步调试

使用 TestRVO 的"单步更新"功能，观察单位进入目标格子后的行为。

## ⚠️ 已知限制

1. **真正不可达的情况**：如果单位被动态障碍物完全包围，也会返回 zero，此时会尝试直接移动（可能撞墙）
2. **高密度场景**：大量单位到达同一目标时，RVO可能让一些单位停在较远位置
3. **移动目标**：如果目标在移动，可能出现追逐循环

## 🔄 未来改进

- [ ] 区分"在目标格子内"和"不可达"两种零方向情况
- [ ] 添加"移动目标"支持，实时更新流场
- [ ] 实现更精细的网格分辨率（0.25单位/格）
- [ ] 添加"接近行为"：在目标格子内使用更平滑的减速曲线

## 📚 参考资料

- [Flow Field Pathfinding](https://leifnode.com/2013/12/flow-field-pathfinding/)
- [Arrival Steering Behavior](https://www.red3d.com/cwr/steer/Arrival.html)
- [Vector Field Pathfinding](https://howtorts.github.io/2013/12/31/generating-vector-fields.html)

---

**版本**: 1.0  
**修复日期**: 2024-10  
**优先级**: 🔴 高 - 影响所有单位的到达判定

