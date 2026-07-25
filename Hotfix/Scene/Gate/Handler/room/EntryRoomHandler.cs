using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Utils;

namespace Hotfix.Scene.Gate.Handler.Room;

/// <summary>
/// 客户端 Outer EntryRoomReq：取 Session 绑定 userId，转发到 Avatars 进房。
/// </summary>
public sealed class EntryRoomHandler : MessageRPC<EntryRoomReq, EntryRoomResp>
{
    protected override async FTask Run(Session session, EntryRoomReq request, EntryRoomResp response, Action reply)
    {
        if (!SessionManager.Instance.TryGetUserId(session, out var userId))
        {
            ReplyNotBound(session, response, reply);
            return;
        }

        AvatarRelayEntryRoomResp? avatarResp = null;
        var req = AvatarRelayEntryRoomReq.Create();
        try
        {
            req.user_id = userId;
            var address = session.Scene.GetSceneAddress(SceneType.Avatars);
            avatarResp = (AvatarRelayEntryRoomResp)await session.Scene.Call(address, req);
            if (!avatarResp.IsOk())
            {
                Log.Warning($"用户 {userId} AvatarRelayEntryRoom 失败，status={avatarResp.ToMessage()}");
                ReplyFail(response, reply, StatusCode.RoomsEnterFailed, avatarResp.ToMessage());
                return;
            }

            ReplyOk(response, reply, avatarResp.room_id);
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Avatars Scene，用户 {userId} EntryRoomReq 转发失败");
            ReplyFail(response, reply, StatusCode.RoomsEnterFailed, "未找到 Avatars Scene");
        }
        finally
        {
            req.Dispose();
            avatarResp?.Dispose();
        }
    }

    private static void ReplyOk(EntryRoomResp response, Action reply, long roomId)
    {
        response.room_id = roomId;
        response.SetOk();
        reply();
    }

    private static void ReplyFail(EntryRoomResp response, Action reply, StatusCode code, string? reason = null)
    {
        response.room_id = 0;
        response.SetStatus(code);
        var error = RespError.Create();
        error.message = string.IsNullOrEmpty(reason) ? code.ToMessage() : reason;
        response.AddError(error);
        reply();
    }

    private static void ReplyNotBound(Session session, EntryRoomResp response, Action reply)
    {
        ReplyFail(response, reply, StatusCode.NotAuthenticated);
        session.Dispose();
    }
}
