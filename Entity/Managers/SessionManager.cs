using System.Collections.Concurrent;
using Entity.VOs.session;
using Fantasy.Network;

namespace Entity.Managers;

/// <summary>
/// 进程级在线会话缓存。
/// 定义在 Entity 程序集，不随 Hotfix 热更卸载；仅进程退出时释放。
/// 索引：userId 与框架 Session（以 Session.Id 为键）双向关联；WsSession 经状态机迁移。
/// 重连策略未完整实现；同 user 再 Bind 时走 Online-&gt;Kicked-&gt;Closed 顶号路径。
/// </summary>
public sealed class SessionManager
{
    private static readonly SessionManager _instance = new();
    public static SessionManager Instance => _instance;

    private readonly ConcurrentDictionary<long, WsSession> _wsByUserId = new();
    private readonly ConcurrentDictionary<long, Session> _sessionByUserId = new();
    private readonly ConcurrentDictionary<long, long> _userIdBySessionId = new();

    private SessionManager()
    {
    }

    /// <summary>
    /// 鉴权成功后绑定：TransitNewToOnline，并建立 userId 与 Session 双向索引。
    /// 同 user 已有在线态时：Online-&gt;Kicked-&gt;Closed 后换新（旧 Session 不在此 Dispose）。
    /// </summary>
    public void Bind(long userId, Session session)
    {
        // TODO: 完整重连策略（同连接复用等）后续再做；当前仅顶号摘旧
        if (_wsByUserId.TryRemove(userId, out var oldWs))
        {
            CloseWs(oldWs, kickIfOnline: true, kickReason: "replaced_by_new_bind");
        }

        if (_sessionByUserId.TryRemove(userId, out var oldSession))
        {
            _userIdBySessionId.TryRemove(oldSession.Id, out _);
        }

        var ws = new WsSession();
        if (!ws.TransitNewToOnline(userId, session))
        {
            return;
        }

        _wsByUserId[userId] = ws;
        _sessionByUserId[userId] = session;
        _userIdBySessionId[session.Id] = userId;
    }

    /// <summary>
    /// 经框架 Session 解析已绑定的 userId。未绑定返回 false。
    /// Handler 用此做“是否已进入”，不做鉴权。
    /// </summary>
    public bool TryGetUserIdBySession(Session session, out long userId)
    {
        return _userIdBySessionId.TryGetValue(session.Id, out userId);
    }

    /// <summary>经 userId 取框架 Session。</summary>
    public bool TryGetSessionByUserId(long userId, out Session? session)
    {
        return _sessionByUserId.TryGetValue(userId, out session);
    }

    /// <summary>经 userId 取在线态。</summary>
    public bool TryGetByUserId(long userId, out WsSession? wsSession)
    {
        return _wsByUserId.TryGetValue(userId, out wsSession);
    }

    /// <summary>解绑并移除索引。</summary>
    public bool RemoveByUserId(long userId)
    {
        if (!_wsByUserId.TryRemove(userId, out var ws))
        {
            return false;
        }

        CloseWs(ws, kickIfOnline: false);
        if (_sessionByUserId.TryRemove(userId, out var session))
        {
            _userIdBySessionId.TryRemove(session.Id, out _);
        }

        return true;
    }

    /// <summary>经框架 Session 解绑。</summary>
    public bool RemoveBySession(Session session)
    {
        if (!_userIdBySessionId.TryRemove(session.Id, out var userId))
        {
            return false;
        }

        _sessionByUserId.TryRemove(userId, out _);
        if (_wsByUserId.TryRemove(userId, out var ws))
        {
            CloseWs(ws, kickIfOnline: false);
        }

        return true;
    }

    public bool ContainsUserId(long userId)
    {
        return _wsByUserId.TryGetValue(userId, out var ws) && ws.IsOnline;
    }

    public int Count => _wsByUserId.Count;

    /// <summary>
    /// 将 WsSession 收敛到 Closed。
    /// kickIfOnline 为 true 时走 Online-&gt;Kicked-&gt;Closed；否则 Online 直接 Online-&gt;Closed。
    /// </summary>
    private static void CloseWs(WsSession ws, bool kickIfOnline, string? kickReason = null)
    {
        switch (ws.State)
        {
            case WsSessionState.Online when kickIfOnline:
                ws.TransitOnlineToKicked(kickReason);
                ws.TransitKickedToClosed();
                break;
            case WsSessionState.Online:
                ws.TransitOnlineToClosed();
                break;
            case WsSessionState.Kicked:
                ws.TransitKickedToClosed();
                break;
            case WsSessionState.Closed:
            case WsSessionState.New:
            case WsSessionState.TimedOut:
                break;
        }
    }
}
