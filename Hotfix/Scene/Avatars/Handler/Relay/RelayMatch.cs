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
/// 匹配转发：门禁后转发到 Match。文件位于 Handler/Relay。
/// </summary>
public sealed class RelayMatch : AddressRPC<FScene, AvatarRelayMatchReq, AvatarRelayMatchResp>
{
    protected override async FTask Run(FScene scene, AvatarRelayMatchReq req, AvatarRelayMatchResp resp, Action reply)
    {
        IRelay relay = scene.GetComponent<Relay>();
        var result = await relay.Match(req.user_id);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.user_id} RelayMatch 失败：{result.Reason}");
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
