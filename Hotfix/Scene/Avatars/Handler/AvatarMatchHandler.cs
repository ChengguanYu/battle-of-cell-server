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
/// Avatars Scene 接收 Gate 的匹配请求，纯转发到 Match Scene。
/// </summary>
public sealed class AvatarMatchHandler : AddressRPC<FScene, AvatarMatchReq, AvatarMatchResp>
{
    protected override async FTask Run(FScene scene, AvatarMatchReq req, AvatarMatchResp resp, Action reply)
    {
        IAvatarsService avatarsService = scene.GetComponent<AvatarsService>();
        var result = await avatarsService.Match(req.userId);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.userId} Avatar 匹配转发失败：{result.Reason}");
            resp.room_id = 0;
            resp.SetError(StatusCode.MatchFailed);
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
