namespace Hotfix.Database;

public readonly record struct RedisConfig(
    string Host,
    string Port,
    string Password,
    int Database
)
{
    public static RedisConfig LoadFromEnv()
    {
        return new RedisConfig(
            Env("REDIS_HOST", "localhost"),
            Env("REDIS_PORT", "6379"),
            Env("REDIS_PASSWORD", string.Empty),
            ParseInt(Env("REDIS_DB", "0"), 0)
        );
    }

    /// <param name="abortConnect">
    /// true：连接失败立即抛错（启动检查用）；
    /// false：后台重连（运行时用）。
    /// </param>
    public string ToConfigurationString(bool abortConnect = false)
    {
        var parts = new List<string>
        {
            $"{Host}:{Port}",
            $"defaultDatabase={Database}",
            $"abortConnect={(abortConnect ? "true" : "false")}",
            "connectTimeout=5000"
        };

        if (!string.IsNullOrEmpty(Password))
        {
            parts.Add($"password={Password}");
        }

        return string.Join(',', parts);
    }

    private static string Env(string key, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(key) ?? defaultValue;
    }

    private static int ParseInt(string value, int defaultValue)
    {
        return int.TryParse(value, out var result) ? result : defaultValue;
    }
}
