using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Avatars.Service;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Avatars.Handler;

/// <summary>
/// Avatars Scene 接收 Gate 的匹配请求，再转发到 Match Scene。
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
            resp.SetError(StatusCode.MatchFailed);
            reply();
            return;
        }

        resp.SetOk();
        reply();
    }
}
