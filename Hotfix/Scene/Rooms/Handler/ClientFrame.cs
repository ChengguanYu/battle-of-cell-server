using Entity.Utils;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Rooms.Service;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Rooms.Handler;

/// <summary>
/// Rooms 接收客户端帧转发并写入帧窗口。
/// </summary>
public sealed class RoomsClientFrameNotifyHandler : Address<FScene, RoomsClientFrameNotify>
{
    protected override async FTask Run(FScene scene, RoomsClientFrameNotify message)
    {
        var roomsService = scene.GetComponent<RoomsService>();

        // 协议层所有权转移：摘 frames 后入窗深拷贝，再 Dispose 入站 ops。
        var frames = FrameMessageUtil.DetachFrames(message);
        await roomsService.OnClientFrame(message.userId, message.frame_number, frames);
        FrameMessageUtil.DisposeFrames(frames);
    }
}
