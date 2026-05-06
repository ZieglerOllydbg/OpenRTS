# Unity场景设置指南 - 点击创建单位

## 快速设置（5分钟完成）

### 1. 创建地面 Plane
1. 在 Hierarchy 右键 → `3D Object` → `Plane`
2. 重命名为 `Ground`
3. 设置 Transform:
   - Position: (0, 0, 0)
   - Scale: (10, 1, 10)

### 2. 创建单位预制体（Cube）
1. 在 Hierarchy 右键 → `3D Object` → `Cube`
2. 重命名为 `UnitCube`
3. 设置 Transform:
   - Position: (0, 0.5, 0)  ← 注意：Y=0.5 让方块在地面上
   - Scale: (1, 1, 1)
4. 拖拽 `UnitCube` 到 `Assets` 文件夹创建预制体
5. 删除场景中的 `UnitCube` 实例（保留预制体）

### 3. 创建 GameWorld 对象
1. 在 Hierarchy 右键 → `Create Empty`
2. 重命名为 `GameWorld`
3. 添加 `GameWorldBridge` 组件（在 Inspector 点击 Add Component）
4. 配置 `GameWorldBridge`:
   - **Logic Frame Rate**: `20`
   - **Enable Smooth Interpolation**: `✓`
   - **Interpolation Speed**: `10`
   - **View Root**: 拖入 `GameWorld` 自己

### 4. 创建 TestManager 对象
1. 在 Hierarchy 右键 → `Create Empty`
2. 重命名为 `TestManager`
3. 添加 `Test` 组件
4. 配置 `Test`:
   - **World Bridge**: 拖入 `GameWorld` 对象
   - **Unit Cube Prefab**: 拖入 `UnitCube` 预制体
   - **Ground Layer**: `Everything` 或选择特定层
   - **Player Id**: `0`

### 5. 测试运行
1. 点击 Play 按钮
2. 在 Game 视图中点击地面任意位置
3. 应该会创建一个 Cube 单位

---

## 完整层级结构

```
Hierarchy:
├── Main Camera
├── Directional Light
├── Ground (Plane)
├── GameWorld (空对象)
│   └── [GameWorldBridge 组件]
└── TestManager (空对象)
    └── [Test 组件]

Assets:
└── UnitCube.prefab (Cube预制体)
```

---

## 功能说明

### ✅ 已实现功能

1. **点击创建单位**
   - 点击地面任意位置创建 Cube 单位
   - 支持 PC 鼠标和移动端触摸

2. **逻辑视觉分离**
   - 逻辑层：使用 `zVector3` 确定性坐标
   - 表现层：Unity GameObject 显示
   - 完全解耦，可独立测试

3. **Gizmos 可视化**
   - 绿色球体：最后点击位置
   - 青色方框：所有单位的逻辑位置
   - 黄色球体+线：移动目标和路径

### 🎮 操作方式

- **左键点击地面**：创建单位（通过 RTSControl Input Actions）
- **右键拖拽**：平移相机（通过 RtsCameraController）
- **鼠标滚轮**：缩放相机

---

## 进阶：测试移动功能

在 `Test.cs` 的 `CreateUnitAtPosition` 方法中，取消注释这一行：

```csharp
// 取消注释下面这行，单位会在创建1秒后自动移动到随机位置
StartCoroutine(TestMoveUnit(entity, logicPosition));
```

这样创建的单位会在 1 秒后自动移动到附近的随机位置，你可以看到 `MovementSystem` 工作的效果。

---

## 右键移动单位（扩展）

如果你想实现右键点击让选中单位移动，可以参考以下代码：

```csharp
// 在 Test.cs 添加右键移动功能
private Entity _selectedEntity;

void Update()
{
    // 右键发送移动命令
    if (Mouse.current.rightButton.wasPressedThisFrame)
    {
        if (_selectedEntity.Id >= 0 && TryGetGroundPosition(
            Mouse.current.position.ReadValue(), out Vector3 worldPos))
        {
            var moveCmd = new MoveCommandComponent(worldPos.ToZVector3());
            worldBridge.LogicWorld.ComponentManager.AddComponent(_selectedEntity, moveCmd);
            Debug.Log($"单位移动到: {worldPos}");
        }
    }
}

// 选中最后创建的单位
private void CreateUnitAtPosition(Vector3 position)
{
    // ... 原有代码 ...
    _selectedEntity = entity; // 记录选中的单位
}
```

---

## 故障排查

### ❌ 点击没有反应
- 检查 `Test` 组件的各项引用是否已分配
- 检查 Console 是否有警告信息
- 确认 Input System 已启用（Project Settings → Player → Active Input Handling → Input System Package）

### ❌ 单位创建在奇怪的位置
- 检查 Ground Plane 的位置是否正确
- 确认 `UnitCube` 预制体的 Y 坐标为 0.5

### ❌ 找不到 GameWorldBridge
- 确认已将 `Packages/ZLockstep/Runtime` 文件夹包含在项目中
- 检查 Assembly Definition 是否正确配置

---

## 下一步

✨ 恭喜！你已经完成了基础设置。现在可以：

1. **创建多个单位**：多次点击创建更多单位
2. **查看 Gizmos**：在 Scene 视图中看到单位的逻辑位置
3. **测试移动**：启用自动移动功能查看 MovementSystem 效果
4. **扩展功能**：添加单位选择、右键移动等功能

参考完整架构文档：`Packages/ZLockstep/Runtime/ARCHITECTURE.md`

