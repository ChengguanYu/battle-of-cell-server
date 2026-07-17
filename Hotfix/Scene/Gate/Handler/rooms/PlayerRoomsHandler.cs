using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Gate.Service;
using Hotfix.Utils;

namespace Hotfix.Scene.Gate.Handler.Rooms;

/// <summary>
/// 客户端发起房间匹配请求。
/// 仅解析是否已 Bind（取 userId），鉴权在 EntryHome 完成；再内部 RPC 到 Rooms Scene。
/// </summary>
public sealed class PlayerRoomsHandler : MessageRPC<PlayerRoomsReq, PlayerRoomsResp>
{
    protected override async FTask Run(Session session, PlayerRoomsReq request, PlayerRoomsResp response, Action reply)
    {
        // 解析已绑定用户；未绑定 = 未进入，不是在此做 JWT 鉴权
        if (!SessionManager.Instance.TryGetUserIdBySession(session, out var userId))
        {
            ReplyNotBound(session, response, reply);
            return;
        }

        ISessionService sessionService = session.Scene.GetComponent<SessionService>();
        var result = await sessionService.PlayerRooms(userId);
        if (!result.IsSuccess)
        {
            Log.Warning($"用户 {userId} 房间匹配失败：{result.Reason}");
            ReplyFail(response, reply, StatusCode.MatchFailed, result.Reason);
            return;
        }

        ReplyOk(response, reply);
    }

    /// <summary>标记响应成功并回复。</summary>
    private static void ReplyOk(PlayerRoomsResp response, Action reply)
    {
        response.SetOk();
        reply();
    }

    /// <summary>写入状态码与错误文案后回复，不断开连接。</summary>
    private static void ReplyFail(PlayerRoomsResp response, Action reply, StatusCode code, string? reason = null)
    {
        response.SetStatus(code);
        var error = RespError.Create();
        error.message = string.IsNullOrEmpty(reason) ? code.ToMessage() : reason;
        response.AddError(error);
        reply();
    }

    /// <summary>未绑定在线态：回复并断开。</summary>
    private static void ReplyNotBound(Session session, PlayerRoomsResp response, Action reply)
    {
        ReplyFail(response, reply, StatusCode.NotAuthenticated);
        session.Dispose();
    }
}
