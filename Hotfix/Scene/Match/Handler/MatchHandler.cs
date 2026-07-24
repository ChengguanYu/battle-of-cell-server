using Entity.DTOs;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Match.Service;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Match.Handler;

/// <summary>
/// Match Scene 处理匹配请求。
/// </summary>
public sealed class MatchHandler : AddressRPC<FScene, MatchReq, MatchResp>
{
    protected override async FTask Run(FScene scene, MatchReq req, MatchResp resp, Action reply)
    {
        IMatchService matchService = scene.GetComponent<MatchService>();
        var result = await matchService.Match(req.user_id);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.user_id} 匹配失败：{result.Reason}");
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
