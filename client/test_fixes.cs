using System;
using zUnity;

class TestFixes
{
    static void Main()
    {
        Console.WriteLine("Testing zVector2 fixes...\n");
        
        // Test 1: Cast operator fix - Test_IsZero_Threshold
        Console.WriteLine("Test 1: Test_IsZero_Threshold");
        zVector2 zero = zVector2.zero;
        Console.WriteLine($"  zero.IsZero() = {zero.IsZero()} (expected: True)");
        
        zVector2 nonZero = new zVector2((zfloat)0.0001, (zfloat)0);
        Console.WriteLine($"  nonZero (0.0001, 0).IsZero() = {nonZero.IsZero()} (expected: False)");
        Console.WriteLine($"  nonZero.x.value = {nonZero.x.value} (expected: 1)");
        
        // Test 2: Magnitude fix - Test_LargeNumber_Precision
        Console.WriteLine("\nTest 2: Test_LargeNumber_Precision");
        zfloat largeX = (zfloat)10000;
        zfloat largeY = (zfloat)10000;
        zVector2 v = new zVector2(largeX, largeY);
        zfloat mag = v.magnitude;
        Console.WriteLine($"  Large vector (10000, 10000).magnitude.value = {mag.value}");
        Console.WriteLine($"  Expected: > 140000000 (≈ 14142 * 10000)");
        Console.WriteLine($"  Pass: {mag.value > 140000000}");
        
        // Test 3: Lerp - Test_Lerp_Interpolation
        Console.WriteLine("\nTest 3: Test_Lerp_Interpolation");
        zVector2 from = new zVector2((zfloat)0, (zfloat)0);
        zVector2 to = new zVector2((zfloat)10, (zfloat)10);
        zVector2 mid = zVector2.Lerp(from, to, (zfloat)0.5);
        Console.WriteLine($"  Lerp from (0,0) to (10,10) at t=0.5");
        Console.WriteLine($"  Result: ({mid.x}, {mid.y}) (expected: (5, 5))");
        Console.WriteLine($"  mid.x.value = {mid.x.value} (expected: 50000 = 5*10000)");
        
        // Test 4: Mul - Test_Mul_DirectMethod
        Console.WriteLine("\nTest 4: Test_Mul_DirectMethod");
        zVector2 v2 = new zVector2((zfloat)2, (zfloat)3);
        zVector2 factor = new zVector2((zfloat)1.5, (zfloat)2);
        Console.WriteLine($"  Before Mul: v2 = ({v2.x}, {v2.y})");
        Console.WriteLine($"  factor = ({factor.x}, {factor.y})");
        v2.Mul(ref factor);
        Console.WriteLine($"  After Mul: v2 = ({v2.x}, {v2.y}) (expected: (3, 6))");
        Console.WriteLine($"  v2.x.value = {v2.x.value} (expected: 30000 = 3*10000)");
        
        // Test 5: Normalize tiny - Test_NormalizeBehavior_VerySmallVectors
        Console.WriteLine("\nTest 5: Test_NormalizeBehavior_VerySmallVectors");
        zVector2 tiny = new zVector2(zfloat.Epsilon, zfloat.Epsilon);
        Console.WriteLine($"  tiny = ({tiny.x}, {tiny.y})");
        Console.WriteLine($"  tiny.x.value = {tiny.x.value}, tiny.y.value = {tiny.y.value}");
        zVector2 normalized = tiny.normalized;
        Console.WriteLine($"  normalized = ({normalized.x}, {normalized.y})");
        Console.WriteLine($"  normalized.IsZero() = {normalized.IsZero()} (expected: False)");
        
        Console.WriteLine("\nAll manual tests completed!");
    }
}
