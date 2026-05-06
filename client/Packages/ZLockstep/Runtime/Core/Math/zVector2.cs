using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace zUnity
{
	/// <summary>
	/// 二维向量结构体，用于确定性计算。
	/// 包含x、y两个分量，每个分量都是zfloat类型的定点数（精度：0.0001）。
	/// 提供了向量运算的各种方法，如加减乘除、点积、叉积、归一化等，
	/// 并使用优化技巧（.value直接操作）确保在ECS系统的高频调用中性能优异。
	/// </summary>
	[Serializable]
	public struct zVector2 : ISerializable 
	{
		public static readonly zVector2 zero = new zVector2((zfloat)0, (zfloat)0);
		public static readonly zVector2 one = new zVector2((zfloat)1, (zfloat)1);
		public static readonly zVector2 up = new zVector2((zfloat)0, (zfloat)1);
		public static readonly zVector2 down = new zVector2((zfloat)0, (zfloat)(-1));
		public static readonly zVector2 left = new zVector2((zfloat)(-1), (zfloat)0);
		public static readonly zVector2 right = new zVector2((zfloat)1, (zfloat)0);

		public zVector2(zfloat x, zfloat y)
		{
			this.x = x;
			this.y = y;
		}

		public zVector2(int x, int y)
		{
			this.x = (zfloat)x;
			this.y = (zfloat)y;
		}

		public zVector2(zVector2 vec)
		{
			this.x = vec.x;
			this.y = vec.y;
		}

		// 添加反序列化构造函数
		public zVector2(SerializationInfo info, StreamingContext context)
		{
			x = zfloat.FromRaw(info.GetInt64("x"));
			y = zfloat.FromRaw(info.GetInt64("y"));
		}

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
		/// 判断向量是否为零向量。
		/// </summary>
		/// <returns>如果向量的x和y都为0，返回true；否则返回false</returns>
		public bool IsZero()
		{
			return this.x.value == 0 && this.y.value == 0;
		}


		public zfloat this[int index]
		{
			get
			{
				if (index == 0)
				{
					return x;
				}
				else if (index == 1)
				{
					return y;
				}
				else
				{
					throw new IndexOutOfRangeException("zVector2 only contains x and y, so the index must be 0 or 1!");
				}
			}

			set
			{
				if (index == 0)
				{
					x = value;
				}
				else if (index == 1)
				{
					y = value;
				}
				else
				{
					throw new IndexOutOfRangeException("zVector2 only contains x and y, so the index must be 0 or 1!");
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
			get
			{
				return zVector2.SqrMagnitude(ref this);
			}
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
			get { return zVector2.Magnitude(ref this); }
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
		public zVector2 normalized
		{
			get
			{
				return zVector2.Normalize(ref this);
			}
		}

		/// <summary>
		/// 获取向量在XY平面的快速近似归一化向量（仅供特殊场景使用）。
		/// 性能特征：O(approx)，使用近似算法而非精确平方根。
		/// </summary>
		/// <remarks>
		/// 此属性使用ApproximateHypotenuse算法，精度略低但性能更优。
		/// 仅在高频率、性能关键的场景（如流场寻路中的向量归一化）下推荐使用。
		/// 对于一般场景，优先使用normalized属性。
		/// </remarks>
		[JsonIgnore]
		public zVector2 approxNormalizedXY
		{
			get
			{
				zVector2 vec;
				zfloat xyLen = zMathf.ApproximateHypotenuse(x, y);
				if (xyLen.value == 0)
				{
					return zVector2.zero;
				}
				vec.x.value = x.value * zfloat.SCALE_10000 / xyLen.value;
				vec.y.value = y.value * zfloat.SCALE_10000 / xyLen.value;
				return vec;
			}
		}

		/// <summary>
		/// 将当前向量原地归一化。
		/// </summary>
		/// <remarks>
		/// 对于零向量，设置为zero常量。
		/// </remarks>
		public void Normalize()
		{
			// Avoid integer truncation for very small vectors
			long x_sq_10000 = (x.value * x.value) / zfloat.SCALE_10000;
			long y_sq_10000 = (y.value * y.value) / zfloat.SCALE_10000;
			long num = x_sq_10000 + y_sq_10000;
			num = zMathf.SqrtScale(num);

			if (num > 0)
			{
				x.value = x.value * zfloat.SCALE_10000 / num;
				y.value = y.value * zfloat.SCALE_10000 / num;
			}
			else
			{
				x.value = 0;
				y.value = 0;
			}
		}

		/// <summary>
		/// 获取向量的归一化向量（返回新的zVector2，不修改原向量）。
		/// 性能特征：O(sqrt)。
		/// </summary>
		/// <param name="vec">要归一化的向量（ref参数用于性能优化，不会被修改）</param>
		/// <returns>归一化后的单位向量，或zero如果原向量为零</returns>
		public static zVector2 Normalize(ref zVector2 vec)
		{
			zVector2 result;
			// Avoid integer truncation for very small vectors
			long x_sq_10000 = (vec.x.value * vec.x.value) / zfloat.SCALE_10000;
			long y_sq_10000 = (vec.y.value * vec.y.value) / zfloat.SCALE_10000;
			long num = x_sq_10000 + y_sq_10000;
			num = zMathf.SqrtScale(num);

			if (num > 0)
			{
				result.x.value = vec.x.value * zfloat.SCALE_10000 / num;
				result.y.value = vec.y.value * zfloat.SCALE_10000 / num;
				return result;
			}
			else
			{
				return zVector2.zero;
			}
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
		public zVector2 GetNormalizedForMagnitude(zfloat magnitude)
		{
			zVector2 vec = this;

			if (magnitude.value > 0)
			{
				vec.x.value = vec.x.value * zfloat.SCALE_10000 / magnitude.value;
				vec.y.value = vec.y.value * zfloat.SCALE_10000 / magnitude.value;
				return vec;
			}
			else
			{
				return zVector2.zero;
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
		public static zfloat Magnitude(ref zVector2 a)
		{
			// To avoid integer overflow when squaring large values,
			// we divide each squared component by SCALE_10000 before adding
			// This gives us (x^2 + y^2) / SCALE_10000 which is the input SqrtScale expects
			
			long x_sq_10000 = (a.x.value * a.x.value) / zfloat.SCALE_10000;
			long y_sq_10000 = (a.y.value * a.y.value) / zfloat.SCALE_10000;
			long lsqr = x_sq_10000 + y_sq_10000;
			
			zfloat result;
			result.value = zMathf.SqrtScale(lsqr);
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
		public static zfloat SqrMagnitude(ref zVector2 a)
		{
			zfloat result;
			result.value = (a.x.value * a.x.value + a.y.value * a.y.value) / zfloat.SCALE_10000;
			return result;
		}

		public static zVector2 Lerp(zVector2 from, zVector2 to, zfloat t)
		{
			t = zMathf.Clamp01(t);
			return new zVector2(from.x + (to.x - from.x) * t, from.y + (to.y - from.y) * t);
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
		public static zVector2 MoveTowards(zVector2 current, zVector2 target, zfloat maxDistanceDelta)
		{
			zVector2 a = target - current;
			zfloat magnitude = a.magnitude;
			if (magnitude <= maxDistanceDelta || magnitude == zfloat.Zero)
			{
				return target;
			}
			return current + a / magnitude * maxDistanceDelta;
		}

		/// <summary>
		/// 将当前向量按照scale向量进行元素级乘法（原地修改）。
		/// 性能特征：O(1)。
		/// </summary>
		/// <param name="scale">缩放因子向量</param>
		/// <remarks>
		/// 此方法执行元素级乘法：result.x = this.x * scale.x，result.y = this.y * scale.y。
		/// 由于zfloat为定点数，乘法后需要除以SCALE_10000来维持精度。
		/// </remarks>
		public void Scale(zVector2 scale)
		{
			x.value = x.value * scale.x.value / zfloat.SCALE_10000;
			y.value = y.value * scale.y.value / zfloat.SCALE_10000;
		}

		/// <summary>
		/// 将向量a和向量b进行元素级乘法。
		/// 性能特征：O(1)。
		/// </summary>
		/// <param name="a">第一个向量</param>
		/// <param name="b">第二个向量（缩放因子）</param>
		/// <returns>元素级相乘的结果向量</returns>
		/// <remarks>
		/// 示例：Scale(new zVector2(2, 3), new zVector2(1.5, 2)) = (3, 6)
		/// </remarks>
		public static zVector2 Scale(zVector2 a, zVector2 b)
		{
			zVector2 result;
			result.x.value = a.x.value * b.x.value / zfloat.SCALE_10000;
			result.y.value = a.y.value * b.y.value / zfloat.SCALE_10000;
			return result;
		}

		/// <summary>
		/// 向当前向量添加另一个向量（原地修改）。
		/// 性能特征：O(1)，使用.value直接操作优化性能。
		/// </summary>
		/// <param name="vec">要添加的向量</param>
		public void Add(ref zVector2 vec)
		{
			x.value += vec.x.value;
			y.value += vec.y.value;
		}

		/// <summary>
		/// 从当前向量减去另一个向量（原地修改）。
		/// 性能特征：O(1)，使用.value直接操作优化性能。
		/// </summary>
		/// <param name="vec">要减去的向量</param>
		public void Sub(ref zVector2 vec)
		{
			x.value -= vec.x.value;
			y.value -= vec.y.value;
		}

		/// <summary>
		/// 将当前向量与另一个向量进行元素级乘法（原地修改）。
		/// 性能特征：O(1)。
		/// </summary>
		/// <param name="vec">乘法因子向量</param>
		public void Mul(ref zVector2 vec)
		{
			x.value = x.value * vec.x.value / zfloat.SCALE_10000;
			y.value = y.value * vec.y.value / zfloat.SCALE_10000;
		}

		/// <summary>
		/// 将当前向量与另一个向量进行元素级除法（原地修改）。
		/// 性能特征：O(1)。
		/// </summary>
		/// <param name="vec">除数向量（不能包含0分量，否则结果为无穷大）</param>
		public void Div(ref zVector2 vec)
		{
			x.value = x.value * zfloat.SCALE_10000 / vec.x.value;
			y.value = y.value * zfloat.SCALE_10000 / vec.y.value;
		}

		public static zVector3 Cross(zVector2 lhs, zVector2 rhs)
		{
			return new zVector3(zfloat.Zero, zfloat.Zero, lhs.x * rhs.y - lhs.y * rhs.x);
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
		public static zfloat Dot(ref zVector2 lhs, ref zVector2 rhs)
		{
			return lhs * rhs;
		}

		/// <summary>
		/// 计算两个向量的点积（数量积）- 非ref参数版本。
		/// 性能特征：O(1)。
		/// </summary>
		/// <param name="lhs">左操作向量</param>
		/// <param name="rhs">右操作向量</param>
		/// <returns>两个向量的点积</returns>
		public static zfloat Dot(zVector2 lhs, zVector2 rhs)
		{
			return Dot(ref lhs, ref rhs);
		}

		public static zVector2 Project(zVector2 vector, zVector2 onNormal)
		{
			zfloat num = zVector2.Dot(onNormal, onNormal);
			if (num == zfloat.Zero)
			{
				return zVector2.zero;
			}
			return onNormal * zVector2.Dot(vector, onNormal) / num;
		}

		/// <summary>
		/// 计算从向量from旋转到向量to的夹角（弧度）。
		/// 性能特征：O(sqrt)，涉及向量归一化。
		/// </summary>
		/// <param name="from">起始向量</param>
		/// <param name="to">目标向量</param>
		/// <returns>两个向量之间的夹角（0 到 π，单位：弧度）</returns>
		/// <remarks>
		/// 结果值范围在[0, π]之间。
		/// 对于接近平行的向量，结果精度可能受定点数影响。
		/// 如需角度值而非弧度，使用 angle * zMathf.Rad2Deg 转换。
		/// </remarks>
		public static zfloat Angle(zVector2 from, zVector2 to)
		{
			from.Normalize();
			to.Normalize();
			zfloat tempNum;
			tempNum.value = zMathf.Acos(zMathf.Clamp(zVector2.Dot(ref from, ref to), zfloat.NegativeOne, zfloat.One)).value * zMathf.Rad2Deg.value / zfloat.SCALE_10000;
			return tempNum;
		}

		/// <summary>
		/// 计算从向量A旋转到向量B的有向夹角（度数），逆时针为正。
		/// 性能特征：O(sqrt)（涉及归一化）。
		/// </summary>
		/// <param name="A">起始向量</param>
		/// <param name="B">目标向量</param>
		/// <returns>旋转角度，范围[-180, 180]度。逆时针为正，顺时针为负</returns>
		/// <remarks>
		/// 此方法先对向量进行归一化，然后使用点积和叉积计算有向角度。
		/// 在2D平面上的旋转计算中常用。
		/// </remarks>
		public static zfloat A2B_angle(zVector2 A, zVector2 B)
		{
			// 先归一化向量
			A.Normalize();
			B.Normalize();
			
			zfloat angle;
			// 使用点积计算角度大小（范围0到180度）
			angle.value = zMathf.Rad2Deg.value * zMathf.Acos(zMathf.Clamp(zVector2.Dot(ref A, ref B), zfloat.NegativeOne, zfloat.One)).value / zfloat.SCALE_10000;
			
			// 使用2D叉积确定旋转方向：A.x * B.y - A.y * B.x
			// 如果叉积为负，说明是顺时针旋转，角度为负
			if (A.x.value * B.y.value - A.y.value * B.x.value < 0)
			{
				angle = -angle;
			}
			return angle;
		}

		public static zfloat Distance(zVector2 a, zVector2 b)
		{
			return (a - b).magnitude;
		}

		public static zVector2 ClampMagnitude(zVector2 vector, zfloat maxLength)
		{
			zfloat sqrMag = vector.sqrMagnitude;
			zfloat maxSqr = maxLength * maxLength;
			
			if (sqrMag <= maxSqr)
			{
				return vector;
			}
			
			// Calculate the actual magnitude
			zfloat mag;
			mag.value = zMathf.SqrtScale(sqrMag.value);
			
			// To minimize precision loss, compute result directly:
			// result = vector * (maxLength / mag)
			// In fixed point arithmetic: result.x.value = vector.x.value * maxLength.value / mag.value
			zVector2 result;
			result.x.value = vector.x.value * maxLength.value / mag.value;
			result.y.value = vector.y.value * maxLength.value / mag.value;
			return result;
		}

		public static zVector2 Min(zVector2 lhs, zVector2 rhs)
		{
			return new zVector2(zMathf.Min(lhs.x, rhs.x), zMathf.Min(lhs.y, rhs.y));
		}

		public static zVector2 Max(zVector2 lhs, zVector2 rhs)
		{
			return new zVector2(zMathf.Max(lhs.x, rhs.x), zMathf.Max(lhs.y, rhs.y));
		}

		#region 加法
		/// <summary>
		/// 向量加法：(lhs.x + rhs.x, lhs.y + rhs.y)。
		/// 性能特征：O(1)，使用.value直接操作。
		/// </summary>
		public static zVector2 operator +(zVector2 lhs, zVector2 rhs)
		{
			zVector2 vec;
			vec.x.value = lhs.x.value + rhs.x.value;
			vec.y.value = lhs.y.value + rhs.y.value;
			return vec;
		}

		/// <summary>
		/// 整数与向量加法。
		/// </summary>
		public static zVector2 operator +(int lhs, zVector2 rhs)
		{
			zVector2 vec;
			vec.x.value = lhs * zfloat.SCALE_10000 + rhs.x.value;
			vec.y.value = lhs * zfloat.SCALE_10000 + rhs.y.value;
			return vec;
		}

		/// <summary>
		/// 向量与整数加法。
		/// </summary>
		public static zVector2 operator +(zVector2 lhs, int rhs)
		{
			zVector2 vec;
			vec.x.value = lhs.x.value + rhs * zfloat.SCALE_10000;
			vec.y.value = lhs.y.value + rhs * zfloat.SCALE_10000;
			return vec;
		}

		/// <summary>
		/// zfloat与向量加法。
		/// </summary>
		public static zVector2 operator +(zfloat lhs, zVector2 rhs)
		{
			zVector2 vec;
			vec.x.value = lhs.value + rhs.x.value;
			vec.y.value = lhs.value + rhs.y.value;
			return vec;
		}

		/// <summary>
		/// 向量与zfloat加法。
		/// </summary>
		public static zVector2 operator +(zVector2 lhs, zfloat rhs)
		{
			zVector2 vec;
			vec.x.value = lhs.x.value + rhs.value;
			vec.y.value = lhs.y.value + rhs.value;
			return vec;
		}
		#endregion

		#region 减法
		/// <summary>
		/// 向量减法：(lhs.x - rhs.x, lhs.y - rhs.y)。
		/// 性能特征：O(1)，使用.value直接操作。
		/// </summary>
		public static zVector2 operator -(zVector2 lhs, zVector2 rhs)
		{
			zVector2 vec;
			vec.x.value = lhs.x.value - rhs.x.value;
			vec.y.value = lhs.y.value - rhs.y.value;
			return vec;
		}

		/// <summary>
		/// 整数与向量减法。
		/// </summary>
		public static zVector2 operator -(int lhs, zVector2 rhs)
		{
			zVector2 vec;
			vec.x.value = lhs * zfloat.SCALE_10000 - rhs.x.value;
			vec.y.value = lhs * zfloat.SCALE_10000 - rhs.y.value;
			return vec;
		}

		/// <summary>
		/// 向量与整数减法。
		/// </summary>
		public static zVector2 operator -(zVector2 lhs, int rhs)
		{
			zVector2 vec;
			vec.x.value = lhs.x.value - rhs * zfloat.SCALE_10000;
			vec.y.value = lhs.y.value - rhs * zfloat.SCALE_10000;
			return vec;
		}

		/// <summary>
		/// zfloat与向量减法。
		/// </summary>
		public static zVector2 operator -(zfloat lhs, zVector2 rhs)
		{
			zVector2 vec;
			vec.x.value = lhs.value - rhs.x.value;
			vec.y.value = lhs.value - rhs.y.value;
			return vec;
		}

		/// <summary>
		/// 向量与zfloat减法。
		/// </summary>
		public static zVector2 operator -(zVector2 lhs, zfloat rhs)
		{
			zVector2 vec;
			vec.x.value = lhs.x.value - rhs.value;
			vec.y.value = lhs.y.value - rhs.value;
			return vec;
		}
		#endregion

		#region 负号
		/// <summary>
		/// 向量取反：(-x, -y)。
		/// 性能特征：O(1)。
		/// </summary>
		public static zVector2 operator -(zVector2 a)
		{
			zVector2 vec;
			vec.x.value = -a.x.value;
			vec.y.value = -a.y.value;
			return vec;
		}
		#endregion

		#region 乘法
		/// <summary>
		/// 向量点积（数量积）。
		/// 语义：返回标量，不是逐分量乘法；逐分量乘法请使用Scale或Mul。
		/// 性能特征：O(1)。
		/// </summary>
		public static zfloat operator *(zVector2 vector1, zVector2 vector2)
		{
			zfloat result;
			result.value = (vector1.x.value * vector2.x.value + vector1.y.value * vector2.y.value) / zfloat.SCALE_10000;
			return result;
		}

		/// <summary>
		/// 整数与向量乘法：(lhs * rhs.x, lhs * rhs.y)。
		/// 性能特征：O(1)，使用.value直接操作。
		/// </summary>
		public static zVector2 operator *(int lhs, zVector2 rhs)
		{
			zVector2 vec;
			vec.x.value = lhs * rhs.x.value;
			vec.y.value = lhs * rhs.y.value;
			return vec;
		}

		/// <summary>
		/// 向量与整数乘法。
		/// </summary>
		public static zVector2 operator *(zVector2 lhs, int rhs)
		{
			zVector2 vec;
			vec.x.value = lhs.x.value * rhs;
			vec.y.value = lhs.y.value * rhs;
			return vec;
		}

		/// <summary>
		/// zfloat与向量乘法（需要除以SCALE_10000）。
		/// 性能特征：O(1)。
		/// </summary>
		public static zVector2 operator *(zfloat lhs, zVector2 rhs)
		{
			zVector2 vec;
			vec.x.value = lhs.value * rhs.x.value / zfloat.SCALE_10000;
			vec.y.value = lhs.value * rhs.y.value / zfloat.SCALE_10000;
			return vec;
		}

		/// <summary>
		/// 向量与zfloat乘法。
		/// </summary>
		public static zVector2 operator *(zVector2 lhs, zfloat rhs)
		{
			zVector2 vec;
			vec.x.value = lhs.x.value * rhs.value / zfloat.SCALE_10000;
			vec.y.value = lhs.y.value * rhs.value / zfloat.SCALE_10000;
			return vec;
		}
		#endregion

		#region 除法
		/// <summary>
		/// 向量除以整数。
		/// 性能特征：O(1)，使用.value直接操作。
		/// </summary>
		public static zVector2 operator /(zVector2 lhs, int rhs)
		{
			zVector2 vec;
			vec.x.value = lhs.x.value / rhs;
			vec.y.value = lhs.y.value / rhs;
			return vec;
		}

		/// <summary>
		/// 向量除以zfloat（需要乘以SCALE_10000）。
		/// 性能特征：O(1)。
		/// </summary>
		public static zVector2 operator /(zVector2 lhs, zfloat rhs)
		{
			zVector2 vec;
			vec.x.value = lhs.x.value * zfloat.SCALE_10000 / rhs.value;
			vec.y.value = lhs.y.value * zfloat.SCALE_10000 / rhs.value;
			return vec;
		}
		#endregion

		#region 相等判定
		/// <summary>
		/// 判断两个向量是否不相等。
		/// 性能特征：O(1)，使用.value直接比较。
		/// </summary>
		public static bool operator !=(zVector2 lhs, zVector2 rhs)
		{
			return (lhs.x.value != rhs.x.value) || (lhs.y.value != rhs.y.value);
		}

		/// <summary>
		/// 判断两个向量是否相等。
		/// 性能特征：O(1)，使用.value直接比较。
		/// </summary>
		public static bool operator ==(zVector2 lhs, zVector2 rhs)
		{
			return (lhs.x.value == rhs.x.value) && (lhs.y.value == rhs.y.value);
		}
		#endregion

		#region 类型转换
		/// <summary>
		/// 从zVector3隐式转换为zVector2（丢弃Z分量）。
		/// </summary>
		public static implicit operator zVector2(zVector3 v)
		{
			zVector2 result;
			result.x = v.x;
			result.y = v.y;
			return result;
		}

		/// <summary>
		/// zVector2隐式转换为zVector3（Z分量设为0）。
		/// </summary>
		public static implicit operator zVector3(zVector2 v)
		{
			return new zVector3(v.x, v.y, zfloat.Zero);
		}

		#endregion

		/// <summary>
		/// 获取向量的字符串表示形式。
		/// </summary>
		/// <returns>格式为"(x , y)"的字符串</returns>
		public override string ToString()
		{
			return ("(" + x + " , " + y + ")");
		}

		/// <summary>
		/// 判断是否与另一个对象相等。
		/// </summary>
		/// <param name="obj">要比较的对象</param>
		/// <returns>如果相等返回true，否则返回false</returns>
		public override bool Equals(object obj)
		{
			if (obj is zVector2 other)
			{
				return x.value == other.x.value && y.value == other.y.value;
			}
			return false;
		}

		/// <summary>
		/// 判断是否与另一个zVector2相等。
		/// </summary>
		/// <param name="other">要比较的zVector2</param>
		/// <returns>如果相等返回true，否则返回false</returns>
		public bool Equals(zVector2 other)
		{
			return x.value == other.x.value && y.value == other.y.value;
		}

		/// <summary>
		/// 获取哈希码。
		/// </summary>
		/// <returns>基于x和y的raw值计算的哈希码</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(x.value, y.value);
		}

		/// <summary>
		/// 实现ISerializable接口的序列化方法。
		/// </summary>
		/// <param name="info">序列化信息</param>
		/// <param name="context">流上下文</param>
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("x", x.value);
			info.AddValue("y", y.value);
		}
	}
}
