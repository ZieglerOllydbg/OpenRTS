using NUnit.Framework;

public class zfloatTests
{
	[Test]
	public void Arithmetic_And_Comparison_Work_With_Int_And_Long()
	{
		zfloat a = zfloat.FromRaw(15000); // 1.5
		zfloat b = zfloat.FromRaw(25000); // 2.5

		Assert.AreEqual(40000, (a + b).value);
		Assert.AreEqual(-10000, (a - b).value);
		Assert.AreEqual(37500, (a * b).value);
		Assert.AreEqual(6000, (a / b).value);
		Assert.AreEqual(10000, (11 % b).value);

		Assert.IsTrue(1 < a);
		Assert.IsFalse(2 < zfloat.One);
		Assert.IsTrue(3 > a);
		Assert.IsTrue(a <= 2);
		Assert.IsTrue(a >= 1);

		zfloat longMul = 3L * a;
		Assert.AreEqual(45000, longMul.value);
	}

	[Test]
	public void OperatorLessThan_IntLeft_Side_Uses_Correct_Logic()
	{
		zfloat onePointFive = zfloat.FromRaw(15000);
		Assert.IsTrue(1 < onePointFive);
		Assert.IsFalse(2 < onePointFive);
	}

	[Test]
	public void Parse_And_TryParse_Handle_Negative_And_Whitespace()
	{
		Assert.IsTrue(zfloat.TryParse(" -1.25 ", out zfloat parsed));
		Assert.AreEqual(-12500, parsed.value);

		zfloat parsedByThrow = zfloat.Parse("-.25");
		Assert.AreEqual(-2500, parsedByThrow.value);

		Assert.IsFalse(zfloat.TryParse("1.2.3", out _));
		Assert.IsFalse(zfloat.TryParse("abc", out _));
		Assert.Throws<System.FormatException>(() => zfloat.Parse("abc"));
	}

	[Test]
	public void Parse_Uses_Truncation_For_More_Than_Four_Decimals()
	{
		zfloat parsed = zfloat.Parse("1.23456");
		Assert.AreEqual(12345, parsed.value);
	}

	[Test]
	public void ToString_Is_Consistent_With_Parse()
	{
		zfloat source = zfloat.FromRaw(-12345);
		string text = source.ToString();
		zfloat roundTrip = zfloat.Parse(text);

		Assert.AreEqual("-1.2345", text);
		Assert.AreEqual(source.value, roundTrip.value);
	}

	[Test]
	public void Core_Apis_Work_As_Expected()
	{
		zfloat byRaw = zfloat.FromRaw(43210);
		zfloat frac = zfloat.Parse("-1.2500").GetFractionalPart();
		Assert.AreEqual(43210, zfloat.FromRaw(43210).value);
		Assert.AreEqual(43210, byRaw.value);
		Assert.AreEqual(-2500, frac.value);
		Assert.AreEqual(-1, zfloat.Parse("-1.2500").GetInteger());
		Assert.IsTrue(zfloat.FromRaw(10009).Approximately(zfloat.One, zfloat.FromRaw(10)));
	}

	[Test]
	public void Boundary_Values_And_Division_By_Zero()
	{
		Assert.AreEqual(0, zfloat.Zero.value);
		Assert.AreEqual(1, zfloat.Epsilon.value);
		Assert.AreEqual(zfloat.MaxValue.value, zfloat.Infinity.value);
		Assert.AreEqual(long.MaxValue, zfloat.MaxValue.value);
		Assert.AreEqual(long.MinValue, zfloat.MinValue.value);
		Assert.Greater(zfloat.Infinity.value, 0);

		Assert.AreEqual(1, zfloat.Sign(zfloat.Epsilon));
		Assert.AreEqual(0, zfloat.Sign(zfloat.Zero));
		Assert.AreEqual(-1, zfloat.Sign(-zfloat.Epsilon));

		Assert.Throws<System.DivideByZeroException>(() =>
		{
			_ = zfloat.One / zfloat.Zero;
		});
	}

	[Test]
	public void Approximately_And_Clamp_Work_As_Expected()
	{
		zfloat center = zfloat.One;
		zfloat near = zfloat.FromRaw(10009);
		zfloat far = zfloat.FromRaw(10020);
		zfloat tolerance = zfloat.FromRaw(10);

		Assert.IsTrue(zfloat.Approximately(center, near, tolerance));
		Assert.IsFalse(zfloat.Approximately(center, far, tolerance));
		Assert.AreEqual(10000, zfloat.Clamp(zfloat.FromRaw(9999), zfloat.One, zfloat.Two).value);
		Assert.AreEqual(20000, zfloat.Clamp(zfloat.FromRaw(30000), zfloat.One, zfloat.Two).value);
		Assert.AreEqual(15000, zfloat.Clamp(zfloat.FromRaw(15000), zfloat.One, zfloat.Two).value);
	}

	[Test]
	public void Safe_Arithmetic_TryApis_Report_Success_And_Failure()
	{
		Assert.IsTrue(zfloat.TryAdd(zfloat.One, zfloat.Two, out zfloat addResult));
		Assert.AreEqual(30000, addResult.value);

		Assert.IsTrue(zfloat.TrySub(zfloat.Two, zfloat.One, out zfloat subResult));
		Assert.AreEqual(10000, subResult.value);

		Assert.IsTrue(zfloat.TryMul(zfloat.FromRaw(15000), zfloat.FromRaw(20000), out zfloat mulResult));
		Assert.AreEqual(30000, mulResult.value);

		Assert.IsTrue(zfloat.TryDiv(zfloat.FromRaw(15000), zfloat.FromRaw(30000), out zfloat divResult));
		Assert.AreEqual(5000, divResult.value);

		Assert.IsFalse(zfloat.TryAdd(zfloat.MaxValue, zfloat.One, out _));
		Assert.IsFalse(zfloat.TryMul(zfloat.MaxValue, zfloat.Two, out _));
		Assert.IsFalse(zfloat.TryDiv(zfloat.One, zfloat.Zero, out _));
	}

	[Test]
	public void Checked_Arithmetic_Throws_On_Overflow_And_DivideByZero()
	{
		Assert.AreEqual(30000, zfloat.CheckedAdd(zfloat.One, zfloat.Two).value);
		Assert.AreEqual(10000, zfloat.CheckedSub(zfloat.Two, zfloat.One).value);
		Assert.AreEqual(30000, zfloat.CheckedMul(zfloat.FromRaw(15000), zfloat.FromRaw(20000)).value);
		Assert.AreEqual(5000, zfloat.CheckedDiv(zfloat.FromRaw(15000), zfloat.FromRaw(30000)).value);

		Assert.Throws<System.OverflowException>(() => zfloat.CheckedAdd(zfloat.MaxValue, zfloat.One));
		Assert.Throws<System.OverflowException>(() => zfloat.CheckedSub(zfloat.MinValue, zfloat.One));
		Assert.Throws<System.OverflowException>(() => zfloat.CheckedMul(zfloat.MaxValue, zfloat.Two));
		Assert.Throws<System.DivideByZeroException>(() => zfloat.CheckedDiv(zfloat.One, zfloat.Zero));
	}

	[Test]
	public void Abs_Handles_LongMinValue()
	{
		zfloat minRaw = zfloat.FromRaw(long.MinValue);
		zfloat abs = zfloat.Abs(minRaw);
		Assert.AreEqual(zfloat.MaxValue.value, abs.value);
	}

	[Test]
	public void UnaryMinus_LongMinValue_Is_Clamped_To_MaxValue()
	{
		zfloat minRaw = zfloat.FromRaw(long.MinValue);
		zfloat negated = -minRaw;
		Assert.AreEqual(zfloat.MaxValue.value, negated.value);
	}

	[Test]
	public void Approximately_Handles_Extreme_Values_Without_Overflow()
	{
		Assert.IsFalse(zfloat.Approximately(zfloat.MaxValue, zfloat.MinValue, zfloat.MaxValue));
		Assert.IsTrue(zfloat.Approximately(zfloat.Zero, zfloat.Epsilon, zfloat.FromRaw(long.MinValue)));
	}

	[Test]
	public void Operators_Throw_On_Overflow()
	{
		Assert.Throws<System.OverflowException>(() =>
		{
			_ = zfloat.MaxValue + zfloat.One;
		});
		Assert.Throws<System.OverflowException>(() =>
		{
			_ = zfloat.MinValue - zfloat.One;
		});
		Assert.Throws<System.OverflowException>(() =>
		{
			_ = zfloat.MaxValue * zfloat.Two;
		});
		Assert.Throws<System.OverflowException>(() =>
		{
			_ = zfloat.MaxValue / zfloat.One;
		});
		Assert.Throws<System.OverflowException>(() =>
		{
			_ = int.MaxValue + zfloat.MaxValue;
		});
		Assert.Throws<System.OverflowException>(() =>
		{
			_ = long.MaxValue % zfloat.One;
		});
	}

	[Test]
	public void Parse_Extreme_Inputs_Are_Handled()
	{
		Assert.IsTrue(zfloat.TryParse("+.25", out zfloat plusNoIntPart));
		Assert.AreEqual(2500, plusNoIntPart.value);
		Assert.IsTrue(zfloat.TryParse("-.25", out zfloat minusNoIntPart));
		Assert.AreEqual(-2500, minusNoIntPart.value);

		Assert.IsFalse(zfloat.TryParse("999999999999999999999999", out _));
		Assert.IsFalse(zfloat.TryParse("9223372036854775807.9999", out _));
	}
}
