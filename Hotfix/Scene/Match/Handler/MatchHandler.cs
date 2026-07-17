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
        var result = await matchService.Match(req.userId);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.userId} 匹配失败：{result.Reason}");
            resp.SetError(StatusCode.MatchFailed);
            reply();
            return;
        }

        resp.SetOk();
        reply();
    }
}
