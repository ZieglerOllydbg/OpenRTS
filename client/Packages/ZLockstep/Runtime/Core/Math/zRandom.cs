using System;

namespace zUnity
{
	/// <summary>
	/// 基于 48-bit LCG 的确定性随机数生成器。
	/// </summary>
	/// <remarks>
	/// 设计目标：跨平台可复现、接口语义统一、边界行为明确。
	/// 区间随机接口统一采用左闭右开语义 [min, max)。
	/// </remarks>
	[Serializable]
	public class zRandom
	{
		private const long multiplier = 0x5DEECE66DL;
		private const long addend = 0xBL;
		private const long mask = (1L << 48) - 1;
		private const long zfloatScaleRaw = zfloat.SCALE_10000;

		private static readonly object SeedLock = new object();
		private static long seedUniquifier = 8682522807148012L;
		private static long debugGlobalDeterminismState;

		private long sseed;

		/// <summary>
		/// 使用时间相关种子初始化一个随机数实例。
		/// </summary>
		public zRandom()
			: this(SeedUniquifier() ^ Environment.TickCount)
		{
		}

		/// <summary>
		/// 使用指定种子初始化一个随机数实例。
		/// </summary>
		/// <param name="seed">原始种子值。</param>
		public zRandom(long seed)
		{
			sseed = InitialScramble(seed);
		}

		/// <summary>
		/// 重置内部种子。
		/// </summary>
		/// <param name="seed">原始种子值。</param>
		public void SetSeed(long seed)
		{
			sseed = InitialScramble(seed);
		}

		/// <summary>
		/// 获取当前实例的内部种子状态（仅调试构建有效）。
		/// </summary>
		public long GetStateRaw()
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			return sseed;
#else
			return 0L;
#endif
		}

		/// <summary>
		/// 获取全局随机状态快照（仅调试构建有效）。
		/// </summary>
		public static long GetGlobalDeterminismStateRaw()
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			return debugGlobalDeterminismState;
#else
			return 0L;
#endif
		}

		/// <summary>
		/// 重置全局随机状态快照（仅调试构建有效）。
		/// </summary>
		public static void ResetGlobalDeterminismStateRaw(long state = 0L)
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			debugGlobalDeterminismState = state;
#endif
		}

		/// <summary>
		/// 生成 32-bit 有符号整数。
		/// </summary>
		public int NextInt()
		{
			return Next(32);
		}

		/// <summary>
		/// 生成区间 [0, n) 的随机整数。
		/// </summary>
		/// <param name="n">上界（不含）。</param>
		/// <returns>随机值，满足 0 &lt;= result &lt; n。</returns>
		/// <exception cref="ArgumentOutOfRangeException">n 小于 0。</exception>
		public int NextInt(int n)
		{
			if (n == 0)
			{
				return 0;
			}

			if (n < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(n), "n 不能为负数。");
			}

			if ((n & -n) == n)
			{
				return (int)((n * (long)Next(31)) >> 31);
			}

			int bits;
			int val;
			do
			{
				bits = Next(31);
				val = bits % n;
			} while (bits - val + (n - 1) < 0);

			return val;
		}

		/// <summary>
		/// 生成区间 [minValue, maxValue) 的随机整数。
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">minValue 大于 maxValue。</exception>
		public int NextInt(int minValue, int maxValue)
		{
			if (minValue > maxValue)
			{
				throw new ArgumentOutOfRangeException(nameof(minValue), "minValue 不能大于 maxValue。");
			}

			if (minValue == maxValue)
			{
				return minValue;
			}

			int range = maxValue - minValue;
			return minValue + NextInt(range);
		}

		/// <summary>
		/// 生成 64-bit 有符号整数。
		/// </summary>
		public long NextLong()
		{
			return ((long)Next(32) << 32) | (uint)Next(32);
		}

		/// <summary>
		/// 生成区间 [minValue, maxValue) 的随机长整数。
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">minValue 大于 maxValue。</exception>
		public long NextLong(long minValue, long maxValue)
		{
			if (minValue > maxValue)
			{
				throw new ArgumentOutOfRangeException(nameof(minValue), "minValue 不能大于 maxValue。");
			}

			if (minValue == maxValue)
			{
				return minValue;
			}

			ulong range = unchecked((ulong)maxValue - (ulong)minValue);
			ulong offset = NextUInt64Below(range);
			return unchecked((long)(offset + (ulong)minValue));
		}

		/// <summary>
		/// 生成区间 [minValue, maxValue) 的定点随机数。
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">minValue 大于 maxValue。</exception>
		public zfloat NextZFloat(zfloat minValue, zfloat maxValue)
		{
			long raw = NextLong(minValue.value, maxValue.value);
			return zfloat.FromRaw(raw);
		}

		/// <summary>
		/// 以 50% 概率返回 true。
		/// </summary>
		public bool NextBool()
		{
			return Next(1) != 0;
		}

		/// <summary>
		/// 按给定概率返回 true。
		/// </summary>
		/// <param name="probability">触发概率，范围 [0, 1]。</param>
		/// <exception cref="ArgumentOutOfRangeException">probability 超出 [0, 1]。</exception>
		public bool NextBool(zfloat probability)
		{
			if (probability < zfloat.Zero || probability > zfloat.One)
			{
				throw new ArgumentOutOfRangeException(nameof(probability), "probability 必须在 [0,1] 范围内。");
			}

			if (probability == zfloat.Zero)
			{
				return false;
			}

			if (probability == zfloat.One)
			{
				return true;
			}

			long sampleRaw = NextLong(0L, zfloatScaleRaw);
			return sampleRaw < probability.value;
		}

		/// <summary>
		/// 生成单位圆上的随机点（模长约等于 1）。
		/// </summary>
		public zVector2 NextUnitVector2()
		{
			int angle = NextInt(0, 360);
			return new zVector2(zMathf.CosAngle(angle), zMathf.SinAngle(angle));
		}

		/// <summary>
		/// 生成单位圆内的随机点（模长小于等于 1）。
		/// </summary>
		/// <remarks>
		/// 采用极坐标采样：theta 均匀，半径使用 sqrt(u) 修正面积分布。
		/// </remarks>
		public zVector2 NextInsideUnitCircle()
		{
			zfloat radius = zMathf.Sqrt(NextZFloat(zfloat.Zero, zfloat.One));
			return NextUnitVector2() * radius;
		}

		/// <summary>
		/// 用随机字节填充缓冲区。
		/// </summary>
		/// <exception cref="ArgumentNullException">bytes 为 null。</exception>
		public void NextBytes(byte[] bytes)
		{
			if (bytes == null)
			{
				throw new ArgumentNullException(nameof(bytes));
			}

			for (int i = 0; i < bytes.Length;)
			{
				int rnd = NextInt();
				int n = Math.Min(bytes.Length - i, sizeof(int));
				for (int j = 0; j < n; j++)
				{
					bytes[i++] = (byte)rnd;
					rnd >>= 8;
				}
			}
		}

		private static long SeedUniquifier()
		{
			lock (SeedLock)
			{
				seedUniquifier = seedUniquifier * 181783497276652981L;
				return seedUniquifier;
			}
		}

		private static long InitialScramble(long seed)
		{
			return (seed ^ multiplier) & mask;
		}

		private int Next(int bits)
		{
			if (bits <= 0 || bits > 32)
			{
				throw new ArgumentOutOfRangeException(nameof(bits), "bits 必须在 1 到 32 之间。");
			}

			sseed = (sseed * multiplier + addend) & mask;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			unchecked
			{
				debugGlobalDeterminismState = (debugGlobalDeterminismState * 1103515245L) + sseed + bits;
			}
#endif
			return (int)(sseed >> (48 - bits));
		}

		private ulong NextUInt64()
		{
			return ((ulong)(uint)Next(32) << 32) | (uint)Next(32);
		}

		private ulong NextUInt64Below(ulong upperExclusive)
		{
			if (upperExclusive == 0)
			{
				return 0;
			}

			if ((upperExclusive & (upperExclusive - 1)) == 0)
			{
				return NextUInt64() & (upperExclusive - 1);
			}

			ulong limit = ulong.MaxValue - (ulong.MaxValue % upperExclusive);
			ulong sample;
			do
			{
				sample = NextUInt64();
			} while (sample >= limit);

			return sample % upperExclusive;
		}
	}
}
