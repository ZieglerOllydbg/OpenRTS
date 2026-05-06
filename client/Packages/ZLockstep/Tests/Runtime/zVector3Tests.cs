using System;
using System.Runtime.Serialization;
using NUnit.Framework;
using zUnity;

namespace ZLockstep.Tests
{
	[TestFixture]
	public class zVector3Tests
	{
		private const long EPSILON = 2;

		[Test]
		public void Constructor_And_Indexer_Work()
		{
			zVector3 v = new zVector3((zfloat)1.5, (zfloat)2.5, (zfloat)3.5);
			Assert.AreEqual(((zfloat)1.5).value, v[0].value);
			Assert.AreEqual(((zfloat)2.5).value, v[1].value);
			Assert.AreEqual(((zfloat)3.5).value, v[2].value);

			v[0] = (zfloat)7;
			v[1] = (zfloat)8;
			v[2] = (zfloat)9;
			Assert.AreEqual(((zfloat)7).value, v.x.value);
			Assert.AreEqual(((zfloat)8).value, v.y.value);
			Assert.AreEqual(((zfloat)9).value, v.z.value);
		}

		[Test]
		public void Indexer_OutOfRange_Throws()
		{
			zVector3 v = zVector3.one;
			Assert.Throws<IndexOutOfRangeException>(() => _ = v[3]);
		}

		[Test]
		public void Scale_Static_ShouldUseEachComponent()
		{
			zVector3 a = new zVector3((zfloat)2, (zfloat)3, (zfloat)4);
			zVector3 b = new zVector3((zfloat)10, (zfloat)20, (zfloat)30);
			zVector3 result = zVector3.Scale(a, b);
			Assert.AreEqual(((zfloat)20).value, result.x.value);
			Assert.AreEqual(((zfloat)60).value, result.y.value);
			Assert.AreEqual(((zfloat)120).value, result.z.value);
		}

		[Test]
		public void Dot_Ref_And_NonRef_ShouldMatch()
		{
			zVector3 a = new zVector3((zfloat)1, (zfloat)2, (zfloat)3);
			zVector3 b = new zVector3((zfloat)4, (zfloat)5, (zfloat)6);
			zfloat refDot = zVector3.Dot(ref a, ref b);
			zfloat plainDot = zVector3.Dot(a, b);
			Assert.AreEqual(refDot.value, plainDot.value);
		}

		[Test]
		public void Magnitude_And_SqrMagnitude_Work()
		{
			zVector3 v = new zVector3((zfloat)2, (zfloat)3, (zfloat)6);
			Assert.AreEqual(((zfloat)49).value, v.sqrMagnitude.value, EPSILON);
			Assert.AreEqual(((zfloat)7).value, v.magnitude.value, 20);
		}

		[Test]
		public void Normalized_Zero_ShouldReturnZero()
		{
			zVector3 n = zVector3.zero.normalized;
			Assert.IsTrue(n.IsZero());
		}

		[Test]
		public void Normalize_ShouldReturnUnitLength_ForNormalCase()
		{
			zVector3 v = new zVector3((zfloat)3, (zfloat)4, (zfloat)0);
			v.Normalize();
			Assert.AreEqual(zfloat.One.value, v.magnitude.value, EPSILON);
		}

		[Test]
		public void Project_OnZeroNormal_ShouldReturnZero()
		{
			zVector3 projected = zVector3.Project(new zVector3((zfloat)5, (zfloat)6, (zfloat)7), zVector3.zero);
			Assert.IsTrue(projected.IsZero());
		}

		[Test]
		public void Angle_WithZeroVector_ShouldReturnZero()
		{
			Assert.AreEqual(zfloat.Zero.value, zVector3.Angle(zVector3.zero, zVector3.right).value);
		}

		[Test]
		public void A2B_Angle_ShouldKeepSign()
		{
			zfloat positive = zVector3.A2B_angle(zVector3.forward, zVector3.right);
			zfloat negative = zVector3.A2B_angle(zVector3.forward, zVector3.left);
			Assert.Greater(positive.value, 0);
			Assert.Less(negative.value, 0);
		}

		[Test]
		public void Slerp_ParallelVectors_ShouldBeStable()
		{
			zVector3 from = zVector3.forward * (zfloat)2;
			zVector3 to = zVector3.forward * (zfloat)8;
			zVector3 mid = zVector3.Slerp(from, to, zfloat.Half);
			Assert.AreEqual(((zfloat)5).value, mid.magnitude.value, EPSILON * 2);
			Assert.AreEqual(zfloat.One.value, mid.normalized.z.value, EPSILON);
		}

		[Test]
		public void Slerp_OppositeVectors_ShouldBeStable()
		{
			zVector3 from = zVector3.forward;
			zVector3 to = zVector3.back;
			zVector3 mid = zVector3.Slerp(from, to, zfloat.Half);
			Assert.AreEqual(zfloat.One.value, mid.magnitude.value, EPSILON * 4);
			Assert.GreaterOrEqual(mid.sqrMagnitude.value, zfloat.Zero.value);
		}

		[Test]
		public void OrthoNormalize_ShouldProduceOrthogonalUnitVectors()
		{
			zVector3 normal = new zVector3((zfloat)1, (zfloat)2, (zfloat)3);
			zVector3 tangent = new zVector3((zfloat)5, (zfloat)6, (zfloat)7);
			zVector3.OrthoNormalize(ref normal, ref tangent);

			Assert.AreEqual(zfloat.One.value, normal.magnitude.value, EPSILON);
			Assert.AreEqual(zfloat.One.value, tangent.magnitude.value, EPSILON);
			Assert.AreEqual(zfloat.Zero.value, zVector3.Dot(normal, tangent).value, EPSILON * 6);
		}

		[Test]
		public void RotateTowards_LegacyOverload_ShouldNotReturnZeroForNormalInput()
		{
			zVector3 result = zVector3.RotateTowards(zVector3.forward, zVector3.right, (zfloat)0.1);
			Assert.IsFalse(result.IsZero());
		}

		[Test]
		public void RotateTowards_UnityStyleOverload_ShouldRespectMagnitudeDelta()
		{
			zVector3 current = zVector3.forward * (zfloat)1;
			zVector3 target = zVector3.right * (zfloat)5;
			zVector3 result = zVector3.RotateTowards(current, target, (zfloat)1000, (zfloat)1);
			Assert.AreEqual(((zfloat)2).value, result.magnitude.value, EPSILON * 2);
		}

		[Test]
		public void RotateTowards_UnityStyleOverload_ShouldLimitAngleStep()
		{
			zVector3 current = zVector3.forward;
			zVector3 target = zVector3.right;
			zfloat before = zVector3.Angle(current, target);
			zVector3 result = zVector3.RotateTowards(current, target, zMathf.PI / 4, zfloat.Zero);
			zfloat after = zVector3.Angle(result.normalized, target.normalized);
			Assert.Less(after.value, before.value);
		}

		[Test]
		public void Equals_And_GetHashCode_ShouldBeConsistent()
		{
			zVector3 a = new zVector3((zfloat)1, (zfloat)2, (zfloat)3);
			zVector3 b = new zVector3((zfloat)1, (zfloat)2, (zfloat)3);
			Assert.IsTrue(a.Equals(b));
			Assert.IsTrue(a == b);
			Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
		}

		[Test]
		public void Serialization_RoundTrip_ShouldKeepRawValues()
		{
			zVector3 source = new zVector3((zfloat)1.25, (zfloat)(-2.5), (zfloat)3.75);
			SerializationInfo info = new SerializationInfo(typeof(zVector3), new FormatterConverter());
			source.GetObjectData(info, new StreamingContext());

			zVector3 restored = new zVector3(info, new StreamingContext());
			Assert.AreEqual(source.x.value, restored.x.value);
			Assert.AreEqual(source.y.value, restored.y.value);
			Assert.AreEqual(source.z.value, restored.z.value);
		}
	}
}
