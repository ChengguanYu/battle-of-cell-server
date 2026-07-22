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
/// Rooms Scene 处理进入房间请求（由 Match/Avatar 发起）。
/// </summary>
public sealed class RoomsEnterHandler : AddressRPC<FScene, RoomsEnterReq, RoomsEnterResp>
{
    protected override async FTask Run(FScene scene, RoomsEnterReq req, RoomsEnterResp resp, Action reply)
    {
        var roomsService = scene.GetComponent<RoomsService>();
        var result = await roomsService.Enter(req.userId);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.userId} 进入房间失败：{result.Reason}");
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
        if (result.Args is { Count: > 0 } && result.Args[0] is long roomId)
        {
            return roomId;
        }

        return 0;
    }
}
