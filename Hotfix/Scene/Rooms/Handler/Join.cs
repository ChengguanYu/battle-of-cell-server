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
/// Match -> Rooms：加入指定房间。
/// </summary>
public sealed class RoomsJoinHandler : AddressRPC<FScene, RoomsJoinReq, RoomsJoinResp>
{
    protected override async FTask Run(FScene scene, RoomsJoinReq req, RoomsJoinResp resp, Action reply)
    {
        var roomsService = scene.GetComponent<RoomsService>();
        if (req.room_id <= 0 || req.room_id > uint.MaxValue)
        {
            Log.Warning($"玩家 {req.userId} Join 房间 {req.room_id} 失败：room_id 非法");
            resp.room_id = 0;
            resp.SetError(StatusCode.RoomsEnterFailed);
            reply();
            return;
        }

        var result = await roomsService.Join(req.userId, (uint)req.room_id);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.userId} Join 房间 {req.room_id} 失败：{result.Reason}");
            resp.room_id = 0;
            resp.SetError(StatusCode.RoomsEnterFailed);
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
