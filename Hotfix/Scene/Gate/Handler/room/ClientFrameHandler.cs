using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Gate.Service;

namespace Hotfix.Scene.Gate.Handler.Room;

/// <summary>
/// 接收客户端 client_frame（单向），校验绑定后转发到 Avatars。
/// </summary>
public sealed class ClientFrameHandler : Message<client_frame>
{
    protected override async FTask Run(Session session, client_frame message)
    {
        if (!SessionManager.Instance.TryGetUserId(session, out var userId))
        {
            Log.Warning("client_frame 丢弃：Session 未绑定");
            return;
        }

        // FIXME: 手工转移 frames 所有权，待统一收口
        // 原因：Fantasy Message.Handle finally 会对入站 message.Dispose()，生成代码会级联
        // foreach Dispose frames。若不先摘引用，下游 Forward 仍持有同一批 frame 池对象 → UAF。
        // 当前写法：摘下 list 交给转发层；父消息换空 list，框架 Dispose 时不再碰真实 ops。
        // 后续：边界深拷贝 / 明确单所有者 API，去掉每跳手写交接。
        var frames = message.frames;
        message.frames = new List<frame>();

        ISessionService sessionService = session.Scene.GetComponent<SessionService>();
        sessionService.ForwardClientFrame(userId, message.frame_number, frames);
        await FTask.CompletedTask;
    }
}
