using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace zUnity
{
	/// <summary>
	/// 四维向量结构体，用于确定性计算。
	/// 包含x、y、z、w四个分量，每个分量都是zfloat类型的定点数（精度：0.0001）。
	/// 提供了向量运算的各种方法，如加减乘除、点积、归一化等，
	/// 并使用优化技巧（.value直接操作）确保在ECS系统的高频调用中性能优异。
	/// </summary>
	[Serializable]
	public struct zVector4 : ISerializable, IEquatable<zVector4>
	{
		#region 常量定义

		/// <summary>
		/// 零向量 (0, 0, 0, 0)
		/// </summary>
		public static readonly zVector4 zero = new zVector4((zfloat)0, (zfloat)0, (zfloat)0, (zfloat)0);

		/// <summary>
		/// 单位向量 (1, 1, 1, 1)
		/// </summary>
		public static readonly zVector4 one = new zVector4((zfloat)1, (zfloat)1, (zfloat)1, (zfloat)1);

		/// <summary>
		/// 前方向量 (0, 0, 1, 0)
		/// </summary>
		public static readonly zVector4 forward = new zVector4((zfloat)0, (zfloat)0, (zfloat)1, (zfloat)0);

		/// <summary>
		/// 后方向量 (0, 0, -1, 0)
		/// </summary>
		public static readonly zVector4 back = new zVector4((zfloat)0, (zfloat)0, (zfloat)(-1), (zfloat)0);

		/// <summary>
		/// 上方向量 (0, 1, 0, 0)
		/// </summary>
		public static readonly zVector4 up = new zVector4((zfloat)0, (zfloat)1, (zfloat)0, (zfloat)0);

		/// <summary>
		/// 下方向量 (0, -1, 0, 0)
		/// </summary>
		public static readonly zVector4 down = new zVector4((zfloat)0, (zfloat)(-1), (zfloat)0, (zfloat)0);

		/// <summary>
		/// 左方向量 (-1, 0, 0, 0)
		/// </summary>
		public static readonly zVector4 left = new zVector4((zfloat)(-1), (zfloat)0, (zfloat)0, (zfloat)0);

		/// <summary>
		/// 右方向量 (1, 0, 0, 0)
		/// </summary>
		public static readonly zVector4 right = new zVector4((zfloat)1, (zfloat)0, (zfloat)0, (zfloat)0);

		/// <summary>
		/// 历史兼容哨兵值。新代码应优先使用显式状态而非该常量。
		/// </summary>
		public static readonly zVector4 NULL = new zVector4((zfloat)(-999999), (zfloat)(-999999), (zfloat)(-999999), (zfloat)(-999999));

		#endregion

		#region 构造函数

		/// <summary>
		/// 使用zfloat分量初始化四维向量。
		/// </summary>
		/// <param name="x">X分量</param>
		/// <param name="y">Y分量</param>
		/// <param name="z">Z分量</param>
		/// <param name="w">W分量</param>
		public zVector4(zfloat x, zfloat y, zfloat z, zfloat w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		/// <summary>
		/// 使用整数分量初始化四维向量。
		/// </summary>
		/// <param name="x">X分量</param>
		/// <param name="y">Y分量</param>
		/// <param name="z">Z分量</param>
		/// <param name="w">W分量</param>
		public zVector4(int x, int y, int z, int w)
		{
			this.x = (zfloat)x;
			this.y = (zfloat)y;
			this.z = (zfloat)z;
			this.w = (zfloat)w;
		}

		/// <summary>
		/// 使用float分量初始化四维向量。
		/// </summary>
		/// <param name="x">X分量</param>
		/// <param name="y">Y分量</param>
		/// <param name="z">Z分量</param>
		/// <param name="w">W分量</param>
		public zVector4(float x, float y, float z, float w)
		{
			this.x = (zfloat)x;
			this.y = (zfloat)y;
			this.z = (zfloat)z;
			this.w = (zfloat)w;
		}

		/// <summary>
		/// 复制构造函数。
		/// </summary>
		/// <param name="vec">要复制的向量</param>
		public zVector4(zVector4 vec)
		{
			this.x = vec.x;
			this.y = vec.y;
			this.z = vec.z;
			this.w = vec.w;
		}

		/// <summary>
		/// 反序列化构造函数。
		/// </summary>
		/// <param name="info">序列化信息</param>
		/// <param name="context">流上下文</param>
		public zVector4(SerializationInfo info, StreamingContext context)
		{
			x = zfloat.FromRaw(info.GetInt64("x"));
			y = zfloat.FromRaw(info.GetInt64("y"));
			z = zfloat.FromRaw(info.GetInt64("z"));
			w = zfloat.FromRaw(info.GetInt64("w"));
		}

		#endregion

		#region 字段

		/// <summary>
		/// X轴坐标分量
		/// </summary>
		[JsonProperty]
		public zfloat x;

		/// <summary>
		/// Y轴坐标分量
		/// </summary>
		[JsonProperty]
		public zfloat y;

		/// <summary>
		/// Z轴坐标分量
		/// </summary>
		[JsonProperty]
		public zfloat z;

		/// <summary>
		/// W轴坐标分量
		/// </summary>
		[JsonProperty]
		public zfloat w;

		#endregion

		#region 属性

		/// <summary>
		/// 判断向量是否为零向量。
		/// </summary>
		/// <returns>如果向量的x、y、z、w都为0，返回true；否则返回false</returns>
		public bool IsZero()
		{
			return this.x.value == 0 && this.y.value == 0 && this.z.value == 0 && this.w.value == 0;
		}

		/// <summary>
		/// 索引器，通过索引访问向量分量。
		/// </summary>
		/// <param name="index">索引：0=x, 1=y, 2=z, 3=w</param>
		/// <returns>对应索引的分量值</returns>
		/// <exception cref="IndexOutOfRangeException">当索引不在0-3范围内时抛出</exception>
		public zfloat this[int index]
		{
			get
			{
				switch (index)
				{
					case 0: return x;
					case 1: return y;
					case 2: return z;
					case 3: return w;
					default: throw new IndexOutOfRangeException("zVector4 only contains x, y, z, w, so the index must be 0, 1, 2 or 3!");
				}
			}
			set
			{
				switch (index)
				{
					case 0: x = value; break;
					case 1: y = value; break;
					case 2: z = value; break;
					case 3: w = value; break;
					default: throw new IndexOutOfRangeException("zVector4 only contains x, y, z, w, so the index must be 0, 1, 2 or 3!");
				}
			}
		}

		/// <summary>
		/// 获取向量的长度平方（无需计算平方根，适用于比较大小）。
		/// 性能特征：O(1)，无浮点数平方根运算。
		/// </summary>
		/// <remarks>
		/// 相比magnitude属性，sqrMagnitude避免了平方根计算，在性能敏感的场景（如距离比较）中应优先使用。
		/// </remarks>
		public zfloat sqrMagnitude
		{
			get { return zVector4.SqrMagnitude(ref this); }
		}

		/// <summary>
		/// 获取向量的长度（模）。
		/// 性能特征：O(sqrt)，涉及平方根计算。
		/// </summary>
		/// <remarks>
		/// 精度限制：zfloat采用定点数(4位小数精度)，在极大或极小值下可能存在精度误差。
		/// 如仅需比较大小，建议使用sqrMagnitude属性。
		/// </remarks>
		public zfloat magnitude
		{
			get { return zVector4.Magnitude(ref this); }
		}

		/// <summary>
		/// 获取向量的归一化向量（单位向量）。
		/// 性能特征：O(sqrt)，每次调用都会重新计算。
		/// </summary>
		/// <remarks>
		/// 对于零向量，返回zero常量。
		/// 如需频繁使用归一化向量，考虑缓存结果而非重复调用此属性。
		/// 定点数精度限制下，非常小的向量可能归一化结果不稳定。
		/// </remarks>
		[JsonIgnore]
		public zVector4 normalized
		{
			get { return zVector4.Normalize(ref this); }
		}

		#endregion

		#region 实例方法

		/// <summary>
		/// 将当前向量原地归一化。
		/// </summary>
		/// <remarks>
		/// 对于零向量，设置为zero常量。
		/// </remarks>
		public void Normalize()
		{
			long num = (x.value * x.value + y.value * y.value + z.value * z.value + w.value * w.value) / zfloat.SCALE_10000;
			num = zMathf.SqrtScale(num);

			if (num > 0)
			{
				x.value = x.value * zfloat.SCALE_10000 / num;
				y.value = y.value * zfloat.SCALE_10000 / num;
				z.value = z.value * zfloat.SCALE_10000 / num;
				w.value = w.value * zfloat.SCALE_10000 / num;
			}
			else
			{
				x.value = 0;
				y.value = 0;
				z.value = 0;
				w.value = 0;
			}
		}

		/// <summary>
		/// 将当前向量按照scale向量进行元素级乘法（原地修改）。
		/// 性能特征：O(1)。
		/// </summary>
		/// <param name="scale">缩放因子向量</param>
		/// <remarks>
		/// 此方法执行元素级乘法：result.x = this.x * scale.x，result.y = this.y * scale.y，以此类推。
		/// 由于zfloat为定点数，乘法后需要除以SCALE_10000来维持精度。
		/// </remarks>
		public void Scale(zVector4 scale)
		{
			x.value = x.value * scale.x.value / zfloat.SCALE_10000;
			y.value = y.value * scale.y.value / zfloat.SCALE_10000;
			z.value = z.value * scale.z.value / zfloat.SCALE_10000;
			w.value = w.value * scale.w.value / zfloat.SCALE_10000;
		}

		/// <summary>
		/// 向当前向量添加另一个向量（原地修改）。
		/// 性能特征：O(1)，使用.value直接操作优化性能。
		/// </summary>
		/// <param name="vec">要添加的向量</param>
		public void Add(ref zVector4 vec)
		{
			x.value += vec.x.value;
			y.value += vec.y.value;
			z.value += vec.z.value;
			w.value += vec.w.value;
		}

		/// <summary>
		/// 从当前向量减去另一个向量（原地修改）。
		/// 性能特征：O(1)，使用.value直接操作优化性能。
		/// </summary>
		/// <param name="vec">要减去的向量</param>
		public void Sub(ref zVector4 vec)
		{
			x.value -= vec.x.value;
			y.value -= vec.y.value;
			z.value -= vec.z.value;
			w.value -= vec.w.value;
		}

		/// <summary>
		/// 将当前向量与另一个向量进行元素级乘法（原地修改）。
		/// 性能特征：O(1)。
		/// </summary>
		/// <param name="vec">乘法因子向量</param>
		public void Mul(ref zVector4 vec)
		{
			x.value = x.value * vec.x.value / zfloat.SCALE_10000;
			y.value = y.value * vec.y.value / zfloat.SCALE_10000;
			z.value = z.value * vec.z.value / zfloat.SCALE_10000;
			w.value = w.value * vec.w.value / zfloat.SCALE_10000;
		}

		/// <summary>
		/// 将当前向量与另一个向量进行元素级除法（原地修改）。
		/// 性能特征：O(1)。
		/// </summary>
		/// <param name="vec">除数向量（不能包含0分量，否则结果为无穷大）</param>
		public void Div(ref zVector4 vec)
		{
			x.value = x.value * zfloat.SCALE_10000 / vec.x.value;
			y.value = y.value * zfloat.SCALE_10000 / vec.y.value;
			z.value = z.value * zfloat.SCALE_10000 / vec.z.value;
			w.value = w.value * zfloat.SCALE_10000 / vec.w.value;
		}

		/// <summary>
		/// 设置向量的所有分量。
		/// </summary>
		/// <param name="new_x">新的X分量</param>
		/// <param name="new_y">新的Y分量</param>
		/// <param name="new_z">新的Z分量</param>
		/// <param name="new_w">新的W分量</param>
		public void Set(zfloat new_x, zfloat new_y, zfloat new_z, zfloat new_w)
		{
			this.x.value = new_x.value;
			this.y.value = new_y.value;
			this.z.value = new_z.value;
			this.w.value = new_w.value;
		}

		/// <summary>
		/// 根据已知向量长度获取归一化向量（性能优化版本，避免重复计算长度）。
		/// 性能特征：O(1)，不需要计算平方根。
		/// </summary>
		/// <param name="magnitude">向量的预计算长度</param>
		/// <returns>归一化后的单位向量，或zero如果magnitude为0</returns>
		/// <remarks>
		/// 此方法用于已知向量长度的场景，避免重复计算。
		/// 如果magnitude参数不准确，会导致结果错误，请确保传入正确的长度值。
		/// </remarks>
		public zVector4 GetNormalizedForMagnitude(zfloat magnitude)
		{
			zVector4 vec = this;

			if (magnitude.value > 0)
			{
				vec.x.value = vec.x.value * zfloat.SCALE_10000 / magnitude.value;
				vec.y.value = vec.y.value * zfloat.SCALE_10000 / magnitude.value;
				vec.z.value = vec.z.value * zfloat.SCALE_10000 / magnitude.value;
				vec.w.value = vec.w.value * zfloat.SCALE_10000 / magnitude.value;
				return vec;
			}
			else
			{
				return zVector4.zero;
			}
		}

		#endregion

		#region 静态方法

		/// <summary>
		/// 获取向量的归一化向量（返回新的zVector4，不修改原向量）。
		/// 性能特征：O(sqrt)。
		/// </summary>
		/// <param name="vec">要归一化的向量（ref参数用于性能优化，不会被修改）</param>
		/// <returns>归一化后的单位向量，或zero如果原向量为零</returns>
		public static zVector4 Normalize(ref zVector4 vec)
		{
			zVector4 result;
			long num = (vec.x.value * vec.x.value + vec.y.value * vec.y.value + vec.z.value * vec.z.value + vec.w.value * vec.w.value) / zfloat.SCALE_10000;
			num = zMathf.SqrtScale(num);

			if (num > 0)
			{
				result.x.value = vec.x.value * zfloat.SCALE_10000 / num;
				result.y.value = vec.y.value * zfloat.SCALE_10000 / num;
				result.z.value = vec.z.value * zfloat.SCALE_10000 / num;
				result.w.value = vec.w.value * zfloat.SCALE_10000 / num;
				return result;
			}
			else
			{
				return zVector4.zero;
			}
		}

		/// <summary>
		/// 计算向量的长度（模）。
		/// 性能特征：O(sqrt)，涉及平方根计算。
		/// </summary>
		/// <param name="a">要计算长度的向量</param>
		/// <returns>向量的长度</returns>
		/// <remarks>
		/// 定点数精度：zfloat采用4位小数精度(0.0001)。
		/// 在极端数值下可能存在精度误差。如仅需比较，推荐使用SqrMagnitude。
		/// </remarks>
		public static zfloat Magnitude(ref zVector4 a)
		{
			long sq = (a.x.value * a.x.value + a.y.value * a.y.value + a.z.value * a.z.value + a.w.value * a.w.value) / zfloat.SCALE_10000;
			zfloat result;
			result.value = zMathf.SqrtScale(sq);
			return result;
		}

		/// <summary>
		/// 计算向量的长度平方（无需平方根运算）。
		/// 性能特征：O(1)，不涉及平方根。
		/// </summary>
		/// <param name="a">要计算长度平方的向量</param>
		/// <returns>向量长度的平方</returns>
		/// <remarks>
		/// 在需要比较向量大小时，使用此方法而非Magnitude避免平方根计算。
		/// 示例：if (sqrMagnitude > maxLength * maxLength) { ... }
		/// </remarks>
		public static zfloat SqrMagnitude(ref zVector4 a)
		{
			zfloat result;
			result.value = (a.x.value * a.x.value + a.y.value * a.y.value + a.z.value * a.z.value + a.w.value * a.w.value) / zfloat.SCALE_10000;
			return result;
		}

		/// <summary>
		/// 线性插值。
		/// </summary>
		/// <param name="from">起始向量</param>
		/// <param name="to">目标向量</param>
		/// <param name="t">插值参数（0到1之间）</param>
		/// <returns>插值结果向量</returns>
		/// <remarks>
		/// 当t=0时返回from，当t=1时返回to。
		/// t值会被限制在[0,1]范围内。
		/// </remarks>
		public static zVector4 Lerp(zVector4 from, zVector4 to, zfloat t)
		{
			t = zMathf.Clamp01(t);
			return new zVector4(from.x + (to.x - from.x) * t, from.y + (to.y - from.y) * t, from.z + (to.z - from.z) * t, from.w + (to.w - from.w) * t);
		}

		/// <summary>
		/// 逐步移动当前向量朝向目标，每次移动距离不超过maxDistanceDelta。
		/// 性能特征：O(sqrt)。
		/// </summary>
		/// <param name="current">当前位置向量</param>
		/// <param name="target">目标位置向量</param>
		/// <param name="maxDistanceDelta">单次移动的最大距离</param>
		/// <returns>移动后的向量（最多移动maxDistanceDelta距离）</returns>
		/// <remarks>
		/// 类似于线性插值，但限制了移动速度。
		/// 若maxDistanceDelta为负值，则向反方向移动。
		/// </remarks>
		public static zVector4 MoveTowards(zVector4 current, zVector4 target, zfloat maxDistanceDelta)
		{
			zVector4 a = target - current;
			zfloat magnitude = a.magnitude;
			if (magnitude <= maxDistanceDelta || magnitude == zfloat.Zero)
			{
				return target;
			}
			return current + a / magnitude * maxDistanceDelta;
		}

		/// <summary>
		/// 将向量a和向量b进行元素级乘法。
		/// 性能特征：O(1)。
		/// </summary>
		/// <param name="a">第一个向量</param>
		/// <param name="b">第二个向量（缩放因子）</param>
		/// <returns>元素级相乘的结果向量</returns>
		/// <remarks>
		/// 示例：Scale(new zVector4(2, 3, 4, 5), new zVector4(1.5, 2, 0.5, 1)) = (3, 6, 2, 5)
		/// </remarks>
		public static zVector4 Scale(zVector4 a, zVector4 b)
		{
			zVector4 result;
			result.x.value = a.x.value * b.x.value / zfloat.SCALE_10000;
			result.y.value = a.y.value * b.y.value / zfloat.SCALE_10000;
			result.z.value = a.z.value * b.z.value / zfloat.SCALE_10000;
			result.w.value = a.w.value * b.w.value / zfloat.SCALE_10000;
			return result;
		}

		/// <summary>
		/// 计算两个向量的点积（数量积）。
		/// 性能特征：O(1)。
		/// </summary>
		/// <param name="lhs">左操作向量</param>
		/// <param name="rhs">右操作向量</param>
		/// <returns>两个向量的点积</returns>
		/// <remarks>
		/// 点积 = |A| * |B| * cos(θ)
		/// 用途：判断两向量的夹角（点积>0为锐角，=0为直角，<0为钝角）、投影长度计算等。
		/// </remarks>
		public static zfloat Dot(ref zVector4 lhs, ref zVector4 rhs)
		{
			zfloat result;
			result.value = (lhs.x.value * rhs.x.value + lhs.y.value * rhs.y.value + lhs.z.value * rhs.z.value + lhs.w.value * rhs.w.value) / zfloat.SCALE_10000;
			return result;
		}

		/// <summary>
		/// 计算两个向量的点积（数量积）- 非ref参数版本。
		/// 性能特征：O(1)。
		/// </summary>
		/// <param name="lhs">左操作向量</param>
		/// <param name="rhs">右操作向量</param>
		/// <returns>两个向量的点积</returns>
		public static zfloat Dot(zVector4 lhs, zVector4 rhs)
		{
			return Dot(ref lhs, ref rhs);
		}

		/// <summary>
		/// 将向量投影到另一个向量上。
		/// </summary>
		/// <param name="vec">要投影的向量</param>
		/// <param name="onNormal">投影方向向量</param>
		/// <returns>投影结果向量</returns>
		/// <remarks>
		/// 如果onNormal为零向量，返回zero。
		/// </remarks>
		public static zVector4 Project(zVector4 vec, zVector4 onNormal)
		{
			zfloat num = zVector4.Dot(onNormal, onNormal);
			if (num == zfloat.Zero)
			{
				return zVector4.zero;
			}
			return onNormal * zVector4.Dot(vec, onNormal) / num;
		}

		/// <summary>
		/// 计算两个向量之间的距离。
		/// </summary>
		/// <param name="a">第一个向量</param>
		/// <param name="b">第二个向量</param>
		/// <returns>两个向量之间的距离</returns>
		public static zfloat Distance(zVector4 a, zVector4 b)
		{
			return (a - b).magnitude;
		}

		/// <summary>
		/// 计算两个向量之间距离的平方。
		/// </summary>
		/// <param name="a">第一个向量</param>
		/// <param name="b">第二个向量</param>
		/// <returns>两个向量之间距离的平方</returns>
		/// <remarks>
		/// 在需要比较距离大小时，使用此方法而非Distance避免平方根计算。
		/// </remarks>
		public static zfloat SqrDistance(zVector4 a, zVector4 b)
		{
			zVector4 diff = a - b;
			return diff.sqrMagnitude;
		}

		/// <summary>
		/// 限制向量长度，如果长度超过maxLength则截取，否则原样返回。
		/// </summary>
		/// <param name="vector">要限制的向量</param>
		/// <param name="maxLength">最大长度</param>
		/// <returns>长度不超过maxLength的向量</returns>
		public static zVector4 ClampMagnitude(zVector4 vector, zfloat maxLength)
		{
			zfloat sqrMag = vector.sqrMagnitude;
			zfloat maxSqr = maxLength * maxLength;

			if (sqrMag <= maxSqr)
			{
				return vector;
			}

			zfloat mag;
			mag.value = zMathf.SqrtScale(sqrMag.value);

			zVector4 result;
			result.x.value = vector.x.value * maxLength.value / mag.value;
			result.y.value = vector.y.value * maxLength.value / mag.value;
			result.z.value = vector.z.value * maxLength.value / mag.value;
			result.w.value = vector.w.value * maxLength.value / mag.value;
			return result;
		}

		/// <summary>
		/// 获取两个向量按分量取最小值的结果。
		/// </summary>
		/// <param name="lhs">第一个向量</param>
		/// <param name="rhs">第二个向量</param>
		/// <returns>每个分量都是两个向量对应分量的最小值</returns>
		public static zVector4 Min(zVector4 lhs, zVector4 rhs)
		{
			return new zVector4(zMathf.Min(lhs.x, rhs.x), zMathf.Min(lhs.y, rhs.y), zMathf.Min(lhs.z, rhs.z), zMathf.Min(lhs.w, rhs.w));
		}

		/// <summary>
		/// 获取两个向量按分量取最大值的结果。
		/// </summary>
		/// <param name="lhs">第一个向量</param>
		/// <param name="rhs">第二个向量</param>
		/// <returns>每个分量都是两个向量对应分量的最大值</returns>
		public static zVector4 Max(zVector4 lhs, zVector4 rhs)
		{
			return new zVector4(zMathf.Max(lhs.x, rhs.x), zMathf.Max(lhs.y, rhs.y), zMathf.Max(lhs.z, rhs.z), zMathf.Max(lhs.w, rhs.w));
		}

		/// <summary>
		/// 获取两个向量夹角（单位：度，范围 [0, 180]）。
		/// </summary>
		/// <param name="from">起始向量</param>
		/// <param name="to">目标向量</param>
		/// <returns>两个向量之间的夹角（度）</returns>
		/// <remarks>
		/// 对于零向量，返回0。
		/// </remarks>
		public static zfloat Angle(zVector4 from, zVector4 to)
		{
			if (from.IsZero() || to.IsZero())
			{
				return zfloat.Zero;
			}

			from.Normalize();
			to.Normalize();
			return zMathf.Acos(zMathf.Clamp(zVector4.Dot(ref from, ref to), zfloat.NegativeOne, zfloat.One)) * zMathf.Rad2Deg;
		}

		#endregion

		#region 运算符重载

		#region 加法
		/// <summary>
		/// 向量加法：(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w)。
		/// 性能特征：O(1)，使用.value直接操作。
		/// </summary>
		public static zVector4 operator +(zVector4 lhs, zVector4 rhs)
		{
			zVector4 vec;
			vec.x.value = lhs.x.value + rhs.x.value;
			vec.y.value = lhs.y.value + rhs.y.value;
			vec.z.value = lhs.z.value + rhs.z.value;
			vec.w.value = lhs.w.value + rhs.w.value;
			return vec;
		}

		/// <summary>
		/// 整数与向量加法。
		/// </summary>
		public static zVector4 operator +(int lhs, zVector4 rhs)
		{
			zVector4 vec;
			vec.x.value = lhs * zfloat.SCALE_10000 + rhs.x.value;
			vec.y.value = lhs * zfloat.SCALE_10000 + rhs.y.value;
			vec.z.value = lhs * zfloat.SCALE_10000 + rhs.z.value;
			vec.w.value = lhs * zfloat.SCALE_10000 + rhs.w.value;
			return vec;
		}

		/// <summary>
		/// 向量与整数加法。
		/// </summary>
		public static zVector4 operator +(zVector4 lhs, int rhs)
		{
			zVector4 vec;
			vec.x.value = lhs.x.value + rhs * zfloat.SCALE_10000;
			vec.y.value = lhs.y.value + rhs * zfloat.SCALE_10000;
			vec.z.value = lhs.z.value + rhs * zfloat.SCALE_10000;
			vec.w.value = lhs.w.value + rhs * zfloat.SCALE_10000;
			return vec;
		}

		/// <summary>
		/// zfloat与向量加法。
		/// </summary>
		public static zVector4 operator +(zfloat lhs, zVector4 rhs)
		{
			zVector4 vec;
			vec.x.value = lhs.value + rhs.x.value;
			vec.y.value = lhs.value + rhs.y.value;
			vec.z.value = lhs.value + rhs.z.value;
			vec.w.value = lhs.value + rhs.w.value;
			return vec;
		}

		/// <summary>
		/// 向量与zfloat加法。
		/// </summary>
		public static zVector4 operator +(zVector4 lhs, zfloat rhs)
		{
			zVector4 vec;
			vec.x.value = lhs.x.value + rhs.value;
			vec.y.value = lhs.y.value + rhs.value;
			vec.z.value = lhs.z.value + rhs.value;
			vec.w.value = lhs.w.value + rhs.value;
			return vec;
		}
		#endregion

		#region 减法
		/// <summary>
		/// 向量减法：(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w)。
		/// 性能特征：O(1)，使用.value直接操作。
		/// </summary>
		public static zVector4 operator -(zVector4 lhs, zVector4 rhs)
		{
			zVector4 vec;
			vec.x.value = lhs.x.value - rhs.x.value;
			vec.y.value = lhs.y.value - rhs.y.value;
			vec.z.value = lhs.z.value - rhs.z.value;
			vec.w.value = lhs.w.value - rhs.w.value;
			return vec;
		}

		/// <summary>
		/// 整数与向量减法。
		/// </summary>
		public static zVector4 operator -(int lhs, zVector4 rhs)
		{
			zVector4 vec;
			vec.x.value = lhs * zfloat.SCALE_10000 - rhs.x.value;
			vec.y.value = lhs * zfloat.SCALE_10000 - rhs.y.value;
			vec.z.value = lhs * zfloat.SCALE_10000 - rhs.z.value;
			vec.w.value = lhs * zfloat.SCALE_10000 - rhs.w.value;
			return vec;
		}

		/// <summary>
		/// 向量与整数减法。
		/// </summary>
		public static zVector4 operator -(zVector4 lhs, int rhs)
		{
			zVector4 vec;
			vec.x.value = lhs.x.value - rhs * zfloat.SCALE_10000;
			vec.y.value = lhs.y.value - rhs * zfloat.SCALE_10000;
			vec.z.value = lhs.z.value - rhs * zfloat.SCALE_10000;
			vec.w.value = lhs.w.value - rhs * zfloat.SCALE_10000;
			return vec;
		}

		/// <summary>
		/// zfloat与向量减法。
		/// </summary>
		public static zVector4 operator -(zfloat lhs, zVector4 rhs)
		{
			zVector4 vec;
			vec.x.value = lhs.value - rhs.x.value;
			vec.y.value = lhs.value - rhs.y.value;
			vec.z.value = lhs.value - rhs.z.value;
			vec.w.value = lhs.value - rhs.w.value;
			return vec;
		}

		/// <summary>
		/// 向量与zfloat减法。
		/// </summary>
		public static zVector4 operator -(zVector4 lhs, zfloat rhs)
		{
			zVector4 vec;
			vec.x.value = lhs.x.value - rhs.value;
			vec.y.value = lhs.y.value - rhs.value;
			vec.z.value = lhs.z.value - rhs.value;
			vec.w.value = lhs.w.value - rhs.value;
			return vec;
		}
		#endregion

		#region 负号
		/// <summary>
		/// 向量取反：(-x, -y, -z, -w)。
		/// 性能特征：O(1)。
		/// </summary>
		public static zVector4 operator -(zVector4 a)
		{
			zVector4 vec;
			vec.x.value = -a.x.value;
			vec.y.value = -a.y.value;
			vec.z.value = -a.z.value;
			vec.w.value = -a.w.value;
			return vec;
		}
		#endregion

		#region 乘法
		/// <summary>
		/// 整数与向量乘法：(lhs * rhs.x, lhs * rhs.y, lhs * rhs.z, lhs * rhs.w)。
		/// 性能特征：O(1)，使用.value直接操作。
		/// </summary>
		public static zVector4 operator *(int lhs, zVector4 rhs)
		{
			zVector4 vec;
			vec.x.value = lhs * rhs.x.value;
			vec.y.value = lhs * rhs.y.value;
			vec.z.value = lhs * rhs.z.value;
			vec.w.value = lhs * rhs.w.value;
			return vec;
		}

		/// <summary>
		/// 向量与整数乘法。
		/// </summary>
		public static zVector4 operator *(zVector4 lhs, int rhs)
		{
			zVector4 vec;
			vec.x.value = lhs.x.value * rhs;
			vec.y.value = lhs.y.value * rhs;
			vec.z.value = lhs.z.value * rhs;
			vec.w.value = lhs.w.value * rhs;
			return vec;
		}

		/// <summary>
		/// zfloat与向量乘法（需要除以SCALE_10000）。
		/// 性能特征：O(1)。
		/// </summary>
		public static zVector4 operator *(zfloat lhs, zVector4 rhs)
		{
			zVector4 vec;
			vec.x.value = lhs.value * rhs.x.value / zfloat.SCALE_10000;
			vec.y.value = lhs.value * rhs.y.value / zfloat.SCALE_10000;
			vec.z.value = lhs.value * rhs.z.value / zfloat.SCALE_10000;
			vec.w.value = lhs.value * rhs.w.value / zfloat.SCALE_10000;
			return vec;
		}

		/// <summary>
		/// 向量与zfloat乘法。
		/// </summary>
		public static zVector4 operator *(zVector4 lhs, zfloat rhs)
		{
			zVector4 vec;
			vec.x.value = lhs.x.value * rhs.value / zfloat.SCALE_10000;
			vec.y.value = lhs.y.value * rhs.value / zfloat.SCALE_10000;
			vec.z.value = lhs.z.value * rhs.value / zfloat.SCALE_10000;
			vec.w.value = lhs.w.value * rhs.value / zfloat.SCALE_10000;
			return vec;
		}
		#endregion

		#region 除法
		/// <summary>
		/// 向量除以整数。
		/// 性能特征：O(1)，使用.value直接操作。
		/// </summary>
		public static zVector4 operator /(zVector4 lhs, int rhs)
		{
			zVector4 vec;
			vec.x.value = lhs.x.value / rhs;
			vec.y.value = lhs.y.value / rhs;
			vec.z.value = lhs.z.value / rhs;
			vec.w.value = lhs.w.value / rhs;
			return vec;
		}

		/// <summary>
		/// 向量除以zfloat（需要乘以SCALE_10000）。
		/// 性能特征：O(1)。
		/// </summary>
		public static zVector4 operator /(zVector4 lhs, zfloat rhs)
		{
			zVector4 vec;
			vec.x.value = lhs.x.value * zfloat.SCALE_10000 / rhs.value;
			vec.y.value = lhs.y.value * zfloat.SCALE_10000 / rhs.value;
			vec.z.value = lhs.z.value * zfloat.SCALE_10000 / rhs.value;
			vec.w.value = lhs.w.value * zfloat.SCALE_10000 / rhs.value;
			return vec;
		}
		#endregion

		#region 相等判定
		/// <summary>
		/// 判断两个向量是否不相等。
		/// 性能特征：O(1)，使用.value直接比较。
		/// </summary>
		public static bool operator !=(zVector4 lhs, zVector4 rhs)
		{
			return (lhs.x.value != rhs.x.value) || (lhs.y.value != rhs.y.value) || (lhs.z.value != rhs.z.value) || (lhs.w.value != rhs.w.value);
		}

		/// <summary>
		/// 判断两个向量是否相等。
		/// 性能特征：O(1)，使用.value直接比较。
		/// </summary>
		public static bool operator ==(zVector4 lhs, zVector4 rhs)
		{
			return (lhs.x.value == rhs.x.value) && (lhs.y.value == rhs.y.value) && (lhs.z.value == rhs.z.value) && (lhs.w.value == rhs.w.value);
		}
		#endregion

		#region 类型转换
		/// <summary>
		/// 从zVector3隐式转换为zVector4（W分量设为0）。
		/// </summary>
		public static implicit operator zVector4(zVector3 v)
		{
			zVector4 result;
			result.x = v.x;
			result.y = v.y;
			result.z = v.z;
			result.w = zfloat.Zero;
			return result;
		}

		/// <summary>
		/// zVector4隐式转换为zVector3（丢弃W分量）。
		/// </summary>
		public static implicit operator zVector3(zVector4 v)
		{
			return new zVector3(v.x, v.y, v.z);
		}

		/// <summary>
		/// 从zVector2隐式转换为zVector4（Z和W分量设为0）。
		/// </summary>
		public static implicit operator zVector4(zVector2 v)
		{
			zVector4 result;
			result.x = v.x;
			result.y = v.y;
			result.z = zfloat.Zero;
			result.w = zfloat.Zero;
			return result;
		}

		/// <summary>
		/// zVector4隐式转换为zVector2（丢弃Z和W分量）。
		/// </summary>
		public static implicit operator zVector2(zVector4 v)
		{
			return new zVector2(v.x, v.y);
		}
		#endregion

		#endregion

		#region Object方法

		/// <summary>
		/// 获取向量的字符串表示形式。
		/// </summary>
		/// <returns>格式为"(x , y , z , w)"的字符串</returns>
		public override string ToString()
		{
			return "(" + x + " , " + y + " , " + z + " , " + w + ")";
		}

		/// <summary>
		/// 判断是否与另一个对象相等。
		/// </summary>
		/// <param name="obj">要比较的对象</param>
		/// <returns>如果相等返回true，否则返回false</returns>
		public override bool Equals(object obj)
		{
			if (obj is zVector4 other)
			{
				return Equals(other);
			}
			return false;
		}

		/// <summary>
		/// 判断是否与另一个zVector4相等。
		/// </summary>
		/// <param name="other">要比较的zVector4</param>
		/// <returns>如果相等返回true，否则返回false</returns>
		public bool Equals(zVector4 other)
		{
			return x.value == other.x.value && y.value == other.y.value && z.value == other.z.value && w.value == other.w.value;
		}

		/// <summary>
		/// 获取哈希码。
		/// </summary>
		/// <returns>基于x、y、z、w的raw值计算的哈希码</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(x.value, y.value, z.value, w.value);
		}

		#endregion

		#region 序列化

		/// <summary>
		/// 实现ISerializable接口的序列化方法。
		/// </summary>
		/// <param name="info">序列化信息</param>
		/// <param name="context">流上下文</param>
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("x", x.value);
			info.AddValue("y", y.value);
			info.AddValue("z", z.value);
			info.AddValue("w", w.value);
		}

		#endregion
	}
}
