using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Utils;

namespace Hotfix.Scene.Gate.Handler.Room;

/// <summary>
/// 客户端 Outer EntryRoomReq：取 Session 绑定 userId，经 Avatar 门禁透传到 Rooms。
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

        RoomsEntryRoomResp? roomsResp = null;
        var req = RoomsEntryRoomReq.Create();
        try
        {
            req.user_id = userId;
            var address = session.Scene.GetSceneAddress(SceneType.Avatars);
            roomsResp = (RoomsEntryRoomResp)await session.Scene.Call(address, req);
            if (!roomsResp.IsOk())
            {
                Log.Warning($"用户 {userId} EntryRoom 失败，status={roomsResp.ToMessage()}");
                ReplyFail(response, reply, StatusCode.RoomsEnterFailed, roomsResp.ToMessage());
                return;
            }

            response.room_id = roomsResp.room_id;
            response.world = roomsResp.world;
            response.SetOk();
            reply();
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Avatars Scene，用户 {userId} EntryRoomReq 失败");
            ReplyFail(response, reply, StatusCode.RoomsEnterFailed, "未找到 Avatars Scene");
        }
        finally
        {
            req.Dispose();
            roomsResp?.Dispose();
        }
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
