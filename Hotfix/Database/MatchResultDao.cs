using System.Text.Json;
using Fantasy;
using StackExchange.Redis;

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

    /// <summary>
    /// 统计房间匹配占位条数：pattern={topic}:{roomId}:*。
    /// 每条 match 请求占 1 位。
    /// </summary>
    public static bool TryCountPlaceholders(RedisComponent redis, long roomId, out int count, out string error)
    {
        count = 0;
        error = string.Empty;

        if (redis == null)
        {
            error = "Redis 实例缺失";
            return false;
        }

        if (roomId <= 0)
        {
            error = "参数非法";
            return false;
        }

        if (!TryResolveTopic(out var topic, out error))
        {
            return false;
        }

        var pattern = BuildRoomPattern(topic, roomId);
        try
        {
            var server = redis.GetServer();
            var db = redis.GetDatabase();
            // Keys 在 StackExchange.Redis 中默认走 SCAN，避免 KEYS 阻塞。
            foreach (var _ in server.Keys(database: db.Database, pattern: pattern, pageSize: 256))
            {
                count++;
            }

            Log.Debug($"MatchResultDao.TryCountPlaceholders: roomId={roomId}, pattern={pattern}, count={count}");
            return true;
        }
        catch (Exception ex)
        {
            error = "Redis 占位计数异常";
            Log.Error($"MatchResultDao.TryCountPlaceholders 异常: roomId={roomId}, pattern={pattern}, ex={ex}");
            return false;
        }
    }

    /// <summary>
    /// 查询房间匹配占位的最大剩余 TTL（毫秒）。
    /// 无占位时 maxRemainMs=0 且返回 true。
    /// </summary>
    public static bool TryGetMaxRemainingTtlMs(RedisComponent redis, long roomId, out int maxRemainMs, out string error)
    {
        maxRemainMs = 0;
        error = string.Empty;

        if (redis == null)
        {
            error = "Redis 实例缺失";
            return false;
        }

        if (roomId <= 0)
        {
            error = "参数非法";
            return false;
        }

        if (!TryResolveTopic(out var topic, out error))
        {
            return false;
        }

        var pattern = BuildRoomPattern(topic, roomId);
        try
        {
            var server = redis.GetServer();
            var db = redis.GetDatabase();
            var maxMs = 0L;
            var scanned = 0;

            foreach (var key in server.Keys(database: db.Database, pattern: pattern, pageSize: 256))
            {
                scanned++;
                var ttl = db.KeyTimeToLive(key);
                if (!ttl.HasValue)
                {
                    // 无 TTL 的脏 key 不当作无限占位；忽略，交由批量删除清理。
                    continue;
                }

                var remainMs = (long)Math.Ceiling(ttl.Value.TotalMilliseconds);
                if (remainMs > maxMs)
                {
                    maxMs = remainMs;
                }
            }

            if (maxMs > int.MaxValue)
            {
                maxMs = int.MaxValue;
            }

            // 至少 1ms，避免 0 被上层当成“无占位”。
            maxRemainMs = maxMs > 0 ? (int)maxMs : 0;
            Log.Debug(
                $"MatchResultDao.TryGetMaxRemainingTtlMs: roomId={roomId}, pattern={pattern}, scanned={scanned}, maxRemainMs={maxRemainMs}");
            return true;
        }
        catch (Exception ex)
        {
            error = "Redis 查询占位 TTL 异常";
            Log.Error($"MatchResultDao.TryGetMaxRemainingTtlMs 异常: roomId={roomId}, pattern={pattern}, ex={ex}");
            return false;
        }
    }

    /// <summary>
    /// 按用户查询匹配结果：pattern={topic}:*:{userId}。
    /// 多条时取 matched_at_unix_ms 最新一条；成功时 roomId 为解析出的房间 ID。
    /// </summary>
    public static bool TryFindByUserId(RedisComponent redis, long userId, out long roomId, out string error)
    {
        roomId = 0;
        error = string.Empty;

        if (redis == null)
        {
            error = "Redis 实例缺失";
            return false;
        }

        if (userId <= 0)
        {
            error = "参数非法";
            return false;
        }

        if (!TryResolveTopic(out var topic, out error))
        {
            return false;
        }

        var pattern = BuildUserPattern(topic, userId);
        try
        {
            var server = redis.GetServer();
            var db = redis.GetDatabase();
            var found = false;
            var bestMatchedAt = long.MinValue;
            var bestRoomId = 0L;
            var scanned = 0;

            foreach (var key in server.Keys(database: db.Database, pattern: pattern, pageSize: 256))
            {
                scanned++;
                var value = db.StringGet(key);
                if (value.IsNullOrEmpty)
                {
                    continue;
                }

                MatchResultMessage? payload;
                try
                {
                    payload = JsonSerializer.Deserialize<MatchResultMessage>((string)value!);
                }
                catch (Exception ex)
                {
                    Log.Warning($"MatchResultDao.TryFindByUserId 解析失败: key={key}, userId={userId}, ex={ex.Message}");
                    continue;
                }

                if (payload == null || payload.room_id <= 0)
                {
                    continue;
                }

                // 兼容脏数据：JSON 里 user_id 不一致时仍以 key 后缀为准，但日志提示。
                if (payload.user_id > 0 && payload.user_id != userId)
                {
                    Log.Warning(
                        $"MatchResultDao.TryFindByUserId user_id 不一致: key={key}, expect={userId}, actual={payload.user_id}, roomId={payload.room_id}");
                }

                if (!found || payload.matched_at_unix_ms >= bestMatchedAt)
                {
                    found = true;
                    bestMatchedAt = payload.matched_at_unix_ms;
                    bestRoomId = payload.room_id;
                }
            }

            if (!found)
            {
                error = "未找到匹配结果";
                Log.Debug($"MatchResultDao.TryFindByUserId 无结果: userId={userId}, pattern={pattern}, scanned={scanned}");
                return false;
            }

            roomId = bestRoomId;
            Log.Debug(
                $"MatchResultDao.TryFindByUserId 成功: userId={userId}, roomId={roomId}, pattern={pattern}, scanned={scanned}, matchedAt={bestMatchedAt}");
            return true;
        }
        catch (Exception ex)
        {
            error = "Redis 查询匹配结果异常";
            Log.Error($"MatchResultDao.TryFindByUserId 异常: userId={userId}, pattern={pattern}, ex={ex}");
            return false;
        }
    }

    /// <summary>
    /// 删除指定用户在房间的匹配占位：key={topic}:{roomId}:{userId}。
    /// key 不存在视为成功（幂等）。
    /// </summary>
    public static bool Delete(RedisComponent redis, long roomId, long userId, out string error)
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

        if (!TryResolveTopic(out var topic, out error))
        {
            return false;
        }

        var key = BuildKey(topic, roomId, userId);
        try
        {
            var db = redis.GetDatabase();
            var deleted = db.KeyDelete(key);
            Log.Debug(
                $"MatchResultDao.Delete: key={key}, userId={userId}, roomId={roomId}, deleted={deleted}");
            return true;
        }
        catch (Exception ex)
        {
            error = "Redis 删除占位异常";
            Log.Error($"MatchResultDao.Delete 异常: key={key}, userId={userId}, roomId={roomId}, ex={ex}");
            return false;
        }
    }

    /// <summary>
    /// 批量删除房间全部匹配占位：pattern={topic}:{roomId}:*。
    /// 无 key 视为成功（幂等）。
    /// </summary>
    public static bool TryDeleteByRoom(RedisComponent redis, long roomId, out int deletedCount, out string error)
    {
        deletedCount = 0;
        error = string.Empty;

        if (redis == null)
        {
            error = "Redis 实例缺失";
            return false;
        }

        if (roomId <= 0)
        {
            error = "参数非法";
            return false;
        }

        if (!TryResolveTopic(out var topic, out error))
        {
            return false;
        }

        var pattern = BuildRoomPattern(topic, roomId);
        try
        {
            var server = redis.GetServer();
            var db = redis.GetDatabase();
            var batch = new List<RedisKey>(64);

            foreach (var key in server.Keys(database: db.Database, pattern: pattern, pageSize: 256))
            {
                batch.Add(key);
                if (batch.Count < 256)
                {
                    continue;
                }

                deletedCount += (int)db.KeyDelete(batch.ToArray());
                batch.Clear();
            }

            if (batch.Count > 0)
            {
                deletedCount += (int)db.KeyDelete(batch.ToArray());
            }

            Log.Info(
                $"MatchResultDao.TryDeleteByRoom: roomId={roomId}, pattern={pattern}, deleted={deletedCount}");
            return true;
        }
        catch (Exception ex)
        {
            error = "Redis 批量删除占位异常";
            Log.Error($"MatchResultDao.TryDeleteByRoom 异常: roomId={roomId}, pattern={pattern}, ex={ex}");
            return false;
        }
    }

    private static bool TryResolveConfig(out string topic, out int ttlSeconds, out string error)
    {
        topic = string.Empty;
        ttlSeconds = 0;
        error = string.Empty;

        if (!TryResolveTopic(out topic, out error))
        {
            return false;
        }

        var ttlRaw = Environment.GetEnvironmentVariable(EnvMatchResultTtlSeconds);
        if (!int.TryParse(ttlRaw, out ttlSeconds) || ttlSeconds <= 0)
        {
            error = $"{EnvMatchResultTtlSeconds} 非法";
            Log.Warning($"MatchResultDao TTL 非法: {EnvMatchResultTtlSeconds}={ttlRaw}");
            return false;
        }

        return true;
    }

    private static bool TryResolveTopic(out string topic, out string error)
    {
        topic = string.Empty;
        error = string.Empty;

        var topicRaw = Environment.GetEnvironmentVariable(EnvMatchResultTopic);
        if (string.IsNullOrWhiteSpace(topicRaw))
        {
            error = $"{EnvMatchResultTopic} 未配置";
            Log.Warning($"MatchResultDao 配置缺失: {EnvMatchResultTopic}");
            return false;
        }

        topic = topicRaw.Trim();
        return true;
    }

    private static string BuildKey(string topic, long roomId, long userId)
    {
        return $"{topic}:{roomId}:{userId}";
    }

    private static string BuildRoomPattern(string topic, long roomId)
    {
        return $"{topic}:{roomId}:*";
    }

    private static string BuildUserPattern(string topic, long userId)
    {
        return $"{topic}:*:{userId}";
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

