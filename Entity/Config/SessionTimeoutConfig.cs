namespace Entity.Config;

/// <summary>
/// 会话超时相关配置，从环境变量注入。
/// </summary>
public static class SessionTimeoutConfig
{
    public const int DefaultTimedOutCloseSeconds = 30;

    /// <summary>
    /// TimedOut 后自动 Closed 的延迟（毫秒）。
    /// 环境变量：WS_SESSION_TIMED_OUT_CLOSE_SECONDS（秒，默认 30）。
    /// </summary>
    public static int TimedOutCloseDelayMs { get; } = LoadTimedOutCloseDelayMs();

    private static int LoadTimedOutCloseDelayMs()
    {
        var raw = Environment.GetEnvironmentVariable("WS_SESSION_TIMED_OUT_CLOSE_SECONDS");
        if (!int.TryParse(raw, out var seconds) || seconds <= 0)
        {
            seconds = DefaultTimedOutCloseSeconds;
        }

        return checked(seconds * 1000);
    }
}
