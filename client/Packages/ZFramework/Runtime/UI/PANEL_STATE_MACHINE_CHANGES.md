# UI面板状态机重构说明

## 概述

将BasePanel的状态管理从多个bool标志（IsOpen, IsLoading, IsVisiable）重构为单一的状态枚举（PanelState），简化了异步加载时的状态管理。

## 主要变更

### 1. 新增PanelState枚举

位置：`Enum/Panel/PanelState.cs`

```csharp
public enum PanelState
{
    Uninitialized,  // 未初始化（刚创建）
    Closed,         // 已关闭
    Loading,        // 正在加载资源
    Loaded,         // 资源已加载，但未显示
    Opening,        // 正在打开（播放动画）
    Opened,         // 已打开并显示
    Closing,        // 正在关闭（播放动画）
}
```

### 2. BasePanel重构

#### 状态管理
- 新增：`private PanelState state` - 当前状态
- 新增：`public PanelState State` - 获取当前状态
- 新增：`TransitionTo(PanelState)` - 状态转换方法（包含合法性检查）
- 新增：`IsValidTransition()` - 状态转换合法性检查
- 新增：`OnStateChanged()` - 状态变化事件（可重写）

#### 兼容属性
原有的bool属性改为兼容属性，确保外部代码无需修改：

```csharp
public bool IsOpen => state == PanelState.Loading || 
                      state == PanelState.Loaded || 
                      state == PanelState.Opening || 
                      state == PanelState.Opened;

public bool IsVisiable => state == PanelState.Opened;

public bool IsLoading => state == PanelState.Loading;
```

#### 重构的方法

1. **Open()** - 使用状态机重写，清晰处理各种状态下的打开逻辑
2. **Close()** - 使用状态机重写，正确处理加载中被关闭的情况
3. **BorrowComplete()** - 简化异步加载完成的处理逻辑
4. **删除TryOpen()** - 逻辑合并到Open()中
5. **删除OnLoaded()** - 逻辑合并到BorrowComplete()中
6. **删除ReallyOpen()** - 拆分为BeginOpen()和OnOpenAnimationComplete()

#### 新增的私有方法

- `BeginOpen()` - 开始打开面板（资源已加载）
- `OnOpenAnimationComplete()` - 打开动画完成
- `BeginClose()` - 开始关闭面板
- `OnCloseAnimationComplete()` - 关闭动画完成

### 3. 状态转换流程

#### 正常打开流程
```
Closed → Loading → Loaded → Opening → Opened
```

#### 打开后关闭流程
```
Opened → Closing → Closed
```

#### 加载中被关闭（异步处理的关键场景）
```
Loading → Closed (在BorrowComplete中检测并处理)
```

## 优势

1. **状态清晰**：一目了然当前处于什么状态
2. **转换安全**：非法状态转换会被拦截并输出错误日志
3. **易于调试**：集中的状态转换日志，便于追踪问题
4. **异步友好**：明确处理加载中被关闭的情况，避免状态混乱
5. **易扩展**：新增状态（如Loading、Opening等）很容易
6. **向后兼容**：兼容属性确保现有代码无需修改

## 使用示例

### 子类中监听状态变化

```csharp
public class MyPanel : BasePanel
{
    protected override void OnStateChanged(PanelState oldState, PanelState newState)
    {
        base.OnStateChanged(oldState, newState);
        
        // 自定义逻辑
        if (newState == PanelState.Opened)
        {
            // 面板已完全打开
        }
    }
}
```

### 外部检查状态

```csharp
var panel = GetPanel();

// 使用新的状态枚举（推荐）
if (panel.State == PanelState.Loading)
{
    // 正在加载中
}

// 使用兼容属性（现有代码）
if (panel.IsOpen)
{
    // 面板打开中（包括Loading, Loaded, Opening, Opened）
}
```

## 注意事项

1. 状态转换会输出日志，便于调试
2. 非法的状态转换会被拦截并输出错误日志
3. 兼容属性保证了现有代码无需修改
4. 动画系统预留了接口（TODO标记），未来可以轻松添加

## 迁移指南

**对于BasePanel的子类：无需修改！**

兼容属性确保了：
- `IsOpen`、`IsVisiable`、`IsLoading` 继续正常工作
- 所有生命周期回调（OnInit, OnBecameVisible等）保持不变
- 现有的Open()、Close()调用保持不变

**建议的改进（可选）：**
- 重写`OnStateChanged()`监听状态变化
- 使用`State`属性获取精确状态，而不是IsOpen等模糊属性

## 日期

2025-11-11

