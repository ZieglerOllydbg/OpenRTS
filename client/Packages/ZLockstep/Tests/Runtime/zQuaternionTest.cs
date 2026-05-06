using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using zUnity;

namespace ZLockstep.Tests
{
	[TestFixture]
	public class zQuaternionTest
	{
		private const long RawTolerance = 20;

		[Test]
		public void Identity_HasExpectedComponents()
		{
			Assert.AreEqual(zfloat.Zero.value, zQuaternion.identity.x.value);
			Assert.AreEqual(zfloat.Zero.value, zQuaternion.identity.y.value);
			Assert.AreEqual(zfloat.Zero.value, zQuaternion.identity.z.value);
			Assert.AreEqual(zfloat.One.value, zQuaternion.identity.w.value);
		}

		[Test]
		public void Indexer_Throws_WhenOutOfRange()
		{
			zQuaternion q = zQuaternion.identity;
			Assert.Throws<IndexOutOfRangeException>(() => _ = q[4]);
			Assert.Throws<IndexOutOfRangeException>(() => q[4] = zfloat.One);
		}

		[Test]
		public void AngleAxis_ReturnsIdentity_WhenAxisIsZero()
		{
			zQuaternion q = zQuaternion.AngleAxis((zfloat)30, zVector3.zero);
			Assert.IsTrue(q == zQuaternion.identity);
		}

		[Test]
		public void Normalize_ReturnsIdentity_WhenInputIsZero()
		{
			zQuaternion q = new zQuaternion(zfloat.Zero, zfloat.Zero, zfloat.Zero, zfloat.Zero);
			zQuaternion n = zQuaternion.Normalize(q);
			Assert.IsTrue(n == zQuaternion.identity);
		}

		[Test]
		public void Inverse_ReturnsIdentity_WhenInputIsZero()
		{
			zQuaternion inv = zQuaternion.Inverse(new zQuaternion(zfloat.Zero, zfloat.Zero, zfloat.Zero, zfloat.Zero));
			Assert.IsTrue(inv == zQuaternion.identity);
		}

		[Test]
		public void Inverse_MultiplyOriginal_ReturnsIdentity()
		{
			zQuaternion q = zQuaternion.Euler((zfloat)20, (zfloat)(-40), (zfloat)15);
			zQuaternion inv = zQuaternion.Inverse(q);
			zQuaternion actual = zQuaternion.Normalize(q * inv);
			Assert.IsTrue(actual == zQuaternion.identity);
		}

		[Test]
		public void Dot_IsInRange_WhenInputsAreNormalized()
		{
			zQuaternion a = zQuaternion.Euler((zfloat)10, (zfloat)20, (zfloat)30);
			zQuaternion b = zQuaternion.Euler((zfloat)(-5), (zfloat)60, (zfloat)0);
			zfloat d = zQuaternion.Dot(a, b);
			Assert.GreaterOrEqual(d.value, zfloat.NegativeOne.value);
			Assert.LessOrEqual(d.value, zfloat.One.value);
		}

		[Test]
		public void Length_And_LengthSquared_AreCorrect_ForIdentity()
		{
			Assert.AreEqual(zfloat.One.value, zQuaternion.identity.Length.value, RawTolerance);
			Assert.AreEqual(zfloat.One.value, zQuaternion.identity.LengthSquared.value, RawTolerance);
			Assert.IsTrue(zQuaternion.identity.IsNormalized);
		}

		[Test]
		public void Lerp_ReturnsCorrectValue_WhenTIsZero()
		{
			zQuaternion from = zQuaternion.Euler((zfloat)0, (zfloat)10, (zfloat)0);
			zQuaternion to = zQuaternion.Euler((zfloat)0, (zfloat)60, (zfloat)0);
			zQuaternion result = zQuaternion.Lerp(from, to, zfloat.Zero);
			Assert.IsTrue(result == from);
		}

		[Test]
		public void Lerp_ReturnsCorrectValue_WhenTIsOne()
		{
			zQuaternion from = zQuaternion.Euler((zfloat)0, (zfloat)10, (zfloat)0);
			zQuaternion to = zQuaternion.Euler((zfloat)0, (zfloat)60, (zfloat)0);
			zQuaternion result = zQuaternion.Lerp(from, to, zfloat.One);
			Assert.IsTrue(result == to);
		}

		[Test]
		public void Slerp_ReturnsCorrectValue_WhenTIsZero()
		{
			zQuaternion from = zQuaternion.Euler((zfloat)0, (zfloat)0, (zfloat)0);
			zQuaternion to = zQuaternion.Euler((zfloat)0, (zfloat)90, (zfloat)0);
			zQuaternion result = zQuaternion.Slerp(from, to, zfloat.Zero);
			Assert.IsTrue(result == from);
		}

		[Test]
		public void Slerp_ReturnsCorrectValue_WhenTIsOne()
		{
			zQuaternion from = zQuaternion.Euler((zfloat)0, (zfloat)0, (zfloat)0);
			zQuaternion to = zQuaternion.Euler((zfloat)0, (zfloat)90, (zfloat)0);
			zQuaternion result = zQuaternion.Slerp(from, to, zfloat.One);
			Assert.IsTrue(result == to);
		}

		[Test]
		public void Slerp_HandlesNegativeDot_WithShortestPath()
		{
			zQuaternion q = zQuaternion.Euler((zfloat)0, (zfloat)45, (zfloat)0);
			zQuaternion neg = new zQuaternion(-q.x, -q.y, -q.z, -q.w);
			zQuaternion mid = zQuaternion.Slerp(q, neg, zfloat.Half);
			Assert.IsTrue(mid == q);
		}

		[Test]
		public void FromToRotation_RotatesFromVectorToTarget()
		{
			zVector3 from = zVector3.forward;
			zVector3 to = zVector3.right;
			zQuaternion q = zQuaternion.FromToRotation(from, to);
			zVector3 rotated = (q * from).normalized;
			AssertVectorApproximately(rotated, to.normalized, RawTolerance * 4);
		}

		[Test]
		public void LookRotation_ReturnsIdentity_WhenForwardIsZero()
		{
			zQuaternion q = zQuaternion.LookRotation(zVector3.zero);
			Assert.IsTrue(q == zQuaternion.identity);
		}

		[Test]
		public void MatrixRoundTrip_PreservesRotation()
		{
			zQuaternion original = zQuaternion.Euler((zfloat)25, (zfloat)(-70), (zfloat)15);
			zMatrix4x4 m = original.RotationMatrix();
			zQuaternion reconstructed = zQuaternion.FromMatrix(m);
			Assert.IsTrue(original == reconstructed);
		}

		[Test]
		public void ToEuler_And_Euler_AreConsistent_ForRotation()
		{
			zQuaternion original = zQuaternion.Euler((zfloat)15, (zfloat)30, (zfloat)45);
			zVector3 euler = zQuaternion.ToEuler(original);
			zQuaternion reconstructed = zQuaternion.Euler(euler);
			Assert.IsTrue(original == reconstructed);
		}

		[Test]
		public void Angle_ReturnsZero_WhenRotationsAreEquivalent()
		{
			zQuaternion q = zQuaternion.Euler((zfloat)0, (zfloat)60, (zfloat)0);
			zQuaternion neg = new zQuaternion(-q.x, -q.y, -q.z, -q.w);
			zfloat angle = zQuaternion.Angle(q, neg);
			Assert.AreEqual(zfloat.Zero.value, angle.value, RawTolerance);
		}

		[Test]
		public void QuaternionVectorMultiplication_MatchesRotationMatrix()
		{
			zQuaternion q = zQuaternion.Euler((zfloat)10, (zfloat)20, (zfloat)30);
			zVector3 v = new zVector3((zfloat)2, (zfloat)3, (zfloat)4);
			zVector3 byQuaternion = q * v;
			zVector3 byMatrix = q.RotationMatrix().MultiplyVector(v);
			AssertVectorApproximately(byQuaternion, byMatrix, RawTolerance * 4);
		}

		[Test]
		public void ToAngleAxis_ReturnsZeroAngle_WhenIdentity()
		{
			zQuaternion.identity.ToAngleAxis(out zfloat angle, out zVector3 axis);
			Assert.AreEqual(zfloat.Zero.value, angle.value, RawTolerance);
			AssertVectorApproximately(axis, zVector3.right, RawTolerance);
		}

		[UnityTest]
		public IEnumerator PublicApi_Smoke_WorksAcrossOneFrame()
		{
			zQuaternion a = zQuaternion.Euler((zfloat)0, (zfloat)0, (zfloat)0);
			zQuaternion b = zQuaternion.Euler((zfloat)0, (zfloat)90, (zfloat)0);
			zQuaternion c = zQuaternion.Slerp(a, b, zfloat.Half);
			Assert.IsTrue(c.IsNormalized);
			yield return null;
		}

		private static void AssertVectorApproximately(zVector3 actual, zVector3 expected, long toleranceRaw)
		{
			Assert.AreEqual(expected.x.value, actual.x.value, toleranceRaw);
			Assert.AreEqual(expected.y.value, actual.y.value, toleranceRaw);
			Assert.AreEqual(expected.z.value, actual.z.value, toleranceRaw);
		}
	}
}
