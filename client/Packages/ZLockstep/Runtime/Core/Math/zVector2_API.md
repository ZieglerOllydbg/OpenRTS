# zVector2 API 参考文档

**文档版本**: 1.0.1  
**对应源码**: `Packages/ZLockstep/Runtime/Core/Math/zVector2.cs`  
**命名空间**: `zUnity`

---

## 概述

`zVector2` 是用于确定性计算的二维定点向量结构体，实现了 `ISerializable`，核心分量为 `zfloat x/y`。

---

## 常量与字段

```csharp
public static readonly zVector2 zero   // (0, 0)
public static readonly zVector2 one    // (1, 1)
public static readonly zVector2 up     // (0, 1)
public static readonly zVector2 down   // (0, -1)
public static readonly zVector2 left   // (-1, 0)
public static readonly zVector2 right  // (1, 0)

public zfloat x;
public zfloat y;
```

---

## 构造函数

```csharp
zVector2(zfloat x, zfloat y)
zVector2(int x, int y)
zVector2(zVector2 vec)
zVector2(SerializationInfo info, StreamingContext context) // 反序列化
```

---

## 属性与索引器

```csharp
zfloat this[int index]              // 0->x, 1->y
zfloat sqrMagnitude                 // O(1)
zfloat magnitude                    // O(sqrt)
zVector2 normalized                 // O(sqrt)
zVector2 approxNormalizedXY         // 近似归一化
```

---

## 实例方法

```csharp
bool IsZero()
void Normalize()                    // 原地归一化
zVector2 GetNormalizedForMagnitude(zfloat magnitude)

void Scale(zVector2 scale)          // 原地逐分量乘
void Add(ref zVector2 vec)          // 原地加
void Sub(ref zVector2 vec)          // 原地减
void Mul(ref zVector2 vec)          // 原地逐分量乘
void Div(ref zVector2 vec)          // 原地逐分量除
```

---

## 静态方法

```csharp
zVector2 Normalize(ref zVector2 vec)
zfloat Magnitude(ref zVector2 a)
zfloat SqrMagnitude(ref zVector2 a)

zVector2 Lerp(zVector2 from, zVector2 to, zfloat t)
zVector2 MoveTowards(zVector2 current, zVector2 target, zfloat maxDistanceDelta)
zVector2 Scale(zVector2 a, zVector2 b)
zVector2 ClampMagnitude(zVector2 vector, zfloat maxLength)

zVector3 Cross(zVector2 lhs, zVector2 rhs)
zfloat Dot(ref zVector2 lhs, ref zVector2 rhs)
zfloat Dot(zVector2 lhs, zVector2 rhs)
zVector2 Project(zVector2 vector, zVector2 onNormal)
zfloat Distance(zVector2 a, zVector2 b)

zfloat Angle(zVector2 from, zVector2 to)       // 返回角度(度)，范围[0,180]
zfloat A2B_angle(zVector2 A, zVector2 B)       // 返回有向角(度)，范围[-180,180]

zVector2 Min(zVector2 lhs, zVector2 rhs)
zVector2 Max(zVector2 lhs, zVector2 rhs)
```

说明:
- `Angle` 当前实现返回的是“度”，不是弧度。
- `A2B_angle` 内部包含归一化和 `Acos`，复杂度不应按 O(1) 理解。

---

## 运算符

```csharp
+  : (zVector2,zVector2) (int,zVector2) (zVector2,int) (zfloat,zVector2) (zVector2,zfloat)
-  : (zVector2,zVector2) (int,zVector2) (zVector2,int) (zfloat,zVector2) (zVector2,zfloat) (zVector2 unary)
*  : (int,zVector2) (zVector2,int) (zfloat,zVector2) (zVector2,zfloat)
/  : (zVector2,int) (zVector2,zfloat)
== : (zVector2,zVector2)
!= : (zVector2,zVector2)
```

---

## 隐式转换

```csharp
implicit zVector2(zVector3 v)   // 丢弃 z
implicit zVector3(zVector2 v)   // z = 0
```

---

## 对象基础方法

```csharp
override string ToString()
override bool Equals(object obj)
bool Equals(zVector2 other)
override int GetHashCode()
void GetObjectData(SerializationInfo info, StreamingContext context)
```

---

## 使用建议

```csharp
// 1) 距离比较优先 sqrMagnitude
if ((a - b).sqrMagnitude < range * range) { ... }

// 2) 高频循环可用原地方法减少中间值
vel.Add(ref accel);

// 3) 角度判定按“度”处理
zfloat deg = zVector2.Angle(dirA, dirB);
```

---

## 更新记录

### v1.0.1 (2026-04-20)
- 按当前 `zVector2.cs` 校正文档接口清单。
- 更正 `Angle` 返回单位为“度”。
- 更正 `A2B_angle` 复杂度描述。
- 补充序列化构造函数、`Dot` 重载、实例 `Scale` 与对象基础方法说明。

