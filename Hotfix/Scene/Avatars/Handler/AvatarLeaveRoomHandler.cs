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
/// Avatars Scene 接收 Gate 的主动退出房间请求。
/// </summary>
public sealed class AvatarLeaveRoomHandler : AddressRPC<FScene, AvatarLeaveRoomReq, AvatarLeaveRoomResp>
{
    protected override async FTask Run(FScene scene, AvatarLeaveRoomReq req, AvatarLeaveRoomResp resp, Action reply)
    {
        IAvatarsService avatarsService = scene.GetComponent<AvatarsService>();
        var result = await avatarsService.LeaveRoom(req.user_id);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.user_id} Avatar 退出房间失败：{result.Reason}");
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
