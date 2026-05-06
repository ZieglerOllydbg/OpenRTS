using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

/// <summary>
/// 4 位小数精度的定点数。
/// 内部以 long 存储放大 10000 倍后的原始值（raw）。
/// 例如：1.2345 的 raw 为 12345。
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
[ComVisible(true)]
public struct zfloat : ISerializable, IComparable, IComparable<zfloat>, IEquatable<zfloat>, IFormattable
{

	/// <summary>缩放系数（10000 = 1）。</summary>
	public const long SCALE_10000 = 10000;
	public const long SCALE_1000 = 1000;
	/// <summary>缩放系数的平方（10000 * 10000）。</summary>
	public const long SCALE_100000000 = 100000000;
	/// <summary>缩放系数开方（100）。</summary>
	public const int SCALE_100 = 100;
	/// <summary>
	/// 缩放系数 SCALE_10000 的四次方根（10000^(1/4)=10）。
	/// 主要用于 Pow_Int_10000 的中间缩放控制，降低中间值膨胀风险。
	/// </summary>
	public const int SCALE_ROOT4_10000 = 10;
	/// <summary>
	/// 兼容旧名称，不建议新代码继续使用。请改用 <see cref="SCALE_ROOT4_10000"/>。
	/// </summary>
	[Obsolete("SCALE_QUARTER is deprecated. Use SCALE_ROOT4_10000 instead.", false)]
	public const int SCALE_QUARTER = SCALE_ROOT4_10000;

	public static readonly zfloat One = new zfloat(1);
	public static readonly zfloat Zero = new zfloat(0);
	public static readonly zfloat Two = new zfloat(2);
	public static readonly zfloat Half = new zfloat(0, 5000);
	public static readonly zfloat Quarter = new zfloat(0, 2500);
	public static readonly zfloat NegativeOne = new zfloat(-1);
	public static readonly zfloat OneHalf = new zfloat(1, 5000);
	public static readonly zfloat SqrtTwo = new zfloat(1, 4142);
	public static readonly zfloat Hundred = new zfloat(100);
	/// <summary>
	/// raw 最小值（long.MinValue）。
	/// </summary>
	public static readonly zfloat MinValue = FromRaw(long.MinValue);
	/// <summary>
	/// raw 最大值（long.MaxValue）。
	/// </summary>
	public static readonly zfloat MaxValue = FromRaw(long.MaxValue);
	/// <summary>
	/// 兼容历史语义的“无穷大”哨兵值，等同于 <see cref="MaxValue"/>。
	/// </summary>
	public static readonly zfloat Infinity = MaxValue;
	public static readonly zfloat Epsilon = new zfloat(0, 1);

	/// <summary>
	/// 放大 10000 倍后的原始值。
	/// </summary>
	public long value;

	/// <summary>
	/// 初始化一个新的 zfloat 实例。
	/// </summary>
	/// <param name="__value">参数 __value。</param>
	public zfloat(int __value)
	{
		value = __value * SCALE_10000;
	}

	/// <summary>
	/// 初始化一个新的 zfloat 实例。
	/// </summary>
	/// <param name="__value">参数 __value。</param>
	public zfloat(long __value)
	{
		value = __value * SCALE_10000;
	}

	/// <summary>
	/// 初始化一个新的 zfloat 实例。
	/// </summary>
	/// <param name="__value">参数 __value。</param>
	public zfloat(float __value)
	{
		value = (long)(__value * SCALE_10000);
	}

	/// <summary>
	/// 分别指定整数部分和小数部分（小数部分单位：1/10000）。
	/// </summary>
	public zfloat(int intPart, int decimalsPart_10000)
	{
		value = intPart * SCALE_10000 + decimalsPart_10000;
	}

	/// <summary>
	/// 分别指定整数部分和小数部分（小数部分单位：1/10000）。
	/// </summary>
	public zfloat(int intPart, long decimalsPart_10000)
	{
		value = intPart * SCALE_10000 + decimalsPart_10000;
	}

	/// <summary>
	/// 初始化一个新的 zfloat 实例。
	/// </summary>
	/// <param name="__value">参数 __value。</param>
	public zfloat(zfloat __value)
	{
		value = __value.value;
	}

	/// <summary>
	/// 通过 raw 值创建 zfloat，raw 的单位为 1/10000。
	/// </summary>
	public static zfloat FromRaw(long raw)
	{
		zfloat zf;
		zf.value = raw;
		return zf;
	}

	/// <summary>
	/// 通过 int 创建 zfloat。
	/// </summary>
	public static zfloat FromInt(int valueInt)
	{
		return new zfloat(valueInt);
	}

	/// <summary>
	/// 通过 long 创建 zfloat。
	/// </summary>
	public static zfloat FromLong(long valueLong)
	{
		return new zfloat(valueLong);
	}

	/// <summary>
	/// 从 float 转换为 zfloat。
	/// 采用截断语义（向零截断），不会四舍五入。
	/// </summary>
	public static zfloat FromFloat(float valueFloat)
	{
		zfloat zf;
		zf.value = (long)(valueFloat * SCALE_10000);
		return zf;
	}

	/// <summary>
	/// 返回小数部分（单位仍为 1/10000，保留符号）。
	/// </summary>
	public zfloat GetFractionalPart()
	{
		return FromRaw(value % SCALE_10000);
	}

	/// <summary>
	/// 返回整数部分（向零截断）。
	/// </summary>
	public int GetInteger()
	{
		return (int)(value / SCALE_10000);
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <returns>返回方法结果。</returns>
	public int ToInt32()
	{
		return (int)(value / SCALE_10000);
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <returns>返回方法结果。</returns>
	public long ToInt64()
	{
		return value / SCALE_10000;
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <returns>返回方法结果。</returns>
	public float ToSingle()
	{
		return value / (float)SCALE_10000;
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <returns>返回方法结果。</returns>
	public double ToDouble()
	{
		return value / (double)SCALE_10000;
	}

	/// <summary>
	/// 将 zfloat 数组转换为 float 数组。
	/// </summary>
	public static float[] ToFloatArray(zfloat[] values)
	{
		if (values == null)
		{
			return null;
		}

		float[] result = new float[values.Length];
		for (int i = 0; i < values.Length; ++i)
		{
			result[i] = values[i].ToSingle();
		}

		return result;
	}

	/// <summary>
	/// 绝对值。
	/// </summary>
	public static zfloat Abs(zfloat value)
	{
		if (value.value >= 0)
		{
			return value;
		}

		if (value.value == long.MinValue)
		{
			return MaxValue;
		}

		return FromRaw(-value.value);
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <param name="left">参数 left。</param>
	/// <param name="right">参数 right。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat Min(zfloat left, zfloat right)
	{
		return left.value <= right.value ? left : right;
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <param name="left">参数 left。</param>
	/// <param name="right">参数 right。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat Max(zfloat left, zfloat right)
	{
		return left.value >= right.value ? left : right;
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <param name="value">参数 value。</param>
	/// <param name="min">参数 min。</param>
	/// <param name="max">参数 max。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat Clamp(zfloat value, zfloat min, zfloat max)
	{
		if (min > max)
		{
			throw new ArgumentException("min cannot be greater than max.");
		}

		if (value < min)
		{
			return min;
		}

		if (value > max)
		{
			return max;
		}

		return value;
	}

	/// <summary>
	/// 返回符号：1（正），0（零），-1（负）。
	/// </summary>
	public static int Sign(zfloat value)
	{
		if (value.value > 0)
		{
			return 1;
		}

		if (value.value < 0)
		{
			return -1;
		}

		return 0;
	}

	/// <summary>
	/// 安全加法。成功时返回 true 并写入结果；发生溢出时返回 false。
	/// </summary>
	public static bool TryAdd(zfloat left, zfloat right, out zfloat result)
	{
		if (TryAddRaw(left.value, right.value, out long raw))
		{
			result = FromRaw(raw);
			return true;
		}

		result = Zero;
		return false;
	}

	/// <summary>
	/// 安全减法。成功时返回 true 并写入结果；发生溢出时返回 false。
	/// </summary>
	public static bool TrySub(zfloat left, zfloat right, out zfloat result)
	{
		if (TrySubRaw(left.value, right.value, out long raw))
		{
			result = FromRaw(raw);
			return true;
		}

		result = Zero;
		return false;
	}

	/// <summary>
	/// 安全乘法。成功时返回 true 并写入结果；发生溢出时返回 false。
	/// </summary>
	public static bool TryMul(zfloat left, zfloat right, out zfloat result)
	{
		if (TryMulRaw(left.value, right.value, out long raw))
		{
			result = FromRaw(raw);
			return true;
		}

		result = Zero;
		return false;
	}

	/// <summary>
	/// 安全除法。成功时返回 true 并写入结果；除零或溢出时返回 false。
	/// </summary>
	public static bool TryDiv(zfloat left, zfloat right, out zfloat result)
	{
		if (TryDivRaw(left.value, right.value, out long raw))
		{
			result = FromRaw(raw);
			return true;
		}

		result = Zero;
		return false;
	}

	/// <summary>
	/// Checked 加法。溢出时抛出 <see cref="OverflowException"/>。
	/// </summary>
	public static zfloat CheckedAdd(zfloat left, zfloat right)
	{
		long raw = checked(left.value + right.value);
		return FromRaw(raw);
	}

	/// <summary>
	/// Checked 减法。溢出时抛出 <see cref="OverflowException"/>。
	/// </summary>
	public static zfloat CheckedSub(zfloat left, zfloat right)
	{
		long raw = checked(left.value - right.value);
		return FromRaw(raw);
	}

	/// <summary>
	/// Checked 乘法。溢出时抛出 <see cref="OverflowException"/>。
	/// </summary>
	public static zfloat CheckedMul(zfloat left, zfloat right)
	{
		long mul = checked(left.value * right.value);
		long raw = checked(mul / SCALE_10000);
		return FromRaw(raw);
	}

	/// <summary>
	/// Checked 除法。除零抛 <see cref="DivideByZeroException"/>，溢出抛 <see cref="OverflowException"/>。
	/// </summary>
	public static zfloat CheckedDiv(zfloat left, zfloat right)
	{
		if (right.value == 0)
		{
			throw new DivideByZeroException();
		}

		long num = checked(left.value * SCALE_10000);
		long raw = checked(num / right.value);
		return FromRaw(raw);
	}

	/// <summary>
	/// 判断两个值是否在给定容差内近似相等。
	/// </summary>
	public static bool Approximately(zfloat value, zfloat target, zfloat tolerance)
	{
		ulong leftAbs = AbsRawToUInt64(value.value);
		ulong rightAbs = AbsRawToUInt64(target.value);
		ulong diff;
		if ((value.value >= 0 && target.value >= 0) || (value.value < 0 && target.value < 0))
		{
			diff = value.value >= target.value ? (ulong)(value.value - target.value) : (ulong)(target.value - value.value);
		}
		else
		{
			diff = leftAbs + rightAbs;
		}

		ulong tol = AbsRawToUInt64(tolerance.value);
		return diff <= tol;
	}

	/// <summary>
	/// 判断当前值与目标值是否在给定容差内近似相等。
	/// </summary>
	public bool Approximately(zfloat target, zfloat tolerance)
	{
		return Approximately(this, target, tolerance);
	}

	/// <summary>
	/// 返回相乘后不缩放的原始值乘积。
	/// 主要用于数学库内部优化。
	/// </summary>
	public static long Multiply_Scale(ref zfloat n1, ref zfloat n2)
	{
		return n1.value * n2.value;
	}

	#region 算术运算符
	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator +(zfloat lhs, zfloat rhs)
	{
		return CheckedAdd(lhs, rhs);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator +(int lhs, zfloat rhs)
	{
		long leftRaw = checked((long)lhs * SCALE_10000);
		return FromRaw(checked(leftRaw + rhs.value));
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator +(zfloat lhs, int rhs)
	{
		long rightRaw = checked((long)rhs * SCALE_10000);
		return FromRaw(checked(lhs.value + rightRaw));
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator -(zfloat lhs, zfloat rhs)
	{
		return CheckedSub(lhs, rhs);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator -(int lhs, zfloat rhs)
	{
		long leftRaw = checked((long)lhs * SCALE_10000);
		return FromRaw(checked(leftRaw - rhs.value));
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator -(zfloat lhs, int rhs)
	{
		long rightRaw = checked((long)rhs * SCALE_10000);
		return FromRaw(checked(lhs.value - rightRaw));
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator *(zfloat lhs, zfloat rhs)
	{
		return CheckedMul(lhs, rhs);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator *(int lhs, zfloat rhs)
	{
		return FromRaw(checked((long)lhs * rhs.value));
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator *(zfloat lhs, int rhs)
	{
		return FromRaw(checked(lhs.value * (long)rhs));
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator *(long lhs, zfloat rhs)
	{
		return FromRaw(checked(lhs * rhs.value));
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator *(zfloat lhs, long rhs)
	{
		return FromRaw(checked(lhs.value * rhs));
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator /(zfloat lhs, zfloat rhs)
	{
		return CheckedDiv(lhs, rhs);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator /(int lhs, zfloat rhs)
	{
		long num = checked((long)lhs * SCALE_100000000);
		return FromRaw(checked(num / rhs.value));
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator /(zfloat lhs, int rhs)
	{
		return FromRaw(lhs.value / rhs);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator /(long lhs, zfloat rhs)
	{
		long num = checked(lhs * SCALE_100000000);
		return FromRaw(checked(num / rhs.value));
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator /(zfloat lhs, long rhs)
	{
		return FromRaw(lhs.value / rhs);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator %(zfloat lhs, zfloat rhs)
	{
		return FromRaw(lhs.value % rhs.value);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator %(int lhs, zfloat rhs)
	{
		long leftRaw = checked((long)lhs * SCALE_10000);
		return FromRaw(leftRaw % rhs.value);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator %(zfloat lhs, int rhs)
	{
		long rightRaw = checked((long)rhs * SCALE_10000);
		return FromRaw(lhs.value % rightRaw);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator %(long lhs, zfloat rhs)
	{
		long leftRaw = checked(lhs * SCALE_10000);
		return FromRaw(leftRaw % rhs.value);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator %(zfloat lhs, long rhs)
	{
		long rightRaw = checked(rhs * SCALE_10000);
		return FromRaw(lhs.value % rightRaw);
	}
	#endregion

	#region 比较运算符
	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="v">参数 v。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator -(zfloat v)
	{
		if (v.value == long.MinValue)
		{
			return MaxValue;
		}

		return FromRaw(-v.value);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="v">参数 v。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator +(zfloat v)
	{
		return v;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator ==(zfloat lhs, zfloat rhs)
	{
		return lhs.value == rhs.value;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator ==(int lhs, zfloat rhs)
	{
		return lhs * SCALE_10000 == rhs.value;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator ==(zfloat lhs, int rhs)
	{
		return lhs.value == rhs * SCALE_10000;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator !=(zfloat lhs, zfloat rhs)
	{
		return lhs.value != rhs.value;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator !=(int lhs, zfloat rhs)
	{
		return lhs * SCALE_10000 != rhs.value;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator !=(zfloat lhs, int rhs)
	{
		return lhs.value != rhs * SCALE_10000;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator >(zfloat lhs, zfloat rhs)
	{
		return lhs.value > rhs.value;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator >(int lhs, zfloat rhs)
	{
		return lhs * SCALE_10000 > rhs.value;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator >(zfloat lhs, int rhs)
	{
		return lhs.value > rhs * SCALE_10000;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator >=(zfloat lhs, zfloat rhs)
	{
		return lhs.value >= rhs.value;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator >=(int lhs, zfloat rhs)
	{
		return lhs * SCALE_10000 >= rhs.value;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator >=(zfloat lhs, int rhs)
	{
		return lhs.value >= rhs * SCALE_10000;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator <(zfloat lhs, zfloat rhs)
	{
		return lhs.value < rhs.value;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator <(int lhs, zfloat rhs)
	{
		return lhs * SCALE_10000 < rhs.value;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator <(zfloat lhs, int rhs)
	{
		return lhs.value < rhs * SCALE_10000;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator <=(zfloat lhs, zfloat rhs)
	{
		return lhs.value <= rhs.value;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator <=(int lhs, zfloat rhs)
	{
		return lhs * SCALE_10000 <= rhs.value;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="lhs">参数 lhs。</param>
	/// <param name="rhs">参数 rhs。</param>
	/// <returns>返回方法结果。</returns>
	public static bool operator <=(zfloat lhs, int rhs)
	{
		return lhs.value <= rhs * SCALE_10000;
	}
	#endregion

	#region 自增/自减
	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="v">参数 v。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator --(zfloat v)
	{
		v.value -= SCALE_10000;
		return v;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="v">参数 v。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat operator ++(zfloat v)
	{
		v.value += SCALE_10000;
		return v;
	}
	#endregion

	#region 类型转换
	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="zf">参数 zf。</param>
	/// <returns>返回方法结果。</returns>
	public static explicit operator long(zfloat zf)
	{
		return zf.value / SCALE_10000;
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="zf">参数 zf。</param>
	/// <returns>返回方法结果。</returns>
	public static explicit operator int(zfloat zf)
	{
		return (int)(zf.value / SCALE_10000);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="valueInt">参数 valueInt。</param>
	/// <returns>返回方法结果。</returns>
	public static explicit operator zfloat(int valueInt)
	{
		return new zfloat(valueInt);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="valueLong">参数 valueLong。</param>
	/// <returns>返回方法结果。</returns>
	public static explicit operator zfloat(long valueLong)
	{
		return new zfloat(valueLong);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="valueFloat">参数 valueFloat。</param>
	/// <returns>返回方法结果。</returns>
	public static explicit operator zfloat(float valueFloat)
	{
		return new zfloat(valueFloat);
	}

	/// <summary>
	/// 执行该运算符重载逻辑。
	/// </summary>
	/// <param name="zf">参数 zf。</param>
	/// <returns>返回方法结果。</returns>
	public static explicit operator float(zfloat zf)
	{
		return zf.value / (float)SCALE_10000;
	}
	#endregion

	#region 解析与格式化
	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <param name="s">参数 s。</param>
	/// <returns>返回方法结果。</returns>
	public static zfloat Parse(string s)
	{
		if (TryParse(s, out zfloat result))
		{
			return result;
		}

		throw new FormatException($"Input string '{s}' was not in a correct format for zfloat.");
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <param name="s">参数 s。</param>
	/// <param name="v">参数 v。</param>
	/// <returns>返回方法结果。</returns>
	public static bool TryParse(string s, out zfloat v)
	{
		if (TryParseRaw(s, out long raw))
		{
			v = FromRaw(raw);
			return true;
		}

		v = Zero;
		return false;
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <returns>返回方法结果。</returns>
	public override string ToString()
	{
		return ToString("F4", CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <param name="format">参数 format。</param>
	/// <returns>返回方法结果。</returns>
	public string ToString(string format)
	{
		return ToString(format, CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <param name="format">参数 format。</param>
	/// <param name="formatProvider">参数 formatProvider。</param>
	/// <returns>返回方法结果。</returns>
	public string ToString(string format, IFormatProvider formatProvider)
	{
		if (string.IsNullOrEmpty(format))
		{
			format = "F4";
		}

		decimal decimalValue = value / (decimal)SCALE_10000;
		return decimalValue.ToString(format, formatProvider ?? CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// 尝试将字符串解析为 raw 值（单位 1/10000）。
	/// 支持：整数、小数、可选前导符号、前后空白。
	/// 规则：小数位超过 4 位时按截断处理；超出 long 边界时返回 false。
	/// </summary>
	private static bool TryParseRaw(string s, out long raw)
	{
		raw = 0;
		if (string.IsNullOrWhiteSpace(s))
		{
			return false;
		}

		string text = s.Trim();
		int sign = 1;
		int index = 0;
		if (text[0] == '+')
		{
			index = 1;
		}
		else if (text[0] == '-')
		{
			sign = -1;
			index = 1;
		}

		if (index >= text.Length)
		{
			return false;
		}

		string number = text.Substring(index);
		int dotIndex = number.IndexOf('.');
		if (dotIndex >= 0 && number.IndexOf('.', dotIndex + 1) >= 0)
		{
			return false;
		}

		string intPartText;
		string fracPartText;
		if (dotIndex < 0)
		{
			intPartText = number;
			fracPartText = string.Empty;
		}
		else
		{
			intPartText = number.Substring(0, dotIndex);
			fracPartText = number.Substring(dotIndex + 1);
		}

		if (intPartText.Length == 0 && fracPartText.Length == 0)
		{
			return false;
		}

		long intPart = 0;
		if (intPartText.Length > 0 && !long.TryParse(intPartText, NumberStyles.None, CultureInfo.InvariantCulture, out intPart))
		{
			return false;
		}

		long fracPart = 0;
		if (fracPartText.Length > 0)
		{
			for (int i = 0; i < fracPartText.Length; ++i)
			{
				if (!char.IsDigit(fracPartText[i]))
				{
					return false;
				}
			}

			string frac4 = fracPartText.Length > 4 ? fracPartText.Substring(0, 4) : fracPartText.PadRight(4, '0');
			if (!long.TryParse(frac4, NumberStyles.None, CultureInfo.InvariantCulture, out fracPart))
			{
				return false;
			}
		}

		try
		{
			long scaled = checked(intPart * SCALE_10000 + fracPart);
			raw = sign > 0 ? scaled : -scaled;
			return true;
		}
		catch (OverflowException)
		{
			return false;
		}
	}
	#endregion

	#region 安全算术内部实现
	private static bool TryAddRaw(long left, long right, out long raw)
	{
		try
		{
			raw = checked(left + right);
			return true;
		}
		catch (OverflowException)
		{
			raw = 0;
			return false;
		}
	}

	private static ulong AbsRawToUInt64(long raw)
	{
		if (raw >= 0)
		{
			return (ulong)raw;
		}

		if (raw == long.MinValue)
		{
			return 1UL << 63;
		}

		return (ulong)(-raw);
	}

	private static bool TrySubRaw(long left, long right, out long raw)
	{
		try
		{
			raw = checked(left - right);
			return true;
		}
		catch (OverflowException)
		{
			raw = 0;
			return false;
		}
	}

	private static bool TryMulRaw(long left, long right, out long raw)
	{
		try
		{
			long mul = checked(left * right);
			raw = checked(mul / SCALE_10000);
			return true;
		}
		catch (OverflowException)
		{
			raw = 0;
			return false;
		}
	}

	private static bool TryDivRaw(long left, long right, out long raw)
	{
		if (right == 0)
		{
			raw = 0;
			return false;
		}

		try
		{
			long num = checked(left * SCALE_10000);
			raw = checked(num / right);
			return true;
		}
		catch (OverflowException)
		{
			raw = 0;
			return false;
		}
	}
	#endregion

	#region 比较与相等
	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <param name="other">参数 other。</param>
	/// <returns>返回方法结果。</returns>
	public int CompareTo(zfloat other)
	{
		return value.CompareTo(other.value);
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <param name="obj">参数 obj。</param>
	/// <returns>返回方法结果。</returns>
	public int CompareTo(object obj)
	{
		if (obj == null)
		{
			return 1;
		}

		if (obj is zfloat other)
		{
			return CompareTo(other);
		}

		throw new ArgumentException("Object must be of type zfloat.", nameof(obj));
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <param name="other">参数 other。</param>
	/// <returns>返回方法结果。</returns>
	public bool Equals(zfloat other)
	{
		return value == other.value;
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <param name="obj">参数 obj。</param>
	/// <returns>返回方法结果。</returns>
	public override bool Equals(object obj)
	{
		return obj is zfloat other && Equals(other);
	}

	/// <summary>
	/// 执行该方法的核心功能。
	/// </summary>
	/// <returns>返回方法结果。</returns>
	public override int GetHashCode()
	{
		return value.GetHashCode();
	}
	#endregion

	#region 序列化
	/// <summary>
	/// ISerializable 序列化入口，仅序列化 raw 值。
	/// </summary>
	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		info.AddValue("value", value);
	}

	/// <summary>
	/// ISerializable 反序列化构造函数，从 raw 值恢复。
	/// </summary>
	public zfloat(SerializationInfo info, StreamingContext context)
	{
		value = info.GetInt64("value");
	}
	#endregion
}

