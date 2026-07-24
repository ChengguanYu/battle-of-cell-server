using Entity.DTOs;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Rooms.Service;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Rooms.Handler;

/// <summary>
/// Avatar -> Rooms：主动离房（在线退出，等待结果）。
/// 若是房间最后一名玩家，RoomManager 会关闭房间。
/// </summary>
public sealed class RoomsLeaveHandler : AddressRPC<FScene, RoomsLeaveReq, RoomsLeaveResp>
{
    protected override async FTask Run(FScene scene, RoomsLeaveReq req, RoomsLeaveResp resp, Action reply)
    {
        var roomsService = scene.GetComponent<RoomsService>();
        var result = await roomsService.Leave(req.user_id, req.reason);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.user_id} RoomsLeave 失败：{result.Reason}");
            resp.room_id = 0;
            resp.SetError(StatusCode.LeaveRoomFailed);
            reply();
            return;
        }

        resp.room_id = TryGetRoomId(result);
        resp.SetOk();
        reply();
    }

    private static long TryGetRoomId(InnerResult result)
    {
        if (result.Args is { Count: > 0 } && result.Args[0] is uint roomId)
        {
            return roomId;
        }

        return 0;
    }
}
