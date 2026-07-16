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
/// 仅解析是否已 Bind（取 userId），鉴权在 EntryHome 完成；再内部 RPC 到 Avatars。
/// </summary>
public sealed class PlayerMatchHandler : MessageRPC<PlayerMatchReq, PlayerMatchResp>
{
    protected override async FTask Run(Session session, PlayerMatchReq request, PlayerMatchResp response, Action reply)
    {
        // 解析已绑定用户；未绑定 = 未进入，不是在此做 JWT 鉴权
        if (!SessionManager.Instance.TryGetUserIdBySession(session, out var userId))
        {
            ReplyNotBound(session, response, reply);
            return;
        }

        ISessionService sessionService = session.Scene.GetComponent<SessionService>();
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

    /// <summary>未绑定在线态：回复并断开。</summary>
    private static void ReplyNotBound(Session session, PlayerMatchResp response, Action reply)
    {
        response.SetStatus(StatusCode.NotAuthenticated);
        var error = RespError.Create();
        error.message = StatusCode.NotAuthenticated.ToMessage();
        response.AddError(error);
        reply();
        session.Dispose();
    }
}
