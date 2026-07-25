using Entity.DTOs;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Avatars.Service;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Avatars.Handler;

/// <summary>
/// 客户端进房转发：门禁后转发到 Rooms。文件位于 Handler/Relay。
/// </summary>
public sealed class RelayEntryRoom : AddressRPC<FScene, AvatarRelayEntryRoomReq, AvatarRelayEntryRoomResp>
{
    protected override async FTask Run(FScene scene, AvatarRelayEntryRoomReq req, AvatarRelayEntryRoomResp resp, Action reply)
    {
        IRelay relay = scene.GetComponent<Relay>();
        var result = await relay.EntryRoom(req.user_id);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.user_id} RelayEntryRoom 失败：{result.Reason}");
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
