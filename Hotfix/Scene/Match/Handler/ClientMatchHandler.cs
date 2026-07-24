using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Match.Handler;

/// <summary>
/// Match 端 Outer MatchReq 空骨架：只回成功，不调用 MatchService，不入房。
/// </summary>
public sealed class ClientMatchHandler : AddressRPC<FScene, InnerClientMatchReq, InnerClientMatchResp>
{
    protected override async FTask Run(FScene scene, InnerClientMatchReq req, InnerClientMatchResp resp, Action reply)
    {
        Log.Debug($"[Match] InnerClientMatch 空转发到达: userId={req.user_id}");
        resp.SetOk();
        reply();
        await FTask.CompletedTask;
    }
}