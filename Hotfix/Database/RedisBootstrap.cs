using StackExchange.Redis;

namespace Hotfix.Database;

/// <summary>
/// 进程启动期 Redis 连通性探测。
/// 只做一次性 PING，不保留共享连接；运行时由各 Scene 的 <see cref="RedisComponent"/> 独立持有实例。
/// </summary>
public static class RedisBootstrap
{
    /// <summary>
    /// 启动时连通性检查：临时连接 Redis 并 PING，随后立即释放。
    /// 失败时抛出异常，阻止服务器启动。
    /// </summary>
    public static void VerifyOrThrow()
    {
        Console.WriteLine("[Redis] 开始检查 Redis 连接...");
        var config = RedisConfig.LoadFromEnv();

        try
        {
            using var connection = ConnectionMultiplexer.Connect(config.ToConfigurationString(abortConnect: true));
            var latency = connection.GetDatabase().Ping();
            Console.WriteLine(
                $"[Redis] 连通性检查通过 {config.Host}:{config.Port} db={config.Database} ping={latency.TotalMilliseconds:F0}ms（不保留共享连接）");
        }
        catch (Exception ex)
        {
            throw new Exception($"无法连接到 Redis 服务器 {config.Host}:{config.Port}", ex);
        }
    }
}
