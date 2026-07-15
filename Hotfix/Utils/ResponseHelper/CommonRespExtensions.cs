/***
 *  为 CommonResp 挂上工具方法
 */

using Entity.Generate.Helper;
using Fantasy;

namespace Hotfix.Utils;

/// <summary>
/// <see cref="ICommonResponse"/> 状态码/错误赋值的语义化封装。
/// 自动填充 <see cref="MetaData.status_code"/> 和 <see cref="MetaData.timestamp"/>。
/// </summary>
public static class CommonRespExtensions
{
    
    /// <summary>设置响应状态码并自动填充毫秒级时间戳。</summary>
    public static void SetStatus(this ICommonResponse commonResponse, StatusCode code)
    {
        commonResponse.meta ??= MetaData.Create();
        commonResponse.meta.status_code = (uint)code;
        commonResponse.meta.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
    
    /// <summary>便捷方法：标记响应成功（<see cref="StatusCode.Ok"/>）。</summary>
    public static void SetOk(this ICommonResponse commonResponse)
        => commonResponse.SetStatus(StatusCode.Ok);

    /// <summary>全量替换响应错误列表（自动归还旧列表中的池对象）。</summary>
    public static void SetError(this ICommonResponse commonResponse, List<RespError> errors)
    {
        foreach (var old in commonResponse.error)
            old.Dispose();
        commonResponse.error.Clear();
        commonResponse.error.AddRange(errors);
    }

    /// <summary>追加一条错误详情。</summary>
    public static void AddError(this ICommonResponse commonResponse, RespError error)
        => commonResponse.error.Add(error);
}
