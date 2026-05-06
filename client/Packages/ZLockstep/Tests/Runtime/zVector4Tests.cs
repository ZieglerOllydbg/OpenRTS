using System;
using NUnit.Framework;

namespace zUnity.Tests
{
	/// <summary>
	/// zVector4 单元测试类
	/// 测试四维定点数向量的各种功能
	/// </summary>
	[TestFixture]
	public class zVector4Tests
	{
		#region 构造函数测试

		[Test]
		public void Constructor_WithZfloatComponents_CreatesVector()
		{
			zfloat x = (zfloat)1.5f;
			zfloat y = (zfloat)2.5f;
			zfloat z = (zfloat)3.5f;
			zfloat w = (zfloat)4.5f;

			zVector4 vec = new zVector4(x, y, z, w);

			Assert.AreEqual(x, vec.x);
			Assert.AreEqual(y, vec.y);
			Assert.AreEqual(z, vec.z);
			Assert.AreEqual(w, vec.w);
		}

		[Test]
		public void Constructor_WithIntComponents_CreatesVector()
		{
			zVector4 vec = new zVector4(1, 2, 3, 4);

			Assert.AreEqual((zfloat)1, vec.x);
			Assert.AreEqual((zfloat)2, vec.y);
			Assert.AreEqual((zfloat)3, vec.z);
			Assert.AreEqual((zfloat)4, vec.w);
		}

		[Test]
		public void CopyConstructor_CopiesAllComponents()
		{
			zVector4 original = new zVector4(1.5f, 2.5f, 3.5f, 4.5f);
			zVector4 copy = new zVector4(original);

			Assert.AreEqual(original.x, copy.x);
			Assert.AreEqual(original.y, copy.y);
			Assert.AreEqual(original.z, copy.z);
			Assert.AreEqual(original.w, copy.w);
		}

		#endregion

		#region 常量测试

		[Test]
		public void ZeroConstant_IsCorrect()
		{
			Assert.AreEqual((zfloat)0, zVector4.zero.x);
			Assert.AreEqual((zfloat)0, zVector4.zero.y);
			Assert.AreEqual((zfloat)0, zVector4.zero.z);
			Assert.AreEqual((zfloat)0, zVector4.zero.w);
		}

		[Test]
		public void OneConstant_IsCorrect()
		{
			Assert.AreEqual((zfloat)1, zVector4.one.x);
			Assert.AreEqual((zfloat)1, zVector4.one.y);
			Assert.AreEqual((zfloat)1, zVector4.one.z);
			Assert.AreEqual((zfloat)1, zVector4.one.w);
		}

		[Test]
		public void DirectionalConstants_AreCorrect()
		{
			Assert.AreEqual((zfloat)0, zVector4.forward.x);
			Assert.AreEqual((zfloat)0, zVector4.forward.y);
			Assert.AreEqual((zfloat)1, zVector4.forward.z);
			Assert.AreEqual((zfloat)0, zVector4.forward.w);

			Assert.AreEqual((zfloat)0, zVector4.back.x);
			Assert.AreEqual((zfloat)0, zVector4.back.y);
			Assert.AreEqual((zfloat)(-1), zVector4.back.z);
			Assert.AreEqual((zfloat)0, zVector4.back.w);

			Assert.AreEqual((zfloat)0, zVector4.up.x);
			Assert.AreEqual((zfloat)1, zVector4.up.y);
			Assert.AreEqual((zfloat)0, zVector4.up.z);
			Assert.AreEqual((zfloat)0, zVector4.up.w);

			Assert.AreEqual((zfloat)0, zVector4.down.x);
			Assert.AreEqual((zfloat)(-1), zVector4.down.y);
			Assert.AreEqual((zfloat)0, zVector4.down.z);
			Assert.AreEqual((zfloat)0, zVector4.down.w);

			Assert.AreEqual((zfloat)(-1), zVector4.left.x);
			Assert.AreEqual((zfloat)0, zVector4.left.y);
			Assert.AreEqual((zfloat)0, zVector4.left.z);
			Assert.AreEqual((zfloat)0, zVector4.left.w);

			Assert.AreEqual((zfloat)1, zVector4.right.x);
			Assert.AreEqual((zfloat)0, zVector4.right.y);
			Assert.AreEqual((zfloat)0, zVector4.right.z);
			Assert.AreEqual((zfloat)0, zVector4.right.w);
		}

		#endregion

		#region 索引器测试

		[Test]
		public void Indexer_GetValidIndex_ReturnsCorrectComponent()
		{
			zVector4 vec = new zVector4(1, 2, 3, 4);

			Assert.AreEqual((zfloat)1, vec[0]);
			Assert.AreEqual((zfloat)2, vec[1]);
			Assert.AreEqual((zfloat)3, vec[2]);
			Assert.AreEqual((zfloat)4, vec[3]);
		}

		[Test]
		public void Indexer_SetValidIndex_SetsComponent()
		{
			zVector4 vec = new zVector4(0, 0, 0, 0);
			vec[0] = (zfloat)1;
			vec[1] = (zfloat)2;
			vec[2] = (zfloat)3;
			vec[3] = (zfloat)4;

			Assert.AreEqual((zfloat)1, vec.x);
			Assert.AreEqual((zfloat)2, vec.y);
			Assert.AreEqual((zfloat)3, vec.z);
			Assert.AreEqual((zfloat)4, vec.w);
		}

		[Test]
		public void Indexer_InvalidIndex_ThrowsException()
		{
			zVector4 vec = new zVector4(0, 0, 0, 0);

			Assert.Throws<System.IndexOutOfRangeException>(() => { var _ = vec[4]; });
			Assert.Throws<System.IndexOutOfRangeException>(() => { var _ = vec[-1]; });
		}

		#endregion

		#region 属性测试

		[Test]
		public void IsZero_ZeroVector_ReturnsTrue()
		{
			zVector4 vec = zVector4.zero;
			Assert.IsTrue(vec.IsZero());
		}

		[Test]
		public void IsZero_NonZeroVector_ReturnsFalse()
		{
			zVector4 vec = new zVector4(1, 0, 0, 0);
			Assert.IsFalse(vec.IsZero());
		}

		[Test]
		public void SqrMagnitude_CalculatesCorrectly()
		{
			zVector4 vec = new zVector4(3, 4, 0, 0);
			zfloat expected = (zfloat)25; // 3^2 + 4^2 = 25

			Assert.AreEqual(expected, vec.sqrMagnitude);
		}

		[Test]
		public void Magnitude_CalculatesCorrectly()
		{
			zVector4 vec = new zVector4(3, 4, 0, 0);
			zfloat expected = (zfloat)5; // sqrt(3^2 + 4^2) = 5

			Assert.AreEqual(expected, vec.magnitude);
		}

		[Test]
		public void Normalized_ReturnsUnitVector()
		{
			zVector4 vec = new zVector4(3, 4, 0, 0);
			zVector4 normalized = vec.normalized;

			Assert.AreEqual((zfloat)1, normalized.magnitude);
		}

		[Test]
		public void Normalized_ZeroVector_ReturnsZero()
		{
			zVector4 vec = zVector4.zero;
			zVector4 normalized = vec.normalized;

			Assert.AreEqual(zVector4.zero, normalized);
		}

		#endregion

		#region 实例方法测试

		[Test]
		public void Normalize_ModifiesVectorInPlace()
		{
			zVector4 vec = new zVector4(3, 4, 0, 0);
			vec.Normalize();

			Assert.AreEqual((zfloat)1, vec.magnitude);
		}

		[Test]
		public void Scale_ScalesComponents()
		{
			zVector4 vec = new zVector4(1, 2, 3, 4);
			zVector4 scale = new zVector4(2, 3, 4, 5);
			vec.Scale(scale);

			Assert.AreEqual((zfloat)2, vec.x);
			Assert.AreEqual((zfloat)6, vec.y);
			Assert.AreEqual((zfloat)12, vec.z);
			Assert.AreEqual((zfloat)20, vec.w);
		}

		[Test]
		public void Add_AddsVectorInPlace()
		{
			zVector4 vec = new zVector4(1, 2, 3, 4);
			zVector4 other = new zVector4(5, 6, 7, 8);
			vec.Add(ref other);

			Assert.AreEqual((zfloat)6, vec.x);
			Assert.AreEqual((zfloat)8, vec.y);
			Assert.AreEqual((zfloat)10, vec.z);
			Assert.AreEqual((zfloat)12, vec.w);
		}

		[Test]
		public void Sub_SubtractsVectorInPlace()
		{
			zVector4 vec = new zVector4(5, 6, 7, 8);
			zVector4 other = new zVector4(1, 2, 3, 4);
			vec.Sub(ref other);

			Assert.AreEqual((zfloat)4, vec.x);
			Assert.AreEqual((zfloat)4, vec.y);
			Assert.AreEqual((zfloat)4, vec.z);
			Assert.AreEqual((zfloat)4, vec.w);
		}

		[Test]
		public void Mul_MultipliesVectorInPlace()
		{
			zVector4 vec = new zVector4(2, 3, 4, 5);
			zVector4 other = new zVector4(2, 2, 2, 2);
			vec.Mul(ref other);

			Assert.AreEqual((zfloat)4, vec.x);
			Assert.AreEqual((zfloat)6, vec.y);
			Assert.AreEqual((zfloat)8, vec.z);
			Assert.AreEqual((zfloat)10, vec.w);
		}

		[Test]
		public void Div_DividesVectorInPlace()
		{
			zVector4 vec = new zVector4(4, 6, 8, 10);
			zVector4 other = new zVector4(2, 2, 2, 2);
			vec.Div(ref other);

			Assert.AreEqual((zfloat)2, vec.x);
			Assert.AreEqual((zfloat)3, vec.y);
			Assert.AreEqual((zfloat)4, vec.z);
			Assert.AreEqual((zfloat)5, vec.w);
		}

		[Test]
		public void Set_SetsAllComponents()
		{
			zVector4 vec = new zVector4(0, 0, 0, 0);
			vec.Set((zfloat)1, (zfloat)2, (zfloat)3, (zfloat)4);

			Assert.AreEqual((zfloat)1, vec.x);
			Assert.AreEqual((zfloat)2, vec.y);
			Assert.AreEqual((zfloat)3, vec.z);
			Assert.AreEqual((zfloat)4, vec.w);
		}

		[Test]
		public void GetNormalizedForMagnitude_ReturnsNormalizedVector()
		{
			zVector4 vec = new zVector4(3, 4, 0, 0);
			zfloat mag = vec.magnitude;
			zVector4 normalized = vec.GetNormalizedForMagnitude(mag);

			Assert.AreEqual((zfloat)1, normalized.magnitude);
		}

		#endregion

		#region 静态方法测试

		[Test]
		public void Normalize_ReturnsNewNormalizedVector()
		{
			zVector4 vec = new zVector4(3, 4, 0, 0);
			zVector4 normalized = zVector4.Normalize(ref vec);

			Assert.AreEqual((zfloat)1, normalized.magnitude);
			Assert.AreNotEqual(normalized, vec); // Original should not be modified
		}

		[Test]
		public void Magnitude_CalculatesCorrectLength()
		{
			zVector4 vec = new zVector4(3, 4, 0, 0);
			zfloat mag = zVector4.Magnitude(ref vec);

			Assert.AreEqual((zfloat)5, mag);
		}

		[Test]
		public void SqrMagnitude_CalculatesCorrectSquaredLength()
		{
			zVector4 vec = new zVector4(3, 4, 0, 0);
			zfloat sqrMag = zVector4.SqrMagnitude(ref vec);

			Assert.AreEqual((zfloat)25, sqrMag);
		}

		[Test]
		public void Lerp_InterpolatesCorrectly()
		{
			zVector4 from = new zVector4(0, 0, 0, 0);
			zVector4 to = new zVector4(10, 10, 10, 10);
			zVector4 result = zVector4.Lerp(from, to, (zfloat)0.5);

			Assert.AreEqual((zfloat)5, result.x);
			Assert.AreEqual((zfloat)5, result.y);
			Assert.AreEqual((zfloat)5, result.z);
			Assert.AreEqual((zfloat)5, result.w);
		}

		[Test]
		public void Lerp_ClampsTValue()
		{
			zVector4 from = new zVector4(0, 0, 0, 0);
			zVector4 to = new zVector4(10, 10, 10, 10);
			zVector4 result1 = zVector4.Lerp(from, to, (zfloat)1.5);
			zVector4 result2 = zVector4.Lerp(from, to, (zfloat)(-0.5));

			Assert.AreEqual(to, result1);
			Assert.AreEqual(from, result2);
		}

		[Test]
		public void MoveTowards_MovesTowardsTarget()
		{
			zVector4 current = new zVector4(0, 0, 0, 0);
			zVector4 target = new zVector4(10, 10, 10, 10);
			zfloat maxDelta = (zfloat)3;
			zVector4 result = zVector4.MoveTowards(current, target, maxDelta);

			// 定点除法会产生截断，允许很小的误差。
			Assert.AreEqual(((zfloat)3).value, result.magnitude.value, 20);
		}

		[Test]
		public void MoveTowards_ReachesTargetIfClose()
		{
			zVector4 current = new zVector4(0, 0, 0, 0);
			zVector4 target = new zVector4(2, 2, 2, 2);
			zfloat maxDelta = (zfloat)10;
			zVector4 result = zVector4.MoveTowards(current, target, maxDelta);

			Assert.AreEqual(target, result);
		}

		[Test]
		public void Scale_MultipliesComponents()
		{
			zVector4 a = new zVector4(1, 2, 3, 4);
			zVector4 b = new zVector4(2, 3, 4, 5);
			zVector4 result = zVector4.Scale(a, b);

			Assert.AreEqual((zfloat)2, result.x);
			Assert.AreEqual((zfloat)6, result.y);
			Assert.AreEqual((zfloat)12, result.z);
			Assert.AreEqual((zfloat)20, result.w);
		}

		[Test]
		public void Dot_CalculatesDotProduct()
		{
			zVector4 a = new zVector4(1, 2, 3, 4);
			zVector4 b = new zVector4(2, 3, 4, 5);
			zfloat result = zVector4.Dot(a, b);

			zfloat expected = (zfloat)(1 * 2 + 2 * 3 + 3 * 4 + 4 * 5); // 2 + 6 + 12 + 20 = 40
			Assert.AreEqual(expected, result);
		}

		[Test]
		public void Dot_RefVersion_CalculatesDotProduct()
		{
			zVector4 a = new zVector4(1, 2, 3, 4);
			zVector4 b = new zVector4(2, 3, 4, 5);
			zfloat result = zVector4.Dot(ref a, ref b);

			zfloat expected = (zfloat)40;
			Assert.AreEqual(expected, result);
		}

		[Test]
		public void Project_ProjectsVector()
		{
			zVector4 vec = new zVector4(2, 2, 0, 0);
			zVector4 normal = new zVector4(1, 0, 0, 0);
			zVector4 result = zVector4.Project(vec, normal);

			Assert.AreEqual((zfloat)2, result.x);
			Assert.AreEqual((zfloat)0, result.y);
		}

		[Test]
		public void Project_ZeroNormal_ReturnsZero()
		{
			zVector4 vec = new zVector4(2, 2, 0, 0);
			zVector4 normal = zVector4.zero;
			zVector4 result = zVector4.Project(vec, normal);

			Assert.AreEqual(zVector4.zero, result);
		}

		[Test]
		public void Distance_CalculatesDistance()
		{
			zVector4 a = new zVector4(0, 0, 0, 0);
			zVector4 b = new zVector4(3, 4, 0, 0);
			zfloat result = zVector4.Distance(a, b);

			Assert.AreEqual((zfloat)5, result);
		}

		[Test]
		public void SqrDistance_CalculatesSquaredDistance()
		{
			zVector4 a = new zVector4(0, 0, 0, 0);
			zVector4 b = new zVector4(3, 4, 0, 0);
			zfloat result = zVector4.SqrDistance(a, b);

			Assert.AreEqual((zfloat)25, result);
		}

		[Test]
		public void ClampMagnitude_ClampsLongVector()
		{
			zVector4 vec = new zVector4(10, 0, 0, 0);
			zfloat maxLength = (zfloat)5;
			zVector4 result = zVector4.ClampMagnitude(vec, maxLength);

			Assert.AreEqual((zfloat)5, result.magnitude);
		}

		[Test]
		public void ClampMagnitude_LeavesShortVectorUnchanged()
		{
			zVector4 vec = new zVector4(3, 0, 0, 0);
			zfloat maxLength = (zfloat)5;
			zVector4 result = zVector4.ClampMagnitude(vec, maxLength);

			Assert.AreEqual(vec, result);
		}

		[Test]
		public void Min_ReturnsComponentWiseMinimum()
		{
			zVector4 a = new zVector4(1, 5, 3, 7);
			zVector4 b = new zVector4(2, 4, 6, 8);
			zVector4 result = zVector4.Min(a, b);

			Assert.AreEqual((zfloat)1, result.x);
			Assert.AreEqual((zfloat)4, result.y);
			Assert.AreEqual((zfloat)3, result.z);
			Assert.AreEqual((zfloat)7, result.w);
		}

		[Test]
		public void Max_ReturnsComponentWiseMaximum()
		{
			zVector4 a = new zVector4(1, 5, 3, 7);
			zVector4 b = new zVector4(2, 4, 6, 8);
			zVector4 result = zVector4.Max(a, b);

			Assert.AreEqual((zfloat)2, result.x);
			Assert.AreEqual((zfloat)5, result.y);
			Assert.AreEqual((zfloat)6, result.z);
			Assert.AreEqual((zfloat)8, result.w);
		}

		[Test]
		public void Angle_CalculatesAngle()
		{
			zVector4 from = new zVector4(1, 0, 0, 0);
			zVector4 to = new zVector4(0, 1, 0, 0);
			zfloat result = zVector4.Angle(from, to);

			// 定点 AcosAngle 存在离散近似，90 度断言需给出小范围容差。
			Assert.AreEqual(((zfloat)90).value, result.value, 3000);
		}

		[Test]
		public void Angle_ZeroVector_ReturnsZero()
		{
			zVector4 from = zVector4.zero;
			zVector4 to = new zVector4(1, 0, 0, 0);
			zfloat result = zVector4.Angle(from, to);

			Assert.AreEqual(zfloat.Zero, result);
		}

		#endregion

		#region 运算符测试

		[Test]
		public void AdditionOperator_AddsVectors()
		{
			zVector4 a = new zVector4(1, 2, 3, 4);
			zVector4 b = new zVector4(5, 6, 7, 8);
			zVector4 result = a + b;

			Assert.AreEqual((zfloat)6, result.x);
			Assert.AreEqual((zfloat)8, result.y);
			Assert.AreEqual((zfloat)10, result.z);
			Assert.AreEqual((zfloat)12, result.w);
		}

		[Test]
		public void SubtractionOperator_SubtractsVectors()
		{
			zVector4 a = new zVector4(5, 6, 7, 8);
			zVector4 b = new zVector4(1, 2, 3, 4);
			zVector4 result = a - b;

			Assert.AreEqual((zfloat)4, result.x);
			Assert.AreEqual((zfloat)4, result.y);
			Assert.AreEqual((zfloat)4, result.z);
			Assert.AreEqual((zfloat)4, result.w);
		}

		[Test]
		public void NegationOperator_NegatesVector()
		{
			zVector4 vec = new zVector4(1, -2, 3, -4);
			zVector4 result = -vec;

			Assert.AreEqual((zfloat)(-1), result.x);
			Assert.AreEqual((zfloat)2, result.y);
			Assert.AreEqual((zfloat)(-3), result.z);
			Assert.AreEqual((zfloat)4, result.w);
		}

		[Test]
		public void MultiplicationOperator_MultipliesByScalar()
		{
			zVector4 vec = new zVector4(1, 2, 3, 4);
			zVector4 result = vec * (zfloat)2;

			Assert.AreEqual((zfloat)2, result.x);
			Assert.AreEqual((zfloat)4, result.y);
			Assert.AreEqual((zfloat)6, result.z);
			Assert.AreEqual((zfloat)8, result.w);
		}

		[Test]
		public void DivisionOperator_DividesByScalar()
		{
			zVector4 vec = new zVector4(4, 6, 8, 10);
			zVector4 result = vec / (zfloat)2;

			Assert.AreEqual((zfloat)2, result.x);
			Assert.AreEqual((zfloat)3, result.y);
			Assert.AreEqual((zfloat)4, result.z);
			Assert.AreEqual((zfloat)5, result.w);
		}

		[Test]
		public void EqualityOperator_EqualVectors_ReturnsTrue()
		{
			zVector4 a = new zVector4(1, 2, 3, 4);
			zVector4 b = new zVector4(1, 2, 3, 4);

			Assert.IsTrue(a == b);
		}

		[Test]
		public void EqualityOperator_DifferentVectors_ReturnsFalse()
		{
			zVector4 a = new zVector4(1, 2, 3, 4);
			zVector4 b = new zVector4(1, 2, 3, 5);

			Assert.IsFalse(a == b);
		}

		[Test]
		public void InequalityOperator_EqualVectors_ReturnsFalse()
		{
			zVector4 a = new zVector4(1, 2, 3, 4);
			zVector4 b = new zVector4(1, 2, 3, 4);

			Assert.IsFalse(a != b);
		}

		[Test]
		public void InequalityOperator_DifferentVectors_ReturnsTrue()
		{
			zVector4 a = new zVector4(1, 2, 3, 4);
			zVector4 b = new zVector4(1, 2, 3, 5);

			Assert.IsTrue(a != b);
		}

		#endregion

		#region 类型转换测试

		[Test]
		public void ImplicitConversion_FromVector3_SetsWToZero()
		{
			zVector3 v3 = new zVector3(1, 2, 3);
			zVector4 v4 = v3;

			Assert.AreEqual((zfloat)1, v4.x);
			Assert.AreEqual((zfloat)2, v4.y);
			Assert.AreEqual((zfloat)3, v4.z);
			Assert.AreEqual(zfloat.Zero, v4.w);
		}

		[Test]
		public void ImplicitConversion_ToVector3_DropsW()
		{
			zVector4 v4 = new zVector4(1, 2, 3, 4);
			zVector3 v3 = v4;

			Assert.AreEqual((zfloat)1, v3.x);
			Assert.AreEqual((zfloat)2, v3.y);
			Assert.AreEqual((zfloat)3, v3.z);
		}

		[Test]
		public void ImplicitConversion_FromVector2_SetsZAndWToZero()
		{
			zVector2 v2 = new zVector2(1, 2);
			zVector4 v4 = v2;

			Assert.AreEqual((zfloat)1, v4.x);
			Assert.AreEqual((zfloat)2, v4.y);
			Assert.AreEqual(zfloat.Zero, v4.z);
			Assert.AreEqual(zfloat.Zero, v4.w);
		}

		[Test]
		public void ImplicitConversion_ToVector2_DropsZAndW()
		{
			zVector4 v4 = new zVector4(1, 2, 3, 4);
			zVector2 v2 = v4;

			Assert.AreEqual((zfloat)1, v2.x);
			Assert.AreEqual((zfloat)2, v2.y);
		}

		#endregion

		#region Object方法测试

		[Test]
		public void ToString_ReturnsFormattedString()
		{
			zVector4 vec = new zVector4(1, 2, 3, 4);
			string result = vec.ToString();

			Assert.IsTrue(result.Contains("1"));
			Assert.IsTrue(result.Contains("2"));
			Assert.IsTrue(result.Contains("3"));
			Assert.IsTrue(result.Contains("4"));
		}

		[Test]
		public void Equals_SameVector_ReturnsTrue()
		{
			zVector4 vec = new zVector4(1, 2, 3, 4);

			Assert.IsTrue(vec.Equals(vec));
		}

		[Test]
		public void Equals_EqualVectors_ReturnsTrue()
		{
			zVector4 a = new zVector4(1, 2, 3, 4);
			zVector4 b = new zVector4(1, 2, 3, 4);

			Assert.IsTrue(a.Equals(b));
		}

		[Test]
		public void Equals_DifferentVectors_ReturnsFalse()
		{
			zVector4 a = new zVector4(1, 2, 3, 4);
			zVector4 b = new zVector4(1, 2, 3, 5);

			Assert.IsFalse(a.Equals(b));
		}

		[Test]
		public void GetHashCode_EqualVectors_ReturnSameHashCode()
		{
			zVector4 a = new zVector4(1, 2, 3, 4);
			zVector4 b = new zVector4(1, 2, 3, 4);

			Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
		}

		[Test]
		public void GetHashCode_DifferentVectors_ReturnDifferentHashCodes()
		{
			zVector4 a = new zVector4(1, 2, 3, 4);
			zVector4 b = new zVector4(1, 2, 3, 5);

			Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());
		}

		#endregion

		#region 边界情况测试

		[Test]
		public void LargeVector_DoesNotOverflow()
		{
			zVector4 vec = new zVector4(1000, 1000, 1000, 1000);
			zfloat mag = vec.magnitude;

			Assert.Greater(mag, (zfloat)0);
		}

		[Test]
		public void VerySmallVector_CanBeNormalized()
		{
			zVector4 vec = new zVector4(0.0001f, 0.0001f, 0.0001f, 0.0001f);
			zVector4 normalized = vec.normalized;

			// 4位小数定点下该向量长度平方会截断到0，归一化结果应为零向量。
			Assert.AreEqual(zVector4.zero, normalized);
		}

		[Test]
		public void MixedSignVector_HandledCorrectly()
		{
			zVector4 vec = new zVector4(-1, 2, -3, 4);
			zfloat mag = vec.magnitude;

			Assert.AreEqual(((zfloat)Math.Sqrt(1 + 4 + 9 + 16)).value, mag.value, 5);
		}

		#endregion
	}
}
