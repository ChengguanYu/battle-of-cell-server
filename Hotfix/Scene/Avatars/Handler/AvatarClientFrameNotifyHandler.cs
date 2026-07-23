using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Avatars.Service;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Avatars.Handler;

/// <summary>
/// Gate 转发的客户端帧；校验后继续转发到 Rooms。
/// 业务暂为日志骨架。
/// </summary>
public sealed class AvatarClientFrameNotifyHandler : Address<FScene, AvatarClientFrameNotify>
{
    protected override async FTask Run(FScene scene, AvatarClientFrameNotify message)
    {
        IAvatarsService avatarsService = scene.GetComponent<AvatarsService>();
        avatarsService.ForwardClientFrame(message.userId, message.frame_number, message.frames_count);
        await FTask.CompletedTask;
    }
}