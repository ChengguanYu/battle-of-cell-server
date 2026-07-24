using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Avatars.Handler;

/// <summary>
/// Gate -> Avatars 客户端匹配转发：透传 user_id / match_type 到 Match。
/// 文件位于 Handler/Relay。不调用 AvatarsService / Relay Service。
/// </summary>
public sealed class RelayClientMatch : AddressRPC<FScene, AvatarRelayClientMatchReq, AvatarRelayClientMatchResp>
{
    protected override async FTask Run(FScene scene, AvatarRelayClientMatchReq req, AvatarRelayClientMatchResp resp, Action reply)
    {
        InnerClientMatchResp? matchResp = null;
        var matchReq = InnerClientMatchReq.Create();
        try
        {
            matchReq.user_id = req.user_id;
            matchReq.match_type = req.match_type;
            var address = scene.GetSceneAddress(SceneType.Match);
            matchResp = (InnerClientMatchResp)await scene.Call(address, matchReq);
            if (!matchResp.IsOk())
            {
                Log.Warning($"玩家 {req.user_id} InnerClientMatch 失败，status={matchResp.ToMessage()}");
                resp.SetError(StatusCode.MatchFailed);
                reply();
                return;
            }

            resp.SetOk();
            reply();
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Match Scene，玩家 {req.user_id} AvatarRelayClientMatch 转发失败");
            resp.SetError(StatusCode.MatchFailed);
            reply();
        }
        finally
        {
            matchReq.Dispose();
            matchResp?.Dispose();
        }
    }
}
