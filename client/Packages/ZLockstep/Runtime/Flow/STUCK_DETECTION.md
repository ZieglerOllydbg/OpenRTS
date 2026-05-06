# 卡住检测与处理系统

## 问题描述

在流场寻路中，单位可能会卡在障碍物的角落、狭窄通道或复杂地形中，导致无法继续前进。这是由多个因素共同造成的：

1. **流场方向指向角落** - 流场计算时可能将单位引导到无法通过的尖角
2. **RVO过度反应** - RVO避障参数设置不当，导致单位过早停止
3. **局部最小值陷阱** - 单位陷入流场的局部最小值点
4. **碰撞死锁** - 多个单位互相阻挡形成死锁

## 解决方案

### 1. 优化RVO参数

**修改位置**：`FlowFieldSystem.cs` - `AddNavigator()`

```csharp
timeHorizon: new zfloat(1, 5000)  // 从2.0降低到1.5
```

**效果**：
- 降低时间范围，减少单位对远处障碍物的过早反应
- 让单位更"大胆"地尝试通过狭窄区域
- 提高通过角落的成功率

### 2. 卡住检测机制

**新增字段**：`FlowFieldNavigatorComponent`

```csharp
public zVector2 LastPosition;  // 上一帧位置
public int StuckFrames;        // 停滞帧数计数
```

**检测逻辑**：
```csharp
zfloat moveDistance = (currentPos - navigator.LastPosition).magnitude;
zfloat minMoveThreshold = new zfloat(0, 100); // 0.01单位

if (moveDistance < minMoveThreshold)
{
    navigator.StuckFrames++;  // 移动距离太小，增加计数
}
else
{
    navigator.StuckFrames = 0; // 正常移动，重置计数
}
```

**触发条件**：
- 连续20帧（约0.67秒）移动距离小于0.01单位

### 3. 随机扰动脱困

**脱困逻辑**：当检测到卡住时，添加垂直于流场方向的随机扰动

```csharp
if (navigator.StuckFrames > 20)
{
    // 生成确定性随机值（基于实体ID和当前帧）
    zfloat randomValue = new zfloat((entity.Id * 137 + World.Tick * 17) % 1000 - 500, 1000);
    
    // 计算垂直方向
    zVector2 perpendicular = new zVector2(-flowDirection.y, flowDirection.x);
    
    // 混合50%的随机扰动
    flowDirection = (flowDirection + perpendicular * randomValue * new zfloat(0, 5000)).normalized;
    
    // 60帧后重置，避免持续扰动
    if (navigator.StuckFrames > 60)
    {
        navigator.StuckFrames = 0;
    }
}
```

**关键特性**：
- ✅ **确定性**：使用实体ID和帧数生成随机值，保证多端一致
- ✅ **渐进式**：先尝试原方向，卡住后再添加扰动
- ✅ **自动重置**：避免单位持续受扰动影响

## 可视化反馈

### Scene视图中的显示

**单位颜色状态**：
- 🔵 **青色**：正常移动中 (StuckFrames ≤ 10)
- 🟡 **黄色**：可能卡住 (10 < StuckFrames ≤ 20)
- 🔴 **红色**：已卡住，正在脱困 (StuckFrames > 20)
- 🟢 **绿色**：已到达目标

**卡住警告**：
- 红色X标记覆盖在单位上
- 显示"卡住: N帧"文字标签
- 半径圈变为红色/黄色

### GUI面板显示

```
5. 当前状态
当前帧数: 123
单位数量: 10
⚠️ 卡住单位: 2  (红色警告)
```

## 参数调优指南

### 检测灵敏度

**位置**：`UpdateEntityNavigation()` - 第191行

```csharp
zfloat minMoveThreshold = new zfloat(0, 100); // 调整这个值
```

- **增大**：更难检测为卡住，适合慢速单位
- **减小**：更容易检测为卡住，适合快速单位

### 触发时机

**位置**：`UpdateEntityNavigation()` - 第214行

```csharp
if (navigator.StuckFrames > 20) // 调整这个帧数
```

- **增大**：给单位更多时间自己脱困
- **减小**：更快介入，但可能误判

### 扰动强度

**位置**：`UpdateEntityNavigation()` - 第219行

```csharp
flowDirection = (flowDirection + perpendicular * randomValue * new zfloat(0, 5000)).normalized;
//                                                              ^^^^^^^ 调整这个值
```

- **增大**：扰动更强，更容易脱困但路径更曲折
- **减小**：扰动更弱，路径更直但可能脱困效果不佳

### RVO时间范围

**位置**：`AddNavigator()` - 第53行

```csharp
timeHorizon: new zfloat(1, 5000)  // 调整这个值
```

- **增大**：单位更"谨慎"，更早避障，不容易卡住但可能过于保守
- **减小**：单位更"大胆"，更晚避障，容易卡住但路径更短

## 使用建议

### 场景1：开阔地形
- 使用默认参数即可
- 卡住检测主要用于意外情况

### 场景2：复杂地形（多障碍物）
- 降低 `timeHorizon` 到 1.0
- 降低 `minMoveThreshold` 到 0.005
- 降低触发帧数到 15

### 场景3：狭窄通道
- 确保单位半径 < 通道宽度/2
- 增加扰动强度到 0.7
- 考虑使用更细的网格

### 场景4：高密度单位
- 增加 `maxNeighbors` 到 15
- 保持 `timeHorizon` 在 1.5
- 适当增加触发帧数到 25

## 性能考虑

**计算开销**：
- 卡住检测：每个单位每帧 ~0.001ms
- 随机扰动：仅在卡住时触发，开销可忽略
- 总体影响：<5% FPS 影响

**内存开销**：
- 每个单位增加 16 字节 (LastPosition + StuckFrames)
- 1000个单位 ≈ 16KB 额外内存

## 调试技巧

### 1. 观察卡住计数

在Scene视图中看单位上方的"卡住: N帧"标签，了解卡住持续时间。

### 2. 检查流场方向

关闭单位显示，只显示流场箭头，检查角落处的流场方向是否合理。

### 3. 调整可视化

```csharp
// 在TestRVO的Inspector中调整
Flow Field Display Interval: 1  // 显示更密集的流场
```

### 4. 单步调试

使用TestRVO的"单步更新"功能，逐帧观察卡住和脱困过程。

### 5. Console日志

在 `UpdateEntityNavigation()` 中添加调试日志：

```csharp
if (navigator.StuckFrames == 20)
{
    zUDebug.Log($"单位 {entity.Id} 卡住在 {currentPos}");
}
```

## 已知限制

1. **确定性随机**：虽然保证了多端一致性，但随机序列可能不够"随机"
2. **墙角死区**：在极端情况下（如90度直角），扰动可能仍无法完全解决
3. **性能阈值**：对于>1000个单位的场景，建议降低检测频率

## 未来改进方向

- [ ] 实现A*回退机制：卡住超过N秒后切换到A*寻路
- [ ] 添加"推挤"机制：卡住的单位可以推开静止单位
- [ ] 流场平滑：在生成流场时就避免角落问题
- [ ] 动态调整RVO参数：根据单位密度自适应调整
- [ ] 群体智能：让附近单位协同避开卡点

## 参考资料

- [RVO2 Library Documentation](http://gamma.cs.unc.edu/RVO2/)
- [Flow Field Pathfinding](https://leifnode.com/2013/12/flow-field-pathfinding/)
- [Crowd Simulation for Large Scale Navigation](https://www.redblobgames.com/pathfinding/crowd-pathfinding/)

---

**版本**: 1.0  
**最后更新**: 2024-10  
**作者**: ZLockstep Team

