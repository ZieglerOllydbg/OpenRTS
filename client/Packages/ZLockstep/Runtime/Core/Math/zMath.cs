
namespace zUnity
{
	/// <summary>
	/// 提供用于 <see cref="zfloat"/> 的确定性数学函数集合。
	/// </summary>
	/// <remarks>
	/// 本类面向锁步逻辑，优先保证跨平台确定性与行为稳定，不追求与 <c>System.Math</c> 完全一致。
	/// </remarks>
	public class zMathf
	{
		/// <summary>
		/// 圆周率常量，定点精度为 4 位小数。
		/// </summary>
		public static readonly zfloat PI = new zfloat(3, 1416);

		/// <summary>
		/// 自然常数 e，定点精度为 4 位小数。
		/// </summary>
		public static readonly zfloat E = new zfloat(2, 7183);

		/// <summary>
		/// 以 10 为底的自然对数常量。
		/// </summary>
		public static readonly zfloat LN_10 = new zfloat(2, 3026);

		/// <summary>
		/// 常量 1。
		/// </summary>
		public static readonly zfloat One = new zfloat(1);

		/// <summary>
		/// 常量 0。
		/// </summary>
		public static readonly zfloat Zero = new zfloat(0);

		/// <summary>
		/// 常量 0.5。
		/// </summary>
		public static readonly zfloat Half = new zfloat(0, zfloat.SCALE_10000 / 2);

		/// <summary>
		/// 角度转弧度的比例系数。
		/// </summary>
		public static zfloat Deg2Rad = new zfloat((PI * 2) / 360);

		/// <summary>
		/// 弧度转角度的比例系数。
		/// </summary>
		public static zfloat Rad2Deg = new zfloat(360 / (PI * 2));

		protected static int[] SIN_DATA_10000 = new int[]{
		0, 174, 348, 523, 697, 
		871, 1045, 1218, 1391, 1564, 
		1736, 1908, 2079, 2249, 2419, 
		2588, 2756, 2923, 3090, 3255, 
		3420, 3583, 3746, 3907, 4067, 
		4226, 4383, 4539, 4694, 4848, 
		5000, 5150, 5299, 5446, 5591, 
		5735, 5877, 6018, 6156, 6293, 
		6427, 6560, 6691, 6819, 6946, 
		7071, 7193, 7313, 7431, 7547, 
		7660, 7771, 7880, 7986, 8090, 
		8191, 8290, 8386, 8480, 8571, 
		8660, 8746, 8829, 8910, 8987, 
		9063, 9135, 9205, 9271, 9335, 
		9396, 9455, 9510, 9563, 9612, 
		9659, 9702, 9743, 9781, 9816, 
		9848, 9876, 9902, 9925, 9945, 
		9961, 9975, 9986, 9993, 9998, 
		10000, 
    };
		protected static int[] POW_FLOAT_DATA_10000 = new int[] { 5000, 2500, 1250, 625, 312, 156, 78, 39 };
		private const long FastSqrtMinRaw = 20000;
		private const long FastSqrtMaxRaw = 1000000;
		private const long SqrtScaleLargeInputThreshold = 100000000;
		#region 三角函数
		/// <summary>
		/// 计算正弦值（输入单位：弧度）。
		/// </summary>
		/// <param name="rad">弧度值。</param>
		/// <returns>正弦值，范围约为 [-1, 1]。</returns>
		/// <remarks>内部先转角度后查表插值，结果为确定性近似值。</remarks>
		public static zfloat Sin(zfloat rad)
		{
            zfloat angle;
            angle.value = rad.value * Rad2Deg.value / zfloat.SCALE_10000;
			return SinAngle(angle);
		}

		/// <summary>
		/// 计算余弦值（输入单位：弧度）。
		/// </summary>
		/// <param name="rad">弧度值。</param>
		/// <returns>余弦值，范围约为 [-1, 1]。</returns>
		/// <remarks>内部先转角度后查表插值，结果为确定性近似值。</remarks>
		public static zfloat Cos(zfloat rad)
		{
            zfloat angle;
            angle.value = rad.value * Rad2Deg.value / zfloat.SCALE_10000;
			return CosAngle(angle);
		}

		/// <summary>
		/// 计算正切值（输入单位：弧度）。
		/// </summary>
		/// <param name="rad">弧度值。</param>
		/// <returns>正切值；当余弦为 0 时返回 <see cref="zfloat.Infinity"/>。</returns>
		/// <remarks>内部先转角度后调用 <see cref="TanAngle(zfloat)"/>。</remarks>
		public static zfloat Tan(zfloat rad)
		{
            zfloat angle;
            angle.value = rad.value * Rad2Deg.value / zfloat.SCALE_10000;
			return TanAngle(angle);
		}

		/// <summary>
		/// 计算正弦值（输入单位：角度，支持小数角）。
		/// </summary>
		/// <param name="angle">角度值。</param>
		/// <returns>正弦值，范围约为 [-1, 1]。</returns>
		/// <remarks>使用整数角查表 + 线性插值。</remarks>
		public static zfloat SinAngle(zfloat angle)
		{
			zfloat n = angle.GetFractionalPart();

			int s = zMathf.Floor(angle);
			int e = zMathf.Ceil(angle);

			zfloat sSin = SinAngle(s);
			zfloat eSin = SinAngle(e);
            zfloat result;
            result.value = zMathf.LerpScale(sSin.value, eSin.value, n.value);
            return result;
		}


		/// <summary>
		/// 计算正弦值（输入单位：整数角度）。
		/// </summary>
		/// <param name="angle">角度值，自动按 360 周期归一化。</param>
		/// <returns>正弦值，范围约为 [-1, 1]。</returns>
		public static zfloat SinAngle(int angle)
		{
			long sin10000;
			if (angle > 360)
			{
				angle %= 360;
			}
			else if (angle < 0)
			{
				angle = 360 - ((-angle) % 360);
			}
			if (angle < 180)
			{
				if (angle < 90)
				{
					sin10000 = SIN_DATA_10000[angle];
				}
				else
				{
					sin10000 = SIN_DATA_10000[180 - angle];
				}
			}
			else
			{
				if (angle < 270)
				{
					sin10000 = (-SIN_DATA_10000[angle - 180]);
				}
				else
				{
					sin10000 = (-SIN_DATA_10000[360 - angle]);
				}
			}
            zfloat result;
            result.value = sin10000;
            return result;
		}

		/// <summary>
		/// 计算余弦值（输入单位：角度，支持小数角）。
		/// </summary>
		/// <param name="angle">角度值。</param>
		/// <returns>余弦值，范围约为 [-1, 1]。</returns>
		/// <remarks>使用整数角查表 + 线性插值。</remarks>
		public static zfloat CosAngle(zfloat angle)
		{
			zfloat n = angle.GetFractionalPart();

			int s = zMathf.Floor(angle);
			int e = zMathf.Ceil(angle);

			zfloat sCos = CosAngle(s);
			zfloat eCos = CosAngle(e);

            zfloat result;
            result.value = zMathf.LerpScale(sCos.value, eCos.value, n.value);
            return result;
		}

		/// <summary>
		/// 计算余弦值（输入单位：整数角度）。
		/// </summary>
		/// <param name="angle">角度值，自动按 360 周期归一化。</param>
		/// <returns>余弦值，范围约为 [-1, 1]。</returns>
		public static zfloat CosAngle(int angle)
		{
			long cos10000;
			if (angle > 360)
			{
				angle %= 360;
			}
			else if (angle < 0)
			{
				angle = 360 - ((-angle) % 360);
			}
			if (angle < 180)
			{
				if (angle < 90)
				{
					cos10000 = SIN_DATA_10000[90 - angle];
				}
				else
				{
					cos10000 = (-SIN_DATA_10000[angle - 90]);
				}
			}
			else
			{
				if (angle < 270)
				{
					cos10000 = (-SIN_DATA_10000[270 - angle]);
				}
				else
				{
					cos10000 = SIN_DATA_10000[angle - 270];
				}
			}
			return zfloat.FromRaw(cos10000);
		}

		/// <summary>
		/// 计算正切值（输入单位：角度，支持小数角）。
		/// </summary>
		/// <param name="angle">角度值。</param>
		/// <returns>正切值；当余弦为 0 时返回 <see cref="zfloat.Infinity"/>。</returns>
		/// <remarks>无副作用版本；如需判断是否可计算，请使用 <see cref="TryTanAngle(zfloat, out zfloat)"/>。</remarks>
		public static zfloat TanAngle(zfloat angle)
		{
			return TryTanAngle(angle, out zfloat result) ? result : zfloat.Infinity;
		}

		/// <summary>
		/// 尝试计算正切值（输入单位：角度，支持小数角）。
		/// </summary>
		/// <param name="angle">角度值。</param>
		/// <param name="result">计算结果；当返回 false 时为 <see cref="zfloat.Infinity"/>。</param>
		/// <returns>当余弦非 0 时返回 true；否则返回 false。</returns>
		public static bool TryTanAngle(zfloat angle, out zfloat result)
		{
			zfloat s = (zfloat)SinAngle(angle);
			zfloat c = (zfloat)CosAngle(angle);
			if (c == 0)
			{
				result = zfloat.Infinity;
				return false;
			}

			result.value = s.value * zfloat.SCALE_10000 / c.value;
			return true;
		}

		/// <summary>
		/// 计算正切值（输入单位：整数角度）。
		/// </summary>
		/// <param name="angle">角度值。</param>
		/// <returns>正切值；当余弦为 0 时返回 <see cref="zfloat.Infinity"/>。</returns>
		/// <remarks>无副作用版本；如需判断是否可计算，请使用 <see cref="TryTanAngle(int, out zfloat)"/>。</remarks>
		public static zfloat TanAngle(int angle)
		{
			return TryTanAngle(angle, out zfloat result) ? result : zfloat.Infinity;
		}

		/// <summary>
		/// 尝试计算正切值（输入单位：整数角度）。
		/// </summary>
		/// <param name="angle">角度值。</param>
		/// <param name="result">计算结果；当返回 false 时为 <see cref="zfloat.Infinity"/>。</param>
		/// <returns>当余弦非 0 时返回 true；否则返回 false。</returns>
		public static bool TryTanAngle(int angle, out zfloat result)
		{
			zfloat s = (zfloat)SinAngle(angle);
			zfloat c = (zfloat)CosAngle(angle);
			if (c == 0)
			{
				result = zfloat.Infinity;
				return false;
			}

			result.value = s.value * zfloat.SCALE_10000 / c.value;
			return true;
		}

		/// <summary>
		/// 计算反正弦值（输入/输出单位均为角度）。
		/// </summary>
		/// <param name="f">正弦值，建议范围 [-1, 1]。</param>
		/// <returns>反正弦角度值（度）。</returns>
		/// <remarks>当输入越界时会被截断到允许范围后再计算。</remarks>
		public static zfloat AsinAngle(zfloat f)
		{
			if (f.value > zfloat.One.value)
			{
				f.value = zfloat.SCALE_10000;
			}
			else if (f.value < zfloat.NegativeOne.value)
			{
				f.value = zfloat.NegativeOne.value;
			}

			bool inv = false;
			if (f.value < 0)
			{
				inv = true;
				f.value = -f.value;
			}

			int startIndex = 0;
			int endIndex = SIN_DATA_10000.Length - 1;
			int currIndex = 0;
			while (endIndex - startIndex != 1)
			{
				currIndex = startIndex + ((endIndex - startIndex) / 2);
				if (SIN_DATA_10000[currIndex] > f.value)
				{
					endIndex = currIndex;
				}
				else
				{
					startIndex = currIndex;
				}
			}

			int angle = f.value - SIN_DATA_10000[startIndex] < SIN_DATA_10000[endIndex] - f.value ? startIndex : endIndex;
			if (inv)
				angle = -angle;

			f.value = angle * zfloat.SCALE_10000;
			return f;
		}

		/// <summary>
		/// 计算反余弦值（输入/输出单位均为角度）。
		/// </summary>
		/// <param name="f">余弦值，建议范围 [-1, 1]。</param>
		/// <returns>反余弦角度值（度）。</returns>
		/// <remarks>当输入越界时会被截断到允许范围后再计算。</remarks>
		public static zfloat AcosAngle(zfloat f)
		{
			if (f.value > zfloat.SCALE_10000)
			{
				f.value = zfloat.One.value;
			}
			else if (f.value < zfloat.NegativeOne.value)
			{
				f.value = zfloat.NegativeOne.value;
			}

			zfloat sin;
            sin.value = zMathf.SqrtScale(zfloat.One.value - f.value * f.value / zfloat.SCALE_10000);
			zfloat angle = AsinAngle(sin);
			if (f.value < 0)
			{
				angle.value = 180 * zfloat.SCALE_10000 - angle.value;
			}
			return angle;
		}

		/// <summary>
		/// 计算反正切值（输入为斜率，输出单位为角度）。
		/// </summary>
		/// <param name="f">斜率值。</param>
		/// <returns>反正切角度值（度）。</returns>
		/// <remarks>内部通过反正弦近似推导实现。</remarks>
		public static zfloat AtanAngle(zfloat f)
		{
			zfloat sin2;
            sin2 = (f * f) / (f * f + zfloat.One);
			zfloat sin;
            sin.value = SqrtScale(sin2.value);

			return SignInt(f.value) * AsinAngle(sin);
		}

		/// <summary>
		/// 计算反正弦值（输入单位无量纲，输出单位弧度）。
		/// </summary>
		/// <param name="f">正弦值，建议范围 [-1, 1]。</param>
		/// <returns>反正弦弧度值。</returns>
		public static zfloat Asin(zfloat f)
		{
            zfloat result;
            result.value = AsinAngle(f).value * Deg2Rad.value / zfloat.SCALE_10000;
            return result;
		}

		/// <summary>
		/// 计算反余弦值（输入单位无量纲，输出单位弧度）。
		/// </summary>
		/// <param name="f">余弦值，建议范围 [-1, 1]。</param>
		/// <returns>反余弦弧度值。</returns>
		public static zfloat Acos(zfloat f)
		{
            zfloat result;
            result.value = AcosAngle(f).value * Deg2Rad.value / zfloat.SCALE_10000;
            return result;
		}

		/// <summary>
		/// 计算反正切值（输出单位弧度）。
		/// </summary>
		/// <param name="f">斜率值。</param>
		/// <returns>反正切弧度值。</returns>
		public static zfloat Atan(zfloat f)
		{
            zfloat result;
            result.value = AtanAngle(f).value * Deg2Rad.value / zfloat.SCALE_10000;
            return result;
		}

		/// <summary>
		/// 以弧度为单位计算并返回 y/x 的反正切值。
		/// </summary>
		/// <param name="y">对边分量。</param>
		/// <param name="x">邻边分量。</param>
		/// <returns>反正切弧度值，范围约为 [-PI, PI]。</returns>
		/// <remarks>行为与锁步项目既有语义保持一致，不完全等同于 <c>System.Math.Atan2</c> 的边界策略。</remarks>
		public static zfloat Atan2(zfloat y, zfloat x)
		{
            if (y.value == 0)
            {
                if (x.value >= 0)
                {
                    return zfloat.Zero;
                }
                else
                {
                    return PI;
                }
            }

            if (x.value == 0)
            {
                if (y.value > 0)
                {
                    return PI / 2;
                }
                else
                {
                    return -PI / 2;
                }
            }


            if (x.value >= 0 && y.value >= 0)  //1  0 < θ < PI/2.
			{
				return Atan(y / x);
			}
			else if (x.value <= 0 && y.value >= 0)  //2   PI/2 < θ≤PI
			{
				return PI + Atan(y / x);
			}
			else if (x.value <= 0 && y.value <= 0)  //3  -PI < θ < -PI/2
			{
				return Atan(y / x) - PI;  
			}
			else    //4    -PI/2 < θ < 0
			{
				return Atan(y / x);
			}
		}

		/// <summary>
		/// 计算定点绝对值。
		/// </summary>
		/// <param name="f">输入值。</param>
		/// <returns>绝对值。</returns>
		public static zfloat Abs(zfloat f)
		{
            f.value = Abs(f.value);
            return f;
		}

		/// <summary>
		/// 计算整数绝对值。
		/// </summary>
		/// <param name="f">输入值。</param>
		/// <returns>绝对值。</returns>
		public static int Abs(int f)
		{
			return f >= 0 ? f : -f;
		}

		/// <summary>
		/// 计算长整数绝对值。
		/// </summary>
		/// <param name="f">输入值。</param>
		/// <returns>绝对值。</returns>
		/// <remarks>与历史实现保持一致：<c>long.MinValue</c> 时会溢出为自身。</remarks>
		public static long Abs(long f)
		{
			return f >= 0 ? f : -f;
		}

		/// <summary>
		/// RVO 使用的数值容差常量。
		/// </summary>
		internal static readonly zfloat RVO_EPSILON = new zfloat(0, 1);

		/// <summary>
		/// 计算二维向量长度。
		/// </summary>
		/// <param name="vector">二维向量。</param>
		/// <returns>向量长度。</returns>
		public static zfloat Abs(zVector2 vector)
		{
			return Sqrt(AbsSq(vector));
		}

		/// <summary>
		/// 计算二维向量长度平方。
		/// </summary>
		/// <param name="vector">二维向量。</param>
		/// <returns>向量长度平方。</returns>
		public static zfloat AbsSq(zVector2 vector)
		{
			return vector * vector;
		}

		/// <summary>
		/// 将二维向量归一化。
		/// </summary>
		/// <param name="vector">二维向量。</param>
		/// <returns>单位向量；当长度小于等于 <see cref="zfloat.Epsilon"/> 时返回 <see cref="zVector2.zero"/>。</returns>
		public static zVector2 Normalize(zVector2 vector)
		{
			zfloat length = Abs(vector);
			if (length <= zfloat.Epsilon)
			{
				return zVector2.zero;
			}

			return vector / length;
		}

		/// <summary>
		/// 计算二维向量行列式（叉积 z 分量）。
		/// </summary>
		/// <param name="vector1">向量1。</param>
		/// <param name="vector2">向量2。</param>
		/// <returns>行列式结果。</returns>
		public static zfloat Det(zVector2 vector1, zVector2 vector2)
		{
			return vector1.x * vector2.y - vector1.y * vector2.x;
		}

		/// <summary>
		/// 计算点到线段的距离平方。
		/// </summary>
		/// <param name="vector1">线段起点。</param>
		/// <param name="vector2">线段终点。</param>
		/// <param name="vector3">待计算点。</param>
		/// <returns>距离平方。</returns>
		public static zfloat DistSqPointLineSegment(zVector2 vector1, zVector2 vector2, zVector2 vector3)
		{
			zfloat denominator = AbsSq(vector2 - vector1);
			if (denominator <= zfloat.Epsilon)
			{
				return AbsSq(vector3 - vector1);
			}

			zfloat r = ((vector3 - vector1) * (vector2 - vector1)) / denominator;

			if (r < zfloat.Zero)
			{
				return AbsSq(vector3 - vector1);
			}

			if (r > zfloat.One)
			{
				return AbsSq(vector3 - vector2);
			}

			return AbsSq(vector3 - (vector1 + r * (vector2 - vector1)));
		}

		/// <summary>
		/// 计算点 c 相对于向量 ab 的有向面积值。
		/// </summary>
		/// <param name="a">点 a。</param>
		/// <param name="b">点 b。</param>
		/// <param name="c">点 c。</param>
		/// <returns>有向面积值。</returns>
		public static zfloat LeftOf(zVector2 a, zVector2 b, zVector2 c)
		{
			return Det(a - c, b - a);
		}

		/// <summary>
		/// 计算平方。
		/// </summary>
		/// <param name="scalar">输入值。</param>
		/// <returns>平方结果。</returns>
		public static zfloat Sqr(zfloat scalar)
		{
			return scalar * scalar;
		}

		#endregion
		/// <summary>
		/// 计算平方根。
		/// </summary>
		/// <param name="f1">被开方数。</param>
		/// <returns>平方根；当输入为负数时返回 0。</returns>
		/// <remarks>无副作用版本；如需判断是否可计算，请使用 <see cref="TrySqrt(zfloat, out zfloat)"/>。</remarks>
		public static zfloat Sqrt(zfloat f1)
		{
#if Z_DEBUG
		checked
		{
#endif
			return TrySqrt(f1, out zfloat result) ? result : zfloat.Zero;

#if Z_DEBUG
		}
#endif
		}

		/// <summary>
		/// 尝试计算平方根。
		/// </summary>
		/// <param name="f1">被开方数。</param>
		/// <param name="result">平方根结果；当返回 false 时为 0。</param>
		/// <returns>当输入为非负数时返回 true；否则返回 false。</returns>
		public static bool TrySqrt(zfloat f1, out zfloat result)
		{
			if (f1.value < 0)
			{
				result = zfloat.Zero;
				return false;
			}

			result.value = SqrtScale(f1.value);
			return true;
		}

		/// <summary>
		/// 返回两个定点值的较小值。
		/// </summary>
		/// <param name="a">第一个值。</param>
		/// <param name="b">第二个值。</param>
		/// <returns>较小值。</returns>
		public static zfloat Min(zfloat a, zfloat b)
		{
#if Z_DEBUG
		checked
		{
#endif
			if (a.value <= b.value)
			{
				return a;
			}
			return b;
#if Z_DEBUG
		}
#endif
		}

		/// <summary>
		/// 返回两个定点值的较大值。
		/// </summary>
		/// <param name="a">第一个值。</param>
		/// <param name="b">第二个值。</param>
		/// <returns>较大值。</returns>
		public static zfloat Max(zfloat a, zfloat b)
		{
#if Z_DEBUG
		checked
		{
#endif
			if (a.value >= b.value)
			{
				return a;
			}
			return b;
#if Z_DEBUG
		}
#endif
		}

		/// <summary>
		/// 返回两个整数的较大值。
		/// </summary>
		/// <param name="a">第一个值。</param>
		/// <param name="b">第二个值。</param>
		/// <returns>较大值。</returns>
		public static int Max(int a, int b)
		{
			if (a >= b)
			{
				return a;
			}
			return b;
		}


		static uint[] ary = new uint[256] {
  0, 16, 16, 16, 32, 32, 32, 32, 32, 48, 48, 48, 48, 48, 48, 48, 64, 64, 68, 68, 72, 72, 76, 76, 76, 80, 80, 84, 84, 84, 88, 88,
 88, 92, 92, 92, 96, 96, 96,100,100,100,104,104,104,108,108,108,108,112,112,112,116,116,116,116,120,120,120,120,124,124,124,124,
128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,143,143,144,145,146,147,148,149,150,150,151,152,153,154,155,155,156,
157,158,159,159,160,161,162,163,163,164,165,166,167,167,168,169,170,170,171,172,173,173,174,175,175,176,177,178,178,179,180,181,
181,182,183,183,184,185,185,186,187,187,188,189,189,190,191,191,192,193,193,194,195,195,196,197,197,198,199,199,200,201,201,202,
203,203,204,204,205,206,206,207,207,208,209,209,210,211,211,212,212,213,214,214,215,215,216,217,217,218,218,219,219,220,221,221,
222,222,223,223,224,225,225,226,226,227,227,228,229,229,230,230,231,231,232,232,233,234,234,235,235,236,236,237,237,238,238,239,
239,240,241,241,242,242,243,243,244,244,245,245,246,246,247,247,248,248,249,249,250,250,251,251,252,252,253,253,254,254,255,255
};
		
		/// <summary>
		/// 旧版平方根查表接口。
		/// </summary>
		/// <param name="n">被开方数。</param>
		/// <returns>平方根近似值。</returns>
		/// <remarks>仅保留兼容，建议改用 <see cref="Sqrt(zfloat)"/> 或 <see cref="SqrtScale(long)"/>。</remarks>
		[System.Obsolete("SqrtTable 为历史兼容接口，建议改用 Sqrt(zfloat) 或 SqrtScale(long)。")]
		public static zfloat SqrtTable(zfloat n)
		{
			return zfloat.FromRaw(SqrtI((uint)n.value / zfloat.SCALE_100) * zfloat.SCALE_1000);
		}

		private static uint SqrtI(uint a)
		{
			uint b;

			if (a < 256) return (ary[a]) >> 4;
			else if (a < (1 << 12)) b = ary[a >> 4] >> 2;
			else if (a < (1 << 14)) b = ary[a >> 6] >> 1;
			else if (a < (1 << 16)) b = ary[a >> 8];
			else if (a > 4294836225u) return 65535;
			else
			{
				int s = (31 - clz((a >> 16) | 1)) >> 1;
				uint c = a >> (s + 2);
				b = ary[c >> (s + 8)]; // b=sqrt(c >> s)
				b = c / b + (b << s); //插值 c/b约＝b<<s
			}
			return (uint)(b - ((a < b * b) ? 1 : 0));
		}

		/// <summary>
		/// 查询位数
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		private static int clz(uint v)
		{
			int n = 64, c = 64;
			while (n != 0)
			{
				n >>= 1;
				if ((v >> n) != 0) { c -= n; v >>= n; }
			}
			return (int)(c - v);
		}

		/// <summary>
		/// 计算幂函数 f^p。
		/// </summary>
		/// <param name="f">底数。</param>
		/// <param name="p">指数。</param>
		/// <returns>幂运算结果。</returns>
		/// <remarks>无副作用版本；如需判断是否可计算，请使用 <see cref="TryPow(zfloat, zfloat, out zfloat)"/>。</remarks>
		public static zfloat Pow(zfloat f, zfloat p)
		{
			return TryPow(f, p, out zfloat result) ? result : zfloat.Zero;
		}

		/// <summary>
		/// 尝试计算幂函数 f^p。
		/// </summary>
		/// <param name="f">底数。</param>
		/// <param name="p">指数。</param>
		/// <param name="result">幂运算结果；当返回 false 时为 0。</param>
		/// <returns>当输入可在当前定点实数域内计算时返回 true；否则返回 false。</returns>
		public static bool TryPow(zfloat f, zfloat p, out zfloat result)
		{
#if Z_DEBUG
		checked
		{
#endif
			if (f < 0 && p.value % zfloat.SCALE_10000 != 0)
			{
				result = zfloat.Zero;
				return false;
			}

			bool inv = false;
			if (p == 0)
			{
				result = zfloat.One;
				return true;
			}
			if (p < 0)
			{
				inv = true;
				p = -p;
			}
			long intPart = p.value / zfloat.SCALE_10000;
			long ip = Pow_Int_10000(f.value, (int)intPart);
			long fp = Pow_Float_10000(f.value, (int)(p.value - intPart * zfloat.SCALE_10000));
			zfloat r = zfloat.FromRaw(ip * fp / zfloat.SCALE_10000);

			if (inv)
			{
				result = 1 / r;
			}
			else
			{
				result = r;
			}
			return true;
#if Z_DEBUG
		}
#endif

		}

		protected static long Pow_Int_10000(long f_10000, int p)
		{
			long result = zfloat.SCALE_10000;
			long f_100 = f_10000 / zfloat.SCALE_ROOT4_10000;
			for (int i = 0; i < p; ++i)
			{
				result /= zfloat.SCALE_ROOT4_10000;
				result *= f_100;
				result /= zfloat.SCALE_100;
			}
			return result;
		}

		/// <summary>
		/// 小数幂数分解
		/// </summary>
		protected static int[] powDecompose = new int[8];
		/// <summary>
		/// 该方法只处理小于1的幂函数，即p_10000必须小于10000
		/// </summary>
		/// <param name="f_10000"></param>
		/// <param name="p_10000"></param>
		/// <returns></returns>
		protected static long Pow_Float_10000(long f_10000, int p_10000)
		{
			int currIndex = 0;
			int startIndex = 0;
			int residue = p_10000;

			for (int i = 0; i < powDecompose.Length; ++i)
			{
				powDecompose[i] = 0;
			}
			bool flag = false;
			//循环分解幂数
			//分解出的个数达到上限，或者剩余幂数已经比预置表中的最小值还小，就不再继续分解了
			while (currIndex <= powDecompose.Length && startIndex < POW_FLOAT_DATA_10000.Length)
			{
				flag = false;
				for (int i = startIndex; i < POW_FLOAT_DATA_10000.Length; ++i)
				{
					if (POW_FLOAT_DATA_10000[i] <= residue)
					{
						powDecompose[currIndex] = i + 1;
						residue -= POW_FLOAT_DATA_10000[i];
						startIndex = i;
						++currIndex;
						flag = true;
						break;
					}
					startIndex = i + 1;
				}
				if (!flag)
				{
					startIndex = POW_FLOAT_DATA_10000.Length;
				}
			}

			long result = zfloat.SCALE_10000;
			long temp = 0;
			for (int i = 0; i < powDecompose.Length; ++i)
			{
				if (powDecompose[i] > 0)
				{
					temp = f_10000;
					for (int j = 0; j < powDecompose[i]; ++j)
					{
						temp = Sqrt(temp * 10000);
					}

					result *= temp;
					result /= zfloat.SCALE_10000;
				}
			}

			return result;
		}

		/// <summary>
		/// 计算 e 的 p 次幂。
		/// </summary>
		/// <param name="p">指数。</param>
		/// <returns>e^p。</returns>
		public static zfloat Exp(zfloat p)
		{
			return Pow(E, p);
		}

		/// <summary>
		/// 以p为底，f的对数
		/// 这是一个近似解，并且，当f的数值越大时，偏差就越大。
		/// 100以内还比较准，200以内凑合，可以通过增大LOG_ACCURACY提升精度。不过会有性能损失
		/// </summary>
		/// <param name="f">真数。</param>
		/// <param name="p">底数。</param>
		/// <returns>对数近似值。</returns>
		public static zfloat Log(zfloat f, zfloat p)
		{
			return Log(f) / Log(p);
		}

		protected const int LOG_ACCURACY = 30;
		/// <summary>
		/// 自然对数,这是一个近似解，并且，当f的数值越大时，偏差就越大。
		/// 100以内还比较准，200以内凑合，可以通过增大LOG_ACCURACY提升精度。不过会有性能损失
		/// 自然对数的泰勒级数：ln(1+x) = Σ(((-1)^(n+1)/n) * x^n)  n->∞
		///						ln(1-x) = Σ((-1/n) * x^n)   n->∞
		///					=>	lh((1+x)/(1-x)) = Σ((2/(2*k+1)) * x^(2*k+1)) = 2x * Σ((2/(2*k+1)) * x^(2*k)) k->∞
		/// </summary>
		/// <param name="f">真数。</param>
		/// <returns>自然对数近似值。</returns>
		public static zfloat Log(zfloat f)
		{
#if Z_DEBUG
		checked
		{
#endif
			zfloat x = (f - 1) / (1 + f);
			zfloat result = zfloat.FromRaw(0);
			for (int k = 0; k < LOG_ACCURACY; ++k)
			{
				zfloat temp = ((zfloat)(1)) / (2 * k + 1) * Pow((zfloat)x, (zfloat)2 * k);
				result += temp;
			}
			result *= 2 * x;

			return result;

#if Z_DEBUG
		}
#endif
		}

		/// <summary>
		/// 计算以 10 为底的对数近似值。
		/// </summary>
		/// <param name="f">真数。</param>
		/// <returns>log10(f) 近似值。</returns>
		public static zfloat Log10(zfloat f)
		{
			return Log(f) / LN_10;
		}


		/// <summary>
		/// 返回不小于输入值的最小整数（向上取整）。
		/// </summary>
		/// <param name="f">输入值。</param>
		/// <returns>向上取整结果。</returns>
		public static int Ceil(zfloat f)
		{
			int intf = (int)(f.value / zfloat.SCALE_10000);
            return (f.value - ((long)intf) * zfloat.SCALE_10000) > 0 ? (intf + 1) : intf;
		}

		/// <summary>
		/// 返回不大于输入值的最大整数（向下取整）。
		/// </summary>
		/// <param name="f">输入值。</param>
		/// <returns>向下取整结果。</returns>
		public static int Floor(zfloat f)
		{
			if (f.value > 0)
			{
				return (int)(f.value / zfloat.SCALE_10000);
			}
			else
			{
				int intf = (int)(f.value / zfloat.SCALE_10000);
				return ((long)intf) * zfloat.SCALE_10000 > f.value ? intf - 1 : intf;
			}
		}

		/// <summary>
		/// 四舍五入到整数。
		/// </summary>
		/// <param name="f">输入值。</param>
		/// <returns>取整结果。</returns>
		/// <remarks>与项目既有语义保持一致，0.5 按远离 0 方向处理。</remarks>
		public static int Round(zfloat f)
		{
			int s = (int)Sign(f);
			f = Abs(f);
			if ((int)(f - Half) < (int)(f))
			{
				return (int)f * s;
			}
			else
			{
				return (int)(f + 1) * s;
			}
		}

		/// <summary>
		/// 将定点数值限制在 [min, max] 区间内。
		/// </summary>
		/// <param name="value">输入值。</param>
		/// <param name="min">最小值。</param>
		/// <param name="max">最大值。</param>
		/// <returns>限制后的结果。</returns>
		public static zfloat Clamp(zfloat value, zfloat min, zfloat max)
		{
			if (value.value < min.value) 
                return min;
			if (value.value > max.value) 
                return max;
			return value;

		}
		/// <summary>
		/// 将长整数限制在 [min, max] 区间内。
		/// </summary>
		/// <param name="value">输入值。</param>
		/// <param name="min">最小值。</param>
		/// <param name="max">最大值。</param>
		/// <returns>限制后的结果。</returns>
		public static long Clamp(long value, long min, long max)
		{
			if (value < min)
				return min;
			if (value > max)
				return max;
			return value;

		}

		/// <summary>
		/// 将定点数值限制在 [0, 1] 区间内。
		/// </summary>
		/// <param name="value">输入值。</param>
		/// <returns>限制后的结果。</returns>
		public static zfloat Clamp01(zfloat value)
		{
            if (value.value < zfloat.Zero.value)
                return zfloat.Zero;
            if (value.value > zfloat.One.value)
                return zfloat.One;
            return value;
		}

		/// <summary>
		/// 在两个定点值之间进行线性插值。
		/// </summary>
		/// <param name="from">起点值。</param>
		/// <param name="to">终点值。</param>
		/// <param name="t">插值系数，通常取 [0, 1]。</param>
		/// <returns>插值结果。</returns>
		/// <remarks><paramref name="t"/> 超出 [0, 1] 时按线性外插处理。</remarks>
		public static zfloat Lerp(zfloat from, zfloat to, zfloat t)
		{
            zfloat result;
            result.value = LerpScale(from.value, to.value, t.value);
            return result;
		}

		/// <summary>
		/// 在两个整数之间进行线性插值。
		/// </summary>
		/// <param name="from">起点值。</param>
		/// <param name="to">终点值。</param>
		/// <param name="t">插值系数。</param>
		/// <returns>插值结果。</returns>
		/// <remarks><paramref name="t"/> 小于等于 0 返回 <paramref name="from"/>，大于等于 1 返回 <paramref name="to"/>。</remarks>
		public static int Lerp(int from, int to, zfloat t)
		{
			if (t >= 1)
				return to;
			else if (t <= 0)
				return from;
			return (int)(from + ((to - from) * t));

		}

        /// <summary>
        /// 用于内部优化使用的插值函数
        /// 传入的参数都是扩大了zfloat.SCALE_10000倍的
        /// 返回值也是扩大zfloat.SCALE_10000倍的
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        protected static long LerpScale(long from, long to, long t)
        {
            if (t >= zfloat.SCALE_10000)
                return to;
            else if (t <= 0)
                return from;
            return from + ((to - from) * t / zfloat.SCALE_10000);
        }

		/// <summary>
		/// 对输入值执行周期重复映射。
		/// </summary>
		/// <param name="t">输入值。</param>
		/// <param name="length">周期长度。</param>
		/// <returns>[0, length) 区间内的重复值。</returns>
		public static zfloat Repeat(zfloat t, zfloat length)
		{
			return t - zMathf.Floor(t / length) * length;
		}

		/// <summary>
		/// 对输入值执行 PingPong 映射。
		/// </summary>
		/// <param name="t">输入值。</param>
		/// <param name="length">半周期长度。</param>
		/// <returns>[0, length] 区间内往返变化的值。</returns>
		public static zfloat PingPong(zfloat t, zfloat length)
		{
			t = zMathf.Repeat(t, length * zfloat.Two);
			return length - zMathf.Abs(t - length);
		}

        /// <summary>
        /// 计算“定点 raw 值（放大 10000）”的平方根，并返回同样放大 10000 的结果。
        /// </summary>
        /// <param name="lx_10000">被开方的 raw 值（必须是放大 10000 的输入）。</param>
        /// <returns>平方根 raw 值（放大 10000）。</returns>
        /// <remarks>该接口用于内部性能优化路径。</remarks>
        public static long SqrtScale(long lx_10000)
        {
			if (lx_10000 > FastSqrtMinRaw && lx_10000 < FastSqrtMaxRaw)
			{
				return FastSqrt(lx_10000);
			}

            if (lx_10000 > SqrtScaleLargeInputThreshold)
            {
                // 这里明确调用 long 版 Sqrt，避免中间值先乘 10000 导致输入膨胀。
                lx_10000 = Sqrt(lx_10000) * zfloat.SCALE_100;
            }
            else
            {
                lx_10000 *= zfloat.SCALE_10000;
                lx_10000 = Sqrt(lx_10000);
            }
            return lx_10000;
        }

		/// <summary>
		/// 基于查表的快速平方根近似（输入区间约 0~100）。
		/// </summary>
		/// <param name="lx">raw 输入值。</param>
		/// <returns>平方根近似 raw 值。</returns>
		protected static long FastSqrt(long lx)
		{
			zfloat fi;
			fi.value = lx;
			int index = zMathf.Floor(fi);
			int index2 = zMathf.Ceil(fi);
			long result = zMathf.Lerp(zSqrtTable.SqrtTable100[index], zSqrtTable.SqrtTable100[index2], fi.GetFractionalPart()) * 10;
			return result;
		}

		protected static long Sqrt(long lx)
		{
			if (lx < 0)
			{
				return 0;
			}
			ulong x = (ulong)lx;
			ulong op, res, one;

			op = x;
			res = 0;

			one = 1UL << (64 - 2);
			while (one > op)
				one >>= 2;

			while (one != 0)
			{
				if (op >= res + one)
				{
					op = op - (res + one);
					res = res + 2 * one;
				}
				res >>= 1;
				one >>= 2;
			}
			return (long)res;
		}


		/// <summary>
		/// 返回 f 的符号。
		/// </summary>
		/// <param name="f">输入值。</param>
		/// <returns>当 f 为正或为 0 返回 1，为负返回 -1。</returns>
		/// <remarks>按历史语义，0 的符号返回 1。</remarks>
		public static int Sign(zfloat f)
		{
			if (f.value >= 0)
			{
				return 1;
			}
			return -1;
		}

		internal static int SignInt(long f)
		{
			if (f >= 0)
			{
				return 1;
			}
			return -1;
		}

		/// <summary>
		/// 求三角形斜边近似值查表的表长度
		/// </summary>
		const int TanApproValueTableLen = 901;

		/// <summary>
		/// 这是用于查表求三角形斜边的近似值
		/// 假设短边长度为100，长边长度为100~1000
		/// 长边大于1000的，则认为是1000
		/// 使用长边的长度的整数部分进行查表索引
		/// </summary>
		static long[] TanApproValue = new long[]{
		1414200,1421300,1428400,1435500,1442700,1450000,1457200,1464500,1471800,1479200,1486600,1494000,1501400,1508900,1516400,1523900,
		1531500,1539100,1546700,1554300,1562000,1569700,1577400,1585200,1592900,1600700,1608600,1616400,1624300,1632200,1640100,1648000,
		1656000,1664000,1672000,1680000,1688000,1696100,1704200,1712300,1720400,1728600,1736700,1744900,1753100,1761300,1769600,1777800,
		1786100,1794400,1802700,1811100,1819400,1827800,1836100,1844500,1852900,1861400,1869800,1878300,1886700,1895200,1903700,1912300,
		1920800,1929300,1937900,1946500,1955000,1963600,1972300,1980900,1989500,1998200,2006800,2015500,2024200,2032900,2041600,2050300,
		2059100,2067800,2076600,2085400,2094100,2102900,2111700,2120500,2129400,2138200,2147000,2155900,2164800,2173600,2182500,2191400,
		2200300,2209200,2218100,2227100,2236000,2245000,2253900,2262900,2271900,2280800,2289800,2298800,2307800,2316900,2325900,2334900,
		2344000,2353000,2362100,2371100,2380200,2389300,2398400,2407500,2416600,2425700,2434800,2443900,2453000,2462200,2471300,2480500,
		2489600,2498800,2507900,2517100,2526300,2535500,2544700,2553900,2563100,2572300,2581500,2590700,2600000,2609200,2618400,2627700,
		2636900,2646200,2655400,2664700,2674000,2683300,2692500,2701800,2711100,2720400,2729700,2739000,2748300,2757600,2767000,2776300,
		2785600,2795000,2804300,2813600,2823000,2832400,2841700,2851100,2860400,2869800,2879200,2888600,2897900,2907300,2916700,2926100,
		2935500,2944900,2954300,2963700,2973200,2982600,2992000,3001400,3010900,3020300,3029700,3039200,3048600,3058100,3067500,3077000,
		3086400,3095900,3105400,3114800,3124300,3133800,3143300,3152700,3162200,3171700,3181200,3190700,3200200,3209700,3219200,3228700,
		3238200,3247700,3257200,3266800,3276300,3285800,3295300,3304900,3314400,3323900,3333500,3343000,3352600,3362100,3371700,3381200,
		3390800,3400300,3409900,3419400,3429000,3438600,3448100,3457700,3467300,3476900,3486400,3496000,3505600,3515200,3524800,3534400,
		3544000,3553600,3563200,3572800,3582400,3592000,3601600,3611200,3620800,3630400,3640000,3649600,3659200,3668900,3678500,3688100,
		3697700,3707400,3717000,3726600,3736300,3745900,3755500,3765200,3774800,3784500,3794100,3803800,3813400,3823100,3832700,3842400,
		3852000,3861700,3871300,3881000,3890700,3900300,3910000,3919700,3929300,3939000,3948700,3958300,3968000,3977700,3987400,3997100,
		4006700,4016400,4026100,4035800,4045500,4055200,4064900,4074600,4084300,4094000,4103700,4113400,4123100,4132800,4142500,4152200,
		4161900,4171600,4181300,4191000,4200700,4210400,4220100,4229900,4239600,4249300,4259000,4268700,4278500,4288200,4297900,4307600,
		4317400,4327100,4336800,4346500,4356300,4366000,4375700,4385500,4395200,4405000,4414700,4424400,4434200,4443900,4453700,4463400,
		4473200,4482900,4492700,4502400,4512200,4521900,4531700,4541400,4551200,4560900,4570700,4580400,4590200,4600000,4609700,4619500,
		4629200,4639000,4648800,4658500,4668300,4678100,4687800,4697600,4707400,4717200,4726900,4736700,4746500,4756300,4766000,4775800,
		4785600,4795400,4805200,4814900,4824700,4834500,4844300,4854100,4863900,4873600,4883400,4893200,4903000,4912800,4922600,4932400,
		4942200,4952000,4961800,4971600,4981400,4991200,5000900,5010700,5020500,5030300,5040100,5050000,5059800,5069600,5079400,5089200,
		5099000,5108800,5118600,5128400,5138200,5148000,5157800,5167600,5177400,5187300,5197100,5206900,5216700,5226500,5236300,5246100,
		5256000,5265800,5275600,5285400,5295200,5305100,5314900,5324700,5334500,5344300,5354200,5364000,5373800,5383600,5393500,5403300,
		5413100,5422900,5432800,5442600,5452400,5462300,5472100,5481900,5491800,5501600,5511400,5521300,5531100,5540900,5550800,5560600,
		5570400,5580300,5590100,5600000,5609800,5619600,5629500,5639300,5649200,5659000,5668800,5678700,5688500,5698400,5708200,5718100,
		5727900,5737800,5747600,5757500,5767300,5777200,5787000,5796900,5806700,5816600,5826400,5836300,5846100,5856000,5865800,5875700,
		5885500,5895400,5905200,5915100,5924900,5934800,5944700,5954500,5964400,5974200,5984100,5994000,6003800,6013700,6023500,6033400,
		6043300,6053100,6063000,6072800,6082700,6092600,6102400,6112300,6122200,6132000,6141900,6151800,6161600,6171500,6181400,6191200,
		6201100,6211000,6220900,6230700,6240600,6250500,6260300,6270200,6280100,6290000,6299800,6309700,6319600,6329400,6339300,6349200,
		6359100,6368900,6378800,6388700,6398600,6408500,6418300,6428200,6438100,6448000,6457800,6467700,6477600,6487500,6497400,6507200,
		6517100,6527000,6536900,6546800,6556700,6566500,6576400,6586300,6596200,6606100,6616000,6625800,6635700,6645600,6655500,6665400,
		6675300,6685200,6695100,6704900,6714800,6724700,6734600,6744500,6754400,6764300,6774200,6784100,6793900,6803800,6813700,6823600,
		6833500,6843400,6853300,6863200,6873100,6883000,6892900,6902800,6912700,6922600,6932500,6942300,6952200,6962100,6972000,6981900,
		6991800,7001700,7011600,7021500,7031400,7041300,7051200,7061100,7071000,7080900,7090800,7100700,7110600,7120500,7130400,7140300,
		7150200,7160100,7170000,7179900,7189800,7199700,7209600,7219500,7229400,7239300,7249300,7259200,7269100,7279000,7288900,7298800,
		7308700,7318600,7328500,7338400,7348300,7358200,7368100,7378000,7387900,7397800,7407800,7417700,7427600,7437500,7447400,7457300,
		7467200,7477100,7487000,7496900,7506900,7516800,7526700,7536600,7546500,7556400,7566300,7576200,7586100,7596100,7606000,7615900,
		7625800,7635700,7645600,7655500,7665500,7675400,7685300,7695200,7705100,7715000,7724900,7734900,7744800,7754700,7764600,7774500,
		7784400,7794400,7804300,7814200,7824100,7834000,7844000,7853900,7863800,7873700,7883600,7893500,7903500,7913400,7923300,7933200,
		7943100,7953100,7963000,7972900,7982800,7992800,8002700,8012600,8022500,8032400,8042400,8052300,8062200,8072100,8082100,8092000,
		8101900,8111800,8121700,8131700,8141600,8151500,8161400,8171400,8181300,8191200,8201100,8211100,8221000,8230900,8240800,8250800,
		8260700,8270600,8280600,8290500,8300400,8310300,8320300,8330200,8340100,8350000,8360000,8369900,8379800,8389800,8399700,8409600,
		8419500,8429500,8439400,8449300,8459300,8469200,8479100,8489100,8499000,8508900,8518800,8528800,8538700,8548600,8558600,8568500,
		8578400,8588400,8598300,8608200,8618200,8628100,8638000,8648000,8657900,8667800,8677800,8687700,8697600,8707600,8717500,8727400,
		8737400,8747300,8757200,8767200,8777100,8787000,8797000,8806900,8816800,8826800,8836700,8846600,8856600,8866500,8876500,8886400,
		8896300,8906300,8916200,8926100,8936100,8946000,8956000,8965900,8975800,8985800,8995700,9005600,9015600,9025500,9035500,9045400,
		9055300,9065300,9075200,9085200,9095100,9105000,9115000,9124900,9134900,9144800,9154700,9164700,9174600,9184600,9194500,9204400,
		9214400,9224300,9234300,9244200,9254100,9264100,9274000,9284000,9293900,9303800,9313800,9323700,9333700,9343600,9353600,9363500,
		9373400,9383400,9393300,9403300,9413200,9423200,9433100,9443000,9453000,9462900,9472900,9482800,9492800,9502700,9512700,9522600,
		9532500,9542500,9552400,9562400,9572300,9582300,9592200,9602200,9612100,9622100,9632000,9641900,9651900,9661800,9671800,9681700,
		9691700,9701600,9711600,9721500,9731500,9741400,9751400,9761300,9771300,9781200,9791200,9801100,9811000,9821000,9830900,9840900,
		9850800,9860800,9870700,9880700,9890600,9900600,9910500,9920500,9930400,9940400,9950300,9960300,9970200,9980200,9990100,10000100,
		10010000,10020000,10029900,10039900,10049800,
		};
		/// <summary>
		/// 计算二维直角三角形斜边长度近似值。
		/// </summary>
		/// <param name="__a">直角边 a。</param>
		/// <param name="__b">直角边 b。</param>
		/// <returns>斜边长度近似值。</returns>
		/// <remarks>为性能选择查表近似，存在可预期误差，不保证与精确开方完全一致。</remarks>
		public static zfloat ApproximateHypotenuse(zfloat __a, zfloat __b)
		{
			__a.value = __a.value >= 0 ? __a.value : -__a.value;
			__b.value = __b.value >= 0 ? __b.value : -__b.value;
			//保证a为短边
			if (__a.value > __b.value)
			{
				long c = __b.value;
				__b.value = __a.value;
				__a.value = c;
			}
			if (__a.value == 0)
			{
				return __b;
			}

			zfloat scale;
			scale.value = zfloat.Hundred.value * zfloat.SCALE_10000/ __a.value;

			__b.value = __b.value * scale.value / zfloat.SCALE_10000;
			int index = (int)(__b.value / zfloat.SCALE_10000 - 100);
			if (index >= TanApproValueTableLen)
			{
				__b.value = __b.value * zfloat.SCALE_10000 / scale.value;
				return __b;
			}
			if (index < 0)
			{
				index = 0;
			}
			__b.value = TanApproValue[index];
			__b.value = __b.value * zfloat.SCALE_10000 / scale.value;
			return __b;
		}
	}
}
