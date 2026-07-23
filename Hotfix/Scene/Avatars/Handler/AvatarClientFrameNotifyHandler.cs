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
        // FIXME: 手工转移 frames 所有权，待统一收口
        // 原因：Fantasy Address.Handle finally 会对入站 message.Dispose()，生成代码会级联
        // foreach Dispose frames。若不先摘引用，下游 Forward 仍持有同一批 frame 池对象 → UAF。
        // 当前写法：摘下 list 交给转发层；父消息换空 list，框架 Dispose 时不再碰真实 ops。
        // 后续：边界深拷贝 / 明确单所有者 API，去掉每跳手写交接。
        var frames = message.frames;
        message.frames = new List<frame>();
        avatarsService.ForwardClientFrame(message.userId, message.frame_number, frames);
        await FTask.CompletedTask;
    }
}
