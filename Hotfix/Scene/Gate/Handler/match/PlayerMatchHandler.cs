using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Gate.Service;
using Hotfix.Utils;

namespace Hotfix.Scene.Gate.Handler.Match;

/// <summary>
/// 客户端发起匹配请求。
/// 鉴权后通过内部 RPC 转发到 Avatars Scene 处理匹配逻辑。
/// </summary>
public sealed class PlayerMatchHandler : MessageRPC<PlayerMatchReq, PlayerMatchResp>
{
    protected override async FTask Run(Session session, PlayerMatchReq request, PlayerMatchResp response, Action reply)
    {
        // 从 SessionManager 反查鉴权时绑定的 userId
        if (!SessionManager.Instance.TryGetUserIdBySession(session, out var userId))
        {
            ReplyAuthError(session, response, reply);
            return;
        }

        var sessionService = session.Scene.GetComponent<SessionService>();
        var result = await sessionService.PlayerMatch(userId);
        if (!result.IsSuccess)
        {
            Log.Warning($"用户 {userId} 匹配失败：{result.Reason}");
            response.SetStatus(StatusCode.MatchFailed);
            var error = RespError.Create();
            error.message = result.Reason;
            response.AddError(error);
            reply();
            return;
        }

        response.SetOk();
        reply();
    }

    /// <summary>鉴权失败：设置错误状态、回复并断开连接。</summary>
    private static void ReplyAuthError(Session session, PlayerMatchResp response, Action reply)
    {
        response.SetStatus(StatusCode.NotAuthenticated);
        var error = RespError.Create();
        error.message = StatusCode.NotAuthenticated.ToMessage();
        response.AddError(error);
        reply();
        session.Dispose();
    }
}
