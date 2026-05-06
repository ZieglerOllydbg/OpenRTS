# zVector4 API 文档

## 概述

`zVector4` 是一个四维定点数向量结构体，用于确定性计算。包含 x、y、z、w 四个分量，每个分量都是 `zfloat` 类型的定点数（精度：0.0001）。提供了向量运算的各种方法，如加减乘除、点积、归一化等，并使用优化技巧（.value 直接操作）确保在 ECS 系统的高频调用中性能优异。

## 命名空间

```csharp
namespace zUnity
```

## 继承关系

```csharp
public struct zVector4 : ISerializable, IEquatable<zVector4>
```

- `ISerializable`: 支持序列化和反序列化
- `IEquatable<zVector4>`: 支持类型安全的相等比较

## 常量

| 常量名 | 类型 | 值 | 描述 |
|--------|------|-----|------|
| `zero` | `zVector4` | (0, 0, 0, 0) | 零向量 |
| `one` | `zVector4` | (1, 1, 1, 1) | 单位向量 |
| `forward` | `zVector4` | (0, 0, 1, 0) | 前方向量 |
| `back` | `zVector4` | (0, 0, -1, 0) | 后方向量 |
| `up` | `zVector4` | (0, 1, 0, 0) | 上方向量 |
| `down` | `zVector4` | (0, -1, 0, 0) | 下方向量 |
| `left` | `zVector4` | (-1, 0, 0, 0) | 左方向量 |
| `right` | `zVector4` | (1, 0, 0, 0) | 右方向量 |
| `NULL` | `zVector4` | (-999999, -999999, -999999, -999999) | 历史兼容哨兵值 |

## 字段

| 字段名 | 类型 | 描述 |
|--------|------|------|
| `x` | `zfloat` | X 轴坐标分量 |
| `y` | `zfloat` | Y 轴坐标分量 |
| `z` | `zfloat` | Z 轴坐标分量 |
| `w` | `zfloat` | W 轴坐标分量 |

## 属性

### 索引器

```csharp
public zfloat this[int index] { get; set; }
```

通过索引访问向量分量。

**参数:**
- `index`: 索引（0=x, 1=y, 2=z, 3=w）

**返回值:** 对应索引的分量值

**异常:**
- `IndexOutOfRangeException`: 当索引不在 0-3 范围内时抛出

**示例:**
```csharp
zVector4 vec = new zVector4(1, 2, 3, 4);
zfloat x = vec[0];  // 1
vec[1] = (zfloat)5;  // 设置 y 为 5
```

### sqrMagnitude

```csharp
public zfloat sqrMagnitude { get; }
```

获取向量的长度平方（无需计算平方根，适用于比较大小）。

**性能特征:** O(1)，无浮点数平方根运算

**备注:** 相比 `magnitude` 属性，`sqrMagnitude` 避免了平方根计算，在性能敏感的场景（如距离比较）中应优先使用。

### magnitude

```csharp
public zfloat magnitude { get; }
```

获取向量的长度（模）。

**性能特征:** O(sqrt)，涉及平方根计算

**备注:** 精度限制：`zfloat` 采用定点数（4 位小数精度），在极大或极小值下可能存在精度误差。如仅需比较大小，建议使用 `sqrMagnitude` 属性。

### normalized

```csharp
[JsonIgnore]
public zVector4 normalized { get; }
```

获取向量的归一化向量（单位向量）。

**性能特征:** O(sqrt)，每次调用都会重新计算

**备注:**
- 对于零向量，返回 `zero` 常量
- 如需频繁使用归一化向量，考虑缓存结果而非重复调用此属性
- 定点数精度限制下，非常小的向量可能归一化结果不稳定

## 实例方法

### IsZero

```csharp
public bool IsZero()
```

判断向量是否为零向量。

**返回值:** 如果向量的 x、y、z、w 都为 0，返回 true；否则返回 false

**示例:**
```csharp
zVector4 vec = zVector4.zero;
bool isZero = vec.IsZero();  // true
```

### Normalize

```csharp
public void Normalize()
```

将当前向量原地归一化。

**备注:** 对于零向量，设置为 `zero` 常量。

**示例:**
```csharp
zVector4 vec = new zVector4(3, 4, 0, 0);
vec.Normalize();
// vec 现在是单位向量
```

### Scale

```csharp
public void Scale(zVector4 scale)
```

将当前向量按照 `scale` 向量进行元素级乘法（原地修改）。

**性能特征:** O(1)

**参数:**
- `scale`: 缩放因子向量

**备注:** 此方法执行元素级乘法：`result.x = this.x * scale.x`，以此类推。由于 `zfloat` 为定点数，乘法后需要除以 `SCALE_10000` 来维持精度。

### Add

```csharp
public void Add(ref zVector4 vec)
```

向当前向量添加另一个向量（原地修改）。

**性能特征:** O(1)，使用 `.value` 直接操作优化性能

**参数:**
- `vec`: 要添加的向量

### Sub

```csharp
public void Sub(ref zVector4 vec)
```

从当前向量减去另一个向量（原地修改）。

**性能特征:** O(1)，使用 `.value` 直接操作优化性能

**参数:**
- `vec`: 要减去的向量

### Mul

```csharp
public void Mul(ref zVector4 vec)
```

将当前向量与另一个向量进行元素级乘法（原地修改）。

**性能特征:** O(1)

**参数:**
- `vec`: 乘法因子向量

### Div

```csharp
public void Div(ref zVector4 vec)
```

将当前向量与另一个向量进行元素级除法（原地修改）。

**性能特征:** O(1)

**参数:**
- `vec`: 除数向量（不能包含 0 分量，否则结果为无穷大）

### Set

```csharp
public void Set(zfloat new_x, zfloat new_y, zfloat new_z, zfloat new_w)
```

设置向量的所有分量。

**参数:**
- `new_x`: 新的 X 分量
- `new_y`: 新的 Y 分量
- `new_z`: 新的 Z 分量
- `new_w`: 新的 W 分量

### GetNormalizedForMagnitude

```csharp
public zVector4 GetNormalizedForMagnitude(zfloat magnitude)
```

根据已知向量长度获取归一化向量（性能优化版本，避免重复计算长度）。

**性能特征:** O(1)，不需要计算平方根

**参数:**
- `magnitude`: 向量的预计算长度

**返回值:** 归一化后的单位向量，或 `zero` 如果 `magnitude` 为 0

**备注:** 此方法用于已知向量长度的场景，避免重复计算。如果 `magnitude` 参数不准确，会导致结果错误，请确保传入正确的长度值。

## 静态方法

### Normalize

```csharp
public static zVector4 Normalize(ref zVector4 vec)
```

获取向量的归一化向量（返回新的 `zVector4`，不修改原向量）。

**性能特征:** O(sqrt)

**参数:**
- `vec`: 要归一化的向量（ref 参数用于性能优化，不会被修改）

**返回值:** 归一化后的单位向量，或 `zero` 如果原向量为零

### Magnitude

```csharp
public static zfloat Magnitude(ref zVector4 a)
```

计算向量的长度（模）。

**性能特征:** O(sqrt)，涉及平方根计算

**参数:**
- `a`: 要计算长度的向量

**返回值:** 向量的长度

**备注:** 定点数精度：`zfloat` 采用 4 位小数精度（0.0001）。在极端数值下可能存在精度误差。如仅需比较，推荐使用 `SqrMagnitude`。

### SqrMagnitude

```csharp
public static zfloat SqrMagnitude(ref zVector4 a)
```

计算向量的长度平方（无需平方根运算）。

**性能特征:** O(1)，不涉及平方根

**参数:**
- `a`: 要计算长度平方的向量

**返回值:** 向量长度的平方

**备注:** 在需要比较向量大小时，使用此方法而非 `Magnitude` 避免平方根计算。

**示例:**
```csharp
if (sqrMagnitude > maxLength * maxLength) { ... }
```

### Lerp

```csharp
public static zVector4 Lerp(zVector4 from, zVector4 to, zfloat t)
```

线性插值。

**参数:**
- `from`: 起始向量
- `to`: 目标向量
- `t`: 插值参数（0 到 1 之间）

**返回值:** 插值结果向量

**备注:**
- 当 `t=0` 时返回 `from`
- 当 `t=1` 时返回 `to`
- `t` 值会被限制在 [0,1] 范围内

**示例:**
```csharp
zVector4 start = new zVector4(0, 0, 0, 0);
zVector4 end = new zVector4(10, 10, 10, 10);
zVector4 mid = zVector4.Lerp(start, end, (zfloat)0.5);  // (5, 5, 5, 5)
```

### MoveTowards

```csharp
public static zVector4 MoveTowards(zVector4 current, zVector4 target, zfloat maxDistanceDelta)
```

逐步移动当前向量朝向目标，每次移动距离不超过 `maxDistanceDelta`。

**性能特征:** O(sqrt)

**参数:**
- `current`: 当前位置向量
- `target`: 目标位置向量
- `maxDistanceDelta`: 单次移动的最大距离

**返回值:** 移动后的向量（最多移动 `maxDistanceDelta` 距离）

**备注:** 类似于线性插值，但限制了移动速度。若 `maxDistanceDelta` 为负值，则向反方向移动。

### Scale

```csharp
public static zVector4 Scale(zVector4 a, zVector4 b)
```

将向量 `a` 和向量 `b` 进行元素级乘法。

**性能特征:** O(1)

**参数:**
- `a`: 第一个向量
- `b`: 第二个向量（缩放因子）

**返回值:** 元素级相乘的结果向量

**示例:**
```csharp
zVector4 result = zVector4.Scale(
    new zVector4(2, 3, 4, 5),
    new zVector4(1.5, 2, 0.5, 1)
);  // (3, 6, 2, 5)
```

### Dot

```csharp
public static zfloat Dot(ref zVector4 lhs, ref zVector4 rhs)
public static zfloat Dot(zVector4 lhs, zVector4 rhs)
```

计算两个向量的点积（数量积）。

**性能特征:** O(1)

**参数:**
- `lhs`: 左操作向量
- `rhs`: 右操作向量

**返回值:** 两个向量的点积

**备注:** 点积 = |A| * |B| * cos(θ)。用途：判断两向量的夹角（点积>0 为锐角，=0 为直角，<0 为钝角）、投影长度计算等。

### Project

```csharp
public static zVector4 Project(zVector4 vec, zVector4 onNormal)
```

将向量投影到另一个向量上。

**参数:**
- `vec`: 要投影的向量
- `onNormal`: 投影方向向量

**返回值:** 投影结果向量

**备注:** 如果 `onNormal` 为零向量，返回 `zero`。

### Distance

```csharp
public static zfloat Distance(zVector4 a, zVector4 b)
```

计算两个向量之间的距离。

**参数:**
- `a`: 第一个向量
- `b`: 第二个向量

**返回值:** 两个向量之间的距离

### SqrDistance

```csharp
public static zfloat SqrDistance(zVector4 a, zVector4 b)
```

计算两个向量之间距离的平方。

**参数:**
- `a`: 第一个向量
- `b`: 第二个向量

**返回值:** 两个向量之间距离的平方

**备注:** 在需要比较距离大小时，使用此方法而非 `Distance` 避免平方根计算。

### ClampMagnitude

```csharp
public static zVector4 ClampMagnitude(zVector4 vector, zfloat maxLength)
```

限制向量长度，如果长度超过 `maxLength` 则截取，否则原样返回。

**参数:**
- `vector`: 要限制的向量
- `maxLength`: 最大长度

**返回值:** 长度不超过 `maxLength` 的向量

### Min

```csharp
public static zVector4 Min(zVector4 lhs, zVector4 rhs)
```

获取两个向量按分量取最小值的结果。

**参数:**
- `lhs`: 第一个向量
- `rhs`: 第二个向量

**返回值:** 每个分量都是两个向量对应分量的最小值

### Max

```csharp
public static zVector4 Max(zVector4 lhs, zVector4 rhs)
```

获取两个向量按分量取最大值的结果。

**参数:**
- `lhs`: 第一个向量
- `rhs`: 第二个向量

**返回值:** 每个分量都是两个向量对应分量的最大值

### Angle

```csharp
public static zfloat Angle(zVector4 from, zVector4 to)
```

获取两个向量夹角（单位：度，范围 [0, 180]）。

**参数:**
- `from`: 起始向量
- `to`: 目标向量

**返回值:** 两个向量之间的夹角（度）

**备注:** 对于零向量，返回 0。

## 运算符重载

### 加法运算符

```csharp
public static zVector4 operator +(zVector4 lhs, zVector4 rhs)
public static zVector4 operator +(int lhs, zVector4 rhs)
public static zVector4 operator +(zVector4 lhs, int rhs)
public static zVector4 operator +(zfloat lhs, zVector4 rhs)
public static zVector4 operator +(zVector4 lhs, zfloat rhs)
```

向量加法或标量加法。

**性能特征:** O(1)，使用 `.value` 直接操作

### 减法运算符

```csharp
public static zVector4 operator -(zVector4 lhs, zVector4 rhs)
public static zVector4 operator -(int lhs, zVector4 rhs)
public static zVector4 operator -(zVector4 lhs, int rhs)
public static zfloat operator -(zfloat lhs, zVector4 rhs)
public static zVector4 operator -(zVector4 lhs, zfloat rhs)
```

向量减法或标量减法。

**性能特征:** O(1)，使用 `.value` 直接操作

### 负号运算符

```csharp
public static zVector4 operator -(zVector4 a)
```

向量取反：(-x, -y, -z, -w)。

**性能特征:** O(1)

### 乘法运算符

```csharp
public static zVector4 operator *(int lhs, zVector4 rhs)
public static zVector4 operator *(zVector4 lhs, int rhs)
public static zVector4 operator *(zfloat lhs, zVector4 rhs)
public static zVector4 operator *(zVector4 lhs, zfloat rhs)
```

向量与标量乘法。

**性能特征:** O(1)，使用 `.value` 直接操作

### 除法运算符

```csharp
public static zVector4 operator /(zVector4 lhs, int rhs)
public static zVector4 operator /(zVector4 lhs, zfloat rhs)
```

向量除以标量。

**性能特征:** O(1)，使用 `.value` 直接操作

### 相等运算符

```csharp
public static bool operator ==(zVector4 lhs, zVector4 rhs)
public static bool operator !=(zVector4 lhs, zVector4 rhs)
```

判断两个向量是否相等。

**性能特征:** O(1)，使用 `.value` 直接比较

### 类型转换运算符

```csharp
public static implicit operator zVector4(zVector3 v)
public static implicit operator zVector3(zVector4 v)
public static implicit operator zVector4(zVector2 v)
public static implicit operator zVector2(zVector4 v)
```

与其他向量类型的隐式转换。

## Object 方法

### ToString

```csharp
public override string ToString()
```

获取向量的字符串表示形式。

**返回值:** 格式为 "(x , y , z , w)" 的字符串

### Equals

```csharp
public override bool Equals(object obj)
public bool Equals(zVector4 other)
```

判断是否与另一个对象或向量相等。

### GetHashCode

```csharp
public override int GetHashCode()
```

获取哈希码。

**返回值:** 基于 x、y、z、w 的 raw 值计算的哈希码

## 序列化

### GetObjectData

```csharp
public void GetObjectData(SerializationInfo info, StreamingContext context)
```

实现 `ISerializable` 接口的序列化方法。

**参数:**
- `info`: 序列化信息
- `context`: 流上下文

## 使用示例

### 基本向量运算

```csharp
// 创建向量
zVector4 a = new zVector4(1, 2, 3, 4);
zVector4 b = new zVector4(5, 6, 7, 8);

// 向量加法
zVector4 sum = a + b;  // (6, 8, 10, 12)

// 向量减法
zVector4 diff = b - a;  // (4, 4, 4, 4)

// 标量乘法
zVector4 scaled = a * (zfloat)2;  // (2, 4, 6, 8)

// 向量归一化
zVector4 normalized = a.normalized;
```

### 距离计算

```csharp
zVector4 point1 = new zVector4(0, 0, 0, 0);
zVector4 point2 = new zVector4(3, 4, 0, 0);

// 计算距离
zfloat distance = zVector4.Distance(point1, point2);  // 5

// 计算平方距离（性能更好）
zfloat sqrDistance = zVector4.SqrDistance(point1, point2);  // 25
```

### 插值和移动

```csharp
zVector4 start = new zVector4(0, 0, 0, 0);
zVector4 target = new zVector4(10, 10, 10, 10);

// 线性插值
zVector4 mid = zVector4.Lerp(start, target, (zfloat)0.5);  // (5, 5, 5, 5)

// 逐步移动
zVector4 current = start;
zfloat maxStep = (zfloat)2;
for (int i = 0; i < 10; i++)
{
    current = zVector4.MoveTowards(current, target, maxStep);
}
```

### 点积和投影

```csharp
zVector4 vec = new zVector4(2, 2, 0, 0);
zVector4 normal = new zVector4(1, 0, 0, 0);

// 计算点积
zfloat dot = zVector4.Dot(vec, normal);  // 2

// 投影
zVector4 projected = zVector4.Project(vec, normal);  // (2, 0, 0, 0)
```

### 角度计算

```csharp
zVector4 from = new zVector4(1, 0, 0, 0);
zVector4 to = new zVector4(0, 1, 0, 0);

// 计算夹角
zfloat angle = zVector4.Angle(from, to);  // 90 度
```

## 性能优化建议

1. **使用 `sqrMagnitude` 代替 `magnitude`**: 在只需要比较向量长度时，使用 `sqrMagnitude` 避免平方根计算
2. **使用 `SqrDistance` 代替 `Distance`**: 在只需要比较距离时，使用 `SqrDistance` 避免平方根计算
3. **缓存归一化结果**: 如果需要频繁使用归一化向量，考虑缓存结果而非重复调用 `normalized` 属性
4. **使用 ref 参数**: 在高频调用的场景中，使用 ref 参数版本的静态方法可以减少拷贝开销
5. **使用 `.value` 直接操作**: 在性能关键的场景中，直接操作 `.value` 字段可以避免额外的运算符重载开销

## 注意事项

1. **定点数精度**: `zfloat` 采用 4 位小数精度（0.0001），在极大或极小值下可能存在精度误差
2. **零向量处理**: 对零向量进行归一化或投影操作时，会返回 `zero` 常量
3. **除零保护**: 除法操作中，除数为零会导致结果为无穷大或未定义行为
4. **序列化**: 使用 `ISerializable` 接口进行序列化时，只序列化 raw 值
5. **JSON 序列化**: `normalized` 属性使用 `[JsonIgnore]` 特性，避免循环引用

## 相关类型

- `zfloat`: 定点数类型
- `zVector2`: 二维向量
- `zVector3`: 三维向量
- `zMathf`: 定点数数学函数库

## 版本历史

- **v1.0**: 初始版本
- **v1.1**: 添加完整 XML 文档注释，修复 Bug，优化性能，添加新方法
