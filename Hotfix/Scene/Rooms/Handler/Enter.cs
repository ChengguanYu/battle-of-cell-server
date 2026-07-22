using Entity.DTOs;
using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Rooms.Handler;

/// <summary>
/// Rooms Scene 处理进入房间请求（旧链路 MatchOrCreate，由 Match/Avatar 发起）。
/// </summary>
public sealed class RoomsEnterHandler : AddressRPC<FScene, RoomsEnterReq, RoomsEnterResp>
{
    protected override async FTask Run(FScene scene, RoomsEnterReq req, RoomsEnterResp resp, Action reply)
    {
        await FTask.CompletedTask;
        Log.Debug($"RoomsEnterHandler 开始: userId={req.userId}");

        if (req.userId <= 0)
        {
            Log.Warning($"玩家 {req.userId} 进入房间失败：userId 非法");
            resp.room_id = 0;
            resp.SetError(StatusCode.RoomsEnterFailed);
            reply();
            return;
        }

        // 旧协议仅带 userId，无 room_id；仍走 MatchOrCreate 兼容。
        var room = RoomManager.Instance.MatchOrCreate(req.userId);
        if (room == null)
        {
            Log.Warning($"玩家 {req.userId} 进入房间失败：无法创建或加入房间");
            resp.room_id = 0;
            resp.SetError(StatusCode.RoomsEnterFailed);
            reply();
            return;
        }

        Log.Debug($"RoomsEnterHandler 成功: userId={req.userId}, roomId={room.RoomId}");
        resp.room_id = room.RoomId;
        resp.SetOk();
        reply();
    }
}
