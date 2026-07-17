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
        Log.Info($"[MatchFlow] Avatar->Match 收到匹配请求 userId={req.userId}");

        IMatchService matchService = scene.GetComponent<MatchService>();
        var result = await matchService.Match(req.userId);
        if (!result.IsSuccess)
        {
            Log.Warning($"[MatchFlow] Match 处理失败 userId={req.userId} reason={result.Reason}");
            resp.SetError(StatusCode.MatchFailed);
            reply();
            return;
        }

        Log.Info($"[MatchFlow] Match 处理完成 userId={req.userId}");
        resp.SetOk();
        reply();
    }
}
