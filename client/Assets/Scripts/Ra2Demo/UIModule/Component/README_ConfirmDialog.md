# ConfirmDialogComponent - 确认对话框组件

轻量级确认对话框，纯 C# 实现，用于需要用户确认的场景。

## 核心使用

### 1️⃣ 初始化
```csharp
private ConfirmDialogComponent confirmDialog;

protected override void OnBecameVisible()
{
    confirmDialog = new ConfirmDialogComponent(PanelObject.transform.Find("Confirm"));
}
```

### 2️⃣ 显示对话框
```csharp
confirmDialog.Show(
    onConfirm: () => { /* 确认逻辑 */ },
    onCancel: () => { /* 取消逻辑（可选） */ },
    message: "确定要执行此操作吗？"
);
```

### 3️⃣ 其他方法
- `Hide()` - 隐藏对话框
- `SetMessage("消息")` - 设置文本

### 4️⃣ 清理事件（重要）
```csharp
protected override void OnBecameInvisible()
{
    confirmDialog?.UnregisterEvents(); // 防止内存泄漏
}
```

## API 速查

| 方法 | 作用 |
|------|------|
| `new ConfirmDialogComponent(transform)` | 构造函数，自动初始化 |
| `Show(onConfirm, onCancel, message)` | 显示对话框 |
| `Hide()` | 隐藏并清理回调 |
| `SetMessage(msg)` | 设置消息文本 |
| `UnregisterEvents()` | 移除事件监听 |

## UI 结构要求
```
Confirm (父对象)
├── OK (Button)
├── Cancel (Button)
└── Text (TMP_Text)
```

## ⚠️ 注意事项
- 必须在 `OnBecameInvisible` 中调用 `UnregisterEvents()`
- 每次 `Show()` 会覆盖之前的回调
- 确保 UI 路径包含 OK/Cancel/Text 子对象

## 设计特点
- ✅ 轻量：不依赖 MonoBehaviour
- ✅ 安全：自动清理回调，防止内存泄漏
- ✅ 灵活：可在任何 C# 类中使用
- ✅ 复用：一次编写，多处使用

## 使用示例（MainPanel 中的完整实现）

```csharp
public class MainPanel : BasePanel
{
    private ConfirmDialogComponent confirmDialog;

    protected override void OnBecameVisible()
    {
        base.OnBecameVisible();
        
        // 初始化确认对话框组件
        confirmDialog = new ConfirmDialogComponent(PanelObject.transform.Find("Confirm"));
    }

    protected override void AddEvent()
    {
        base.AddEvent();
        // 组件内部已自动注册按钮事件，无需额外操作
    }

    protected override void RemoveEvent()
    {
        base.RemoveEvent();
        // 组件内部已自动注册按钮事件，无需额外操作
    }

    protected override void OnBecameInvisible()
    {
        base.OnBecameInvisible();
        
        // 清理事件监听
        if (confirmDialog != null)
        {
            confirmDialog.UnregisterEvents();
        }
    }

    // 使用示例：退出按钮点击处理
    private void OnExitButtonClick()
    {
        confirmDialog.Show(
            onConfirm: () =>
            {
                Frame.DispatchEvent(new RestartGameEvent());
            },
            onCancel: () =>
            {
                // 取消时只需关闭弹窗，ClearCallbacksAndHide 会自动处理
            },
            message: "确定要退出游戏吗？"
        );
    }
}
```

## 注意事项
1. ⚠️ **必须在面板销毁前调用 `UnregisterEvents()` 清理事件**，防止内存泄漏
2. ⚠️ 每次调用 `Show()` 会覆盖之前的回调
3. ⚠️ 调用 `Hide()` 会自动清理回调委托
4. ⚠️ 组件在构造函数中自动完成初始化和事件注册
5. ⚠️ 确保传入的 Transform 路径正确（如 "Confirm"），包含 OK、Cancel 按钮和 Text 子对象

## 设计优势

### 相比 MonoBehaviour 版本的优势
- 🚀 **更轻量**：不依赖 Unity 组件系统，内存占用更小
- ⚡ **性能更好**：没有 MonoBehaviour 的生命周期开销
- 🔧 **更灵活**：可以在任何 C# 类中使用，不限于 MonoBehaviour
- 📦 **易测试**：纯 C# 类更容易进行单元测试
- 🎯 **职责单一**：专注于对话框逻辑，不涉及 Unity 生命周期

### 通用设计原则
- 📦 **组件化设计**：可独立复用，不依赖特定面板
- 🔒 **内存安全**：自动清理回调，防止内存泄漏
- 🎯 **职责分离**：UI 组件负责展示，业务逻辑由调用方提供
- ♻️ **高度复用**：一次编写，多处使用

## UI 层级结构要求

使用该组件时，确保 UI 层级结构如下：

```
Confirm (父对象)
├── OK (Button)
├── Cancel (Button)
└── Text (TMP_Text)
```

- **OK**: 确认按钮，挂载 Button 组件
- **Cancel**: 取消按钮，挂载 Button 组件
- **Text**: 消息文本，挂载 TMP_Text 组件

## 工作流程

1. **初始化阶段**（构造函数）
   - 获取 UI 组件引用（OK/Cancel 按钮、Text 文本）
   - 自动注册按钮点击事件
   - 初始隐藏对话框

2. **显示阶段**（Show 方法）
   - 保存回调委托
   - 设置消息文本（如果提供）
   - 激活对话框面板

3. **交互阶段**
   - 用户点击确认 → 执行确认回调 → 隐藏对话框
   - 用户点击取消 → 执行取消回调 → 隐藏对话框

4. **清理阶段**（UnregisterEvents 方法）
   - 移除所有按钮事件监听
   - 防止内存泄漏