using Fantasy;
using Fantasy.Async;
using Fantasy.Network.Interface;
using Hotfix.Scene.Rooms.Service;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Rooms.Handler;

/// <summary>
/// Avatar -> Rooms：客户端进房（读 Redis 匹配结果后 Entry）。
/// </summary>
public sealed class RoomsEntryRoomHandler : AddressRPC<FScene, RoomsEntryRoomReq, RoomsEntryRoomResp>
{
    protected override async FTask Run(FScene scene, RoomsEntryRoomReq req, RoomsEntryRoomResp resp, Action reply)
    {
        var roomsService = scene.GetComponent<RoomsService>();
        var result = await roomsService.EntryRoom(req.user_id, resp);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.user_id} EntryRoom 失败：{result.Reason}");
            resp.room_id = 0;
            resp.SetError(StatusCode.RoomsEnterFailed);
            reply();
            return;
        }

        resp.SetOk();
        reply();
    }
}
