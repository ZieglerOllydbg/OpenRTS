using System;
using System.Diagnostics;
using UnityEngine;

public class zUDebug
{
	public static bool enable = true;

	[Conditional("ZLOGTRACE")]
	public static void Log(object obj)
	{
#if !ONLYCSHARP
		// 只有编辑器下才输出日志
		if (enable && Application.isEditor)
			UnityEngine.Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] {obj}");
#else
		Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {obj}");
#endif
	}

	[Conditional("ZLOGINFO")]
	public static void LogInfo(object obj)
	{
#if !ONLYCSHARP
		if (enable)
			UnityEngine.Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] {obj}");
#else
		Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {obj}");
#endif
	}

	[Conditional("ZLOGWARN")]
	public static void LogWarning(object obj)
	{
#if !ONLYCSHARP
		if (enable)
			UnityEngine.Debug.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] {obj}");
#else
		Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {obj}");
#endif
	}

	public static void LogError(object obj)
	{
#if !ONLYCSHARP
		if (enable)
			UnityEngine.Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] {obj}");
#else
		Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {obj}");
#endif
	}
}