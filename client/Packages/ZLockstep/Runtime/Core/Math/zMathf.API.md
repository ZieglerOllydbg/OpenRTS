# zMathf API 文档

## 1. 概述

`zMathf` 是 `ZLockstep` 的定点数学工具类，位于 `zUnity` 命名空间。

- 输入/输出主类型为 `zfloat`
- 优先保证确定性和历史语义兼容
- 与 `System.Math` 结果可能存在可预期差异（近似算法、边界策略）

---

## 2. 常量与换算

- `PI`：圆周率近似
- `E`：自然常数近似
- `LN_10`：`ln(10)` 近似
- `One` / `Zero` / `Half`
- `Deg2Rad`：角度转弧度系数
- `Rad2Deg`：弧度转角度系数

---

## 3. 三角与反三角

### 正向三角（弧度输入）

- `Sin(zfloat rad)`
- `Cos(zfloat rad)`
- `Tan(zfloat rad)`

### 正向三角（角度输入）

- `SinAngle(zfloat angle)`
- `SinAngle(int angle)`
- `CosAngle(zfloat angle)`
- `CosAngle(int angle)`
- `TanAngle(zfloat angle)`
- `TanAngle(int angle)`

说明：

- `Tan*` 在余弦为 0 时返回 `zfloat.Infinity`，并输出错误日志。
- 角度版本使用查表/插值，属于确定性近似。

### 反三角

- `AsinAngle(zfloat f)` / `AcosAngle(zfloat f)` / `AtanAngle(zfloat f)`：返回角度
- `Asin(zfloat f)` / `Acos(zfloat f)` / `Atan(zfloat f)` / `Atan2(zfloat y, zfloat x)`：返回弧度

说明：

- `Asin/Acos` 越界输入会先裁剪到 `[-1, 1]` 再计算。
- `Atan2` 保持项目历史象限与边界处理语义。

---

## 4. 数值与插值工具

- 绝对值：`Abs(zfloat/int/long)`
- 开方：`Sqrt(zfloat)`, `SqrtScale(long)`
- 范围：`Min`, `Max`, `Clamp`, `Clamp01`
- 取整：`Ceil`, `Floor`, `Round`
- 插值：`Lerp(zfloat, zfloat, zfloat)`, `Lerp(int, int, zfloat)`
- 周期函数：`Repeat`, `PingPong`
- 幂/对数：`Pow`, `Exp`, `Log`, `Log10`
- 符号：`Sign(zfloat)`（注意 `Sign(0)` 按历史语义返回 `1`）
- 近似斜边：`ApproximateHypotenuse(zfloat, zfloat)`

---

## 5. 精度与边界约定

- 多数三角、对数、近似斜边接口为“确定性近似解”。
- `Sqrt(zfloat)` 负数输入返回 `0` 并记录日志。
- `ApproximateHypotenuse` 面向性能，不能等价替代精确开方。

---

## 6. 弃用迁移表

| 旧 API | 状态 | 替代建议 |
|---|---|---|
| `SqrtTable(zfloat)` | `[Obsolete]`（兼容保留） | 优先使用 `Sqrt(zfloat)`；若你在处理 raw 值路径，使用 `SqrtScale(long)` |

---

## 7. 维护建议

- 新增数学 API 时优先补齐 XML 文档：`summary/param/returns/remarks`。
- 行为变更前应补测试并先做兼容迁移，再考虑硬删除旧接口。
