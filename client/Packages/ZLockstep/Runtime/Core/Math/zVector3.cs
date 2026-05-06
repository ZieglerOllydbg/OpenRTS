using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace zUnity
{
	/// <summary>
	/// 三维向量结构体，用于确定性计算
	/// 包含x、y、z三个分量，每个分量都是zfloat类型
	/// 提供了向量运算的各种方法，如加减乘除、点积、叉积、归一化等
	/// </summary>
	[Serializable]
	public struct zVector3 : ISerializable, IEquatable<zVector3>
	{
		public static readonly zVector3 zero = new zVector3((zfloat)0, (zfloat)0, (zfloat)0);
		public static readonly zVector3 one = new zVector3((zfloat)1, (zfloat)1, (zfloat)1);
		public static readonly zVector3 forward = new zVector3((zfloat)0, (zfloat)0, (zfloat)1);
		public static readonly zVector3 back = new zVector3((zfloat)0, (zfloat)0, (zfloat)(-1));
		public static readonly zVector3 up = new zVector3((zfloat)0, (zfloat)1, (zfloat)0);
		public static readonly zVector3 down = new zVector3((zfloat)0, (zfloat)(-1), (zfloat)0);
		public static readonly zVector3 left = new zVector3((zfloat)(-1), (zfloat)0, (zfloat)0);
		public static readonly zVector3 right = new zVector3((zfloat)1, (zfloat)0, (zfloat)0);

		/// <summary>
		/// 历史兼容哨兵值。新代码应优先使用显式状态而非该常量。
		/// </summary>
		public static readonly zVector3 NULL = new zVector3((zfloat)(-999999), (zfloat)(-999999), (zfloat)(-999999));

		public bool IsZero()
		{
			return this.x.value == 0 && this.y.value == 0 && this.z.value == 0;
		}

		public zVector3(zfloat x, zfloat y, zfloat z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public zVector3(int x, int y, int z)
		{
			this.x = (zfloat)x;
			this.y = (zfloat)y;
			this.z = (zfloat)z;
		}

		public zVector3(zVector3 vec)
		{
			this.x = vec.x;
			this.y = vec.y;
			this.z = vec.z;
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
		/// Z轴坐标分量
		/// </summary>
		[JsonProperty]
		public zfloat z;

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
				else if (index == 2)
				{
					return z;
				}
				else
				{
					throw new IndexOutOfRangeException("zVector3 Only Contains x,y,z，so the index must use 0,1 or 2 !");
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
				else if (index == 2)
				{
					z = value;
				}
				else
				{
					throw new IndexOutOfRangeException("zVector3 Only Contains x,y,z，so the index must use 0,1 or 2 !");
				}
			}
		}



		public void Add(ref zVector3 vec)
		{
			x.value += vec.x.value;
			y.value += vec.y.value;
			z.value += vec.z.value;
		}

		public void Sub(ref zVector3 vec)
		{
			x.value -= vec.x.value;
			y.value -= vec.y.value;
			z.value -= vec.z.value;
		}

		public void Mul(ref zVector3 vec)
		{
			x.value = x.value * vec.x.value / zfloat.SCALE_10000;
			y.value = y.value * vec.y.value / zfloat.SCALE_10000;
			z.value = z.value * vec.z.value / zfloat.SCALE_10000;
		}

		public void Div(ref zVector3 vec)
		{
			x.value = x.value * zfloat.SCALE_10000 / vec.x.value;
			y.value = y.value * zfloat.SCALE_10000 / vec.y.value;
			z.value = z.value * zfloat.SCALE_10000 / vec.z.value;
		}

		//	private zVector3 normalized;

		/// <summary>
		/// 获取归一化向量，注意每次调用，都会重新归一化。
		/// </summary>
		[JsonIgnore]
		public zVector3 normalized
		{
			get
			{
				return zVector3.Normalize(ref this);
			}
		}

		public zVector3 GetNormalizedForMagnitude(zfloat __magnitude)
		{
			zVector3 vec = this;

			if (__magnitude > 0)
			{
				vec.x.value = vec.x.value * zfloat.SCALE_10000 / __magnitude.value;
				vec.y.value = vec.y.value * zfloat.SCALE_10000 / __magnitude.value;
				vec.z.value = vec.z.value * zfloat.SCALE_10000 / __magnitude.value;
				return vec;
			}
			else
			{
				return zVector3.zero;
			}
		}

		[JsonIgnore]
		public zVector3 approxNormalizedXZ
		{
			get
			{
				zVector3 vec;
				vec.y.value = 0;

				zfloat xzLen = zMathf.ApproximateHypotenuse(x, z);
				if (xzLen.value == 0)
				{
					return zVector3.zero;
				}
				vec.x.value = x.value * zfloat.SCALE_10000 / xzLen.value;
				vec.z.value = z.value * zfloat.SCALE_10000 / xzLen.value;

				return vec;
			}
		}

		public zVector3 GetApproxNormalizedXZForMagnitude(zfloat __magnitude)
		{
			zVector3 vec;
			vec.y.value = 0;

			//zfloat xzLen = zMathf.ApproximateHypotenuse(x, z);
			if (__magnitude.value == 0)
			{
				return zVector3.zero;
			}
			vec.x.value = x.value * zfloat.SCALE_10000 / __magnitude.value;
			vec.z.value = z.value * zfloat.SCALE_10000 / __magnitude.value;

			return vec;
		}

		/// <summary>
		/// 向量长度的平方
		/// </summary>
		public zfloat sqrMagnitude
		{
			get
			{
				return zVector3.SqrMagnitude(ref this);
			}
		}


		public zfloat magnitude
		{
			get { return zVector3.Magnitude(ref this); }

		}

		/// <summary>
		/// 归一化
		/// </summary>
		public void Normalize()
		{
			long num = (x.value * x.value + y.value * y.value + z.value * z.value) / zfloat.SCALE_10000;
			num = zMathf.SqrtScale(num);

			if (num > 0)
			{
				x.value = x.value * zfloat.SCALE_10000 / num;
				y.value = y.value * zfloat.SCALE_10000 / num;
				z.value = z.value * zfloat.SCALE_10000 / num;
			}
			else
			{
				x.value = 0;
				y.value = 0;
				z.value = 0;
			}
		}

		/// <summary>
		/// 归一化
		/// </summary>
		/// <param name="vec">ref传入只是为了节省性能，不会修改vec的值</param>
		/// <returns></returns>
		public static zVector3 Normalize(ref zVector3 __vec)
		{
			zVector3 vec = __vec;
			long num = (vec.x.value * vec.x.value + vec.y.value * vec.y.value + vec.z.value * vec.z.value) / zfloat.SCALE_10000;
			num = zMathf.SqrtScale(num);

			if (num > 0)
			{
				vec.x.value = vec.x.value * zfloat.SCALE_10000 / num;
				vec.y.value = vec.y.value * zfloat.SCALE_10000 / num;
				vec.z.value = vec.z.value * zfloat.SCALE_10000 / num;
				return vec;
			}
			else
			{
				return zVector3.zero;
			}
		}

		/// <summary>
		/// 获取向量长度（模）。
		/// </summary>
		/// <param name="v3">待计算向量。</param>
		/// <returns>向量长度。</returns>
		public static zfloat Magnitude(ref zVector3 v3)
		{
			long sq = (v3.x.value * v3.x.value + v3.y.value * v3.y.value + v3.z.value * v3.z.value) / zfloat.SCALE_10000;
			zfloat result;
			result.value = zMathf.SqrtScale(sq);
			return result;
		}

		/// <summary>
		/// 将向量按照scale进行缩放。即当前向量与scale对应位相乘
		/// </summary>
		/// <param name="scale"></param>
		public void Scale(zVector3 scale)
		{
			x.value = x.value * scale.x.value / zfloat.SCALE_10000;
			y.value = y.value * scale.y.value / zfloat.SCALE_10000;
			z.value = z.value * scale.z.value / zfloat.SCALE_10000;
		}

		/// <summary>
		/// 向量a长度的平方
		/// </summary>
		/// <param name="a"></param>
		/// <returns></returns>
		public static zfloat SqrMagnitude(ref zVector3 a)
		{
			zfloat f;
			f.value = (a.x.value * a.x.value + a.y.value * a.y.value + a.z.value * a.z.value) / zfloat.SCALE_10000;
			return f;
		}

		/// <summary>
		/// 获取向量 A 旋转到向量 B 的有向角（单位：度，范围约 [-180, 180]）。
		/// 旋转方向以 Y 轴分量的右手系符号判定。
		/// </summary>
		public static zfloat A2B_angle(zVector3 A, zVector3 B)
		{
			if (A.IsZero() || B.IsZero())
			{
				return zfloat.Zero;
			}

			A.Normalize();
			B.Normalize();
			zfloat dot = zMathf.Clamp(zVector3.Dot(ref A, ref B), zfloat.NegativeOne, zfloat.One);
			zfloat angle = zMathf.Acos(dot) * zMathf.Rad2Deg;
			if (zVector3.Cross(A, B).y.value < 0)
			{
				angle = -angle;
			}
			return angle;
		}

		/// <summary>
		/// 按照数字t在from到to之间插值。
		/// value = from + t * to
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="t">应在0~1之间，超过该范围，会自动规范到该范围</param>
		/// <returns></returns>
		public static zVector3 Lerp(zVector3 from, zVector3 to, zfloat t)
		{
			t = zMathf.Clamp01(t);
			return new zVector3(from.x + (to.x - from.x) * t, from.y + (to.y - from.y) * t, from.z + (to.z - from.z) * t);
		}

		/// <summary>
		/// 球面线性插值。方向按球面插值，长度按线性插值。
		/// </summary>
		/// <remarks>
		/// 对零向量、近平行和近反向场景做了稳定性退化处理，避免除零和数值抖动。
		/// </remarks>
		public static zVector3 Slerp(zVector3 from, zVector3 to, zfloat t)
		{
			t = zMathf.Clamp01(t);
			if (from.IsZero() || to.IsZero())
			{
				return Lerp(from, to, t);
			}

			zfloat fromMag = from.magnitude;
			zfloat toMag = to.magnitude;
			zVector3 fromDir = from / fromMag;
			zVector3 toDir = to / toMag;
			zfloat dot = zMathf.Clamp(Dot(ref fromDir, ref toDir), zfloat.NegativeOne, zfloat.One);

			// 近平行场景直接退化到线性插值，避免 sin(theta) 过小导致抖动。
			if (dot.Approximately(zfloat.One, zfloat.FromRaw(10)))
			{
				return Lerp(from, to, t);
			}

			// 近反向时构造稳定正交方向，避免插值平面不确定。
			if (dot.Approximately(zfloat.NegativeOne, zfloat.FromRaw(10)))
			{
				zVector3 ortho = GetAnyPerpendicularNormalized(fromDir);
				zfloat thetaPi = zMathf.PI * t;
				zVector3 dirOpposite = fromDir * zMathf.Cos(thetaPi) + ortho * zMathf.Sin(thetaPi);
				zfloat magOpposite = zMathf.Lerp(fromMag, toMag, t);
				return dirOpposite.normalized * magOpposite;
			}

			zfloat theta = zMathf.Acos(dot);
			zfloat sinTheta = zMathf.Sin(theta);
			if (sinTheta.Approximately(zfloat.Zero, zfloat.FromRaw(10)))
			{
				return Lerp(from, to, t);
			}

			zfloat wFrom = zMathf.Sin((zfloat.One - t) * theta) / sinTheta;
			zfloat wTo = zMathf.Sin(t * theta) / sinTheta;
			zVector3 dir = (fromDir * wFrom + toDir * wTo).normalized;
			zfloat mag = zMathf.Lerp(fromMag, toMag, t);
			return dir * mag;
		}

		/// <summary>
		/// 正交化并归一化两个向量。
		/// </summary>
		/// <remarks>
		/// 输出满足：normal 与 tangent 均为单位向量，且 tangent 垂直于 normal。
		/// 当输入为零向量或近平行时，函数会自动选择稳定回退方向。
		/// </summary>
		public static void OrthoNormalize(ref zVector3 normal, ref zVector3 tangent)
		{
			if (normal.IsZero())
			{
				normal = tangent.IsZero() ? zVector3.forward : tangent.normalized;
			}
			else
			{
				normal.Normalize();
			}

			tangent = tangent - Project(tangent, normal);
			if (tangent.IsZero())
			{
				tangent = GetAnyPerpendicularNormalized(normal);
			}
			else
			{
				tangent.Normalize();
				// 定点量化会在第一次归一化后留下微小平行分量，二次正交化可稳定收敛到更小误差。
				tangent = tangent - Project(tangent, normal);
				if (tangent.IsZero())
				{
					tangent = GetAnyPerpendicularNormalized(normal);
				}
				else
				{
					tangent.Normalize();
				}
			}
		}

		/// <summary>
		/// 当前的地点移向目标
		/// 这个函数基本上和Vector3.Lerp相同，而是该函数将确保我们的速度不会超过maxDistanceDelta。
		/// maxDistanceDelta的负值从目标推开向量，就是说maxDistanceDelta是正值，当前地点移向目标，如果是负值当前地点将远离目标。
		/// </summary>
		/// <param name="current"></param>
		/// <param name="target"></param>
		/// <param name="maxDistanceDelta"></param>
		/// <returns></returns>
		public static zVector3 MoveTowards(zVector3 current, zVector3 target, zfloat maxDistanceDelta)
		{
			zVector3 a = target - current;
			zfloat magnitude = a.magnitude;
			if (magnitude <= maxDistanceDelta || magnitude == zfloat.Zero)
			{
				return target;
			}
			return current + a / magnitude * maxDistanceDelta;
		}

		/// <summary>
		/// 兼容历史签名：仅限制长度变化，不限制角速度。
		/// </summary>
		public static zVector3 RotateTowards(zVector3 current, zVector3 target, zfloat maxMagnitudeDelta)
		{
			return RotateTowards(current, target, zfloat.MaxValue, maxMagnitudeDelta);
		}

		/// <summary>
		/// 使向量从 current 旋转并逼近到 target。
		/// </summary>
		/// <param name="current">当前向量。</param>
		/// <param name="target">目标向量。</param>
		/// <param name="maxRadiansDelta">本次允许的最大旋转弧度。</param>
		/// <param name="maxMagnitudeDelta">本次允许的最大长度变化。</param>
		/// <returns>旋转与缩放后的新向量。</returns>
		public static zVector3 RotateTowards(zVector3 current, zVector3 target, zfloat maxRadiansDelta, zfloat maxMagnitudeDelta)
		{
			zfloat currentMag = current.magnitude;
			zfloat targetMag = target.magnitude;
			zfloat magDelta = targetMag - currentMag;
			zfloat absMaxMagDelta = zMathf.Abs(maxMagnitudeDelta);
			zfloat newMag;
			if (zMathf.Abs(magDelta) <= absMaxMagDelta)
			{
				newMag = targetMag;
			}
			else
			{
				newMag = currentMag + (magDelta > zfloat.Zero ? absMaxMagDelta : -absMaxMagDelta);
			}

			if (currentMag == zfloat.Zero && targetMag == zfloat.Zero)
			{
				return zVector3.zero;
			}

			if (currentMag == zfloat.Zero)
			{
				return target.normalized * newMag;
			}

			zVector3 currentDir = current / currentMag;
			zVector3 targetDir = targetMag == zfloat.Zero ? currentDir : target / targetMag;
			zfloat angleDeg = Angle(currentDir, targetDir);
			if (angleDeg == zfloat.Zero)
			{
				return currentDir * newMag;
			}

			zfloat absMaxRadiansDelta = zMathf.Abs(maxRadiansDelta);
			if (absMaxRadiansDelta >= zMathf.PI)
			{
				return targetDir * newMag;
			}

			zfloat maxAngleDeg = absMaxRadiansDelta * zMathf.Rad2Deg;
			zfloat t = zMathf.Clamp01(maxAngleDeg / angleDeg);
			zVector3 rotatedDir = Slerp(currentDir, targetDir, t).normalized;
			return rotatedDir * newMag;
		}

		/// <summary>
		/// 随着时间的推移，逐渐改变一个向量朝向预期的目标。
		/// 该实现与 Unity 同名 API 的行为保持一致。
		/// </summary>
		/// <param name="current"></param>
		/// <param name="target"></param>
		/// <param name="currentVelocity"></param>
		/// <param name="smoothTime"></param>
		/// <param name="maxSpeed"></param>
		/// <param name="deltaTime"></param>
		/// <returns></returns>
		public static zVector3 SmoothDamp(zVector3 current, zVector3 target, ref zVector3 currentVelocity, zfloat smoothTime, zfloat maxSpeed, zfloat deltaTime)
		{
			smoothTime = zMathf.Max(new zfloat(0, 1), smoothTime);
			zfloat num = zfloat.Two / smoothTime;
			zfloat num2 = num * deltaTime;
			zfloat d = zfloat.One / (zfloat.One + num2 + new zfloat(0, 4800) * num2 * num2 + new zfloat(0, 2350) * num2 * num2 * num2);
			zVector3 vector = current - target;
			zVector3 vector2 = target;
			zfloat maxLength = maxSpeed * smoothTime;
			vector = zVector3.ClampMagnitude(vector, maxLength);
			target = current - vector;
			zVector3 vector3 = (currentVelocity + num * vector) * deltaTime;
			currentVelocity = (currentVelocity - num * vector3) * d;
			zVector3 vector4 = target + (vector + vector3) * d;
			zVector3 v2ToCurr = vector2 - current;
			zVector3 v4Tov2 = vector4 - vector2;
			if (zVector3.Dot(ref v2ToCurr, ref v4Tov2) > zfloat.Zero)
			{
				vector4 = vector2;
				currentVelocity = (vector4 - vector2) / deltaTime;
			}
			return vector4;
		}

		/// <summary>
		/// 两个向量按分量相乘。
		/// </summary>
		public static zVector3 Scale(zVector3 a, zVector3 b)
		{
			zVector3 vec;
			vec.x.value = a.x.value * b.x.value / zfloat.SCALE_10000;
			vec.y.value = a.y.value * b.y.value / zfloat.SCALE_10000;
			vec.z.value = a.z.value * b.z.value / zfloat.SCALE_10000;
			return vec;
		}


		/// <summary>
		/// 叉乘  向量积
		/// </summary>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static zVector3 Cross(zVector3 lhs, zVector3 rhs)
		{
			zVector3 vec;
			vec.x.value = (lhs.y.value * rhs.z.value - lhs.z.value * rhs.y.value) / zfloat.SCALE_10000;
			vec.y.value = (lhs.z.value * rhs.x.value - lhs.x.value * rhs.z.value) / zfloat.SCALE_10000;
			vec.z.value = (lhs.x.value * rhs.y.value - lhs.y.value * rhs.x.value) / zfloat.SCALE_10000;

			return vec;
		}

		/// <summary>
		/// 反射向量
		/// </summary>
		/// <param name="inDir"></param>
		/// <param name="inNormal"></param>
		/// <returns></returns>
		public static zVector3 Reflect(zVector3 inDir, zVector3 inNormal)
		{
			long lTempNum;
			zVector3 vec;
			lTempNum = -2 * zVector3.Dot(ref inNormal, ref inDir).value;
			vec.x.value = lTempNum * inNormal.x.value / zfloat.SCALE_10000 + inDir.x.value;
			vec.y.value = lTempNum * inNormal.y.value / zfloat.SCALE_10000 + inDir.y.value;
			vec.z.value = lTempNum * inNormal.z.value / zfloat.SCALE_10000 + inDir.z.value;
			return vec;
		}

		/// <summary>
		/// 点积  数量积
		/// </summary>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static zfloat Dot(ref zVector3 lhs, ref zVector3 rhs)
		{
			zfloat num;
			num.value = (lhs.x.value * rhs.x.value + lhs.y.value * rhs.y.value + lhs.z.value * rhs.z.value) / zfloat.SCALE_10000;
			return num;
		}

		/// <summary>
		/// 点积（非 ref 重载，便于调用端使用）。
		/// </summary>
		public static zfloat Dot(zVector3 lhs, zVector3 rhs)
		{
			return Dot(ref lhs, ref rhs);
		}

		public void Set(zfloat new_x, zfloat new_y, zfloat new_z)
		{
			this.x.value = new_x.value;
			this.y.value = new_y.value;
			this.z.value = new_z.value;
		}


		/// <summary>
		/// 将 <paramref name="vector"/> 投影到 <paramref name="onNormal"/> 方向上。
		/// </summary>
		public static zVector3 Project(zVector3 vector, zVector3 onNormal)
		{
			long lNorDotNor = (onNormal.x.value * onNormal.x.value + onNormal.y.value * onNormal.y.value + onNormal.z.value * onNormal.z.value) / zfloat.SCALE_10000;
			if (lNorDotNor == 0)
			{
				return zVector3.zero;
			}
			long lVecDotNor = (vector.x.value * onNormal.x.value + vector.y.value * onNormal.y.value + vector.z.value * onNormal.z.value) / zfloat.SCALE_10000;
			onNormal.x.value = onNormal.x.value * lVecDotNor / lNorDotNor;
			onNormal.y.value = onNormal.y.value * lVecDotNor / lNorDotNor;
			onNormal.z.value = onNormal.z.value * lVecDotNor / lNorDotNor;
			return onNormal;

		}

		/// <summary>
		/// 获取两个向量夹角（单位：度，范围 [0, 180]）。
		/// </summary>
		public static zfloat Angle(zVector3 from, zVector3 to)
		{
			if (from.IsZero() || to.IsZero())
			{
				return zfloat.Zero;
			}

			from.Normalize();
			to.Normalize();
			return zMathf.Acos(zMathf.Clamp(zVector3.Dot(ref from, ref to), zfloat.NegativeOne, zfloat.One)) * zMathf.Rad2Deg;
		}

		/// <summary>
		/// 两点间距离
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static zfloat Distance(zVector3 a, zVector3 b)
		{
			//  zVector3 vector = a - b;
			// return zMathf.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);

			zVector3 vector;
			vector.x.value = a.x.value - b.x.value;
			vector.y.value = a.y.value - b.y.value;
			vector.z.value = a.z.value - b.z.value;
			long lNum = (vector.x.value * vector.x.value + vector.y.value * vector.y.value + vector.z.value * vector.z.value) / zfloat.SCALE_10000;

			lNum = zMathf.SqrtScale(lNum);

			//if (lNum > 100000000)
			//{
			//    lNum = zMathf.Sqrt(lNum) * zfloat.SCALE_100;
			//}
			//else
			//{
			//    lNum *= zfloat.SCALE_10000;
			//    lNum = zMathf.Sqrt(lNum);
			//}
			zfloat fNum;
			fNum.value = lNum;
			return fNum;
		}

		/// <summary>
		/// 限制向量长度，如果长度超过maxLength则截取，否则原样返回
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="maxLength"></param>
		/// <returns></returns>
		public static zVector3 ClampMagnitude(zVector3 vector, zfloat maxLength)
		{
			if (vector.sqrMagnitude > maxLength * maxLength)
			{
				return vector.normalized * maxLength;
			}
			return vector;
		}

		public static zVector3 Min(zVector3 lhs, zVector3 rhs)
		{
			return new zVector3(zMathf.Min(lhs.x, rhs.x), zMathf.Min(lhs.y, rhs.y), zMathf.Min(lhs.z, rhs.z));
		}

		public static zVector3 Max(zVector3 lhs, zVector3 rhs)
		{
			return new zVector3(zMathf.Max(lhs.x, rhs.x), zMathf.Max(lhs.y, rhs.y), zMathf.Max(lhs.z, rhs.z));
		}

		#region 加法
		public static zVector3 operator +(zVector3 lhs, zVector3 rhs)
		{
			zVector3 vec;
			vec.x.value = lhs.x.value + rhs.x.value;
			vec.y.value = lhs.y.value + rhs.y.value;
			vec.z.value = lhs.z.value + rhs.z.value;
			return vec;
		}

		public static zVector3 operator +(int lhs, zVector3 rhs)
		{
			zVector3 vec;
			vec.x.value = lhs * zfloat.SCALE_10000 + rhs.x.value;
			vec.y.value = lhs * zfloat.SCALE_10000 + rhs.y.value;
			vec.z.value = lhs * zfloat.SCALE_10000 + rhs.z.value;
			return vec;
		}

		public static zVector3 operator +(zVector3 lhs, int rhs)
		{
			zVector3 vec;
			vec.x.value = lhs.x.value + rhs * zfloat.SCALE_10000;
			vec.y.value = lhs.y.value + rhs * zfloat.SCALE_10000;
			vec.z.value = lhs.z.value + rhs * zfloat.SCALE_10000;
			return vec;
		}

		public static zVector3 operator +(zfloat lhs, zVector3 rhs)
		{
			zVector3 vec;
			vec.x.value = lhs.value + rhs.x.value;
			vec.y.value = lhs.value + rhs.y.value;
			vec.z.value = lhs.value + rhs.z.value;
			return vec;
		}

		public static zVector3 operator +(zVector3 lhs, zfloat rhs)
		{
			zVector3 vec;
			vec.x.value = lhs.x.value + rhs.value;
			vec.y.value = lhs.y.value + rhs.value;
			vec.z.value = lhs.z.value + rhs.value;
			return vec;
		}
		#endregion

		#region 减法
		public static zVector3 operator -(zVector3 lhs, zVector3 rhs)
		{
			zVector3 vec;
			vec.x.value = lhs.x.value - rhs.x.value;
			vec.y.value = lhs.y.value - rhs.y.value;
			vec.z.value = lhs.z.value - rhs.z.value;
			return vec;
		}
		public static zVector3 operator -(int lhs, zVector3 rhs)
		{
			zVector3 vec;
			vec.x.value = lhs * zfloat.SCALE_10000 - rhs.x.value;
			vec.y.value = lhs * zfloat.SCALE_10000 - rhs.y.value;
			vec.z.value = lhs * zfloat.SCALE_10000 - rhs.z.value;
			return vec;
		}
		public static zVector3 operator -(zVector3 lhs, int rhs)
		{
			zVector3 vec;
			vec.x.value = lhs.x.value - rhs * zfloat.SCALE_10000;
			vec.y.value = lhs.y.value - rhs * zfloat.SCALE_10000;
			vec.z.value = lhs.z.value - rhs * zfloat.SCALE_10000;
			return vec;
		}
		public static zVector3 operator -(zfloat lhs, zVector3 rhs)
		{
			zVector3 vec;
			vec.x.value = lhs.value - rhs.x.value;
			vec.y.value = lhs.value - rhs.y.value;
			vec.z.value = lhs.value - rhs.z.value;
			return vec;
		}
		public static zVector3 operator -(zVector3 lhs, zfloat rhs)
		{
			zVector3 vec;
			vec.x.value = lhs.x.value - rhs.value;
			vec.y.value = lhs.y.value - rhs.value;
			vec.z.value = lhs.z.value - rhs.value;
			return vec;
		}
		#endregion

		#region 负号
		public static zVector3 operator -(zVector3 a)
		{
			zVector3 vec;
			vec.x.value = -a.x.value;
			vec.y.value = -a.y.value;
			vec.z.value = -a.z.value;
			return vec;
		}

		#endregion

		#region 乘法
		public static zVector3 operator *(int lhs, zVector3 rhs)
		{
			zVector3 vec;
			vec.x.value = lhs * rhs.x.value;
			vec.y.value = lhs * rhs.y.value;
			vec.z.value = lhs * rhs.z.value;

			return vec;
		}
		public static zVector3 operator *(zVector3 lhs, int rhs)
		{
			zVector3 vec;
			vec.x.value = lhs.x.value * rhs;
			vec.y.value = lhs.y.value * rhs;
			vec.z.value = lhs.z.value * rhs;

			return vec;
		}
		public static zVector3 operator *(zfloat lhs, zVector3 rhs)
		{
			zVector3 vec;
			vec.x.value = lhs.value * rhs.x.value / zfloat.SCALE_10000;
			vec.y.value = lhs.value * rhs.y.value / zfloat.SCALE_10000;
			vec.z.value = lhs.value * rhs.z.value / zfloat.SCALE_10000;

			return vec;
		}
		public static zVector3 operator *(zVector3 lhs, zfloat rhs)
		{
			zVector3 vec;
			vec.x.value = lhs.x.value * rhs.value / zfloat.SCALE_10000;
			vec.y.value = lhs.y.value * rhs.value / zfloat.SCALE_10000;
			vec.z.value = lhs.z.value * rhs.value / zfloat.SCALE_10000;

			return vec;
		}
		#endregion

		#region 除法
		public static zVector3 operator /(zVector3 lhs, int rhs)
		{
			zVector3 vec;
			vec.x.value = lhs.x.value / rhs;
			vec.y.value = lhs.y.value / rhs;
			vec.z.value = lhs.z.value / rhs;
			return vec;
		}
		public static zVector3 operator /(zVector3 lhs, zfloat rhs)
		{
			zVector3 vec;
			vec.x.value = lhs.x.value * zfloat.SCALE_10000 / rhs.value;
			vec.y.value = lhs.y.value * zfloat.SCALE_10000 / rhs.value;
			vec.z.value = lhs.z.value * zfloat.SCALE_10000 / rhs.value;
			return vec;
		}
		#endregion

		#region 相等判定
		public static bool operator !=(zVector3 lhs, zVector3 rhs)
		{
			return (lhs.x.value != rhs.x.value) || (lhs.y.value != rhs.y.value) || (lhs.z.value != rhs.z.value);
		}

		public static bool operator ==(zVector3 lhs, zVector3 rhs)
		{
			return (lhs.x.value == rhs.x.value) && (lhs.y.value == rhs.y.value) && (lhs.z.value == rhs.z.value);
		}
		#endregion

		/// <summary>
		/// 判断是否与另一个对象相等
		/// </summary>
		/// <param name="obj">要比较的对象</param>
		/// <returns>如果相等返回true，否则返回false</returns>
		public override bool Equals(object obj)
		{
			if (obj is zVector3 other)
			{
				return Equals(other);
			}
			return false;
		}

		/// <summary>
		/// 判断是否与另一个向量按分量完全相等。
		/// </summary>
		public bool Equals(zVector3 other)
		{
			return x.value == other.x.value && y.value == other.y.value && z.value == other.z.value;
		}

		/// <summary>
		/// 获取哈希码
		/// </summary>
		/// <returns>哈希码值</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(x.value, y.value, z.value);
		}
		public override string ToString()
		{
			return ("(" + x + " , " + y + " , " + z + ")");
		}

		/// <summary>
		/// 实现ISerializable接口的序列化方法
		/// </summary>
		/// <param name="info">序列化信息</param>
		/// <param name="context">流上下文</param>
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("x", x.value);
			info.AddValue("y", y.value);
			info.AddValue("z", z.value);
		}

		/// <summary>
		/// 反序列化构造函数
		/// </summary>
		/// <param name="info">序列化信息</param>
		/// <param name="context">流上下文</param>
		public zVector3(SerializationInfo info, StreamingContext context)
		{
			x = zfloat.FromRaw(info.GetInt64("x"));
			y = zfloat.FromRaw(info.GetInt64("y"));
			z = zfloat.FromRaw(info.GetInt64("z"));
		}

		private static zVector3 GetAnyPerpendicularNormalized(zVector3 normal)
		{
			zVector3 axis = zMathf.Abs(normal.y) < zfloat.OneHalf ? zVector3.up : zVector3.right;
			zVector3 perpendicular = zVector3.Cross(normal, axis);
			if (perpendicular.IsZero())
			{
				perpendicular = zVector3.Cross(normal, zVector3.forward);
			}
			return perpendicular.IsZero() ? zVector3.right : perpendicular.normalized;
		}
	}
}
