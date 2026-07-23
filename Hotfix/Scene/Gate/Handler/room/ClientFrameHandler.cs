using Entity.Managers;
using Entity.Utils;
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

        // 协议层所有权转移：摘 frames，父消息挂空 list，避免 Handler finally 级联 Dispose 造成 UAF。
        var frames = FrameMessageUtil.DetachFrames(message);

        ISessionService sessionService = session.Scene.GetComponent<SessionService>();
        sessionService.ForwardClientFrame(userId, message.frame_number, frames);
        await FTask.CompletedTask;
    }
}
