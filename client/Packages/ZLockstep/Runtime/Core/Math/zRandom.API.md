# zRandom API 参考文档

**文档版本**: 1.0.0  
**对应源码**: `Packages/ZLockstep/Runtime/Core/Math/zRandom.cs`  
**命名空间**: `zUnity`

---

## 概述

`zRandom` 是一个面向锁步/定点逻辑的确定性随机数工具，基于 48-bit LCG（线性同余生成器）实现。

- 同一种子可复现同一随机序列
- 区间随机语义统一为左闭右开 `[min, max)`
- 使用标准化 `Next*` 接口族

---

## 构造与种子

```csharp
zRandom()
zRandom(long seed)
void SetSeed(long seed)
```

说明：
- `zRandom()` 使用时间相关种子，适合普通运行时场景。
- 需要严格复现时，请使用 `zRandom(long seed)` 并固定种子。

---

## 核心随机 API

```csharp
int NextInt()                              // 任意 32-bit int
int NextInt(int n)                         // [0, n)
int NextInt(int minValue, int maxValue)    // [min, max)

long NextLong()                            // 任意 64-bit long
long NextLong(long minValue, long maxValue)// [min, max)

zfloat NextZFloat(zfloat minValue, zfloat maxValue) // [min, max)

void NextBytes(byte[] bytes)

bool NextBool()                            // 50%
bool NextBool(zfloat probability)          // probability in [0,1]
```

---

## 向量随机 API

```csharp
zVector2 NextUnitVector2()      // 单位圆上（模长约等于 1）
zVector2 NextInsideUnitCircle() // 单位圆内（模长 <= 1）
```

说明：
- `NextInsideUnitCircle()` 采用极坐标采样，半径使用 `sqrt(u)` 修正面积分布。

---

## 区间语义与异常约定

所有区间接口统一：
- 语义：`[min, max)`
- `min == max`：返回 `min`
- `min > max`：抛 `ArgumentOutOfRangeException`

专项约定：
- `NextInt(int n)`：
  - `n == 0` 返回 `0`
  - `n < 0` 抛 `ArgumentOutOfRangeException`
- `NextBool(zfloat probability)`：
  - `probability < 0` 或 `> 1` 抛 `ArgumentOutOfRangeException`
- `NextBytes(byte[] bytes)`：
  - `bytes == null` 抛 `ArgumentNullException`

---

## 示例

```csharp
// 1) 可复现随机序列
var rng = new zRandom(20260421);
int a = rng.NextInt(0, 10);
long b = rng.NextLong(-1000, 1000);
zfloat c = rng.NextZFloat(zfloat.Zero, zfloat.One);

// 2) 概率判定
if (rng.NextBool(zfloat.FromRaw(2500))) // 25%
{
    // ...
}

// 3) 方向与散点
zVector2 dir = rng.NextUnitVector2();
zVector2 p = rng.NextInsideUnitCircle();
```

---

## 迁移建议（旧 -> 新）

替换映射：

- `Range(int, int)` -> `NextInt(int, int)`
- `Range(long, long)` -> `NextLong(long, long)`
- `Range(zfloat, zfloat)` -> `NextZFloat(zfloat, zfloat)`
- `onUnitCircle` -> `NextUnitVector2()`
- `inUnitCircle` -> `NextInsideUnitCircle()`

推荐迁移步骤：

1. 先执行全仓替换，将 `Range/onUnitCircle/inUnitCircle` 改为对应 `Next*` 新接口。
2. 替换后跑随机相关回归测试，重点验证区间边界和固定 seed 可复现性。
3. 确认无旧调用后再升级依赖分支，避免编译期中断。

迁移注意事项：

- 迁移前后区间语义一致，均为左闭右开 `[min, max)`。
- `NextBool(zfloat probability)` 仅接受 `[0,1]`，越界会抛异常。
- 旧接口已移除，需保证所有调用点已完成替换。
