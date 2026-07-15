using Fantasy.Network.Interface;

namespace Hotfix.Utils;

/// <summary>
/// <see cref="IResponse.ErrorCode"/> 赋值的语义化封装。
/// 仅适用于 Inner RPC（<see cref="IAddressResponse"/>），
/// Outer 响应不应操作 ErrorCode。
/// </summary>
public static class ResponseErrorCodeExtensions
{
    /// <summary>给响应设置错误码。(仅Inner有效)</summary>
    public static void SetError(this IAddressResponse response, StatusCode code)
        => response.ErrorCode = (uint)code;

    /// <summary>标记响应成功（ErrorCode = 0，框架约定的成功值）(仅Inner有效)。</summary>
    public static void SetOk(this IAddressResponse response)
        => response.ErrorCode = (uint)StatusCode.Ok;

    /// <summary>获取响应错误码对应的消息文案。(仅Inner有效)</summary>
    public static string ToMessage(this IAddressResponse response)
        => response.ErrorCode.ToMessage();
}
