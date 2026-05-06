# 游戏暂停功能实现指南

## ═══════════════════════════════════════════════════════════
## 概述
## ═══════════════════════════════════════════════════════════

游戏暂停功能允许在运行时暂停和恢复游戏逻辑执行，同时保持表现层的一致性。

## ═══════════════════════════════════════════════════════════
## 核心实现
## ═══════════════════════════════════════════════════════════

### Game 类中的暂停机制

```csharp
// 暂停状态属性
public bool IsPaused { get; private set; }

// 暂停方法
public void Pause() 
{
    IsPaused = true;
    zUDebug.Log("[Game] 游戏已暂停");
}

// 恢复方法
public void Resume() 
{
    IsPaused = false;
    zUDebug.Log("[Game] 游戏已恢复");
}
```

### 暂停时的逻辑处理

在 [Game.Update()](file:///C:/MyZiegler/github/ra2/Packages/ZLockstep/Runtime/Sync/Game.cs#L145-L202) 方法中添加暂停检查：

```csharp
public bool Update()
{
    // 暂停状态下不执行逻辑更新（追帧模式除外）
    if (IsPaused && !IsCatchingUp)
    {
        return false;
    }
    
    // ... 其余逻辑
}
```

注意：追帧模式下即使游戏暂停也会继续执行，以确保快速追赶服务器帧。

## ═══════════════════════════════════════════════════════════
## 表现层处理
## ═══════════════════════════════════════════════════════════

在 [GameWorldBridge.Update()](file:///C:/MyZiegler/github/ra2/Packages/ZLockstep/Runtime/View/GameWorldBridge.cs#L138-L164) 中处理暂停时的表现层更新：

```csharp
private void Update()
{
    // 追帧时跳过插值更新（避免卡顿）
    // 暂停时也跳过插值更新
    if (_game.IsCatchingUp || _game.IsPaused)
    {
        return;
    }
    
    // Unity特定：插值更新（让移动更平滑）
    if (enableSmoothInterpolation && _presentationSystem != null)
    {
        _presentationSystem.LerpUpdate(Time.deltaTime, interpolationSpeed);
    }
}
```

## ═══════════════════════════════════════════════════════════
## Unity 接口
## ═══════════════════════════════════════════════════════════

[GameWorldBridge](file:///C:/MyZiegler/github/ra2/Packages/ZLockstep/Runtime/View/GameWorldBridge.cs#L57-L398) 提供了方便的 Unity 接口来控制暂停：

```csharp
/// <summary>
/// 暂停游戏
/// </summary>
public void PauseGame()
{
    _game?.Pause();
}

/// <summary>
/// 恢复游戏
/// </summary>
public void ResumeGame()
{
    _game?.Resume();
}

/// <summary>
/// 切换游戏暂停/恢复状态
/// </summary>
public void TogglePause()
{
    if (_game != null)
    {
        if (_game.IsPaused)
        {
            _game.Resume();
        }
        else
        {
            _game.Pause();
        }
    }
}
```

## ═══════════════════════════════════════════════════════════
## 使用示例
## ═══════════════════════════════════════════════════════════

### 1. 通过代码控制暂停

```csharp
// 获取 GameWorldBridge 实例
GameWorldBridge gameBridge = FindObjectOfType<GameWorldBridge>();

// 暂停游戏
gameBridge.PauseGame();

// 恢复游戏
gameBridge.ResumeGame();

// 切换暂停状态
gameBridge.TogglePause();
```

### 2. 通过 Unity UI 控制

```csharp
public class PauseButton : MonoBehaviour
{
    [SerializeField] private GameWorldBridge worldBridge;
    
    public void OnPauseButtonClicked()
    {
        worldBridge.TogglePause();
    }
}
```

### 3. 通过按键控制

```csharp
public class KeyboardPause : MonoBehaviour
{
    [SerializeField] private GameWorldBridge worldBridge;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            worldBridge.TogglePause();
        }
    }
}
```

## ═══════════════════════════════════════════════════════════
## 注意事项
## ═══════════════════════════════════════════════════════════

1. **网络模式下的暂停**：暂停只影响本地逻辑执行，不会影响服务器或其他客户端。

2. **追帧与暂停**：追帧过程中暂停不会阻止追帧逻辑的执行，确保能够快速追赶服务器帧。

3. **表现层同步**：暂停时会停止插值动画，使游戏画面静止，保持一致性。

4. **事件处理**：暂停期间不会清空事件队列，确保恢复后能够正确处理累积的事件。

---

## ═══════════════════════════════════════════════════════════
## 测试验证
## ═══════════════════════════════════════════════════════════

可以通过以下方式验证暂停功能是否正常工作：

1. 在 Unity 场景中添加暂停按钮并测试点击效果
2. 检查控制台输出确认暂停/恢复日志
3. 验证暂停时游戏逻辑确实停止执行
4. 验证恢复后游戏能够正常继续运行
5. 在追帧过程中测试暂停功能是否正常工作

---

**文档版本**: v1.0  
**最后更新**: 2025-10-29  
**维护者**: ZLockstep团队