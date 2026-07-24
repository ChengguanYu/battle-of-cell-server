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
/// Match 端 Outer MatchReq：匹配/建房后写 Redis，不入房。
/// </summary>
public sealed class ClientMatchHandler : AddressRPC<FScene, InnerClientMatchReq, InnerClientMatchResp>
{
    protected override async FTask Run(FScene scene, InnerClientMatchReq req, InnerClientMatchResp resp, Action reply)
    {
        IMatchService matchService = scene.GetComponent<MatchService>();
        var result = await matchService.ClientMatch(req.user_id, req.match_type);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.user_id} ClientMatch 失败：{result.Reason}");
            resp.SetError(StatusCode.MatchFailed);
            reply();
            return;
        }

        resp.SetOk();
        reply();
    }
}
