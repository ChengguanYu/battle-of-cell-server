using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Rooms.Service;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Rooms.Handler;

/// <summary>
/// Rooms Scene 处理房间匹配请求。
/// </summary>
public sealed class RoomsMatchHandler : AddressRPC<FScene, RoomsMatchReq, RoomsMatchResp>
{
    protected override async FTask Run(FScene scene, RoomsMatchReq req, RoomsMatchResp resp, Action reply)
    {
        Log.Info($"玩家 {req.userId} 发起房间匹配请求");

        IRoomsService roomsService = scene.GetComponent<RoomsService>();
        var result = await roomsService.Match(req.userId);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.userId} 房间匹配失败：{result.Reason}");
            resp.SetError(StatusCode.MatchFailed);
            reply();
            return;
        }

        resp.SetOk();
        reply();
    }
}
