using Fantasy;
using Fantasy.Entitas;
using StackExchange.Redis;

namespace Hotfix.Database;

/// <summary>
/// Scene 级 Redis 客户端。
/// 每个 Scene 各自持有独立 <see cref="ConnectionMultiplexer"/>，不跨 Scene 共享。
/// </summary>
public sealed class RedisComponent : Fantasy.Entitas.Entity
{
    private ConnectionMultiplexer? _connection;
    private RedisConfig _config;
    private readonly object _lock = new();

    public RedisConfig Config => _config;

    public bool IsConnected => _connection is { IsConnected: true };

    /// <summary>
    /// 建立本 Scene 的独立 Redis 连接。
    /// </summary>
    /// <param name="abortOnFail">true 时连接失败立即抛错；false 时允许后台重连语义。</param>
    public void Connect(bool abortOnFail = true)
    {
        _config = RedisConfig.LoadFromEnv();
        try
        {
            var connection = ConnectionMultiplexer.Connect(_config.ToConfigurationString(abortConnect: abortOnFail));
            if (abortOnFail)
            {
                _ = connection.GetDatabase().Ping();
            }

            lock (_lock)
            {
                _connection?.Dispose();
                _connection = connection;
            }

            Log.Info(
                $"[Redis] Scene 连接成功 sceneType={Scene?.SceneType}, sceneId={Scene?.SceneConfigId}, runtimeId={Scene?.RuntimeId}, {_config.Host}:{_config.Port} db={_config.Database}");
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"[Redis] Scene 连接失败 sceneType={Scene?.SceneType}, sceneId={Scene?.SceneConfigId}, {_config.Host}:{_config.Port}",
                ex);
        }
    }

    public IDatabase GetDatabase(int db = -1)
    {
        return EnsureConnection().GetDatabase(db);
    }

    public ISubscriber GetSubscriber()
    {
        return EnsureConnection().GetSubscriber();
    }

    public IServer GetServer()
    {
        var connection = EnsureConnection();
        var endpoint = connection.GetEndPoints().FirstOrDefault()
            ?? throw new InvalidOperationException("Redis 未配置可用 endpoint");
        return connection.GetServer(endpoint);
    }

    /// <summary>
    /// 关闭并释放本 Scene 的 Redis 连接。
    /// </summary>
    public void Close()
    {
        lock (_lock)
        {
            if (_connection == null)
            {
                return;
            }

            try
            {
                _connection.Dispose();
            }
            catch (Exception ex)
            {
                Log.Warning(
                    $"[Redis] Scene 关闭连接异常 sceneType={Scene?.SceneType}, sceneId={Scene?.SceneConfigId}, ex={ex.Message}");
            }
            finally
            {
                _connection = null;
            }
        }
    }

    private ConnectionMultiplexer EnsureConnection()
    {
        if (_connection is { IsConnected: true })
        {
            return _connection;
        }

        lock (_lock)
        {
            if (_connection is { IsConnected: true })
            {
                return _connection;
            }

            _connection?.Dispose();
            _config = RedisConfig.LoadFromEnv();
            // 运行时允许后台重连
            _connection = ConnectionMultiplexer.Connect(_config.ToConfigurationString(abortConnect: false));
            Log.Info(
                $"[Redis] Scene 运行时重连 sceneType={Scene?.SceneType}, sceneId={Scene?.SceneConfigId}, {_config.Host}:{_config.Port} db={_config.Database}");
            return _connection;
        }
    }
}
