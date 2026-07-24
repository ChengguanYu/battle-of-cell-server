using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Utils;

namespace Hotfix.Scene.Gate.Handler.Match;

/// <summary>
/// 客户端 Outer MatchReq：取 Session 绑定 userId，透传 match_type 到 Avatars。
/// 不调用 SessionService.PlayerMatch。
/// </summary>
public sealed class MatchHandler : MessageRPC<MatchReq, MatchResp>
{
    protected override async FTask Run(Session session, MatchReq request, MatchResp response, Action reply)
    {
        if (!SessionManager.Instance.TryGetUserId(session, out var userId))
        {
            ReplyNotBound(session, response, reply);
            return;
        }

        AvatarRelayClientMatchResp? avatarResp = null;
        var req = AvatarRelayClientMatchReq.Create();
        try
        {
            req.user_id = userId;
            req.match_type = request.match_type;
            var address = session.Scene.GetSceneAddress(SceneType.Avatars);
            avatarResp = (AvatarRelayClientMatchResp)await session.Scene.Call(address, req);
            if (!avatarResp.IsOk())
            {
                Log.Warning($"用户 {userId} AvatarRelayClientMatch 失败，status={avatarResp.ToMessage()}");
                ReplyFail(response, reply, StatusCode.MatchFailed, avatarResp.ToMessage());
                return;
            }

            ReplyOk(response, reply);
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Avatars Scene，用户 {userId} MatchReq 转发失败");
            ReplyFail(response, reply, StatusCode.MatchFailed, "未找到 Avatars Scene");
        }
        finally
        {
            req.Dispose();
            avatarResp?.Dispose();
        }
    }

    private static void ReplyOk(MatchResp response, Action reply)
    {
        response.SetOk();
        reply();
    }

    private static void ReplyFail(MatchResp response, Action reply, StatusCode code, string? reason = null)
    {
        response.SetStatus(code);
        var error = RespError.Create();
        error.message = string.IsNullOrEmpty(reason) ? code.ToMessage() : reason;
        response.AddError(error);
        reply();
    }

    private static void ReplyNotBound(Session session, MatchResp response, Action reply)
    {
        ReplyFail(response, reply, StatusCode.NotAuthenticated);
        session.Dispose();
    }
}
