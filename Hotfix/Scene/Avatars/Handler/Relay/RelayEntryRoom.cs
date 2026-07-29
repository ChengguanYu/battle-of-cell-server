using Fantasy;
using Fantasy.Async;
using Fantasy.Network.Interface;
using Hotfix.Scene.Avatars.Service;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Avatars.Handler;

/// <summary>
/// Gate -> Avatar -> Rooms 进房转发：Avatar 层只做门禁 + 字段映射透传。
/// </summary>
public sealed class RelayEntryRoom : AddressRPC<FScene, AvatarRelayEntryRoomReq, AvatarRelayEntryRoomResp>
{
    protected override async FTask Run(FScene scene, AvatarRelayEntryRoomReq req, AvatarRelayEntryRoomResp resp, Action reply)
    {
        IRelay relay = scene.GetComponent<Relay>();
        var roomsResp = await relay.EntryRoom(req.user_id);
        if (roomsResp == null)
        {
            Log.Warning($"玩家 {req.user_id} RelayEntryRoom 失败");
            resp.room_id = 0;
            resp.SetError(StatusCode.RoomsEnterFailed);
            reply();
            return;
        }

        resp.room_id = roomsResp.room_id;
        resp.world = roomsResp.world;
        resp.position = roomsResp.position;
        resp.SetOk();
        reply();
    }
}
