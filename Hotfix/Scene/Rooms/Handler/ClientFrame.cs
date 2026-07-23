using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Rooms.Service;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Rooms.Handler;

/// <summary>
/// Rooms 接收客户端帧转发（业务暂为日志骨架）。
/// </summary>
public sealed class RoomsClientFrameNotifyHandler : Address<FScene, RoomsClientFrameNotify>
{
    protected override async FTask Run(FScene scene, RoomsClientFrameNotify message)
    {
        var roomsService = scene.GetComponent<RoomsService>();
        await roomsService.OnClientFrame(message.userId, message.frame_number, message.frames_count);
    }
}