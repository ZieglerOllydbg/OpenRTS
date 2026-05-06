# zfloat API 文档

## 1. 概述

`zfloat` 是 `ZLockstep` 的定点数类型，使用 `long` 存储放大后的 `raw` 值，缩放系数为 `10000`。

- 实际值 = `raw / 10000`
- 例如：`1.2345` 的 `raw` 为 `12345`
- 设计目标：确定性计算（跨平台逻辑一致）

---

## 2. 缩放与常量

### 缩放常量

- `SCALE_10000 = 10000`
- `SCALE_1000 = 1000`
- `SCALE_100000000 = 100000000`
- `SCALE_100 = 100`
- `SCALE_ROOT4_10000 = 10`

### 数值常量

- `Zero`, `One`, `Two`, `Half`, `Quarter`
- `NegativeOne`, `OneHalf`, `SqrtTwo`, `Hundred`
- `Epsilon`（最小单位，`raw = 1`）
- `MinValue`（`raw = long.MinValue`）
- `MaxValue`（`raw = long.MaxValue`）
- `Infinity`（当前等同 `MaxValue`，作为“极大值哨兵”使用）

---

## 3. 构造与工厂

### 构造函数

- `zfloat(int value)`
- `zfloat(long value)`
- `zfloat(int intPart, int decimalsPart_10000)`
- `zfloat(int intPart, long decimalsPart_10000)`
- `zfloat(zfloat value)`

### 推荐工厂方法

- `FromRaw(long raw)`：按 `raw` 直接创建
- `FromInt(int valueInt)`：按整数创建
- `FromLong(long valueLong)`：按整数创建
- `FromFloat(float valueFloat)`：`float -> zfloat`（向零截断，不四舍五入）

---

## 4. 值访问与转换

### 字段

- `long value`：内部 `raw` 值

### 成员方法

- `GetFractionalPart()`：返回小数部分（保留符号，单位仍是 `raw`）
- `GetInteger()`：返回整数部分（向零截断）
- `ToInt32()`, `ToInt64()`
- `ToSingle()`, `ToDouble()`
- `ToFloatArray(zfloat[] values)`

### 显式转换运算符

- `(long)zfloat`
- `(int)zfloat`
- `(float)zfloat`
- `(zfloat)int`
- `(zfloat)long`

---

## 5. 算术与比较

### 运算符

- 算术：`+ - * / %`
- 一元：`+ -`
- 自增/自减：`++ --`
- 比较：`== != > >= < <=`

### 基础数学 API

- `Abs(zfloat value)`
- `Min(zfloat left, zfloat right)`
- `Max(zfloat left, zfloat right)`
- `Clamp(zfloat value, zfloat min, zfloat max)`
- `Sign(zfloat value)`（返回 `-1/0/1`）

### 近似比较

- `Approximately(zfloat value, zfloat target, zfloat tolerance)`
- `bool Approximately(zfloat target, zfloat tolerance)`

示例：

```csharp
bool isNearlyOne = someValue.Approximately(zfloat.One, zfloat.FromRaw(10)); // 容差 0.0010
```

---

## 6. 安全算术 API

用于显式处理溢出/除零，不改变普通运算符行为。

### Try 系列（失败返回 false）

- `TryAdd(zfloat left, zfloat right, out zfloat result)`
- `TrySub(zfloat left, zfloat right, out zfloat result)`
- `TryMul(zfloat left, zfloat right, out zfloat result)`
- `TryDiv(zfloat left, zfloat right, out zfloat result)`（除零返回 false）

### Checked 系列（失败抛异常）

- `CheckedAdd(...)`
- `CheckedSub(...)`
- `CheckedMul(...)`
- `CheckedDiv(...)`

异常语义：

- 溢出：`OverflowException`
- 除零：`DivideByZeroException`

---

## 7. 解析与格式化

### 解析

- `Parse(string s)`：失败抛 `FormatException`
- `TryParse(string s, out zfloat v)`：失败返回 `false`

支持输入：

- 可选符号：`+` / `-`
- 整数：`123`
- 小数：`123.45`, `.25`, `-.25`
- 前后空白允许（会 `Trim`）

规则：

- 小数超过 4 位时截断（非四舍五入）
- 超出 `long` 边界返回失败

### 格式化

- `ToString()` 默认 `F4` 且使用 `InvariantCulture`
- `ToString(string format)`
- `ToString(string format, IFormatProvider provider)`

---

## 8. 比较与相等接口

`zfloat` 实现：

- `IComparable`
- `IComparable<zfloat>`
- `IEquatable<zfloat>`
- `IFormattable`
- `ISerializable`

相关方法：

- `CompareTo(zfloat other)`
- `CompareTo(object obj)`（`obj` 不是 `zfloat` 时抛 `ArgumentException`）
- `Equals(zfloat other)`
- `Equals(object obj)`
- `GetHashCode()`

---

## 9. 序列化

- `GetObjectData(SerializationInfo info, StreamingContext context)`
- `zfloat(SerializationInfo info, StreamingContext context)`

仅序列化/反序列化 `raw` 字段 `value`。

---

## 10. 使用建议

- 需要精确控制单位时优先用 `FromRaw`。
- 与配置字段对接时先明确该字段是否已是万分比（`10000 = 1`）。
- 高风险路径优先使用 `Try*`/`Checked*`，避免静默溢出。
- 近似判断统一使用 `Approximately(..., tolerance)`，不要写魔法阈值散落在业务代码中。
