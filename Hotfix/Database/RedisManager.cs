using StackExchange.Redis;

namespace Hotfix.Database;

public static class RedisManager
{
    private static ConnectionMultiplexer? _connection;
    private static readonly object _lock = new();

    /// <summary>
    /// 启动时连通性检查：连接 Redis 并 PING。
    /// 失败时抛出异常，阻止服务器启动。
    /// </summary>
    public static void Initialize()
    {
        Console.WriteLine("[Redis] 开始检查 Redis 连接...");
        var config = RedisConfig.LoadFromEnv();

        try
        {
            // 启动阶段要求立即成功，避免 abortConnect=false 把失败拖到后台
            var connection = ConnectionMultiplexer.Connect(config.ToConfigurationString(abortConnect: true));
            var latency = connection.GetDatabase().Ping();

            lock (_lock)
            {
                _connection?.Dispose();
                _connection = connection;
            }

            Console.WriteLine($"[Redis] 连接成功 {config.Host}:{config.Port} db={config.Database} ping={latency.TotalMilliseconds:F0}ms");
        }
        catch (Exception ex)
        {
            throw new Exception($"无法连接到 Redis 服务器 {config.Host}:{config.Port}", ex);
        }
    }

    public static ConnectionMultiplexer GetInstance()
    {
        if (_connection is { IsConnected: true }) return _connection;

        lock (_lock)
        {
            if (_connection is { IsConnected: true }) return _connection;

            _connection?.Dispose();
            // 运行时允许后台重连
            _connection = ConnectionMultiplexer.Connect(
                RedisConfig.LoadFromEnv().ToConfigurationString(abortConnect: false));
        }

        return _connection;
    }

    public static IDatabase GetDatabase(int db = -1)
    {
        return GetInstance().GetDatabase(db);
    }

    public static ISubscriber GetSubscriber()
    {
        return GetInstance().GetSubscriber();
    }

    public static IServer GetServer()
    {
        var connection = GetInstance();
        var endpoint = connection.GetEndPoints().FirstOrDefault()
            ?? throw new InvalidOperationException("Redis 未配置可用 endpoint");
        return connection.GetServer(endpoint);
    }
}
