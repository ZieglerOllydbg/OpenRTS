using NUnit.Framework;
using zUnity;

public class zRandomTests
{
	[Test]
	public void SameSeed_ProducesSameSequence()
	{
		zRandom a = new zRandom(123456789);
		zRandom b = new zRandom(123456789);

		for (int i = 0; i < 128; i++)
		{
			Assert.AreEqual(a.NextInt(), b.NextInt(), $"index={i}");
		}
	}

	[Test]
	public void DifferentSeeds_UsuallyProduceDifferentSequence()
	{
		zRandom a = new zRandom(1);
		zRandom b = new zRandom(2);
		bool hasDifference = false;

		for (int i = 0; i < 16; i++)
		{
			if (a.NextInt() != b.NextInt())
			{
				hasDifference = true;
				break;
			}
		}

		Assert.IsTrue(hasDifference);
	}

	[Test]
	public void NextIntRange_UsesLeftClosedRightOpen()
	{
		zRandom random = new zRandom(777);
		for (int i = 0; i < 5000; i++)
		{
			int v = random.NextInt(-17, 23);
			Assert.GreaterOrEqual(v, -17);
			Assert.Less(v, 23);
		}
	}

	[Test]
	public void NextLongRange_UsesLeftClosedRightOpen_AndSupportsLargeSpan()
	{
		zRandom random = new zRandom(999);
		for (int i = 0; i < 2000; i++)
		{
			long v = random.NextLong(long.MinValue, long.MaxValue);
			Assert.GreaterOrEqual(v, long.MinValue);
			Assert.Less(v, long.MaxValue);
		}
	}

	[Test]
	public void NextZFloatRange_UsesLeftClosedRightOpen()
	{
		zRandom random = new zRandom(404);
		zfloat min = zfloat.FromRaw(-12345);
		zfloat max = zfloat.FromRaw(54321);

		for (int i = 0; i < 5000; i++)
		{
			zfloat v = random.NextZFloat(min, max);
			Assert.GreaterOrEqual(v.value, min.value);
			Assert.Less(v.value, max.value);
		}
	}

	[Test]
	public void RangeBoundaries_AndExceptions_AreStandardized()
	{
		zRandom random = new zRandom(2026);

		Assert.AreEqual(8, random.NextInt(8, 8));
		Assert.AreEqual(100L, random.NextLong(100L, 100L));
		Assert.AreEqual(3456, random.NextZFloat(zfloat.FromRaw(3456), zfloat.FromRaw(3456)).value);

		Assert.Throws<System.ArgumentOutOfRangeException>(() => random.NextInt(9, 8));
		Assert.Throws<System.ArgumentOutOfRangeException>(() => random.NextLong(100, 99));
		Assert.Throws<System.ArgumentOutOfRangeException>(() => random.NextZFloat(zfloat.One, zfloat.Zero));
	}

	[Test]
	public void NextBytes_CoversEmptyAndNonAlignedLength()
	{
		zRandom random = new zRandom(31415);
		byte[] zero = new byte[0];
		byte[] one = new byte[1];
		byte[] seven = new byte[7];

		random.NextBytes(zero);
		random.NextBytes(one);
		random.NextBytes(seven);

		Assert.AreEqual(0, zero.Length);
		Assert.AreEqual(1, one.Length);
		Assert.AreEqual(7, seven.Length);

		Assert.Throws<System.ArgumentNullException>(() => random.NextBytes(null));
	}

	[Test]
	public void NextBool_WithProbability_HandlesBoundaryAndValidation()
	{
		zRandom random = new zRandom(42);
		Assert.IsFalse(random.NextBool(zfloat.Zero));
		Assert.IsTrue(random.NextBool(zfloat.One));
		Assert.Throws<System.ArgumentOutOfRangeException>(() => random.NextBool(zfloat.FromRaw(-1)));
		Assert.Throws<System.ArgumentOutOfRangeException>(() => random.NextBool(zfloat.FromRaw(10001)));
	}

	[Test]
	public void Distribution_BasicBucketCheck_IsReasonablyBalanced()
	{
		zRandom random = new zRandom(13579);
		int[] buckets = new int[10];
		const int sampleCount = 10000;

		for (int i = 0; i < sampleCount; i++)
		{
			buckets[random.NextInt(10)]++;
		}

		for (int i = 0; i < buckets.Length; i++)
		{
			Assert.Greater(buckets[i], 700, $"bucket[{i}] too small: {buckets[i]}");
			Assert.Less(buckets[i], 1300, $"bucket[{i}] too large: {buckets[i]}");
		}
	}

	[Test]
	public void CircleApis_ReturnExpectedMagnitudeConstraints()
	{
		zRandom random = new zRandom(271828);
		for (int i = 0; i < 1000; i++)
		{
			zVector2 on = random.NextUnitVector2();
			zVector2 inside = random.NextInsideUnitCircle();

			long onDiff = zfloat.Abs(on.magnitude - zfloat.One).value;
			Assert.LessOrEqual(onDiff, 150);
			Assert.LessOrEqual(inside.magnitude.value, zfloat.One.value);
		}
	}

	[Test]
	public void ModernApis_AreDeterministic_WithSameSeed()
	{
		const int seed = 9527;
		zRandom a = new zRandom(seed);
		zRandom b = new zRandom(seed);

		for (int i = 0; i < 32; i++)
		{
			Assert.AreEqual(a.NextInt(-8, 17), b.NextInt(-8, 17));
		}

		a = new zRandom(seed);
		b = new zRandom(seed);
		for (int i = 0; i < 32; i++)
		{
			Assert.AreEqual(a.NextLong(-500000000000L, 500000000000L), b.NextLong(-500000000000L, 500000000000L));
		}

		a = new zRandom(seed);
		b = new zRandom(seed);
		for (int i = 0; i < 32; i++)
		{
			Assert.AreEqual(a.NextZFloat(zfloat.FromRaw(-5000), zfloat.FromRaw(9000)).value, b.NextZFloat(zfloat.FromRaw(-5000), zfloat.FromRaw(9000)).value);
		}

		a = new zRandom(seed);
		b = new zRandom(seed);
		for (int i = 0; i < 16; i++)
		{
			zVector2 va = a.NextUnitVector2();
			zVector2 vb = b.NextUnitVector2();
			Assert.AreEqual(va.x.value, vb.x.value);
			Assert.AreEqual(va.y.value, vb.y.value);
		}

		a = new zRandom(seed);
		b = new zRandom(seed);
		for (int i = 0; i < 16; i++)
		{
			zVector2 va = a.NextInsideUnitCircle();
			zVector2 vb = b.NextInsideUnitCircle();
			Assert.AreEqual(va.x.value, vb.x.value);
			Assert.AreEqual(va.y.value, vb.y.value);
		}
	}
}
