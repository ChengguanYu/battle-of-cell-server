using Entity.DTOs;
using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Gate.Service;
using Hotfix.Utils;

namespace Hotfix.Scene.Gate.Handler.Room;

/// <summary>
/// 客户端主动退出房间。
/// 仅解析是否已 Bind（取 userId），鉴权在 EntryHome 完成；再内部 RPC 到 Avatars Scene。
/// </summary>
public sealed class PlayerLeaveRoomHandler : MessageRPC<PlayerLeaveRoomReq, PlayerLeaveRoomResp>
{
    protected override async FTask Run(Session session, PlayerLeaveRoomReq request, PlayerLeaveRoomResp response, Action reply)
    {
        if (!SessionManager.Instance.TryGetUserId(session, out var userId))
        {
            ReplyNotBound(session, response, reply);
            return;
        }

        ISessionService sessionService = session.Scene.GetComponent<SessionService>();
        var result = await sessionService.PlayerLeaveRoom(userId);
        if (!result.IsSuccess)
        {
            Log.Warning($"用户 {userId} 退出房间失败：{result.Reason}");
            ReplyFail(response, reply, StatusCode.LeaveRoomFailed, result.Reason);
            return;
        }

        ReplyOk(response, reply, TryGetRoomId(result));
    }

    private static void ReplyOk(PlayerLeaveRoomResp response, Action reply, long roomId)
    {
        response.room_id = roomId;
        response.SetOk();
        reply();
    }

    private static void ReplyFail(PlayerLeaveRoomResp response, Action reply, StatusCode code, string? reason = null)
    {
        response.room_id = 0;
        response.SetStatus(code);
        var error = RespError.Create();
        error.message = string.IsNullOrEmpty(reason) ? code.ToMessage() : reason;
        response.AddError(error);
        reply();
    }

    private static void ReplyNotBound(Session session, PlayerLeaveRoomResp response, Action reply)
    {
        ReplyFail(response, reply, StatusCode.NotAuthenticated);
        session.Dispose();
    }

    private static long TryGetRoomId(Entity.DTOs.InnerResult result)
    {
        if (result.Args is { Count: > 0 } && result.Args[0] is long roomId)
        {
            return roomId;
        }

        return 0;
    }
}
