namespace Hotfix.Utils;

/// <summary>
/// <see cref="StatusCode"/> 到可读消息的翻译。
/// 集中维护枚举值与文案的映射，供日志、响应回包等场景使用。
/// 新增状态码时需同步在此补充对应消息；未登记的码走默认兜底，不抛异常。
/// </summary>
public static class StatusCodeExtensions
{
    /// <summary>返回状态码对应的中文消息，未登记的码返回兜底文案。</summary>
    public static string ToMessage(this StatusCode code) => ToMessage((uint)code);

    /// <summary>
    /// 按原始状态码(uint)查询消息。协议线上 status 字段为 uint32，
    /// 调用点无需强转即可翻译，如 <c>resp.status.ToMessage()</c>。
    /// </summary>
    public static string ToMessage(this uint code) => code switch
    {
        (uint)StatusCode.Ok => "成功",
        (uint)StatusCode.TokenInvalid => "Token无效",
        (uint)StatusCode.SessionEntryFailed => "会话进入失败",
        (uint)StatusCode.NotAuthenticated => "未鉴权",
        (uint)StatusCode.PlayerNotFound => "玩家不存在",
        (uint)StatusCode.LoadPlayerFailed => "加载玩家失败",
        (uint)StatusCode.MatchFailed => "匹配失败",
        (uint)StatusCode.RoomsEnterFailed => "进入房间失败",
        _ => $"未知状态码({code})",
    };
}
