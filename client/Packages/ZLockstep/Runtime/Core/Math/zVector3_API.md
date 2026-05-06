# zVector3 API 参考文档

**文档版本**: 1.0.0  
**对应源码**: `Packages/ZLockstep/Runtime/Core/Math/zVector3.cs`  
**命名空间**: `zUnity`

---

## 概述

`zVector3` 是用于确定性计算的三维定点向量结构体，实现了 `ISerializable` 与 `IEquatable<zVector3>`。

---

## 常量与字段

```csharp
public static readonly zVector3 zero
public static readonly zVector3 one
public static readonly zVector3 forward
public static readonly zVector3 back
public static readonly zVector3 up
public static readonly zVector3 down
public static readonly zVector3 left
public static readonly zVector3 right
public static readonly zVector3 NULL     // 历史兼容哨兵值

public zfloat x
public zfloat y
public zfloat z
```

---

## 构造函数

```csharp
zVector3(zfloat x, zfloat y, zfloat z)
zVector3(int x, int y, int z)
zVector3(zVector3 vec)
zVector3(SerializationInfo info, StreamingContext context)
```

---

## 属性与索引器

```csharp
zfloat this[int index]          // 0->x, 1->y, 2->z
zfloat sqrMagnitude
zfloat magnitude
zVector3 normalized
zVector3 approxNormalizedXZ
```

---

## 实例方法

```csharp
bool IsZero()
void Set(zfloat new_x, zfloat new_y, zfloat new_z)
void Normalize()
zVector3 GetNormalizedForMagnitude(zfloat magnitude)
zVector3 GetApproxNormalizedXZForMagnitude(zfloat magnitude)

void Scale(zVector3 scale)
void Add(ref zVector3 vec)
void Sub(ref zVector3 vec)
void Mul(ref zVector3 vec)
void Div(ref zVector3 vec)
```

---

## 静态方法

```csharp
zVector3 Normalize(ref zVector3 vec)
zfloat Magnitude(ref zVector3 a)
zfloat SqrMagnitude(ref zVector3 a)

zVector3 Lerp(zVector3 from, zVector3 to, zfloat t)
zVector3 Slerp(zVector3 from, zVector3 to, zfloat t)
void OrthoNormalize(ref zVector3 normal, ref zVector3 tangent)

zVector3 MoveTowards(zVector3 current, zVector3 target, zfloat maxDistanceDelta)
zVector3 RotateTowards(zVector3 current, zVector3 target, zfloat maxMagnitudeDelta)
zVector3 RotateTowards(zVector3 current, zVector3 target, zfloat maxRadiansDelta, zfloat maxMagnitudeDelta)
zVector3 SmoothDamp(zVector3 current, zVector3 target, ref zVector3 currentVelocity, zfloat smoothTime, zfloat maxSpeed, zfloat deltaTime)

zVector3 Scale(zVector3 a, zVector3 b)
zVector3 Cross(zVector3 lhs, zVector3 rhs)
zVector3 Reflect(zVector3 inDir, zVector3 inNormal)
zfloat Dot(ref zVector3 lhs, ref zVector3 rhs)
zfloat Dot(zVector3 lhs, zVector3 rhs)
zVector3 Project(zVector3 vector, zVector3 onNormal)

zfloat Angle(zVector3 from, zVector3 to)       // 度，范围[0,180]
zfloat A2B_angle(zVector3 A, zVector3 B)       // 度，范围约[-180,180]
zfloat Distance(zVector3 a, zVector3 b)

zVector3 ClampMagnitude(zVector3 vector, zfloat maxLength)
zVector3 Min(zVector3 lhs, zVector3 rhs)
zVector3 Max(zVector3 lhs, zVector3 rhs)
```

---

## 运算符

```csharp
+ : (zVector3,zVector3) (int,zVector3) (zVector3,int) (zfloat,zVector3) (zVector3,zfloat)
- : (zVector3,zVector3) (int,zVector3) (zVector3,int) (zfloat,zVector3) (zVector3,zfloat) (zVector3 unary)
* : (int,zVector3) (zVector3,int) (zfloat,zVector3) (zVector3,zfloat)
/ : (zVector3,int) (zVector3,zfloat)
==, != : (zVector3,zVector3)
```

---

## 对象基础方法

```csharp
override string ToString()
override bool Equals(object obj)
bool Equals(zVector3 other)
override int GetHashCode()
void GetObjectData(SerializationInfo info, StreamingContext context)
```

---

## 注意事项

- `Angle` 与 `A2B_angle` 返回单位为“度”，不是弧度。
- 对零向量参与归一化、角度、投影的场景，API 会返回稳定兜底值（通常是 `zero` 或 `0`）。
- `RotateTowards` 新增了 Unity 语义重载；旧签名保留兼容。
- `NULL` 仅用于历史兼容，业务代码建议使用显式状态字段替代。
