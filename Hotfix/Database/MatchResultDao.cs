using System.Text.Json;
using Fantasy;

namespace Hotfix.Database;

/// <summary>
/// 匹配结果 Redis 访问层。
/// 业务层只通过本 DAO 访问匹配结果；底层连接必须来自当前 Scene 的 <see cref="RedisComponent"/>。
/// </summary>
public static class MatchResultDao
{
    private const string EnvMatchResultTopic = "MATCH_RESULT_TOPIC";
    private const string EnvMatchResultTtlSeconds = "MATCH_RESULT_TTL_SECONDS";

    /// <summary>
    /// 写入匹配结果：key={topic}:{roomId}:{userId}，JSON value，键级 TTL。
    /// </summary>
    /// <param name="redis">当前 Scene 的独立 Redis 实例。</param>
    /// <returns>成功返回 true；失败时 <paramref name="error"/> 为原因。</returns>
    public static bool TrySave(RedisComponent redis, long userId, long roomId, int matchType, out string error)
    {
        error = string.Empty;

        if (redis == null)
        {
            error = "Redis 实例缺失";
            return false;
        }

        if (userId <= 0 || roomId <= 0)
        {
            error = "参数非法";
            return false;
        }

        if (!TryResolveConfig(out var topic, out var ttlSeconds, out error))
        {
            return false;
        }

        var key = BuildKey(topic, roomId, userId);
        var payload = new MatchResultMessage
        {
            user_id = userId,
            room_id = roomId,
            match_type = matchType,
            matched_at_unix_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ttl_seconds = ttlSeconds,
        };

        try
        {
            var json = JsonSerializer.Serialize(payload);
            var db = redis.GetDatabase();
            var ok = db.StringSet(key, json, TimeSpan.FromSeconds(ttlSeconds));
            if (!ok)
            {
                error = "Redis 写入失败";
                Log.Warning($"MatchResultDao.TrySave StringSet 返回 false: key={key}, userId={userId}, roomId={roomId}");
                return false;
            }

            Log.Debug(
                $"MatchResultDao.TrySave 成功: key={key}, ttl={ttlSeconds}s, userId={userId}, roomId={roomId}, matchType={matchType}");
            return true;
        }
        catch (Exception ex)
        {
            error = "Redis 写入异常";
            Log.Error($"MatchResultDao.TrySave 异常: key={key}, userId={userId}, roomId={roomId}, ex={ex}");
            return false;
        }
    }

    private static bool TryResolveConfig(out string topic, out int ttlSeconds, out string error)
    {
        topic = string.Empty;
        ttlSeconds = 0;
        error = string.Empty;

        var topicRaw = Environment.GetEnvironmentVariable(EnvMatchResultTopic);
        if (string.IsNullOrWhiteSpace(topicRaw))
        {
            error = $"{EnvMatchResultTopic} 未配置";
            Log.Warning($"MatchResultDao 配置缺失: {EnvMatchResultTopic}");
            return false;
        }

        var ttlRaw = Environment.GetEnvironmentVariable(EnvMatchResultTtlSeconds);
        if (!int.TryParse(ttlRaw, out ttlSeconds) || ttlSeconds <= 0)
        {
            error = $"{EnvMatchResultTtlSeconds} 非法";
            Log.Warning($"MatchResultDao TTL 非法: {EnvMatchResultTtlSeconds}={ttlRaw}");
            return false;
        }

        topic = topicRaw.Trim();
        return true;
    }

    private static string BuildKey(string topic, long roomId, long userId)
    {
        return $"{topic}:{roomId}:{userId}";
    }

    private sealed class MatchResultMessage
    {
        public long user_id { get; set; }
        public long room_id { get; set; }
        public int match_type { get; set; }
        public long matched_at_unix_ms { get; set; }
        public int ttl_seconds { get; set; }
    }
}
