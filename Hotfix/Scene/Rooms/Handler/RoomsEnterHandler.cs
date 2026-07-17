using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Rooms.Service;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Rooms.Handler;

/// <summary>
/// Rooms Scene 处理进入房间请求。
/// </summary>
public sealed class RoomsEnterHandler : AddressRPC<FScene, RoomsEnterReq, RoomsEnterResp>
{
    protected override async FTask Run(FScene scene, RoomsEnterReq req, RoomsEnterResp resp, Action reply)
    {
        Log.Info($"玩家 {req.userId} 发起进入房间请求");

        IRoomsService roomsService = scene.GetComponent<RoomsService>();
        var result = await roomsService.Enter(req.userId);
        if (!result.IsSuccess)
        {
            Log.Warning($"玩家 {req.userId} 进入房间失败：{result.Reason}");
            resp.SetError(StatusCode.RoomsEnterFailed);
            reply();
            return;
        }

        resp.SetOk();
        reply();
    }
}
