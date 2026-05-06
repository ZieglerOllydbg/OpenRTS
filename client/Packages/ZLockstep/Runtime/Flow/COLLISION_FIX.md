# RVO静态障碍物碰撞修复

## 🐛 **根本问题**

单位会**走进障碍物格子里**，导致卡住无法移动。

### 症状
- ✅ 单位正常移动
- ❌ 单位突然进入墙壁/建筑内部
- ❌ 进入后停止不动（因为障碍物格子方向为zero）
- ❌ 永远无法脱困

### 原因分析

**RVO2 只处理动态避障，不知道静态障碍物！**

#### RVO2.cs 第129行
```csharp
agents[i].position += agents[i].velocity * deltaTime;  // ❌ 直接移动，不检查地图
```

**问题流程**：
```
1. 流场告诉单位："朝前走"
2. RVO计算避开其他单位的速度
3. RVO直接移动单位位置 ❌ 不检查是否撞墙
4. 单位进入障碍物格子
5. 下一帧流场返回 zero（障碍物格子内）
6. 单位卡住
```

**为什么会发生？**

```
地图布局：

┌─────┬─────┬█████┐
│  →  │  →  │█████│  ← 障碍物
├─────┼─────┼█████┤
│  →  │  →  │  ↓  │
├─────┼─────┼─────┤
│  ↗  │  →  │  ↗  │
└─────┴─────┴─────┘

单位路径：
1. 在(5, 5)，流场说"往右走"
2. RVO计算速度 = (1, 0)
3. 新位置 = (6, 5) ❌ 这是障碍物格子！
4. RVO不检查地图，直接移动
5. 单位进入障碍物，卡住
```

## ✅ **解决方案**

### 核心思路

**在RVO移动后，检查新位置是否合法，如果在障碍物里就回退**

### 实现位置

**FlowFieldSystem.cs** - `SyncPosition()` 方法

### 修复代码

**修复前**：
```csharp
private void SyncPosition(Entity entity)
{
    zVector2 pos2D = rvoSimulator.GetAgentPosition(navigator.RvoAgentId);
    
    transform.Position.x = pos2D.x;  // ❌ 直接接受RVO位置
    transform.Position.z = pos2D.y;
}
```

**修复后**：
```csharp
private void SyncPosition(Entity entity)
{
    zVector2 oldPos = new zVector2(transform.Position.x, transform.Position.z);
    zVector2 newPos = rvoSimulator.GetAgentPosition(navigator.RvoAgentId);
    
    // ===== 碰撞检测 =====
    if (map != null)
    {
        map.WorldToGrid(newPos, out int gridX, out int gridY);
        
        // 检查新位置是否可行走
        if (!map.IsWalkable(gridX, gridY))
        {
            // 回退到旧位置
            newPos = oldPos;
            rvoSimulator.SetAgentPosition(navigator.RvoAgentId, oldPos);
            
            // 停止速度，避免持续尝试进入
            rvoSimulator.SetAgentPrefVelocity(navigator.RvoAgentId, zVector2.zero);
        }
    }
    
    transform.Position.x = newPos.x;
    transform.Position.z = newPos.y;
}
```

### 修改的接口

**FlowFieldNavigationSystem.InitializeNavigation()**

**修改前**：
```csharp
public void InitializeNavigation(FlowFieldManager ffMgr, RVO2Simulator rvoSim)
```

**修改后**：
```csharp
public void InitializeNavigation(FlowFieldManager ffMgr, RVO2Simulator rvoSim, IFlowFieldMap gameMap)
//                                                                           ^^^^^^^^^^^^^ 新增参数
```

## 🎯 **工作原理**

### 检测流程

```
每帧同步位置时：
1. 获取RVO计算的新位置
2. 将世界坐标转换为网格坐标
3. 检查该网格是否可行走
4. 可行走？
   ├─ 是 → 应用新位置 ✅
   └─ 否 → 回退到旧位置 + 停止速度 ✅
```

### 防止进入障碍物

```
正常移动：
┌─────┬─────┬█████┐
│  A  │  →  │█████│
└─────┴─────┴█████┘
  旧位置  新位置  障碍物

检测逻辑：
1. 新位置在障碍物？→ 否 → 允许移动 ✅

碰撞情况：
┌─────┬█████┬█████┐
│  A  │█→ X │█████│
└─────┴█████┴█████┘
  旧位置  新位置  障碍物

检测逻辑：
1. 新位置在障碍物？→ 是 → 回退到旧位置 ✅
2. 停止速度，避免下一帧继续尝试
```

### 为什么停止速度？

```csharp
rvoSimulator.SetAgentPrefVelocity(navigator.RvoAgentId, zVector2.zero);
```

**原因**：
- 单位尝试进入障碍物说明它的目标方向有问题
- 停止速度让单位暂停，等待流场重新规划
- 下一帧流场会提供新的可行方向
- 避免单位持续"撞墙"

## 📊 **效果对比**

### 修复前
```
单位轨迹：
起点 ━━→ ━━→ ━━→ [进入墙壁] X
                      ↑
                 卡在障碍物里
```

### 修复后
```
单位轨迹：
起点 ━━→ ━━→ ━━→ [检测到墙壁] ↓
                      ↓
                    绕开障碍物
                      ↓
                    继续前进 ✅
```

## 🧪 **测试验证**

### 测试场景1：沿墙移动

```csharp
// 创建单位在墙边
var unit = CreateUnit(new zVector2(5, 5));

// 目标在墙另一边
navSystem.SetMoveTarget(unit, new zVector2(15, 5));

// 预期：单位绕开墙壁，不会卡在墙里
```

### 测试场景2：角落脱困

```csharp
// 单位在角落
var unit = CreateUnit(new zVector2(1, 1));

// 目标在对面角落
navSystem.SetMoveTarget(unit, new zVector2(100, 100));

// 预期：单位不会卡在角落里
```

### 测试场景3：高密度单位

```csharp
// 多个单位通过狭窄通道
for (int i = 0; i < 20; i++)
{
    var unit = CreateUnit(startPos[i]);
    navSystem.SetMoveTarget(unit, targetPos);
}

// 预期：所有单位都能通过，不会卡在墙里
```

### 在TestRVO中观察

启用可视化后：
- ✅ 单位应该始终在灰色可行走区域
- ❌ 如果单位进入红色障碍物 → 说明碰撞检测失效

## ⚙️ **配置要点**

### 确保地图一致性

**关键**：流场使用的地图和碰撞检测使用的地图**必须是同一个**！

```csharp
// ✅ 正确
var mapManager = new SimpleMapManager();
var flowFieldManager = new FlowFieldManager();
flowFieldManager.Initialize(mapManager, ...);

var navSystem = new FlowFieldNavigationSystem();
navSystem.InitializeNavigation(flowFieldManager, rvoSimulator, mapManager);
//                                                              ^^^^^^^^^^ 同一个地图

// ❌ 错误
var mapManager1 = new SimpleMapManager();
var mapManager2 = new SimpleMapManager();  // 不同的地图实例！
flowFieldManager.Initialize(mapManager1, ...);
navSystem.InitializeNavigation(..., mapManager2);  // ❌ 会导致不一致
```

### 网格分辨率影响

| 格子大小 | 碰撞精度 | 性能 |
|---------|---------|------|
| 0.25单位 | 很高 | 低（更多检查） |
| 0.5单位 | 高 | 中等 ✅ 推荐 |
| 1.0单位 | 中等 | 高 |
| 2.0单位 | 低 | 很高 |

**建议**：使用 0.5 单位格子平衡精度和性能

### 单位半径设置

```csharp
// 单位半径应该 < 格子大小
var unit = CreateUnit(
    startPos,
    radius: new zfloat(0, 3000),  // 0.3 单位
    maxSpeed: new zfloat(2)
);

// 规则：radius < gridSize * 0.7
// 格子0.5单位 → 半径应 < 0.35单位
// 格子1.0单位 → 半径应 < 0.7单位
```

## ⚠️ **已知限制**

### 1. 边界情况

单位中心在可行走格子，但身体一部分伸入障碍物：

```
┌─────┬█████┐
│  ⚪ │█████│  ← 单位中心在左边格子（合法）
│  │) │█████│  ← 但身体伸入右边障碍物
└─────┴█████┘
```

**解决**：使用更小的网格或更小的单位半径

### 2. 高速移动

单位速度很快，一帧可能穿过多个格子：

```
格子1(合法) → 格子2(障碍物) → 格子3(合法)
            ↑
      单位可能直接跳到这里
```

**解决**：降低 `maxSpeed` 或提高帧率

### 3. 动态障碍物延迟

建造建筑后，RVO内的单位可能已经进入新障碍物区域：

```
1. 单位在(5,5)
2. 建造建筑在(6,5)
3. RVO更新，单位移动到(6,5)
4. 碰撞检测发现障碍物，回退
```

**解决**：建造时立即检查并移开单位

## 🔄 **未来改进**

- [ ] 实现"滑动"碰撞响应而不是完全停止
- [ ] 添加连续碰撞检测（检查移动路径）
- [ ] 支持圆形/多边形障碍物而不只是网格
- [ ] 实现RVO内置的静态障碍物支持
- [ ] 添加"推力"机制让单位主动避开墙壁

## 🐞 **调试技巧**

### 1. 添加碰撞日志

```csharp
if (!map.IsWalkable(gridX, gridY))
{
    zUDebug.Log($"单位 {entity.Id} 尝试进入障碍物 ({gridX}, {gridY})");
    // 回退...
}
```

### 2. 可视化检测

在TestRVO中，被回退的单位会：
- 位置不变（回退到旧位置）
- 速度箭头消失（速度被设为zero）
- 下一帧获得新方向

### 3. 检查地图一致性

```csharp
// 在初始化后检查
Debug.Log($"流场地图: {flowFieldManager.GetMapWidth()}x{flowFieldManager.GetMapHeight()}");
Debug.Log($"碰撞地图: {map.GetWidth()}x{map.GetHeight()}");
// 应该一致！
```

## 📚 **相关文档**

- `ZERO_DIRECTION_FIX.md` - 零方向问题修复（互补）
- `STUCK_DETECTION.md` - 卡住检测系统
- `FLOW_FIELD_README.md` - 流场系统完整文档

## 🎓 **技术总结**

### 核心教训

**RVO本身是完美的避障算法，但它只知道动态障碍物（其他单位）。静态障碍物（地图）必须由上层逻辑处理。**

### 设计模式

这是经典的**职责分离**：
- RVO负责：单位之间避障
- 流场负责：全局路径规划
- 碰撞检测负责：静态障碍物处理

三者配合才能实现完整的导航系统。

---

**版本**: 1.0  
**优先级**: 🔴 关键 - 防止单位进入障碍物  
**作者**: ZLockstep Team

