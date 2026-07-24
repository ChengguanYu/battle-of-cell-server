using Entity.Utils;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Avatars.Service;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Avatars.Handler;

/// <summary>
/// Gate 转发的客户端帧；校验后继续转发到 Rooms。
/// </summary>
public sealed class AvatarClientFrameNotifyHandler : Address<FScene, AvatarClientFrameNotify>
{
    protected override async FTask Run(FScene scene, AvatarClientFrameNotify message)
    {
        IAvatarsService avatarsService = scene.GetComponent<AvatarsService>();

        // 协议层所有权转移：摘 frames，父消息挂空 list，避免 Handler finally 级联 Dispose 造成 UAF。
        var frames = FrameMessageUtil.DetachFrames(message);
        avatarsService.ForwardClientFrame(message.user_id, message.frame_number, frames);
        await FTask.CompletedTask;
    }
}
