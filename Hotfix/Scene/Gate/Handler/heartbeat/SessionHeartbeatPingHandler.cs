using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Utils;

namespace Hotfix.Scene.Gate.Handler.Heartbeat;

/// <summary>
/// 刷新已绑定玩家的在线心跳，并返回服务器时间。
/// </summary>
public sealed class SessionHeartbeatPingHandler : MessageRPC<SessionHeartbeatPing, SessionHeartbeatPong>
{
    protected override async FTask Run(Session session, SessionHeartbeatPing request, SessionHeartbeatPong response, Action reply)
    {
        if (!SessionManager.Instance.TryGetUserIdBySession(session, out var userId) ||
            !SessionManager.Instance.TryGetByUserId(userId, out var wsSession) ||
            wsSession == null ||
            !wsSession.UpdateHeartbeat())
        {
            response.ErrorCode = (uint)StatusCode.NotAuthenticated;
            reply();
            session.Dispose();
            return;
        }

        response.timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        reply();
        await FTask.CompletedTask;
    }
}
