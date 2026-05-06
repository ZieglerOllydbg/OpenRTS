using NUnit.Framework;
using zUnity;

public class zMathfTests
{
	private static void AssertRawClose(zfloat expected, zfloat actual, long toleranceRaw)
	{
		long diff = expected.value - actual.value;
		if (diff < 0)
		{
			diff = -diff;
		}

		Assert.LessOrEqual(diff, toleranceRaw, $"expectedRaw={expected.value}, actualRaw={actual.value}, tolerance={toleranceRaw}");
	}

	[Test]
	public void SinCosAngle_IntegerAngles_ReturnExpectedRaw()
	{
		Assert.AreEqual(0, zMathf.SinAngle(0).value);
		Assert.AreEqual(5000, zMathf.SinAngle(30).value);
		Assert.AreEqual(10000, zMathf.SinAngle(90).value);
		Assert.AreEqual(0, zMathf.SinAngle(180).value);
		Assert.AreEqual(-10000, zMathf.SinAngle(-90).value);
		Assert.AreEqual(0, zMathf.SinAngle(360).value);

		Assert.AreEqual(10000, zMathf.CosAngle(0).value);
		Assert.AreEqual(5000, zMathf.CosAngle(60).value);
		Assert.AreEqual(0, zMathf.CosAngle(90).value);
		Assert.AreEqual(-10000, zMathf.CosAngle(180).value);
	}

	[Test]
	public void TanAngle_WhenCosIsZero_ReturnsInfinity()
	{
		Assert.AreEqual(zfloat.Infinity.value, zMathf.TanAngle(90).value);
		Assert.AreEqual(zfloat.Infinity.value, zMathf.TanAngle(-90).value);
	}

	[Test]
	public void TryTanAngle_ReturnsStateWithoutLogging()
	{
		Assert.IsFalse(zMathf.TryTanAngle(90, out zfloat invalid));
		Assert.AreEqual(zfloat.Infinity.value, invalid.value);

		Assert.IsTrue(zMathf.TryTanAngle(45, out zfloat valid));
		AssertRawClose(zfloat.One, valid, 120);
	}

	[Test]
	public void Trig_RadianApis_KeepExpectedScale()
	{
		AssertRawClose(zfloat.One, zMathf.Sin(zMathf.PI / 2), 20);
		AssertRawClose(zfloat.Zero, zMathf.Cos(zMathf.PI / 2), 40);
		AssertRawClose(zfloat.One, zMathf.Cos(zfloat.Zero), 1);
	}

	[Test]
	public void InverseTrig_And_Atan2_WorkWithQuadrants()
	{
		AssertRawClose(zMathf.PI / 6, zMathf.Asin(zfloat.Half), 80);
		AssertRawClose(zMathf.PI / 6, zMathf.Acos(zfloat.FromRaw(8660)), 120);
		AssertRawClose(zMathf.PI / 4, zMathf.Atan(zfloat.One), 80);
		AssertRawClose(zMathf.PI / 4, zMathf.Atan2(zfloat.One, zfloat.One), 80);
		AssertRawClose(zMathf.PI * 3 / 4, zMathf.Atan2(zfloat.One, -zfloat.One), 80);
		AssertRawClose(-zMathf.PI * 3 / 4, zMathf.Atan2(-zfloat.One, -zfloat.One), 80);
	}

	[Test]
	public void InverseTrig_OutOfRange_ClampsToDomain()
	{
		AssertRawClose(zMathf.PI / 2, zMathf.Asin(zfloat.FromRaw(20000)), 80);
		AssertRawClose(-zMathf.PI / 2, zMathf.Asin(zfloat.FromRaw(-20000)), 80);
		AssertRawClose(zfloat.Zero, zMathf.Acos(zfloat.FromRaw(20000)), 80);
		AssertRawClose(zMathf.PI, zMathf.Acos(zfloat.FromRaw(-20000)), 120);
	}

	[Test]
	public void Sqrt_And_SqrtScale_HandleBoundaries()
	{
		Assert.AreEqual(0, zMathf.Sqrt(zfloat.Zero).value);
		Assert.AreEqual(10000, zMathf.Sqrt(zfloat.One).value);
		Assert.AreEqual(20000, zMathf.Sqrt((zfloat)4).value);
		Assert.AreEqual(0, zMathf.Sqrt(-zfloat.One).value);

		Assert.AreEqual(10000, zMathf.SqrtScale(10000));
		Assert.AreEqual(20000, zMathf.SqrtScale(40000));
	}

	[Test]
	public void TrySqrt_ReturnsStateWithoutLogging()
	{
		Assert.IsFalse(zMathf.TrySqrt(-zfloat.One, out zfloat invalid));
		Assert.AreEqual(zfloat.Zero.value, invalid.value);

		Assert.IsTrue(zMathf.TrySqrt((zfloat)4, out zfloat valid));
		Assert.AreEqual(20000, valid.value);
	}

	[Test]
	public void BasicNumericUtilities_KeepHistoricalBehavior()
	{
		Assert.AreEqual(5000, zMathf.Abs(zfloat.FromRaw(-5000)).value);
		Assert.AreEqual(2, zMathf.Abs(-2));
		Assert.AreEqual(3L, zMathf.Abs(-3L));

		Assert.AreEqual(10000, zMathf.Min(zfloat.One, zfloat.Two).value);
		Assert.AreEqual(20000, zMathf.Max(zfloat.One, zfloat.Two).value);
		Assert.AreEqual(3, zMathf.Max(3, 2));

		Assert.AreEqual(10000, zMathf.Clamp(zfloat.FromRaw(9000), zfloat.One, zfloat.Two).value);
		Assert.AreEqual(20000, zMathf.Clamp(zfloat.FromRaw(21000), zfloat.One, zfloat.Two).value);
		Assert.AreEqual(15000, zMathf.Clamp(zfloat.FromRaw(15000), zfloat.One, zfloat.Two).value);

		Assert.AreEqual(0, zMathf.Clamp01(zfloat.FromRaw(-1)).value);
		Assert.AreEqual(10000, zMathf.Clamp01(zfloat.FromRaw(10001)).value);

		Assert.AreEqual(2, zMathf.Ceil(zfloat.FromRaw(10001)));
		Assert.AreEqual(-2, zMathf.Floor(zfloat.FromRaw(-10001)));
		Assert.AreEqual(2, zMathf.Round(zfloat.FromRaw(15000)));
		Assert.AreEqual(-2, zMathf.Round(zfloat.FromRaw(-15000)));

		Assert.AreEqual(1, zMathf.Sign(zfloat.Zero));
		Assert.AreEqual(1, zMathf.Sign(zfloat.One));
		Assert.AreEqual(-1, zMathf.Sign(-zfloat.One));
	}

	[Test]
	public void LerpRepeatPingPong_BoundaryCases_Work()
	{
		Assert.AreEqual(10000, zMathf.Lerp(zfloat.One, zfloat.Two, zfloat.Zero).value);
		Assert.AreEqual(15000, zMathf.Lerp(zfloat.One, zfloat.Two, zfloat.Half).value);
		Assert.AreEqual(20000, zMathf.Lerp(zfloat.One, zfloat.Two, zfloat.One).value);

		Assert.AreEqual(1, zMathf.Lerp(1, 3, zfloat.Zero));
		Assert.AreEqual(2, zMathf.Lerp(1, 3, zfloat.Half));
		Assert.AreEqual(3, zMathf.Lerp(1, 3, zfloat.One));

		Assert.AreEqual(5000, zMathf.Repeat(zfloat.FromRaw(25000), zfloat.One).value);
		Assert.AreEqual(5000, zMathf.PingPong(zfloat.FromRaw(15000), zfloat.One).value);
		Assert.AreEqual(5000, zMathf.PingPong(zfloat.FromRaw(35000), zfloat.One).value);
	}

	[Test]
	public void ApproximateHypotenuse_IsMonotonicAndClose()
	{
		zfloat h1 = zMathf.ApproximateHypotenuse((zfloat)3, (zfloat)4);
		zfloat h2 = zMathf.ApproximateHypotenuse((zfloat)3, (zfloat)8);
		zfloat exact = zMathf.Sqrt((zfloat)25);

		Assert.Greater(h2.value, h1.value);
		AssertRawClose(exact, h1, 600);
	}

	[Test]
	public void TryPow_HandlesInvalidDomainWithoutLogging()
	{
		Assert.IsFalse(zMathf.TryPow((zfloat)(-2), zfloat.Half, out zfloat invalid));
		Assert.AreEqual(zfloat.Zero.value, invalid.value);

		Assert.IsTrue(zMathf.TryPow((zfloat)2, (zfloat)3, out zfloat valid));
		Assert.AreEqual(80000, valid.value);
	}
}
