using System;

namespace zUnity
{
	/// <summary>
	/// 空间坐标采用列向量 操作时 使用4X4矩阵 乘以 4X1的矩阵
	/// 变换使用方式 V = M3 * M2 * M1 * V0
	/// 矩阵数据 前面的序号表示行,后面的序号表示列，如:第1行第2列，用m12表示
	/// 每一列表示该矩阵所代表空间的每个轴，如：第0列表示x轴
	/// </summary>
	public struct zMatrix4x4
	{
		public zfloat m00;
		public zfloat m10;
		public zfloat m20;
		public zfloat m30;
		public zfloat m01;
		public zfloat m11;
		public zfloat m21;
		public zfloat m31;
		public zfloat m02;
		public zfloat m12;
		public zfloat m22;
		public zfloat m32;
		public zfloat m03;
		public zfloat m13;
		public zfloat m23;
		public zfloat m33;
		public zfloat this[int row, int column]
		{
			get
			{
				return this[row + column * 4];
			}
			set
			{
                this[row + column * 4] = value;
			}
		}
		public zfloat this[int index]
		{
			get
			{
				switch (index)
				{
					case 0:
						return this.m00; //
					case 1:
						return this.m10;
					case 2:
						return this.m20;
					case 3:
						return this.m30;
					case 4:
						return this.m01;
					case 5:
						return this.m11; //
					case 6:
						return this.m21;
					case 7:
						return this.m31;
					case 8:
						return this.m02;
					case 9:
						return this.m12;
					case 10:
						return this.m22; //
					case 11:
						return this.m32;
					case 12:
						return this.m03;
					case 13:
						return this.m13;
					case 14:
						return this.m23;
					case 15:
						return this.m33; //
					default:
						throw new IndexOutOfRangeException("Invalid matrix index!");
				}
			}
			set
			{
				switch (index)
				{
					case 0:
						this.m00.value = value.value;
						break;
					case 1:
                        this.m10.value = value.value;
						break;
					case 2:
                        this.m20.value = value.value;
						break;
					case 3:
                        this.m30.value = value.value;
						break;
					case 4:
                        this.m01.value = value.value;
						break;
					case 5:
                        this.m11.value = value.value;
						break;
					case 6:
                        this.m21.value = value.value;
						break;
					case 7:
                        this.m31.value = value.value;
						break;
					case 8:
                        this.m02.value = value.value;
						break;
					case 9:
                        this.m12.value = value.value;
						break;
					case 10:
                        this.m22.value = value.value;
						break;
					case 11:
                        this.m32.value = value.value;
						break;
					case 12:
                        this.m03.value = value.value;
						break;
					case 13:
                        this.m13.value = value.value;
						break;
					case 14:
                        this.m23.value = value.value;
						break;
					case 15:
                        this.m33.value = value.value;
						break;
					default:
						throw new IndexOutOfRangeException("Invalid matrix index!");
				}
			}
		}


		/// <summary>
		/// 逆矩阵
		/// </summary>
		public zMatrix4x4 inverse
		{
			get
			{
				zMatrix4x4 result;
				zMatrix4x4.Inverse(this, out result);
				return result;
			}
		}

		/// <summary>
		/// 转置矩阵
		/// </summary>
		public zMatrix4x4 transpose
		{
			get
			{
				return zMatrix4x4.Transpose(this);
			}
		}

		/// <summary>
		/// 是否是单位矩阵
		/// </summary>
		public bool isIdentity
		{
			get
			{
				for (int i = 0; i < 16; ++i)
				{
					if (i == 0 || i == 5 || i == 10 || i == 15)
					{
						if (this[i].value != zfloat.SCALE_10000)
						{
							return false;
						}
					}
					else if (this[i].value != 0)
					{
						return false;
					}
				}
				return true;
			}
		}


		public static readonly zMatrix4x4 zero = _zero;
		public static readonly zMatrix4x4 identity = _identity;

		private static zMatrix4x4 _zero
		{
			get
			{
				return new zMatrix4x4
				{
					m00 = zfloat.Zero,
					m01 = zfloat.Zero,
					m02 = zfloat.Zero,
					m03 = zfloat.Zero,
					m10 = zfloat.Zero,
					m11 = zfloat.Zero,
					m12 = zfloat.Zero,
					m13 = zfloat.Zero,
					m20 = zfloat.Zero,
					m21 = zfloat.Zero,
					m22 = zfloat.Zero,
					m23 = zfloat.Zero,
					m30 = zfloat.Zero,
					m31 = zfloat.Zero,
					m32 = zfloat.Zero,
					m33 = zfloat.Zero
				};
			}
		}


		/// <summary>
		/// 单位矩阵
		/// </summary>
		private static zMatrix4x4 _identity
		{
			get
			{
				return new zMatrix4x4
				{
					m00 = zfloat.One,
					m01 = zfloat.Zero,
					m02 = zfloat.Zero,
					m03 = zfloat.Zero,
					m10 = zfloat.Zero,
					m11 = zfloat.One,
					m12 = zfloat.Zero,
					m13 = zfloat.Zero,
					m20 = zfloat.Zero,
					m21 = zfloat.Zero,
					m22 = zfloat.One,
					m23 = zfloat.Zero,
					m30 = zfloat.Zero,
					m31 = zfloat.Zero,
					m32 = zfloat.Zero,
					m33 = zfloat.One
				};
			}
		}

		public override int GetHashCode()
		{
			return this.GetColumn(0).GetHashCode() ^ this.GetColumn(1).GetHashCode() << 2 ^ this.GetColumn(2).GetHashCode() >> 2 ^ this.GetColumn(3).GetHashCode() >> 1;
		}

		public override bool Equals(object other)
		{
			if (!(other is zMatrix4x4))
			{
				return false;
			}
			zMatrix4x4 matrix4x = (zMatrix4x4)other;
			return this.GetColumn(0).Equals(matrix4x.GetColumn(0)) && this.GetColumn(1).Equals(matrix4x.GetColumn(1)) && this.GetColumn(2).Equals(matrix4x.GetColumn(2)) && this.GetColumn(3).Equals(matrix4x.GetColumn(3));
		}

		/// <summary>
		/// 逆矩阵
		/// </summary>
		/// <param name="matrix"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static bool Inverse(zMatrix4x4 matrix, out zMatrix4x4 result)
		{
			//                                       -1
			// If you have matrix M, inverse Matrix M   can compute
			//
			//     -1       1      
			//    M   = --------- A
			//            det(M)
			//
			// A is adjugate (adjoint) of M, where,
			//
			//      T
			// A = C
			//
			// C is Cofactor matrix of M, where,
			//           i + j
			// C   = (-1)      * det(M  )
			//  ij                    ij
			//
			//     [ a b c d ]
			// M = [ e f g h ]
			//     [ i j k l ]
			//     [ m n o p ]
			//
			// First Row
			//           2 | f g h |
			// C   = (-1)  | j k l | = + ( f ( kp - lo ) - g ( jp - ln ) + h ( jo - kn ) )
			//  11         | n o p |
			//
			//           3 | e g h |
			// C   = (-1)  | i k l | = - ( e ( kp - lo ) - g ( ip - lm ) + h ( io - km ) )
			//  12         | m o p |
			//
			//           4 | e f h |
			// C   = (-1)  | i j l | = + ( e ( jp - ln ) - f ( ip - lm ) + h ( in - jm ) )
			//  13         | m n p |
			//
			//           5 | e f g |
			// C   = (-1)  | i j k | = - ( e ( jo - kn ) - f ( io - km ) + g ( in - jm ) )
			//  14         | m n o |
			//
			// Second Row
			//           3 | b c d |
			// C   = (-1)  | j k l | = - ( b ( kp - lo ) - c ( jp - ln ) + d ( jo - kn ) )
			//  21         | n o p |
			//
			//           4 | a c d |
			// C   = (-1)  | i k l | = + ( a ( kp - lo ) - c ( ip - lm ) + d ( io - km ) )
			//  22         | m o p |
			//
			//           5 | a b d |
			// C   = (-1)  | i j l | = - ( a ( jp - ln ) - b ( ip - lm ) + d ( in - jm ) )
			//  23         | m n p |
			//
			//           6 | a b c |
			// C   = (-1)  | i j k | = + ( a ( jo - kn ) - b ( io - km ) + c ( in - jm ) )
			//  24         | m n o |
			//
			// Third Row
			//           4 | b c d |
			// C   = (-1)  | f g h | = + ( b ( gp - ho ) - c ( fp - hn ) + d ( fo - gn ) )
			//  31         | n o p |
			//
			//           5 | a c d |
			// C   = (-1)  | e g h | = - ( a ( gp - ho ) - c ( ep - hm ) + d ( eo - gm ) )
			//  32         | m o p |
			//
			//           6 | a b d |
			// C   = (-1)  | e f h | = + ( a ( fp - hn ) - b ( ep - hm ) + d ( en - fm ) )
			//  33         | m n p |
			//
			//           7 | a b c |
			// C   = (-1)  | e f g | = - ( a ( fo - gn ) - b ( eo - gm ) + c ( en - fm ) )
			//  34         | m n o |
			//
			// Fourth Row
			//           5 | b c d |
			// C   = (-1)  | f g h | = - ( b ( gl - hk ) - c ( fl - hj ) + d ( fk - gj ) )
			//  41         | j k l |
			//
			//           6 | a c d |
			// C   = (-1)  | e g h | = + ( a ( gl - hk ) - c ( el - hi ) + d ( ek - gi ) )
			//  42         | i k l |
			//
			//           7 | a b d |
			// C   = (-1)  | e f h | = - ( a ( fl - hj ) - b ( el - hi ) + d ( ej - fi ) )
			//  43         | i j l |
			//
			//           8 | a b c |
			// C   = (-1)  | e f g | = + ( a ( fk - gj ) - b ( ek - gi ) + c ( ej - fi ) )
			//  44         | i j k |
			//
			// Cost of operation
			// 53 adds, 104 muls, and 1 div.
			zfloat a = matrix.m00, b = matrix.m01, c = matrix.m02, d = matrix.m03;
			zfloat e = matrix.m10, f = matrix.m11, g = matrix.m12, h = matrix.m13;
			zfloat i = matrix.m20, j = matrix.m21, k = matrix.m22, l = matrix.m23;
			zfloat m = matrix.m30, n = matrix.m31, o = matrix.m32, p = matrix.m33;

			zfloat kp_lo = k * p - l * o;
			zfloat jp_ln = j * p - l * n;
			zfloat jo_kn = j * o - k * n;
			zfloat ip_lm = i * p - l * m;
			zfloat io_km = i * o - k * m;
			zfloat in_jm = i * n - j * m;

			zfloat a11 = +(f * kp_lo - g * jp_ln + h * jo_kn);
			zfloat a12 = -(e * kp_lo - g * ip_lm + h * io_km);
			zfloat a13 = +(e * jp_ln - f * ip_lm + h * in_jm);
			zfloat a14 = -(e * jo_kn - f * io_km + g * in_jm);

			zfloat det = a * a11 + b * a12 + c * a13 + d * a14;

			if (zMathf.Abs(det) < zfloat.Zero)
			{
				result = zero;
				return false;
			}

			zfloat invDet = zfloat.One / det;

			result.m00 = a11 * invDet;
			result.m10 = a12 * invDet;
			result.m20 = a13 * invDet;
			result.m30 = a14 * invDet;

			result.m01 = -(b * kp_lo - c * jp_ln + d * jo_kn) * invDet;
			result.m11 = +(a * kp_lo - c * ip_lm + d * io_km) * invDet;
			result.m21 = -(a * jp_ln - b * ip_lm + d * in_jm) * invDet;
			result.m31 = +(a * jo_kn - b * io_km + c * in_jm) * invDet;

			zfloat gp_ho = g * p - h * o;
			zfloat fp_hn = f * p - h * n;
			zfloat fo_gn = f * o - g * n;
			zfloat ep_hm = e * p - h * m;
			zfloat eo_gm = e * o - g * m;
			zfloat en_fm = e * n - f * m;

			result.m02 = +(b * gp_ho - c * fp_hn + d * fo_gn) * invDet;
			result.m12 = -(a * gp_ho - c * ep_hm + d * eo_gm) * invDet;
			result.m22 = +(a * fp_hn - b * ep_hm + d * en_fm) * invDet;
			result.m32 = -(a * fo_gn - b * eo_gm + c * en_fm) * invDet;

			zfloat gl_hk = g * l - h * k;
			zfloat fl_hj = f * l - h * j;
			zfloat fk_gj = f * k - g * j;
			zfloat el_hi = e * l - h * i;
			zfloat ek_gi = e * k - g * i;
			zfloat ej_fi = e * j - f * i;

			result.m03 = -(b * gl_hk - c * fl_hj + d * fk_gj) * invDet;
			result.m13 = +(a * gl_hk - c * el_hi + d * ek_gi) * invDet;
			result.m23 = -(a * fl_hj - b * el_hi + d * ej_fi) * invDet;
			result.m33 = +(a * fk_gj - b * ek_gi + c * ej_fi) * invDet;

			return true;
		}

		/// <summary>
		/// 转置矩阵
		/// </summary>
		/// <param name="matrix"></param>
		/// <returns></returns>
		public static zMatrix4x4 Transpose(zMatrix4x4 matrix)
		{
			zMatrix4x4 result;

            result.m00.value = matrix.m00.value;
            result.m01.value = matrix.m10.value;
            result.m02.value = matrix.m20.value;
            result.m03.value = matrix.m30.value;
            result.m10.value = matrix.m01.value;
            result.m11.value = matrix.m11.value;
            result.m12.value = matrix.m21.value;
            result.m13.value = matrix.m31.value;
            result.m20.value = matrix.m02.value;
            result.m21.value = matrix.m12.value;
            result.m22.value = matrix.m22.value;
            result.m23.value = matrix.m32.value;
            result.m30.value = matrix.m03.value;
            result.m31.value = matrix.m13.value;
            result.m32.value = matrix.m23.value;
            result.m33.value = matrix.m33.value;

			return result;
		}

		public zVector4 GetColumn(int i)
		{
            zVector4 v4;
            v4.x.value = this[0, i].value;
            v4.y.value = this[1, i].value;
            v4.z.value = this[2, i].value;
            v4.w.value = this[3, i].value;
            return v4;
			//return new zVector4(this[0, i], this[1, i], this[2, i], this[3, i]);
		}
		public zVector4 GetRow(int i)
		{
            zVector4 v4;
            v4.x.value = this[i, 0].value;
            v4.y.value = this[i, 1].value;
            v4.z.value = this[i, 2].value;
            v4.w.value = this[i, 3].value;
            return v4;

			//return new zVector4(this[i, 0], this[i, 1], this[i, 2], this[i, 3]);
		}
		public void SetColumn(int i, zVector4 v)
		{
			this[0, i] = v.x;
			this[1, i] = v.y;
			this[2, i] = v.z;
			this[3, i] = v.w;
		}
		public void SetRow(int i, zVector4 v)
		{
			this[i, 0] = v.x;
			this[i, 1] = v.y;
			this[i, 2] = v.z;
			this[i, 3] = v.w;
		}

		/// <summary>
		/// 使用该矩阵变换一个点
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public zVector3 MultiplyPoint(zVector3 v)
		{
            //zVector3 result;
            //result.x = this.m00 * v.x + this.m01 * v.y + this.m02 * v.z + this.m03;
            //result.y = this.m10 * v.x + this.m11 * v.y + this.m12 * v.z + this.m13;
            //result.z = this.m20 * v.x + this.m21 * v.y + this.m22 * v.z + this.m23;
            //zfloat num = this.m30 * v.x + this.m31 * v.y + this.m32 * v.z + this.m33;
            //num = zfloat.One / num;
            //result.x *= num;
            //result.y *= num;
            //result.z *= num;
            //return result;

            zVector3 result;
            result.x.value = (this.m00.value * v.x.value + this.m01.value * v.y.value + this.m02.value * v.z.value) / zfloat.SCALE_10000 + this.m03.value;
            result.y.value = (this.m10.value * v.x.value + this.m11.value * v.y.value + this.m12.value * v.z.value) / zfloat.SCALE_10000 + this.m13.value;
            result.z.value = (this.m20.value * v.x.value + this.m21.value * v.y.value + this.m22.value * v.z.value) / zfloat.SCALE_10000 + this.m23.value;
            zfloat num;
            num.value = (this.m30.value * v.x.value + this.m31.value * v.y.value + this.m32.value * v.z.value)/zfloat.SCALE_10000 + this.m33.value;
            num.value = zfloat.SCALE_100000000 / num.value;
            result.x.value = result.x.value * num.value / zfloat.SCALE_10000;
            result.y.value = result.y.value * num.value / zfloat.SCALE_10000;
            result.z.value = result.z.value * num.value / zfloat.SCALE_10000;
            return result;
		}


		/// <summary>
		/// 使用该矩阵变换一个点，只能进行正交变换。比MultiplyPoint速度快，但是无法执行投影变换。
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public zVector3 MultiplyPoint3x4(zVector3 v)
		{
            //zVector3 result;
            //result.x = this.m00 * v.x + this.m01 * v.y + this.m02 * v.z + this.m03;
            //result.y = this.m10 * v.x + this.m11 * v.y + this.m12 * v.z + this.m13;
            //result.z = this.m20 * v.x + this.m21 * v.y + this.m22 * v.z + this.m23;
            //return result;

            zVector3 result;
            result.x.value = (this.m00.value * v.x.value + this.m01.value * v.y.value + this.m02.value * v.z.value) / zfloat.SCALE_10000 + this.m03.value;
            result.y.value = (this.m10.value * v.x.value + this.m11.value * v.y.value + this.m12.value * v.z.value) / zfloat.SCALE_10000 + this.m13.value;
            result.z.value = (this.m20.value * v.x.value + this.m21.value * v.y.value + this.m22.value * v.z.value) / zfloat.SCALE_10000 + this.m23.value;
            return result;
		}

		/// <summary>
		/// 使用该矩阵变换一个向量
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public zVector3 MultiplyVector(zVector3 v)
		{
            //zVector3 result;
            //result.x = this.m00 * v.x + this.m01 * v.y + this.m02 * v.z;
            //result.y = this.m10 * v.x + this.m11 * v.y + this.m12 * v.z;
            //result.z = this.m20 * v.x + this.m21 * v.y + this.m22 * v.z;
            //return result;

            zVector3 result;
            result.x.value = (this.m00.value * v.x.value + this.m01.value * v.y.value + this.m02.value * v.z.value) / zfloat.SCALE_10000;
            result.y.value = (this.m10.value * v.x.value + this.m11.value * v.y.value + this.m12.value * v.z.value) / zfloat.SCALE_10000;
            result.z.value = (this.m20.value * v.x.value + this.m21.value * v.y.value + this.m22.value * v.z.value) / zfloat.SCALE_10000;
            return result;
		}


		public void SetTRS(zVector3 pos, zQuaternion q, zVector3 s)
		{
			this = zMatrix4x4.TRS(pos, q, s);
		}

		public override string ToString()
		{
			string str = "";// "{ ";
			for (int i = 0; i < 4; ++i)
			{
				for (int j = 0; j < 4; ++j)
				{
					str += (this[i, j]);
					str += "  ";

				}
				str += "\n";
			}
			//str += " }";
			return str;
		}

		public static zMatrix4x4 TRS(zVector3 pos, zQuaternion rotate, zVector3 scale)
		{
			zMatrix4x4 posM = CreateTranslation(pos);
			zMatrix4x4 rotM = CreateFromQuaternion(rotate);
			zMatrix4x4 scaleM = CreateScale(scale);
			zMatrix4x4 tempM = zMatrix4x4.identity;
			Multiply(ref posM, ref rotM, ref tempM);
			Multiply(ref tempM, ref scaleM, ref posM);
			return posM;
			//zMatrix4x4 result = CreateTranslation(pos) * CreateFromQuaternion(rotate) * CreateScale(scale);
			//return result;
		}

		/// <summary>
		/// 创建一个缩放矩阵
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public static zMatrix4x4 CreateScale(zVector3 v)
		{
			return new zMatrix4x4
			{
				m00 = v.x,
				m01 = zfloat.Zero,
				m02 = zfloat.Zero,
				m03 = zfloat.Zero,
				m10 = zfloat.Zero,
				m11 = v.y,
				m12 = zfloat.Zero,
				m13 = zfloat.Zero,
				m20 = zfloat.Zero,
				m21 = zfloat.Zero,
				m22 = v.z,
				m23 = zfloat.Zero,
				m30 = zfloat.Zero,
				m31 = zfloat.Zero,
				m32 = zfloat.Zero,
				m33 = zfloat.One
			};
		}

		/// <summary>
		/// 创建一个位移变换
		/// </summary>
		/// <param name="xPosition"></param>
		/// <param name="yPosition"></param>
		/// <param name="zPosition"></param>
		/// <returns></returns>
		public static zMatrix4x4 CreateTranslation(zfloat xPosition, zfloat yPosition, zfloat zPosition)
		{
			zMatrix4x4 result;

			result.m00 = zfloat.One;
			result.m01 = zfloat.Zero;
			result.m02 = zfloat.Zero;
			//result.m03 = zfloat.Zero;
			result.m10 = zfloat.Zero;
			result.m11 = zfloat.One;
			result.m12 = zfloat.Zero;
			//result.m13 = zfloat.Zero;
			result.m20 = zfloat.Zero;
			result.m21 = zfloat.Zero;
			result.m22 = zfloat.One;
			//result.m23 = zfloat.Zero;

			result.m03 = xPosition;
			result.m13 = yPosition;
			result.m23 = zPosition;
			result.m33 = zfloat.One;

			result.m30 = zfloat.Zero;
			result.m31 = zfloat.Zero;
			result.m32 = zfloat.Zero;

			return result;
		}

		public static zMatrix4x4 CreateTranslation(zVector3 pos)
		{
			return CreateTranslation(pos.x, pos.y, pos.z);
		}

		/// <summary>
		/// 根据四元数创建旋转矩阵
		/// </summary>
		/// <param name="quaternion"></param>
		/// <returns></returns>
		public static zMatrix4x4 CreateFromQuaternion(zQuaternion quaternion)
		{
			zMatrix4x4 result;

			/*zfloat xx = zfloat.FromRaw(quaternion.x.value * quaternion.x.value /zfloat.SCALE_10000);
			zfloat yy = zfloat.FromRaw(quaternion.y.value * quaternion.y.value /zfloat.SCALE_10000);
			zfloat zz = zfloat.FromRaw(quaternion.z.value * quaternion.z.value / zfloat.SCALE_10000);

			zfloat xy = zfloat.FromRaw(quaternion.x.value * quaternion.y.value / zfloat.SCALE_10000);
			zfloat wz = zfloat.FromRaw(quaternion.z.value * quaternion.w.value / zfloat.SCALE_10000);
			zfloat xz = zfloat.FromRaw(quaternion.z.value * quaternion.x.value / zfloat.SCALE_10000);
			zfloat wy = zfloat.FromRaw(quaternion.y.value * quaternion.w.value / zfloat.SCALE_10000);
			zfloat yz = zfloat.FromRaw(quaternion.y.value * quaternion.z.value / zfloat.SCALE_10000);
			zfloat wx = zfloat.FromRaw(quaternion.x.value * quaternion.w.value / zfloat.SCALE_10000);
			 
			 result.m00.value = (1 - 2 * (yy + zz)).value;
			result.m10.value = (2 * (xy + wz)).value;
			result.m20.value = (2 * (xz - wy)).value;
			result.m30.value = 0;
			result.m01.value = (2 * (xy - wz)).value;
			result.m11.value = (1 - 2 * (zz + xx)).value;
			result.m21.value = (2 * (yz + wx)).value;
			result.m31.value = 0;
			result.m02.value = (2 * (xz + wy)).value;
			result.m12.value = (2 * (yz - wx)).value;
			result.m22.value = (1 - 2 * (yy + xx)).value;
			result.m32.value = 0;
			result.m03.value = 0;
			result.m13.value = 0;
			result.m23.value = 0;
			result.m33.value = zfloat.SCALE_10000;
			 
			 */

			long lxx = quaternion.x.value * quaternion.x.value / zfloat.SCALE_10000;
			long lyy = quaternion.y.value * quaternion.y.value /zfloat.SCALE_10000;
			long lzz = quaternion.z.value * quaternion.z.value / zfloat.SCALE_10000;

			long lxy = quaternion.x.value * quaternion.y.value / zfloat.SCALE_10000;
			long lwz = quaternion.z.value * quaternion.w.value / zfloat.SCALE_10000;
			long lxz = quaternion.z.value * quaternion.x.value / zfloat.SCALE_10000;
			long lwy = quaternion.y.value * quaternion.w.value / zfloat.SCALE_10000;
			long lyz = quaternion.y.value * quaternion.z.value / zfloat.SCALE_10000;
			long lwx = quaternion.x.value * quaternion.w.value / zfloat.SCALE_10000;

			result.m00.value = (1 * zfloat.SCALE_10000 - 2 * (lyy + lzz));
			result.m10.value = (2 * (lxy + lwz));
			result.m20.value = (2 * (lxz - lwy));
			result.m30.value = 0;
			result.m01.value = (2 * (lxy - lwz));
			result.m11.value = (1 * zfloat.SCALE_10000 - 2 * (lzz + lxx));
			result.m21.value = (2 * (lyz + lwx));
			result.m31.value = 0;
			result.m02.value = (2 * (lxz + lwy));
			result.m12.value = (2 * (lyz - lwx));
			result.m22.value = (1 * zfloat.SCALE_10000 - 2 * (lyy + lxx));
			result.m32.value = 0;
			result.m03.value = 0;
			result.m13.value = 0;
			result.m23.value = 0;
			result.m33.value = zfloat.SCALE_10000;

			return result;
		}

		/// <summary>
		/// 使用四元数变换给定的矩阵
		/// </summary>
		/// <param name="value"></param>
		/// <param name="rotation"></param>
		/// <returns></returns>
		/* public static zMatrix4x4 Rotation(zMatrix4x4 value, zQuaternion rotation)
		 {
			 // Compute rotation matrix.
			 zfloat x2 = rotation.X + rotation.X;
			 zfloat y2 = rotation.Y + rotation.Y;
			 zfloat z2 = rotation.Z + rotation.Z;

			 zfloat wx2 = rotation.W * x2;
			 zfloat wy2 = rotation.W * y2;
			 zfloat wz2 = rotation.W * z2;
			 zfloat xx2 = rotation.X * x2;
			 zfloat xy2 = rotation.X * y2;
			 zfloat xz2 = rotation.X * z2;
			 zfloat yy2 = rotation.Y * y2;
			 zfloat yz2 = rotation.Y * z2;
			 zfloat zz2 = rotation.Z * z2;

			 zfloat q11 = 1.0f - yy2 - zz2;
			 zfloat q21 = xy2 - wz2;
			 zfloat q31 = xz2 + wy2;

			 zfloat q12 = xy2 + wz2;
			 zfloat q22 = 1.0f - xx2 - zz2;
			 zfloat q32 = yz2 - wx2;

			 zfloat q13 = xz2 - wy2;
			 zfloat q23 = yz2 + wx2;
			 zfloat q33 = 1.0f - xx2 - yy2;

			 zMatrix4x4 result;

			 // First row
			 result.m00 = value.m00 * q11 + value.m01 * q21 + value.m02 * q31;
			 result.M12 = value.M11 * q12 + value.M12 * q22 + value.M13 * q32;
			 result.M13 = value.M11 * q13 + value.M12 * q23 + value.M13 * q33;
			 result.M14 = value.M14;

			 // Second row
			 result.M21 = value.M21 * q11 + value.M22 * q21 + value.M23 * q31;
			 result.M22 = value.M21 * q12 + value.M22 * q22 + value.M23 * q32;
			 result.M23 = value.M21 * q13 + value.M22 * q23 + value.M23 * q33;
			 result.M24 = value.M24;

			 // Third row
			 result.M31 = value.M31 * q11 + value.M32 * q21 + value.M33 * q31;
			 result.M32 = value.M31 * q12 + value.M32 * q22 + value.M33 * q32;
			 result.M33 = value.M31 * q13 + value.M32 * q23 + value.M33 * q33;
			 result.M34 = value.M34;

			 // Fourth row
			 result.M41 = value.M41 * q11 + value.M42 * q21 + value.M43 * q31;
			 result.M42 = value.M41 * q12 + value.M42 * q22 + value.M43 * q32;
			 result.M43 = value.M41 * q13 + value.M42 * q23 + value.M43 * q33;
			 result.M44 = value.M44;

			 return result;
		 }*/

		/*
		/// <summary>
		/// 创建一个正交矩阵
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="bottom"></param>
		/// <param name="top"></param>
		/// <param name="zNear"></param>
		/// <param name="zFar"></param>
		/// <returns></returns>
		public static zMatrix4x4 Ortho(zfloat left, zfloat right, zfloat bottom, zfloat top, zfloat zNear, zfloat zFar)
		{
			zfloat width = right - left;
			zfloat height = bottom - top;
			zMatrix4x4 result;

			result.m00 = zfloat.Two / width;
			result.m10 = result.m20 = result.m30 = zfloat.Zero;

			result.m11 = zfloat.Two / height;
			result.m01 = result.m21 = result.m31 = zfloat.Zero;

			result.m22 = zfloat.One / (zNear - zFar);
			result.m02 = result.m12 = result.m32 = zfloat.Zero;

			result.m03 = result.m13 = zfloat.Zero;
			result.m23 = zNear / (zNear - zFar);
			result.m33 = zfloat.One;

			return result;
		}
		*/


		/// <summary>
		/// TODO TODO 因为该库主要用于游戏逻辑，不用于渲染，所以暂时只需要正交变换。暂不实现
		/// 创建一个透视投影矩阵
		/// </summary>
		/// <param name="fov"></param>
		/// <param name="aspect"></param>
		/// <param name="zNear"></param>
		/// <param name="zFar"></param>
		/// <returns></returns>
		public static zMatrix4x4 Perspective(zfloat fov, zfloat aspect, zfloat zNear, zfloat zFar)
		{
			return zero;
		}
		public static zMatrix4x4 operator *(zMatrix4x4 lhs, zMatrix4x4 rhs)
		{
            zMatrix4x4 zm;
            zm.m00.value = (lhs.m00.value * rhs.m00.value + lhs.m01.value * rhs.m10.value + lhs.m02.value * rhs.m20.value + lhs.m03.value * rhs.m30.value) / zfloat.SCALE_10000;
            zm.m01.value = (lhs.m00.value * rhs.m01.value + lhs.m01.value * rhs.m11.value + lhs.m02.value * rhs.m21.value + lhs.m03.value * rhs.m31.value) / zfloat.SCALE_10000;
            zm.m02.value = (lhs.m00.value * rhs.m02.value + lhs.m01.value * rhs.m12.value + lhs.m02.value * rhs.m22.value + lhs.m03.value * rhs.m32.value) / zfloat.SCALE_10000;
            zm.m03.value = (lhs.m00.value * rhs.m03.value + lhs.m01.value * rhs.m13.value + lhs.m02.value * rhs.m23.value + lhs.m03.value * rhs.m33.value) / zfloat.SCALE_10000;
            zm.m10.value = (lhs.m10.value * rhs.m00.value + lhs.m11.value * rhs.m10.value + lhs.m12.value * rhs.m20.value + lhs.m13.value * rhs.m30.value) / zfloat.SCALE_10000;
            zm.m11.value = (lhs.m10.value * rhs.m01.value + lhs.m11.value * rhs.m11.value + lhs.m12.value * rhs.m21.value + lhs.m13.value * rhs.m31.value) / zfloat.SCALE_10000;
            zm.m12.value = (lhs.m10.value * rhs.m02.value + lhs.m11.value * rhs.m12.value + lhs.m12.value * rhs.m22.value + lhs.m13.value * rhs.m32.value) / zfloat.SCALE_10000;
            zm.m13.value = (lhs.m10.value * rhs.m03.value + lhs.m11.value * rhs.m13.value + lhs.m12.value * rhs.m23.value + lhs.m13.value * rhs.m33.value) / zfloat.SCALE_10000;
            zm.m20.value = (lhs.m20.value * rhs.m00.value + lhs.m21.value * rhs.m10.value + lhs.m22.value * rhs.m20.value + lhs.m23.value * rhs.m30.value) / zfloat.SCALE_10000;
            zm.m21.value = (lhs.m20.value * rhs.m01.value + lhs.m21.value * rhs.m11.value + lhs.m22.value * rhs.m21.value + lhs.m23.value * rhs.m31.value) / zfloat.SCALE_10000;
            zm.m22.value = (lhs.m20.value * rhs.m02.value + lhs.m21.value * rhs.m12.value + lhs.m22.value * rhs.m22.value + lhs.m23.value * rhs.m32.value) / zfloat.SCALE_10000;
            zm.m23.value = (lhs.m20.value * rhs.m03.value + lhs.m21.value * rhs.m13.value + lhs.m22.value * rhs.m23.value + lhs.m23.value * rhs.m33.value) / zfloat.SCALE_10000;
            zm.m30.value = (lhs.m30.value * rhs.m00.value + lhs.m31.value * rhs.m10.value + lhs.m32.value * rhs.m20.value + lhs.m33.value * rhs.m30.value) / zfloat.SCALE_10000;
            zm.m31.value = (lhs.m30.value * rhs.m01.value + lhs.m31.value * rhs.m11.value + lhs.m32.value * rhs.m21.value + lhs.m33.value * rhs.m31.value) / zfloat.SCALE_10000;
            zm.m32.value = (lhs.m30.value * rhs.m02.value + lhs.m31.value * rhs.m12.value + lhs.m32.value * rhs.m22.value + lhs.m33.value * rhs.m32.value) / zfloat.SCALE_10000;
            zm.m33.value = (lhs.m30.value * rhs.m03.value + lhs.m31.value * rhs.m13.value + lhs.m32.value * rhs.m23.value + lhs.m33.value * rhs.m33.value) / zfloat.SCALE_10000;
            return zm;

            //return new zMatrix4x4
            //{
            //    m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20 + lhs.m03 * rhs.m30,
            //    m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21 + lhs.m03 * rhs.m31,
            //    m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22 + lhs.m03 * rhs.m32,
            //    m03 = lhs.m00 * rhs.m03 + lhs.m01 * rhs.m13 + lhs.m02 * rhs.m23 + lhs.m03 * rhs.m33,
            //    m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20 + lhs.m13 * rhs.m30,
            //    m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21 + lhs.m13 * rhs.m31,
            //    m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22 + lhs.m13 * rhs.m32,
            //    m13 = lhs.m10 * rhs.m03 + lhs.m11 * rhs.m13 + lhs.m12 * rhs.m23 + lhs.m13 * rhs.m33,
            //    m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20 + lhs.m23 * rhs.m30,
            //    m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21 + lhs.m23 * rhs.m31,
            //    m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22 + lhs.m23 * rhs.m32,
            //    m23 = lhs.m20 * rhs.m03 + lhs.m21 * rhs.m13 + lhs.m22 * rhs.m23 + lhs.m23 * rhs.m33,
            //    m30 = lhs.m30 * rhs.m00 + lhs.m31 * rhs.m10 + lhs.m32 * rhs.m20 + lhs.m33 * rhs.m30,
            //    m31 = lhs.m30 * rhs.m01 + lhs.m31 * rhs.m11 + lhs.m32 * rhs.m21 + lhs.m33 * rhs.m31,
            //    m32 = lhs.m30 * rhs.m02 + lhs.m31 * rhs.m12 + lhs.m32 * rhs.m22 + lhs.m33 * rhs.m32,
            //    m33 = lhs.m30 * rhs.m03 + lhs.m31 * rhs.m13 + lhs.m32 * rhs.m23 + lhs.m33 * rhs.m33
            //};
		}

		/* static zfloat t1 = new zfloat();
		 static zfloat t2 = new zfloat();
		 static zfloat t3 = new zfloat();
		 static zfloat t4 = new zfloat();*/
		public static void Multiply(ref zMatrix4x4 lhs, ref zMatrix4x4 rhs, ref zMatrix4x4 zm)
		{
			
			//  zMatrix4x4 zm = new zMatrix4x4();

			// return new zMatrix4x4
			// {

			//m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20 + lhs.m03 * rhs.m30,
            zm.m00.value = (lhs.m00.value * rhs.m00.value + lhs.m01.value * rhs.m10.value + lhs.m02.value * rhs.m20.value + lhs.m03.value * rhs.m30.value) / zfloat.SCALE_10000;

			//m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21 + lhs.m03 * rhs.m31,
            zm.m01.value = (lhs.m00.value * rhs.m01.value + lhs.m01.value * rhs.m11.value + lhs.m02.value * rhs.m21.value + lhs.m03.value * rhs.m31.value) / zfloat.SCALE_10000;

			//m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22 + lhs.m03 * rhs.m32,
            zm.m02.value = (lhs.m00.value * rhs.m02.value + lhs.m01.value * rhs.m12.value + lhs.m02.value * rhs.m22.value + lhs.m03.value * rhs.m32.value) / zfloat.SCALE_10000;

			//m03 = lhs.m00 * rhs.m03 + lhs.m01 * rhs.m13 + lhs.m02 * rhs.m23 + lhs.m03 * rhs.m33,
            zm.m03.value = (lhs.m00.value * rhs.m03.value + lhs.m01.value * rhs.m13.value + lhs.m02.value * rhs.m23.value + lhs.m03.value * rhs.m33.value) / zfloat.SCALE_10000;

			//m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20 + lhs.m13 * rhs.m30,
            zm.m10.value = (lhs.m10.value * rhs.m00.value + lhs.m11.value * rhs.m10.value + lhs.m12.value * rhs.m20.value + lhs.m13.value * rhs.m30.value) / zfloat.SCALE_10000;
			
			//m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21 + lhs.m13 * rhs.m31,
            zm.m11.value = (lhs.m10.value * rhs.m01.value + lhs.m11.value * rhs.m11.value + lhs.m12.value * rhs.m21.value + lhs.m13.value * rhs.m31.value) / zfloat.SCALE_10000;
			
			//m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22 + lhs.m13 * rhs.m32,
            zm.m12.value = (lhs.m10.value * rhs.m02.value + lhs.m11.value * rhs.m12.value + lhs.m12.value * rhs.m22.value + lhs.m13.value * rhs.m32.value) / zfloat.SCALE_10000;
			
			//m13 = lhs.m10 * rhs.m03 + lhs.m11 * rhs.m13 + lhs.m12 * rhs.m23 + lhs.m13 * rhs.m33,
            zm.m13.value = (lhs.m10.value * rhs.m03.value + lhs.m11.value * rhs.m13.value + lhs.m12.value * rhs.m23.value + lhs.m13.value * rhs.m33.value) / zfloat.SCALE_10000;
			
			//m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20 + lhs.m23 * rhs.m30,
            zm.m20.value = (lhs.m20.value * rhs.m00.value + lhs.m21.value * rhs.m10.value + lhs.m22.value * rhs.m20.value + lhs.m23.value * rhs.m30.value) / zfloat.SCALE_10000;
			
			//m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21 + lhs.m23 * rhs.m31,
            zm.m21.value = (lhs.m20.value * rhs.m01.value + lhs.m21.value * rhs.m11.value + lhs.m22.value * rhs.m21.value + lhs.m23.value * rhs.m31.value) / zfloat.SCALE_10000;
			
			//m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22 + lhs.m23 * rhs.m32,
            zm.m22.value = (lhs.m20.value * rhs.m02.value + lhs.m21.value * rhs.m12.value + lhs.m22.value * rhs.m22.value + lhs.m23.value * rhs.m32.value) / zfloat.SCALE_10000;
			
			//m23 = lhs.m20 * rhs.m03 + lhs.m21 * rhs.m13 + lhs.m22 * rhs.m23 + lhs.m23 * rhs.m33,
            zm.m23.value = (lhs.m20.value * rhs.m03.value + lhs.m21.value * rhs.m13.value + lhs.m22.value * rhs.m23.value + lhs.m23.value * rhs.m33.value) / zfloat.SCALE_10000;
			
			//m30 = lhs.m30 * rhs.m00 + lhs.m31 * rhs.m10 + lhs.m32 * rhs.m20 + lhs.m33 * rhs.m30,
            zm.m30.value = (lhs.m30.value * rhs.m00.value + lhs.m31.value * rhs.m10.value + lhs.m32.value * rhs.m20.value + lhs.m33.value * rhs.m30.value) / zfloat.SCALE_10000;
			
			//m31 = lhs.m30 * rhs.m01 + lhs.m31 * rhs.m11 + lhs.m32 * rhs.m21 + lhs.m33 * rhs.m31,
            zm.m31.value = (lhs.m30.value * rhs.m01.value + lhs.m31.value * rhs.m11.value + lhs.m32.value * rhs.m21.value + lhs.m33.value * rhs.m31.value) / zfloat.SCALE_10000;
			
			//m32 = lhs.m30 * rhs.m02 + lhs.m31 * rhs.m12 + lhs.m32 * rhs.m22 + lhs.m33 * rhs.m32,
            zm.m32.value = (lhs.m30.value * rhs.m02.value + lhs.m31.value * rhs.m12.value + lhs.m32.value * rhs.m22.value + lhs.m33.value * rhs.m32.value) / zfloat.SCALE_10000;
			
			//m33 = lhs.m30 * rhs.m03 + lhs.m31 * rhs.m13 + lhs.m32 * rhs.m23 + lhs.m33 * rhs.m33
            zm.m33.value = (lhs.m30.value * rhs.m03.value + lhs.m31.value * rhs.m13.value + lhs.m32.value * rhs.m23.value + lhs.m33.value * rhs.m33.value) / zfloat.SCALE_10000;
			
			// };

			// return zm;
		}

		public static void Multiply(ref zMatrix4x4 lhs, ref zVector4 v, ref zVector4 result)
		{
			//result.x = lhs.m00 * v.x + lhs.m01 * v.y + lhs.m02 * v.z + lhs.m03 * v.w;
            result.x.value = (lhs.m00.value * v.x.value + lhs.m01.value * v.y.value + lhs.m02.value * v.z.value + lhs.m03.value * v.w.value) / zfloat.SCALE_10000;

			//result.y = lhs.m10 * v.x + lhs.m11 * v.y + lhs.m12 * v.z + lhs.m13 * v.w;
            result.y.value = (lhs.m10.value * v.x.value + lhs.m11.value * v.y.value + lhs.m12.value * v.z.value + lhs.m13.value * v.w.value) / zfloat.SCALE_10000;

			//result.z = lhs.m20 * v.x + lhs.m21 * v.y + lhs.m22 * v.z + lhs.m23 * v.w;
            result.z.value = (lhs.m20.value * v.x.value + lhs.m21.value * v.y.value + lhs.m22.value * v.z.value + lhs.m23.value * v.w.value) / zfloat.SCALE_10000;

			//result.w = lhs.m30 * v.x + lhs.m31 * v.y + lhs.m32 * v.z + lhs.m33 * v.w;
            result.w.value = (lhs.m30.value * v.x.value + lhs.m31.value * v.y.value + lhs.m32.value * v.z.value + lhs.m33.value * v.w.value) / zfloat.SCALE_10000;

		}


		public static zVector4 operator *(zMatrix4x4 lhs, zVector4 v)
		{
            //zVector4 result;
            //result.x = lhs.m00 * v.x + lhs.m01 * v.y + lhs.m02 * v.z + lhs.m03 * v.w;
            //result.y = lhs.m10 * v.x + lhs.m11 * v.y + lhs.m12 * v.z + lhs.m13 * v.w;
            //result.z = lhs.m20 * v.x + lhs.m21 * v.y + lhs.m22 * v.z + lhs.m23 * v.w;
            //result.w = lhs.m30 * v.x + lhs.m31 * v.y + lhs.m32 * v.z + lhs.m33 * v.w;
            //return result;

            zVector4 result;
            result.x.value = (lhs.m00.value * v.x.value + lhs.m01.value * v.y.value + lhs.m02.value * v.z.value + lhs.m03.value * v.w.value) / zfloat.SCALE_10000;
            result.y.value = (lhs.m10.value * v.x.value + lhs.m11.value * v.y.value + lhs.m12.value * v.z.value + lhs.m13.value * v.w.value) / zfloat.SCALE_10000;
            result.z.value = (lhs.m20.value * v.x.value + lhs.m21.value * v.y.value + lhs.m22.value * v.z.value + lhs.m23.value * v.w.value) / zfloat.SCALE_10000;
            result.w.value = (lhs.m30.value * v.x.value + lhs.m31.value * v.y.value + lhs.m32.value * v.z.value + lhs.m33.value * v.w.value) / zfloat.SCALE_10000;
            return result;
		}
		public static bool operator ==(zMatrix4x4 lhs, zMatrix4x4 rhs)
		{
			return lhs.GetColumn(0) == rhs.GetColumn(0) && lhs.GetColumn(1) == rhs.GetColumn(1) && lhs.GetColumn(2) == rhs.GetColumn(2) && lhs.GetColumn(3) == rhs.GetColumn(3);
		}
		public static bool operator !=(zMatrix4x4 lhs, zMatrix4x4 rhs)
		{
			return !(lhs == rhs);
		}

	}
}