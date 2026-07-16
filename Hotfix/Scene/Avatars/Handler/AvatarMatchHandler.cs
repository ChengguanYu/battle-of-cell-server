using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Avatars.Handler;

/// <summary>
/// Avatars Scene 处理匹配请求。
/// 匹配逻辑待实现，当前仅打印日志。
/// </summary>
public sealed class AvatarMatchHandler : AddressRPC<FScene, AvatarMatchReq, AvatarMatchResp>
{
    protected override async FTask Run(FScene scene, AvatarMatchReq req, AvatarMatchResp resp, Action reply)
    {
        Log.Info($"玩家 {req.userId} 发起匹配请求");

        // TODO: 匹配逻辑待实现

        resp.SetOk();
        reply();
    }
}
