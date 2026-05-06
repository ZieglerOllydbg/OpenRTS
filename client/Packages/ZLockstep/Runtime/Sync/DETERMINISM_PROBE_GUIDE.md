# 回放不同步定位工具使用文档

本文档说明如何使用 `DeterminismProbe` + `ReplayDiffTool` 定位“同一份回放每次结果不一致”的问题。

## 1. 工具目标

- 快速定位**第一个分叉帧**（`firstMismatchTick`）。
- 判定分叉来源：
  - `CommandMismatch`：命令流差异
  - `RandomMismatch`：随机状态差异
  - `StateMismatch`：ECS 状态差异
- 输出可追溯差异字段（实体、组件、raw 值）。

---

## 2. 当前实现范围（首版）

每帧输出 `FrameDigest`（JSONL 一行一帧）：

- 帧基础：`tick`、`activeEntityCount`
- 命令：`commandCount`、`commandTypes`、`commandTypeSignature`、`commandHash`
- 随机：`randomStateRaw`
- 状态：`stateHash`、`campEconomySnapshots`、`stateSamples`

关键状态组件（按实体 ID 排序）：

- `TransformComponent`
- `HealthComponent`
- `MoveCommandComponent`
- `AttackComponent`
- `ProduceComponent`

---

## 3. 运行时接入（Ra2Demo 现状）

当前探针默认关闭（`DeterminismProbe.Enabled = false`），并在业务入口显式开启：

- `MatchPanel.StartDeterminismProbe(...)`：单机开局时先 `Enabled = true`，再 `Start(...)`
- `ReplaySubPanel.StartDeterminismProbe(...)`：回放开局时先 `Enabled = true`，再 `Start(...)`

启用后会创建：

- 单机开局（SOLO）会创建：
  - `determinism_probe_standalone_yyyyMMdd_HHmmss.jsonl`
- 回放开局（REPLAY）会创建：
  - `determinism_probe_replay_yyyyMMdd_HHmmss.jsonl`

路径：`Application.persistentDataPath`

同时命令录制文件：

- `command_record_yyyyMMdd_HHmmss.txt`

---

## 4. 手动接入方式（代码）

如果你在其他入口或模式里使用（推荐模式：默认关闭，按需开启）：

```csharp
// 1) 初始化后启动探针
string probeFile = System.IO.Path.Combine(
    Application.persistentDataPath,
    $"determinism_probe_custom_{System.DateTime.Now:yyyyMMdd_HHmmss}.jsonl");

game.World.DeterminismProbe.Enabled = true;
game.World.DeterminismProbe.SampleInterval = 1; // 每帧采样
game.World.DeterminismProbe.Start(probeFile, truncate: true);

// 2) 结束时主动收口关闭
game.World.DeterminismProbe.Stop();
game.World.DeterminismProbe.Enabled = false;
```

注意：

- 采样发生在 `zWorld.Update()` 末尾，属于只读采样。
- `SampleInterval > 1` 可降低开销（例如 2 表示每 2 帧采样一次）。
- `zWorld.Shutdown()` 现已包含收口逻辑：`Stop()` + `Enabled = false`，避免跨局残留开启状态。

---

## 5. 如何做一次完整定位

### 步骤 1：生成基线

1. 进入单机（SOLO）并完成一局可复现操作。
2. 得到：
   - `command_record_xxx.txt`
   - `determinism_probe_standalone_xxx.jsonl`

### 步骤 2：回放同一份命令

1. 进入回放（REPLAY），读取最近的 `command_record_xxx.txt`。
2. 得到：
   - `determinism_probe_replay_xxx.jsonl`

### 步骤 3：离线对比

```csharp
using ZLockstep.Sync.Determinism;

var result = ReplayDiffTool.CompareFiles(
    baselinePath: @"C:\...\determinism_probe_standalone_xxx.jsonl",
    replayPath:   @"C:\...\determinism_probe_replay_xxx.jsonl");

if (!result.IsMatch)
{
    UnityEngine.Debug.Log(
        $"Mismatch tick={result.FirstMismatchTick}, type={result.MismatchType}\n" +
        string.Join("\n", result.Details));
}
```

---

## 6. 结果解读

### `CommandMismatch`

优先检查：

- 该帧命令数量是否一致（`commandCount`）。
- 命令类型序列是否一致（`commandTypeSignature`）。
- 命令执行帧是否被覆盖。

### `RandomMismatch`

说明随机状态先分叉了。优先检查：

- 是否存在非确定性随机调用路径（条件分支差异）。
- 是否在逻辑帧外调用了会影响全局随机状态的逻辑。

### `StateMismatch`

命令和随机一致但状态不同。优先检查：

- `Details` 中第一批差异的 `E:<id>:<Component>.<Field>=raw`。
- 差异实体是否与该帧命令关联。

---

## 7. 已修复的关键一致性点

命令录制帧号改为：

- `command.ExecuteFrame >= 0` 时记录该执行帧
- 否则回退 `world.Tick`

回放装载改为：

- 仅在命令内帧无效时才回填记录帧
- 若命令内帧与记录帧不一致，保留命令内帧并打印告警

---

## 8. 常见问题

### Q1：为什么 `randomStateRaw` 在正式包里是 0？

`zRandom` 调试状态仅在 `UNITY_EDITOR || DEVELOPMENT_BUILD` 下有效。正式包默认返回 0。

### Q2：JSONL 很大怎么办？

- 提高 `SampleInterval`（例如 2 或 5）。
- 只在排查阶段开启 `DeterminismProbe.Enabled`。

### Q3：首版找不到分叉原因怎么办？

首版只采样关键组件。可按业务扩展采样器（实现 `IDeterminismSampler`）加入更多组件字段。

---

## 9. 最短排查流程（建议）

1. 先跑一局单机，拿到 `standalone probe + command_record`。  
2. 立刻跑同一份回放，拿到 `replay probe`。  
3. 用 `ReplayDiffTool.CompareFiles` 看 `firstMismatchTick`。  
4. 按 `MismatchType` 走对应检查路径：命令 → 随机 → 状态。  
5. 只修“第一个分叉帧”相关逻辑，修完重复 1~4 验证。  

---

## 10. Editor 离线对比

已提供 Editor 工具：`Tools/Replay Diff Tool`

使用步骤：

1. 打开 Unity 菜单 `Tools/Replay Diff Tool`。  
2. 在 `Baseline 文件名` 与 `Replay 文件名` 输入两份 jsonl 文件名（仅文件名，不填路径）。  
3. 工具会自动拼接 `Application.persistentDataPath` 后做存在性校验。  
4. 点击“对比”，窗口会展示：`IsMatch`、`FirstMismatchTick`、`MismatchType`、`Summary`、`Details`。  
5. 点击“复制结果”可一键复制完整文本（含解析后的绝对路径）。  

结果解读重点：

- `FirstMismatchTick`：第一个分叉帧。  
- `MismatchType`：
  - `CommandMismatch`：优先看命令数量/类型签名。
  - `RandomMismatch`：优先看随机调用路径是否一致。
  - `StateMismatch`：优先看 `Details` 里首批实体字段差异。  
