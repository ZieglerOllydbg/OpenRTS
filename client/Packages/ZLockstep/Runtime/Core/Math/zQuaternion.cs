using System;

namespace zUnity
{
	/// <summary>
	/// 定点四元数（Q4：1 = 10000）。
	/// 所有计算均基于整数缩放运算，不在本类型内部使用 float/double。
	/// </summary>
	[Serializable]
	public struct zQuaternion : IEquatable<zQuaternion>
	{
		private const long RawOne = zfloat.SCALE_10000;
		private const long NormalizeToleranceRaw = 10;
		private const long NearParallelRaw = 9999;
		private const long NearOppositeRaw = -9999;

		/// <summary>四元数 X 分量。</summary>
		public zfloat x;
		/// <summary>四元数 Y 分量。</summary>
		public zfloat y;
		/// <summary>四元数 Z 分量。</summary>
		public zfloat z;
		/// <summary>四元数 W 分量。</summary>
		public zfloat w;

		/// <summary>单位四元数。</summary>
		public static readonly zQuaternion identity = new zQuaternion(zfloat.Zero, zfloat.Zero, zfloat.Zero, zfloat.One);

		/// <summary>
		/// X 轴正方向旋转（等价于 LookRotation(zVector3.right)）。
		/// </summary>
		public static readonly zQuaternion xPositive = LookRotation(zVector3.right);

		/// <summary>
		/// 通过索引访问分量：0=x, 1=y, 2=z, 3=w。
		/// </summary>
		/// <exception cref="IndexOutOfRangeException">index 不在 [0,3]。</exception>
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
					default: throw new IndexOutOfRangeException("Invalid zQuaternion index.");
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
					default: throw new IndexOutOfRangeException("Invalid zQuaternion index.");
				}
			}
		}

		/// <summary>
		/// 欧拉角（单位：度）。get 为当前四元数转欧拉角，set 为由欧拉角构造四元数。
		/// </summary>
		public zVector3 eulerAngles
		{
			get => ToEuler(this);
			set => this = Euler(value);
		}

		/// <summary>
		/// 长度平方（|q|^2）。
		/// </summary>
		public zfloat LengthSquared => Dot(this, this);

		/// <summary>
		/// 长度（|q|）。
		/// </summary>
		public zfloat Length
		{
			get
			{
				zfloat ls = LengthSquared;
				return ls <= zfloat.Zero ? zfloat.Zero : zMathf.Sqrt(ls);
			}
		}

		/// <summary>
		/// 是否近似单位四元数（默认容差为 raw=10，即 0.001）。
		/// </summary>
		public bool IsNormalized => zfloat.Approximately(LengthSquared, zfloat.One, zfloat.FromRaw(NormalizeToleranceRaw));

		/// <summary>
		/// 返回归一化后的副本；零长度时返回 identity。
		/// </summary>
		public zQuaternion Normalized => Normalize(this);

		/// <summary>
		/// 通过分量构造四元数。
		/// </summary>
		public zQuaternion(zfloat x, zfloat y, zfloat z, zfloat w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		/// <summary>
		/// 设置四元数分量（兼容旧调用）。
		/// </summary>
		public void Set(zfloat new_x, zfloat new_y, zfloat new_z, zfloat new_w)
		{
			x = new_x;
			y = new_y;
			z = new_z;
			w = new_w;
		}

		/// <summary>
		/// 四元数点积，结果范围理论上在 [-1,1]（对单位四元数）。
		/// </summary>
		public static zfloat Dot(zQuaternion a, zQuaternion b)
		{
			long raw = AddChecked(
				AddChecked(MulRaw(a.x.value, b.x.value), MulRaw(a.y.value, b.y.value)),
				AddChecked(MulRaw(a.z.value, b.z.value), MulRaw(a.w.value, b.w.value)));
			return zfloat.FromRaw(raw);
		}

		/// <summary>
		/// 返回两个旋转之间的夹角（度），范围 [0,180]。
		/// </summary>
		public static zfloat Angle(zQuaternion a, zQuaternion b)
		{
			zfloat lsA = a.LengthSquared;
			zfloat lsB = b.LengthSquared;
			if (lsA <= zfloat.Zero || lsB <= zfloat.Zero)
			{
				return zfloat.Zero;
			}

			zfloat denom = zMathf.Sqrt(lsA * lsB);
			if (denom <= zfloat.Zero)
			{
				return zfloat.Zero;
			}

			zfloat cos = zMathf.Abs(Dot(a, b) / denom);
			cos = zMathf.Clamp(cos, zfloat.Zero, zfloat.One);
			return zMathf.Acos(cos) * zMathf.Rad2Deg * 2;
		}

		/// <summary>
		/// 根据轴角（度）创建四元数。若轴为零向量，返回 identity。
		/// </summary>
		public static zQuaternion AngleAxis(zfloat angle, zVector3 axis)
		{
			if (axis.sqrMagnitude <= zfloat.Zero)
			{
				return identity;
			}

			zVector3 n = axis.normalized;
			zfloat halfAngle = angle * zfloat.Half;
			zfloat s = zMathf.SinAngle(halfAngle);
			zfloat c = zMathf.CosAngle(halfAngle);

			zQuaternion result = new zQuaternion(n.x * s, n.y * s, n.z * s, c);
			return Normalize(result);
		}

		/// <summary>
		/// 根据旋转矩阵创建四元数。
		/// 输入需接近正交旋转矩阵，否则结果会有精度损失。
		/// </summary>
		public static zQuaternion FromMatrix(zMatrix4x4 m)
		{
			zQuaternion q;
			zfloat tr = m.m00 + m.m11 + m.m22;

			if (tr > zfloat.Zero)
			{
				zfloat s = zfloat.FromRaw(checked(zMathf.SqrtScale(checked(tr.value + RawOne)) * 2));
				if (s.value == 0) return identity;
				q.w = zfloat.Quarter * s;
				q.x = (m.m21 - m.m12) / s;
				q.y = (m.m02 - m.m20) / s;
				q.z = (m.m10 - m.m01) / s;
			}
			else if (m.m00 > m.m11 && m.m00 > m.m22)
			{
				zfloat s = zfloat.FromRaw(checked(zMathf.SqrtScale(checked(RawOne + m.m00.value - m.m11.value - m.m22.value)) * 2));
				if (s.value == 0) return identity;
				q.w = (m.m21 - m.m12) / s;
				q.x = zfloat.Quarter * s;
				q.y = (m.m01 + m.m10) / s;
				q.z = (m.m02 + m.m20) / s;
			}
			else if (m.m11 > m.m22)
			{
				zfloat s = zfloat.FromRaw(checked(zMathf.SqrtScale(checked(RawOne + m.m11.value - m.m00.value - m.m22.value)) * 2));
				if (s.value == 0) return identity;
				q.w = (m.m02 - m.m20) / s;
				q.x = (m.m01 + m.m10) / s;
				q.y = zfloat.Quarter * s;
				q.z = (m.m12 + m.m21) / s;
			}
			else
			{
				zfloat s = zfloat.FromRaw(checked(zMathf.SqrtScale(checked(RawOne + m.m22.value - m.m00.value - m.m11.value)) * 2));
				if (s.value == 0) return identity;
				q.w = (m.m10 - m.m01) / s;
				q.x = (m.m02 + m.m20) / s;
				q.y = (m.m12 + m.m21) / s;
				q.z = zfloat.Quarter * s;
			}

			return Normalize(q);
		}

		/// <summary>
		/// 将四元数转换为轴角（度）。
		/// 零旋转时返回 angle=0, axis=(1,0,0)。
		/// </summary>
		public void ToAngleAxis(out zfloat angle, out zVector3 axis)
		{
			zQuaternion n = Normalize(this);
			zfloat xyzSqr = n.x * n.x + n.y * n.y + n.z * n.z;

			if (xyzSqr <= zfloat.Zero)
			{
				angle = zfloat.Zero;
				axis = zVector3.right;
				return;
			}

			angle = zMathf.AcosAngle(zMathf.Clamp(n.w, zfloat.NegativeOne, zfloat.One)) * 2;
			zfloat inv = zfloat.One / zMathf.Sqrt(xyzSqr);
			axis = new zVector3(n.x * inv, n.y * inv, n.z * inv);
		}

		/// <summary>
		/// 输出旋转后的坐标轴方向。
		/// </summary>
		public static void ToAxis(zQuaternion q, ref zVector3 vx, ref zVector3 vy, ref zVector3 vz)
		{
			zMatrix4x4 rot = q.RotationMatrix();
			vx.x = rot.m00; vx.y = rot.m01; vx.z = rot.m02;
			vy.x = rot.m10; vy.y = rot.m11; vy.z = rot.m12;
			vz.x = rot.m20; vz.y = rot.m21; vz.z = rot.m22;
		}

		/// <summary>
		/// 计算从 fromDirection 旋转到 toDirection 的四元数。
		/// 任一输入为零向量时返回 identity。
		/// </summary>
		public static zQuaternion FromToRotation(zVector3 fromDirection, zVector3 toDirection)
		{
			if (fromDirection.sqrMagnitude <= zfloat.Zero || toDirection.sqrMagnitude <= zfloat.Zero)
			{
				return identity;
			}

			zVector3 from = fromDirection.normalized;
			zVector3 to = toDirection.normalized;
			zfloat dot = zMathf.Clamp(zVector3.Dot(ref from, ref to), zfloat.NegativeOne, zfloat.One);

			if (dot.value >= NearParallelRaw)
			{
				return identity;
			}

			if (dot.value <= NearOppositeRaw)
			{
				zVector3 axis = zVector3.Cross(zVector3.up, from);
				if (axis.sqrMagnitude <= zfloat.Zero)
				{
					axis = zVector3.Cross(zVector3.right, from);
				}

				return AngleAxis((zfloat)180, axis);
			}

			zVector3 cross = zVector3.Cross(from, to);
			zfloat angle = zMathf.AcosAngle(dot);
			return AngleAxis(angle, cross);
		}

		/// <summary>
		/// 将当前四元数设置为从 fromDirection 到 toDirection 的旋转（兼容旧调用）。
		/// </summary>
		public void SetFromToRotation(zVector3 fromDirection, zVector3 toDirection)
		{
			this = FromToRotation(fromDirection, toDirection);
		}

		/// <summary>
		/// 转换为旋转矩阵。
		/// </summary>
		public zMatrix4x4 RotationMatrix()
		{
			return zMatrix4x4.CreateFromQuaternion(this);
		}

		/// <summary>
		/// 由前向与上方向创建旋转。forward 为零向量时返回 identity。
		/// </summary>
		public static zQuaternion LookRotation(zVector3 forward, zVector3 upwards)
		{
			if (forward.sqrMagnitude <= zfloat.Zero)
			{
				return identity;
			}

			zVector3 f = forward.normalized;
			zVector3 up = upwards.sqrMagnitude > zfloat.Zero ? upwards.normalized : zVector3.up;
			zVector3 right = zVector3.Cross(up, f);

			if (right.sqrMagnitude <= zfloat.Zero)
			{
				right = zVector3.Cross(zVector3.up, f);
				if (right.sqrMagnitude <= zfloat.Zero)
				{
					right = zVector3.Cross(zVector3.right, f);
					if (right.sqrMagnitude <= zfloat.Zero)
					{
						return identity;
					}
				}
			}

			right = right.normalized;
			up = zVector3.Cross(f, right).normalized;

			zMatrix4x4 mat = zMatrix4x4.identity;
			mat.m00 = right.x; mat.m10 = right.y; mat.m20 = right.z;
			mat.m01 = up.x; mat.m11 = up.y; mat.m21 = up.z;
			mat.m02 = f.x; mat.m12 = f.y; mat.m22 = f.z;
			return FromMatrix(mat);
		}

		/// <summary>
		/// 由前向创建旋转（up 使用世界上方向）。
		/// </summary>
		public static zQuaternion LookRotation(zVector3 forward)
		{
			return LookRotation(forward, zVector3.up);
		}

		/// <summary>
		/// 将当前四元数设置为朝向 view 的旋转（兼容旧调用）。
		/// </summary>
		public void SetLookRotation(zVector3 view)
		{
			this = LookRotation(view);
		}

		/// <summary>
		/// 将当前四元数设置为朝向 view 且约束上方向的旋转（兼容旧调用）。
		/// </summary>
		public void SetLookRotation(zVector3 view, zVector3 up)
		{
			this = LookRotation(view, up);
		}

		/// <summary>
		/// 原地归一化。零长度输入会被设置为 identity。
		/// </summary>
		public void Normalize()
		{
			this = Normalize(this);
		}

		/// <summary>
		/// 返回归一化后的四元数。零长度输入返回 identity。
		/// </summary>
		public static zQuaternion Normalize(zQuaternion value)
		{
			zfloat ls = value.LengthSquared;
			if (ls <= zfloat.Zero)
			{
				return identity;
			}

			if (zfloat.Approximately(ls, zfloat.One, zfloat.FromRaw(NormalizeToleranceRaw)))
			{
				return value;
			}

			zfloat invNorm = zfloat.One / zMathf.Sqrt(ls);
			return new zQuaternion(value.x * invNorm, value.y * invNorm, value.z * invNorm, value.w * invNorm);
		}

		/// <summary>
		/// 球面线性插值，t 会被限制到 [0,1]。
		/// 对接近平行的输入会退化到 Lerp。
		/// </summary>
		public static zQuaternion Slerp(zQuaternion from, zQuaternion to, zfloat amount)
		{
			zfloat t = zMathf.Clamp01(amount);
			zQuaternion a = Normalize(from);
			zQuaternion b = Normalize(to);

			zfloat cosOmega = Dot(a, b);
			if (cosOmega < zfloat.Zero)
			{
				b = new zQuaternion(-b.x, -b.y, -b.z, -b.w);
				cosOmega = -cosOmega;
			}

			cosOmega = zMathf.Clamp(cosOmega, zfloat.Zero, zfloat.One);
			if (cosOmega.value >= NearParallelRaw)
			{
				return Lerp(a, b, t);
			}

			zfloat omega = zMathf.Acos(cosOmega);
			zfloat sinOmega = zMathf.Sin(omega);
			if (zMathf.Abs(sinOmega).value <= NormalizeToleranceRaw)
			{
				return Lerp(a, b, t);
			}

			zfloat s1 = zMathf.Sin((zfloat.One - t) * omega) / sinOmega;
			zfloat s2 = zMathf.Sin(t * omega) / sinOmega;

			zQuaternion result = new zQuaternion(
				s1 * a.x + s2 * b.x,
				s1 * a.y + s2 * b.y,
				s1 * a.z + s2 * b.z,
				s1 * a.w + s2 * b.w);
			return Normalize(result);
		}

		/// <summary>
		/// 线性插值，t 会被限制到 [0,1]；插值后会归一化。
		/// </summary>
		public static zQuaternion Lerp(zQuaternion from, zQuaternion to, zfloat amount)
		{
			zfloat t = zMathf.Clamp01(amount);
			zQuaternion a = Normalize(from);
			zQuaternion b = Normalize(to);

			if (Dot(a, b) < zfloat.Zero)
			{
				b = new zQuaternion(-b.x, -b.y, -b.z, -b.w);
			}

			zfloat inv = zfloat.One - t;
			zQuaternion result = new zQuaternion(
				inv * a.x + t * b.x,
				inv * a.y + t * b.y,
				inv * a.z + t * b.z,
				inv * a.w + t * b.w);
			return Normalize(result);
		}

		/// <summary>
		/// 求逆。零长度输入返回 identity（防止除零）。
		/// </summary>
		public static zQuaternion Inverse(zQuaternion rotation)
		{
			zfloat ls = rotation.LengthSquared;
			if (ls <= zfloat.Zero)
			{
				return identity;
			}

			zfloat invNorm = zfloat.One / ls;
			return new zQuaternion(
				-rotation.x * invNorm,
				-rotation.y * invNorm,
				-rotation.z * invNorm,
				rotation.w * invNorm);
		}

		/// <summary>
		/// 根据欧拉角（度）构造四元数，参数顺序为 x/y/z。
		/// </summary>
		public static zQuaternion Euler(zfloat x, zfloat y, zfloat z)
		{
			zfloat halfRoll = z * zfloat.Half;
			zfloat sr = zMathf.SinAngle(halfRoll);
			zfloat cr = zMathf.CosAngle(halfRoll);

			zfloat halfPitch = x * zfloat.Half;
			zfloat sp = zMathf.SinAngle(halfPitch);
			zfloat cp = zMathf.CosAngle(halfPitch);

			zfloat halfYaw = y * zfloat.Half;
			zfloat sy = zMathf.SinAngle(halfYaw);
			zfloat cy = zMathf.CosAngle(halfYaw);

			zQuaternion result = new zQuaternion(
				cy * sp * cr + sy * cp * sr,
				sy * cp * cr - cy * sp * sr,
				cy * cp * sr - sy * sp * cr,
				cy * cp * cr + sy * sp * sr);
			return Normalize(result);
		}

		/// <summary>
		/// 根据欧拉角（度）构造四元数。
		/// </summary>
		public static zQuaternion Euler(zVector3 euler)
		{
			return Euler(euler.x, euler.y, euler.z);
		}

		/// <summary>
		/// 四元数共轭。
		/// </summary>
		public static zQuaternion Conjugate(zQuaternion value)
		{
			return new zQuaternion(-value.x, -value.y, -value.z, value.w);
		}

		/// <summary>
		/// 转换为欧拉角（度）。
		/// 为保证稳定性，会先归一化并处理万向节锁邻域。
		/// </summary>
		public static zVector3 ToEuler(zQuaternion zq)
		{
			zQuaternion n = Normalize(zq);
			zfloat sqw = n.w * n.w;
			zfloat sqx = n.x * n.x;
			zfloat sqy = n.y * n.y;
			zfloat sqz = n.z * n.z;
			zfloat unit = sqx + sqy + sqz + sqw;
			zfloat test = n.x * n.w - n.y * n.z;
			zfloat threshold = zfloat.FromRaw(4995) * unit;

			if (test > threshold)
			{
				zVector3 v;
				v.y = zMathf.Atan2(n.y, n.x) * 2 * zMathf.Rad2Deg;
				v.x = (zfloat)90;
				v.z = zfloat.Zero;
				return NormalizeAngles(v);
			}

			if (test < -threshold)
			{
				zVector3 v;
				v.y = -zMathf.Atan2(n.y, n.x) * 2 * zMathf.Rad2Deg;
				v.x = (zfloat)(-90);
				v.z = zfloat.Zero;
				return NormalizeAngles(v);
			}

			zQuaternion q = new zQuaternion(n.w, n.z, n.x, n.y);
			zVector3 euler;
			euler.x = zMathf.Asin(zMathf.Clamp(2 * (q.x * q.z - q.w * q.y), zfloat.NegativeOne, zfloat.One)) * zMathf.Rad2Deg;
			euler.y = zMathf.Atan2(2 * q.x * q.w + 2 * q.y * q.z, zfloat.One - 2 * (q.z * q.z + q.w * q.w)) * zMathf.Rad2Deg;
			euler.z = zMathf.Atan2(2 * q.x * q.y + 2 * q.z * q.w, zfloat.One - 2 * (q.y * q.y + q.z * q.z)) * zMathf.Rad2Deg;
			return NormalizeAngles(euler);
		}

		/// <summary>
		/// 文本输出。
		/// </summary>
		public override string ToString()
		{
			return x + " , " + y + " , " + z + " , " + w;
		}

		/// <summary>
		/// 哈希值。
		/// </summary>
		public override int GetHashCode()
		{
			return x.value.GetHashCode() ^ (y.value.GetHashCode() << 2) ^ (z.value.GetHashCode() >> 2) ^ (w.value.GetHashCode() >> 1);
		}

		/// <summary>
		/// 值相等比较（分量逐一比较）。
		/// </summary>
		public override bool Equals(object other)
		{
			return other is zQuaternion quaternion && Equals(quaternion);
		}

		/// <summary>
		/// 值相等比较（分量逐一比较）。
		/// </summary>
		public bool Equals(zQuaternion other)
		{
			return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z) && w.Equals(other.w);
		}

		/// <summary>
		/// 四元数乘法（组合旋转）。
		/// </summary>
		public static zQuaternion operator *(zQuaternion lhs, zQuaternion rhs)
		{
			long rx = AddChecked(
				AddChecked(MulRaw(lhs.w.value, rhs.x.value), MulRaw(lhs.x.value, rhs.w.value)),
				SubChecked(MulRaw(lhs.y.value, rhs.z.value), MulRaw(lhs.z.value, rhs.y.value)));
			long ry = AddChecked(
				AddChecked(MulRaw(lhs.w.value, rhs.y.value), MulRaw(lhs.y.value, rhs.w.value)),
				SubChecked(MulRaw(lhs.z.value, rhs.x.value), MulRaw(lhs.x.value, rhs.z.value)));
			long rz = AddChecked(
				AddChecked(MulRaw(lhs.w.value, rhs.z.value), MulRaw(lhs.z.value, rhs.w.value)),
				SubChecked(MulRaw(lhs.x.value, rhs.y.value), MulRaw(lhs.y.value, rhs.x.value)));
			long rw = SubChecked(
				SubChecked(AddChecked(MulRaw(lhs.w.value, rhs.w.value), -MulRaw(lhs.x.value, rhs.x.value)), MulRaw(lhs.y.value, rhs.y.value)),
				MulRaw(lhs.z.value, rhs.z.value));

			return new zQuaternion(zfloat.FromRaw(rx), zfloat.FromRaw(ry), zfloat.FromRaw(rz), zfloat.FromRaw(rw));
		}

		/// <summary>
		/// 使用四元数旋转向量。
		/// </summary>
		public static zVector3 operator *(zQuaternion rotation, zVector3 point)
		{
			zfloat num = rotation.x * zfloat.Two;
			zfloat num2 = rotation.y * zfloat.Two;
			zfloat num3 = rotation.z * zfloat.Two;
			zfloat num4 = rotation.x * num;
			zfloat num5 = rotation.y * num2;
			zfloat num6 = rotation.z * num3;
			zfloat num7 = rotation.x * num2;
			zfloat num8 = rotation.x * num3;
			zfloat num9 = rotation.y * num3;
			zfloat num10 = rotation.w * num;
			zfloat num11 = rotation.w * num2;
			zfloat num12 = rotation.w * num3;

			zVector3 result;
			result.x = (zfloat.One - (num5 + num6)) * point.x + (num7 - num12) * point.y + (num8 + num11) * point.z;
			result.y = (num7 + num12) * point.x + (zfloat.One - (num4 + num6)) * point.y + (num9 - num10) * point.z;
			result.z = (num8 - num11) * point.x + (num9 + num10) * point.y + (zfloat.One - (num4 + num5)) * point.z;
			return result;
		}

		/// <summary>
		/// 旋转意义上的近似相等（q 与 -q 视为同一旋转）。
		/// </summary>
		public static bool operator ==(zQuaternion lhs, zQuaternion rhs)
		{
			zfloat d = zMathf.Abs(Dot(Normalize(lhs), Normalize(rhs)));
			return zfloat.Approximately(d, zfloat.One, zfloat.FromRaw(NormalizeToleranceRaw));
		}

		/// <summary>
		/// 旋转意义上的不相等。
		/// </summary>
		public static bool operator !=(zQuaternion lhs, zQuaternion rhs)
		{
			return !(lhs == rhs);
		}

		private static zVector3 NormalizeAngles(zVector3 angles)
		{
			angles.x = NormalizeAngle(angles.x);
			angles.y = NormalizeAngle(angles.y);
			angles.z = NormalizeAngle(angles.z);
			return angles;
		}

		private static zfloat NormalizeAngle(zfloat angle)
		{
			zfloat result = angle % 360;
			if (result < zfloat.Zero)
			{
				result += 360;
			}

			return result;
		}

		private static long MulRaw(long a, long b)
		{
			return checked((a * b) / RawOne);
		}

		private static long AddChecked(long a, long b)
		{
			return checked(a + b);
		}

		private static long SubChecked(long a, long b)
		{
			return checked(a - b);
		}
	}
}
