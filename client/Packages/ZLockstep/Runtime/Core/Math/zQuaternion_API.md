# zQuaternion API 参考文档

**文档版本**: 1.0.0  
**对应源码**: `Packages/ZLockstep/Runtime/Core/Math/zQuaternion.cs`  
**命名空间**: `zUnity`

---

## 概述

`zQuaternion` 是用于确定性计算的定点四元数结构体，实现了 `IEquatable<zQuaternion>`，核心分量为 `zfloat x/y/z/w`。

---

## 字段

```csharp
public zfloat x
public zfloat y
public zfloat z
public zfloat w

public static readonly zQuaternion identity
public static readonly zQuaternion xPositive
```

---

## 构造函数

```csharp
zQuaternion(zfloat x, zfloat y, zfloat z, zfloat w)
```

---

## 属性与索引器

```csharp
zfloat this[int index]          // 0->x, 1->y, 2->z, 3->w
zVector3 eulerAngles            // 度；get=ToEuler, set=Euler
zfloat LengthSquared            // |q|^2
zfloat Length                   // |q|
bool IsNormalized               // 近似单位四元数
zQuaternion Normalized          // 归一化副本
```

---

## 实例方法

```csharp
void Set(zfloat new_x, zfloat new_y, zfloat new_z, zfloat new_w)
void ToAngleAxis(out zfloat angle, out zVector3 axis)
void SetFromToRotation(zVector3 fromDirection, zVector3 toDirection)
zMatrix4x4 RotationMatrix()
void SetLookRotation(zVector3 view)
void SetLookRotation(zVector3 view, zVector3 up)
void Normalize()

override string ToString()
override int GetHashCode()
override bool Equals(object other)
bool Equals(zQuaternion other)
```

---

## 静态方法

```csharp
zfloat Dot(zQuaternion a, zQuaternion b)
zfloat Angle(zQuaternion a, zQuaternion b)                      // 度，范围[0,180]
zQuaternion AngleAxis(zfloat angle, zVector3 axis)
zQuaternion FromMatrix(zMatrix4x4 m)
void ToAxis(zQuaternion q, ref zVector3 vx, ref zVector3 vy, ref zVector3 vz)
zQuaternion FromToRotation(zVector3 fromDirection, zVector3 toDirection)

zQuaternion LookRotation(zVector3 forward, zVector3 upwards)
zQuaternion LookRotation(zVector3 forward)

zQuaternion Normalize(zQuaternion value)
zQuaternion Slerp(zQuaternion from, zQuaternion to, zfloat amount)
zQuaternion Lerp(zQuaternion from, zQuaternion to, zfloat amount)
zQuaternion Inverse(zQuaternion rotation)

zQuaternion Euler(zfloat x, zfloat y, zfloat z)                 // 输入单位为度
zQuaternion Euler(zVector3 euler)
zQuaternion Conjugate(zQuaternion value)
zVector3 ToEuler(zQuaternion zq)                                // 输出单位为度
```

---

## 运算符

```csharp
*  : (zQuaternion,zQuaternion)   // 组合旋转
*  : (zQuaternion,zVector3)      // 旋转向量
== : (zQuaternion,zQuaternion)
!= : (zQuaternion,zQuaternion)
```

---

## 语义说明

- `==` / `!=` 是旋转语义比较，不是逐分量比较；`q` 与 `-q` 会被视为同一旋转。
- `Equals(zQuaternion)` 是逐分量比较，语义与 `==` 不同。
- `Angle` 对非单位四元数同样有效，内部会按 `abs(dot(a,b)) / (|a||b|)` 计算并做夹紧。
- 对零长度输入，`Normalize`/`Inverse` 等会返回 `identity`，用于避免除零。
- `Slerp`/`Lerp` 的 `amount` 会被限制到 `[0,1]`。

---

## 使用建议

```csharp
// 1) 需要“旋转是否相同”时，用 ==（考虑 q 与 -q 等价）
if (a == b) { ... }

// 2) 需要“分量完全一致”时，用 Equals
if (a.Equals(b)) { ... }

// 3) 统一用度数接口
zQuaternion q = zQuaternion.Euler(0, 90, 0);
zfloat deg = zQuaternion.Angle(q, zQuaternion.identity);
```
