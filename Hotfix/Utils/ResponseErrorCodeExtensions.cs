using Entity.Common;
using Fantasy.Network.Interface;

namespace Hotfix.Utils;

/// <summary>
/// <see cref="IResponse.ErrorCode"/> 赋值的语义化封装。
/// 框架约定 ErrorCode 字段类型为 uint、0 为成功，
/// 这里把 enum→uint 的强转收敛到一处，调用点只表达意图。
/// </summary>
public static class ResponseErrorCodeExtensions
{
    /// <summary>给响应设置错误码。</summary>
    public static void SetError(this IResponse response, ErrorCode code)
        => response.ErrorCode = (uint)code;

    /// <summary>标记响应成功（ErrorCode = 0，框架约定的成功值）。</summary>
    public static void SetOk(this IResponse response)
        => response.ErrorCode = (uint)ErrorCode.Ok;
}