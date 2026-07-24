using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Avatars.Service;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Avatars.Handler;

/// <summary>
/// Gate 通知 Avatar：WsSession 已清理，执行玩家下线清理编排。
/// </summary>
public sealed class AvatarCleanupNotifyHandler : Address<FScene, AvatarCleanupNotify>
{
    protected override async FTask Run(FScene scene, AvatarCleanupNotify message)
    {
        IAvatarsService avatarsService = scene.GetComponent<AvatarsService>();
        await avatarsService.CleanupPlayer(message.user_id, message.reason);
    }
}
