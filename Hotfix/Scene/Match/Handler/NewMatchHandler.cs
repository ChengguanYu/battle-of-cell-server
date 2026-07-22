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
/// Match Scene：新匹配请求（Avatar -> Match）。
/// </summary>
public sealed class NewMatchHandler : AddressRPC<FScene, NewMatchReq, NewMatchResp>
{
    protected override async FTask Run(FScene scene, NewMatchReq req, NewMatchResp resp, Action reply)
    {
        IMatchService matchService = scene.GetComponent<MatchService>();
        var result = await matchService.NewMatch(req.userId);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.userId} NewMatch 失败：{result.Reason}");
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
        if (result.Args is { Count: > 0 } && result.Args[0] is long roomId)
        {
            return roomId;
        }

        return 0;
    }
}
