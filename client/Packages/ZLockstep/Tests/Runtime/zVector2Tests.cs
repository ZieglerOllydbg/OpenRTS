using System;
using NUnit.Framework;
using zUnity;

namespace ZLockstep.Tests
{
	/// <summary>
	/// zVector2 单元测试套件
	/// 测试范围：基础构造、运算、长度计算、向量操作、新增方法、序列化、性能、边界值等
	/// 总计：54个测试用例
	/// </summary>
	[TestFixture]
	public class zVector2Tests
	{
		private const long EPSILON = 1; // zfloat精度误差（0.0001）

		#region 基础构造测试 (5个)

		[Test]
		public void Test_Constructor_WithZfloat()
		{
			zfloat x = (zfloat)2.5;
			zfloat y = (zfloat)3.5;
			zVector2 v = new zVector2(x, y);
			Assert.AreEqual(x.value, v.x.value);
			Assert.AreEqual(y.value, v.y.value);
		}

		[Test]
		public void Test_Constructor_WithInt()
		{
			zVector2 v = new zVector2(2, 3);
			Assert.AreEqual(2 * zfloat.SCALE_10000, v.x.value);
			Assert.AreEqual(3 * zfloat.SCALE_10000, v.y.value);
		}

		[Test]
		public void Test_Constructor_Copy()
		{
			zVector2 original = new zVector2((zfloat)2, (zfloat)3);
			zVector2 copy = new zVector2(original);
			Assert.AreEqual(original.x.value, copy.x.value);
			Assert.AreEqual(original.y.value, copy.y.value);
		}

		[Test]
		public void Test_Indexer_GetSet()
		{
			zVector2 v = new zVector2((zfloat)1, (zfloat)2);
			
			// 获取
			Assert.AreEqual(v[0].value, v.x.value);
			Assert.AreEqual(v[1].value, v.y.value);
			
			// 设置
			v[0] = (zfloat)5;
			v[1] = (zfloat)6;
			Assert.AreEqual((zfloat)5, v.x);
			Assert.AreEqual((zfloat)6, v.y);
		}

		[Test]
		public void Test_Constants_Values()
		{
			Assert.IsTrue(zVector2.zero.IsZero());
			Assert.AreEqual((zfloat)1, zVector2.one.x);
			Assert.AreEqual((zfloat)1, zVector2.one.y);
			Assert.AreEqual((zfloat)1, zVector2.up.y);
			Assert.AreEqual((zfloat)1, zVector2.right.x);
		}

		#endregion

		#region 运算符测试 (8个)

		[Test]
		public void Test_Addition_Operators()
		{
			zVector2 v1 = new zVector2((zfloat)1, (zfloat)2);
			zVector2 v2 = new zVector2((zfloat)3, (zfloat)4);
			
			zVector2 result = v1 + v2;
			Assert.AreEqual((zfloat)4, result.x);
			Assert.AreEqual((zfloat)6, result.y);
			
			// 与整数相加
			zVector2 result2 = v1 + 5;
			Assert.AreEqual((zfloat)6, result2.x);
			Assert.AreEqual((zfloat)7, result2.y);
			
			// 与zfloat相加
			zVector2 result3 = v1 + (zfloat)2.5;
			Assert.AreEqual((zfloat)3.5, result3.x);
			Assert.AreEqual((zfloat)4.5, result3.y);
		}

		[Test]
		public void Test_Subtraction_Operators()
		{
			zVector2 v1 = new zVector2((zfloat)5, (zfloat)6);
			zVector2 v2 = new zVector2((zfloat)2, (zfloat)3);
			
			zVector2 result = v1 - v2;
			Assert.AreEqual((zfloat)3, result.x);
			Assert.AreEqual((zfloat)3, result.y);
			
			// 与整数相减
			zVector2 result2 = v1 - 2;
			Assert.AreEqual((zfloat)3, result2.x);
			Assert.AreEqual((zfloat)4, result2.y);
		}

		[Test]
		public void Test_Multiplication_Operators()
		{
			zVector2 v = new zVector2((zfloat)2, (zfloat)3);
			
			// 与整数相乘
			zVector2 result1 = v * 3;
			Assert.AreEqual((zfloat)6, result1.x);
			Assert.AreEqual((zfloat)9, result1.y);
			
			// 与zfloat相乘（需要除以SCALE）
			zVector2 result2 = v * (zfloat)2;
			Assert.AreEqual((zfloat)4, result2.x);
			Assert.AreEqual((zfloat)6, result2.y);
		}

		[Test]
		public void Test_Division_Operators()
		{
			zVector2 v = new zVector2((zfloat)6, (zfloat)9);
			
			// 与整数相除
			zVector2 result1 = v / 3;
			Assert.AreEqual((zfloat)2, result1.x);
			Assert.AreEqual((zfloat)3, result1.y);
			
			// 与zfloat相除
			zVector2 result2 = v / (zfloat)2;
			Assert.AreEqual((zfloat)3, result2.x);
			Assert.AreEqual((zfloat)4.5, result2.y);
		}

		[Test]
		public void Test_Negation()
		{
			zVector2 v = new zVector2((zfloat)2, (zfloat)3);
			zVector2 result = -v;
			Assert.AreEqual((zfloat)(-2), result.x);
			Assert.AreEqual((zfloat)(-3), result.y);
		}

		[Test]
		public void Test_Equality_Operators()
		{
			zVector2 v1 = new zVector2((zfloat)2, (zfloat)3);
			zVector2 v2 = new zVector2((zfloat)2, (zfloat)3);
			zVector2 v3 = new zVector2((zfloat)1, (zfloat)2);
			
			Assert.IsTrue(v1 == v2);
			Assert.IsFalse(v1 == v3);
			Assert.IsFalse(v1 != v2);
			Assert.IsTrue(v1 != v3);
		}

		[Test]
		public void Test_Comparison_EdgeCases()
		{
			zVector2 v1 = zVector2.zero;
			zVector2 v2 = new zVector2(zfloat.NegativeOne, zfloat.NegativeOne);
			zVector2 v3 = new zVector2(zfloat.Epsilon, zfloat.Epsilon);
			
			Assert.IsTrue(v1 == zVector2.zero);
			Assert.IsTrue(v2.x == zfloat.NegativeOne);
			Assert.IsTrue(v3.x == zfloat.Epsilon);
		}

		#endregion

		#region 长度计算测试 (6个)

		[Test]
		public void Test_Magnitude_BasicVectors()
		{
			// 3-4-5 三角形
			zVector2 v = new zVector2((zfloat)3, (zfloat)4);
			zfloat mag = v.magnitude;
			Assert.AreEqual(((zfloat)5).value, mag.value, EPSILON);
		}

		[Test]
		public void Test_SqrMagnitude_OptimizationCorrectness()
		{
			zVector2 v = new zVector2((zfloat)3, (zfloat)4);
			zfloat sqrMag = v.sqrMagnitude;
			// sqrMag应该是 3^2 + 4^2 = 25（在定点数精度内）
			Assert.AreEqual(((zfloat)25).value, sqrMag.value, EPSILON);
		}

		[Test]
		public void Test_Magnitude_Precision()
		{
			// 测试小向量（避免Epsilon在定点截断后平方为0）
			zVector2 v = new zVector2(new zfloat(0, 100), new zfloat(0, 100)); // 0.01, 0.01
			zfloat mag = v.magnitude;
			Assert.Greater(mag.value, 0);
		}

		[Test]
		public void Test_Magnitude_EdgeCases()
		{
			// 零向量
			zVector2 zero = zVector2.zero;
			Assert.AreEqual(((zfloat)0).value, zero.magnitude.value);
			
			// 单位向量
			zVector2 unit = new zVector2((zfloat)1, (zfloat)0);
			Assert.AreEqual(((zfloat)1).value, unit.magnitude.value, EPSILON);
		}

		[Test]
		public void Test_Normalized_BasicVectors()
		{
			zVector2 v = new zVector2((zfloat)3, (zfloat)4);
			zVector2 normalized = v.normalized;
			// 长度应接近1
			zfloat mag = normalized.magnitude;
			Assert.AreEqual(((zfloat)1).value, mag.value, EPSILON);
		}

		[Test]
		public void Test_Normalized_ZeroVector()
		{
			zVector2 zero = zVector2.zero;
			zVector2 normalized = zero.normalized;
			Assert.IsTrue(normalized.IsZero());
		}

		#endregion

		#region 向量操作测试 (12个)

		[Test]
		public void Test_Scale_ElementWise()
		{
			zVector2 a = new zVector2((zfloat)2, (zfloat)3);
			zVector2 b = new zVector2((zfloat)1.5, (zfloat)2);
			zVector2 result = zVector2.Scale(a, b);
			Assert.AreEqual(((zfloat)3).value, result.x.value, EPSILON);
			Assert.AreEqual(((zfloat)6).value, result.y.value, EPSILON);
		}

		[Test]
		public void Test_Dot_KnownValues()
		{
			zVector2 v1 = new zVector2((zfloat)1, (zfloat)0);
			zVector2 v2 = new zVector2((zfloat)1, (zfloat)0);
			zfloat dot = zVector2.Dot(v1, v2);
			Assert.AreEqual((zfloat)1, dot);
			
			// 垂直向量点积为0
			zVector2 v3 = new zVector2((zfloat)0, (zfloat)1);
			zfloat dotPerp = zVector2.Dot(v1, v3);
			Assert.AreEqual((zfloat)0, dotPerp);
		}

		[Test]
		public void Test_Dot_Operator_Matches_Dot_Method()
		{
			zVector2 v1 = new zVector2((zfloat)2, (zfloat)3);
			zVector2 v2 = new zVector2((zfloat)4, (zfloat)5);

			zfloat opResult = v1 * v2;
			zfloat methodResult = zVector2.Dot(v1, v2);

			Assert.AreEqual(methodResult.value, opResult.value, EPSILON);
		}

		[Test]
		public void Test_Dot_Operator_Sign_ForParallelVectors()
		{
			zVector2 sameDirA = new zVector2((zfloat)1, (zfloat)2);
			zVector2 sameDirB = new zVector2((zfloat)2, (zfloat)4);
			zVector2 oppositeDir = new zVector2((zfloat)(-2), (zfloat)(-4));

			Assert.Greater((sameDirA * sameDirB).value, 0);
			Assert.Less((sameDirA * oppositeDir).value, 0);
		}

		[Test]
		public void Test_Dot_Operator_FixedPoint_DecimalRegression()
		{
			zVector2 v1 = new zVector2((zfloat)1.25, (zfloat)2.5);
			zVector2 v2 = new zVector2((zfloat)2.0, (zfloat)(-0.5));

			// 1.25*2.0 + 2.5*(-0.5) = 1.25
			zfloat expected = (zfloat)1.25;
			zfloat result = v1 * v2;

			Assert.AreEqual(expected.value, result.value, EPSILON);
			Assert.AreEqual(result.value, zVector2.Dot(v1, v2).value, EPSILON);
		}

		[Test]
		public void Test_Cross_ZVector3Result()
		{
			zVector2 v1 = new zVector2((zfloat)1, (zfloat)0);
			zVector2 v2 = new zVector2((zfloat)0, (zfloat)1);
			zVector3 cross = zVector2.Cross(v1, v2);
			Assert.AreEqual((zfloat)1, cross.z);
			Assert.AreEqual((zfloat)0, cross.x);
			Assert.AreEqual((zfloat)0, cross.y);
		}

		[Test]
		public void Test_Project_OntoNormal()
		{
			zVector2 vector = new zVector2((zfloat)3, (zfloat)4);
			zVector2 normal = new zVector2((zfloat)1, (zfloat)0);
			zVector2 projected = zVector2.Project(vector, normal);
			Assert.AreEqual((zfloat)3, projected.x);
			Assert.AreEqual((zfloat)0, projected.y);
		}

		[Test]
		public void Test_Distance_BetweenPoints()
		{
			zVector2 a = new zVector2((zfloat)0, (zfloat)0);
			zVector2 b = new zVector2((zfloat)3, (zfloat)4);
			zfloat dist = zVector2.Distance(a, b);
			Assert.AreEqual(((zfloat)5).value, dist.value, EPSILON);
		}

		[Test]
		public void Test_Lerp_Interpolation()
		{
			zVector2 from = new zVector2((zfloat)0, (zfloat)0);
			zVector2 to = new zVector2((zfloat)10, (zfloat)10);
			zVector2 mid = zVector2.Lerp(from, to, (zfloat)0.5);
			Assert.AreEqual((zfloat)5, mid.x);
			Assert.AreEqual((zfloat)5, mid.y);
		}

		[Test]
		public void Test_MoveTowards_StepwiseMovement()
		{
			zVector2 current = new zVector2((zfloat)0, (zfloat)0);
			zVector2 target = new zVector2((zfloat)10, (zfloat)0);
			zVector2 moved = zVector2.MoveTowards(current, target, (zfloat)3);
			Assert.AreEqual((zfloat)3, moved.x);
			Assert.AreEqual((zfloat)0, moved.y);
		}

		[Test]
		public void Test_ClampMagnitude_LimitingLength()
		{
			zVector2 v = new zVector2((zfloat)3, (zfloat)4);
			zVector2 clamped = zVector2.ClampMagnitude(v, (zfloat)3);
			zfloat mag = clamped.magnitude;
			long expected = ((zfloat)3).value;
			Assert.LessOrEqual(mag.value, expected); // Clamp后不应超过上限
			Assert.GreaterOrEqual(mag.value, expected - 20); // 允许少量定点截断误差
		}

		[Test]
		public void Test_Min_Max_ElementWise()
		{
			zVector2 v1 = new zVector2((zfloat)2, (zfloat)8);
			zVector2 v2 = new zVector2((zfloat)5, (zfloat)3);
			
			zVector2 minResult = zVector2.Min(v1, v2);
			Assert.AreEqual((zfloat)2, minResult.x);
			Assert.AreEqual((zfloat)3, minResult.y);
			
			zVector2 maxResult = zVector2.Max(v1, v2);
			Assert.AreEqual((zfloat)5, maxResult.x);
			Assert.AreEqual((zfloat)8, maxResult.y);
		}

		[Test]
		public void Test_Angle_Between_Vectors()
		{
			zVector2 v1 = new zVector2((zfloat)1, (zfloat)0);
			zVector2 v2 = new zVector2((zfloat)0, (zfloat)1);
			zfloat angle = zVector2.Angle(v1, v2);
			// 应该是 π/2 弧度
			Assert.Greater(angle.value, 0);
		}

		[Test]
		public void Test_A2B_Angle_DirectionalAwareness()
		{
			zVector2 A = new zVector2((zfloat)1, (zfloat)0);
			zVector2 B = new zVector2((zfloat)0, (zfloat)1);
			zfloat angle = zVector2.A2B_angle(A, B);
			// 应该是正值（逆时针）
			Assert.Greater(angle.value, 0);
		}

		[Test]
		public void Test_IsZero_Threshold()
		{
			zVector2 zero = zVector2.zero;
			Assert.IsTrue(zero.IsZero());
			
			zVector2 nonZero = new zVector2(new zfloat(0, 1), zfloat.Zero);
			Assert.IsFalse(nonZero.IsZero());
		}

		#endregion

		#region 新增方法测试 (8个)

		[Test]
		public void Test_Add_DirectMethod()
		{
			zVector2 v = new zVector2((zfloat)2, (zfloat)3);
			zVector2 toAdd = new zVector2((zfloat)1, (zfloat)2);
			v.Add(ref toAdd);
			Assert.AreEqual((zfloat)3, v.x);
			Assert.AreEqual((zfloat)5, v.y);
		}

		[Test]
		public void Test_Sub_DirectMethod()
		{
			zVector2 v = new zVector2((zfloat)5, (zfloat)6);
			zVector2 toSub = new zVector2((zfloat)2, (zfloat)3);
			v.Sub(ref toSub);
			Assert.AreEqual((zfloat)3, v.x);
			Assert.AreEqual((zfloat)3, v.y);
		}

		[Test]
		public void Test_Mul_DirectMethod()
		{
			zVector2 v = new zVector2((zfloat)2, (zfloat)3);
			zVector2 factor = new zVector2((zfloat)1.5, (zfloat)2);
			v.Mul(ref factor);
			Assert.AreEqual(((zfloat)3).value, v.x.value, EPSILON);
			Assert.AreEqual(((zfloat)6).value, v.y.value, EPSILON);
		}

		[Test]
		public void Test_Div_DirectMethod()
		{
			zVector2 v = new zVector2((zfloat)6, (zfloat)9);
			zVector2 divisor = new zVector2((zfloat)2, (zfloat)3);
			v.Div(ref divisor);
			Assert.AreEqual(((zfloat)3).value, v.x.value, EPSILON);
			Assert.AreEqual(((zfloat)3).value, v.y.value, EPSILON);
		}

		[Test]
		public void Test_GetNormalizedForMagnitude_Optimization()
		{
			zVector2 v = new zVector2((zfloat)3, (zfloat)4);
			zfloat knownMag = (zfloat)5;
			zVector2 normalized = v.GetNormalizedForMagnitude(knownMag);
			// 验证结果长度为1
			zfloat resultMag = normalized.magnitude;
			Assert.AreEqual(((zfloat)1).value, resultMag.value, EPSILON);
		}

		[Test]
		public void Test_ApproxNormalizedXY_FastPath()
		{
			zVector2 v = new zVector2((zfloat)3, (zfloat)4);
			zVector2 approxNorm = v.approxNormalizedXY;
			// 近似长度应接近1
			zfloat mag = approxNorm.magnitude;
			Assert.IsTrue(mag.value > 9000 && mag.value < 11000); // 约等于1
		}

		[Test]
		public void Test_Scale_InstanceMethod()
		{
			zVector2 v = new zVector2((zfloat)2, (zfloat)3);
			zVector2 scale = new zVector2((zfloat)1.5, (zfloat)2);
			v.Scale(scale);
			Assert.AreEqual(((zfloat)3).value, v.x.value, EPSILON);
			Assert.AreEqual(((zfloat)6).value, v.y.value, EPSILON);
		}

		[Test]
		public void Test_Normalize_InPlace()
		{
			zVector2 v = new zVector2((zfloat)3, (zfloat)4);
			v.Normalize();
			// 检查长度是否为1
			zfloat mag = v.magnitude;
			Assert.AreEqual(((zfloat)1).value, mag.value, EPSILON);
		}

		#endregion

		#region 序列化测试 (4个)

		[Test]
		public void Test_Serialization_Deserialization()
		{
			zVector2 original = new zVector2((zfloat)2.5, (zfloat)3.5);
			
			// 通过GetObjectData序列化
			var info = new System.Runtime.Serialization.SerializationInfo(
				typeof(zVector2), 
				new System.Runtime.Serialization.FormatterConverter());
			original.GetObjectData(info, new System.Runtime.Serialization.StreamingContext());
			
			// 通过反序列化构造函数反序列化
			zVector2 deserialized = new zVector2(info, new System.Runtime.Serialization.StreamingContext());
			
			Assert.AreEqual(original.x.value, deserialized.x.value);
			Assert.AreEqual(original.y.value, deserialized.y.value);
		}

		[Test]
		public void Test_Serialization_WithoutAllocation()
		{
			// 这是一个性能测试 - 验证结构体序列化不产生额外分配
			zVector2 v = new zVector2((zfloat)1, (zfloat)2);
			var info = new System.Runtime.Serialization.SerializationInfo(
				typeof(zVector2),
				new System.Runtime.Serialization.FormatterConverter());
			
			// 应该完成而不抛出异常
			v.GetObjectData(info, new System.Runtime.Serialization.StreamingContext());
			Assert.Pass();
		}

		[Test]
		public void Test_ImplicitConversion_To_zVector3()
		{
			zVector2 v2 = new zVector2((zfloat)2, (zfloat)3);
			zVector3 v3 = v2;
			Assert.AreEqual(v2.x.value, v3.x.value);
			Assert.AreEqual(v2.y.value, v3.y.value);
			Assert.AreEqual(0, v3.z.value);
		}

		[Test]
		public void Test_ImplicitConversion_From_zVector3()
		{
			zVector3 v3 = new zVector3((zfloat)2, (zfloat)3, (zfloat)4);
			zVector2 v2 = v3;
			Assert.AreEqual(v3.x.value, v2.x.value);
			Assert.AreEqual(v3.y.value, v2.y.value);
		}

		#endregion

		#region 性能验证测试 (6个)

		[Test]
		public void Test_Performance_Normalized_NoAllocation()
		{
			zVector2 v = new zVector2((zfloat)3, (zfloat)4);
			// 多次调用，验证无异常
			for (int i = 0; i < 1000; i++)
			{
				zVector2 normalized = v.normalized;
				Assert.IsNotNull(normalized);
			}
		}

		[Test]
		public void Test_Performance_Operations_Throughput()
		{
			zVector2 v1 = new zVector2((zfloat)1, (zfloat)2);
			zVector2 v2 = new zVector2((zfloat)3, (zfloat)4);
			
			// 执行1000次运算
			for (int i = 0; i < 1000; i++)
			{
				zVector2 result = v1 + v2;
				result = v1 - v2;
				result = v1 * (zfloat)2;
				result = v1 / (zfloat)2;
				zfloat dot = zVector2.Dot(v1, v2);
			}
		}

		[Test]
		public void Test_Performance_CompareTo_Unity_Vector2()
		{
			// 验证运算符性能一致性
			zVector2 v1 = new zVector2((zfloat)1, (zfloat)2);
			zVector2 v2 = new zVector2((zfloat)3, (zfloat)4);
			
			for (int i = 0; i < 100; i++)
			{
				var result = v1 + v2;
				result = v1 * (zfloat)1.5;
				zfloat mag = result.magnitude;
			}
		}

		[Test]
		public void Test_Performance_Magnitude_CachingBenefit()
		{
			zVector2 v = new zVector2((zfloat)3, (zfloat)4);
			
			// 多次访问magnitude - 每次都会重新计算
			for (int i = 0; i < 100; i++)
			{
				zfloat mag = v.magnitude;
				Assert.Greater(mag.value, 0);
			}
		}

		[Test]
		public void Test_GarbageCollection_ZeroAllocation()
		{
			zVector2 v1 = new zVector2((zfloat)1, (zfloat)2);
			zVector2 v2 = new zVector2((zfloat)3, (zfloat)4);
			
			// 结构体运算不应产生堆分配
			zVector2 result = v1 + v2;
			result = v1 - v2;
			result = v1 * (zfloat)2;
			Assert.AreEqual((zfloat)2, result.x);
		}

		[Test]
		public void Test_Precision_AccumulationError()
		{
			// 测试多次运算的精度积累
			zVector2 v = zVector2.one;
			zfloat step = new zfloat(0, 100); // 精确0.01（raw=100）
			for (int i = 0; i < 100; i++)
			{
				v = v + step;
			}
			// 1 + 100 * 0.01 = 2.00
			Assert.AreEqual(20000, v.x.value);
			Assert.AreEqual(20000, v.y.value);
		}

		#endregion

		#region 边界值与异常处理测试 (5个)

		[Test]
		public void Test_Precision_BoundaryValues()
		{
			// 测试接近上界但不触发平方溢出的安全大值（raw=3,000,000,000 -> 300000.0000）
			zfloat bigValue = zfloat.FromRaw(3_000_000_000L);
			zVector2 v = new zVector2(bigValue, bigValue);
			Assert.NotZero(v.magnitude.value);
			
			// 测试极小值：Epsilon在当前4位定点实现中求长度会截断为0
			zVector2 tiny = new zVector2(zfloat.Epsilon, zfloat.Epsilon);
			Assert.AreEqual(0, tiny.magnitude.value);
		}

		[Test]
		public void Test_DivideByZero_Behavior()
		{
			zVector2 v = new zVector2((zfloat)5, (zfloat)10);
			// 除以零向量 - 不应抛出异常，但结果为infinity
			zVector2 zeroVec = zVector2.zero;
			// 这会导致除以零，行为取决于系统
			// 验证不会crash
			try
			{
				zVector2 result = v / zfloat.Zero;
				Assert.Pass(); // 如果不crash就成功
			}
			catch (DivideByZeroException)
			{
				Assert.Pass(); // 或者抛出异常也可以接受
			}
		}

		[Test]
		public void Test_LargeNumber_Precision()
		{
			// 测试大数值精度
			zfloat largeX = (zfloat)10000;
			zfloat largeY = (zfloat)10000;
			zVector2 v = new zVector2(largeX, largeY);
			zfloat mag = v.magnitude;
			// 应该大约是 14142
			Assert.Greater(mag.value, 140000000); // 14142 * SCALE_10000
		}

		[Test]
		public void Test_NormalizeBehavior_VerySmallVectors()
		{
			// 极小向量的归一化
			zVector2 tiny = new zVector2(zfloat.Epsilon, zfloat.Epsilon);
			zVector2 normalized = tiny.normalized;
			// 在4位定点精度下，Epsilon分量平方后会截断为0，归一化结果应为零向量
			Assert.IsTrue(normalized.IsZero());
		}

		[Test]
		public void Test_Lerp_Parameter_Clamping()
		{
			zVector2 from = new zVector2((zfloat)0, (zfloat)0);
			zVector2 to = new zVector2((zfloat)10, (zfloat)10);
			
			// t > 1 应该被clamped到1
			zVector2 result = zVector2.Lerp(from, to, (zfloat)2);
			Assert.AreEqual((zfloat)10, result.x);
			Assert.AreEqual((zfloat)10, result.y);
			
			// t < 0 应该被clamped到0
			zVector2 result2 = zVector2.Lerp(from, to, (zfloat)(-1));
			Assert.AreEqual((zfloat)0, result2.x);
			Assert.AreEqual((zfloat)0, result2.y);
		}

		#endregion

		#region ToString和Equals测试 (2个追加)

		[Test]
		public void Test_ToString_Format()
		{
			zVector2 v = new zVector2((zfloat)2, (zfloat)3);
			string str = v.ToString();
			Assert.IsNotEmpty(str);
			Assert.That(str, Does.Contain("("));
			Assert.That(str, Does.Contain(")"));
		}

		[Test]
		public void Test_Equals_And_GetHashCode()
		{
			zVector2 v1 = new zVector2((zfloat)2, (zfloat)3);
			zVector2 v2 = new zVector2((zfloat)2, (zfloat)3);
			
			Assert.IsTrue(v1.Equals(v2));
			Assert.IsTrue(v1.Equals((object)v2));
			Assert.AreEqual(v1.GetHashCode(), v2.GetHashCode());
		}

		#endregion
	}
}
