using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Scene.Gate.Service;

namespace Hotfix.Scene.Gate.Handler.Room;

/// <summary>
/// 接收客户端 client_frame（单向），校验绑定后转发到 Avatars。
/// 业务暂为日志骨架。
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

        var framesCount = message.frames?.Count ?? 0;
        Log.Debug(
            $"[Gate] 收到 client_frame: userId={userId}, frame={message.frame_number}, ops={framesCount}");

        ISessionService sessionService = session.Scene.GetComponent<SessionService>();
        sessionService.ForwardClientFrame(userId, message.frame_number, framesCount);
        await FTask.CompletedTask;
    }
}