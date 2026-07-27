using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Avatars.Service;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Avatars.Handler;

/// <summary>
/// 客户端帧转发：门禁后转发到 Rooms。文件位于 Handler/Relay。
/// </summary>
public sealed class RelayClientFrame : Address<FScene, AvatarRelayClientFrameNotify>
{
    protected override async FTask Run(FScene scene, AvatarRelayClientFrameNotify message)
    {
        IRelay relay = scene.GetComponent<Relay>();
        var frames = FrameMessageUtil.DetachFrames(message);
        relay.ClientFrame(message.user_id, message.frame_number, frames);
        await FTask.CompletedTask;
    }
}
