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
/// Match -> Rooms：创建并进入（纯聚合 Create + Enter）。
/// </summary>
public sealed class RoomsCreateAndEntryHandler
    : AddressRPC<FScene, RoomsCreateAndEntryReq, RoomsCreateAndEntryResp>
{
    protected override async FTask Run(
        FScene scene,
        RoomsCreateAndEntryReq req,
        RoomsCreateAndEntryResp resp,
        Action reply)
    {
        var roomsService = scene.GetComponent<RoomsService>();
        var result = await roomsService.CreateAndEntry(req.user_id);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.user_id} CreateAndEntry 失败：{result.Reason}");
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
