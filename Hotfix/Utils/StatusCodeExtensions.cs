namespace Hotfix.Utils;

/// <summary>
/// <see cref="StatusCode"/> 到可读消息的翻译。
/// 集中维护枚举值与文案的映射，供日志、响应回包等场景使用。
/// 新增状态码时需同步在此补充对应消息；未登记的码走默认兜底，不抛异常。
/// </summary>
public static class StatusCodeExtensions
{
    /// <summary>返回状态码对应的中文消息，未登记的码返回兜底文案。</summary>
    public static string ToMessage(this StatusCode code) => code switch
    {
        StatusCode.Ok => "成功",
        StatusCode.TokenInvalid => "Token无效",
        StatusCode.SessionEntryFailed => "会话进入失败",
        StatusCode.PlayerNotFound => "玩家不存在",
        StatusCode.LoadPlayerFailed => "加载玩家失败",
        _ => $"未知状态码({(uint)code})",
    };
}
