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
        IRoomsService roomsService = scene.GetComponent<RoomsService>();
        var result = await roomsService.GetRoomListSnap();
        if (!result.IsSuccess)
        {
            Log.Warning($"GetRoomListSnap 失败：{result.Reason}");
            resp.rooms.Clear();
            resp.SetError(StatusCode.RoomsEnterFailed);
            reply();
            return;
        }

        // TODO: 将 result.Args 中的快照填入 resp.rooms
        resp.SetOk();
        reply();
    }
}
