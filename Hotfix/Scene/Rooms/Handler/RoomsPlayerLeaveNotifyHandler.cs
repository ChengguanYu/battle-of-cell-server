using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Rooms.Service;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Rooms.Handler;

/// <summary>
/// Rooms Scene 处理玩家离房检查（由 Avatar 会话清理等触发）。
/// 若是房间最后一名玩家，RoomManager 会关闭房间。
/// </summary>
public sealed class RoomsPlayerLeaveNotifyHandler : Address<FScene, RoomsPlayerLeaveNotify>
{
    protected override async FTask Run(FScene scene, RoomsPlayerLeaveNotify message)
    {
        IRoomsService roomsService = scene.GetComponent<RoomsService>();
        await roomsService.Leave(message.userId, message.reason);
    }
}
