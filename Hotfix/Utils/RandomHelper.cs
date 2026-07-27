namespace Hotfix.Utils;

/// <summary>
/// 线程安全的随机数生成器（每个线程独立 Random 实例）。
/// </summary>
public static class RandomHelper
{
    private static readonly ThreadLocal<Random> RandomLocal = new(() =>
        new Random(Guid.NewGuid().GetHashCode()));

    /// <summary>[0, max) 随机整数。</summary>
    public static int NextInt32(int max)
    {
        return RandomLocal.Value!.Next(max);
    }

    /// <summary>[min, max) 随机整数。</summary>
    public static int NextInt32(int min, int max)
    {
        return RandomLocal.Value!.Next(min, max);
    }

    /// <summary>[0, 1.0) 随机浮点数。</summary>
    public static double NextDouble()
    {
        return RandomLocal.Value!.NextDouble();
    }

    /// <summary>填充随机字节。</summary>
    public static void NextBytes(byte[] buffer)
    {
        RandomLocal.Value!.NextBytes(buffer);
    }
}
