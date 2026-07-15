namespace Entity.DTOs;

/// <summary>
/// 内部调用结果状态（进程内 Domain / Service 之间传递，非网络协议）。
/// </summary>
public enum InnerResultState
{
    Success = 0,
    Failed = 1,
}

/// <summary>
/// 内部消息传递结果。
/// </summary>
public sealed class InnerResult
{
    public InnerResultState State { get; set; }

    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 附加参数；无数据时为 null。
    /// </summary>
    public List<object>? Args { get; set; }

    public bool IsSuccess => State == InnerResultState.Success;

    public static InnerResult Ok(string reason = "", params object[] args)
    {
        return new InnerResult
        {
            State = InnerResultState.Success,
            Reason = reason ?? string.Empty,
            Args = args is { Length: > 0 } ? new List<object>(args) : null,
        };
    }

    public static InnerResult Fail(string reason, params object[] args)
    {
        return new InnerResult
        {
            State = InnerResultState.Failed,
            Reason = reason ?? string.Empty,
            Args = args is { Length: > 0 } ? new List<object>(args) : null,
        };
    }
}
