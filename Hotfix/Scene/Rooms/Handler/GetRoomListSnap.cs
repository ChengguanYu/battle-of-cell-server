using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Rooms.Service;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Rooms.Handler;

/// <summary>
/// Match -> Rooms：拉取房间列表快照（只读线索）。
/// </summary>
public sealed class RoomsGetRoomListSnapHandler
    : AddressRPC<FScene, RoomsGetRoomListSnapReq, RoomsGetRoomListSnapResp>
{
    protected override async FTask Run(
        FScene scene,
        RoomsGetRoomListSnapReq req,
        RoomsGetRoomListSnapResp resp,
        Action reply)
    {
        var roomsService = scene.GetComponent<RoomsService>();
        var snaps = await roomsService.GetRoomListSnap();

        resp.rooms.Clear();
        if (snaps is { Count: > 0 })
        {
            resp.rooms.AddRange(snaps);
            resp.IsEmpty = false;
        }
        else
        {
            resp.IsEmpty = true;
        }

        resp.SetOk();
        reply();
    }
}
