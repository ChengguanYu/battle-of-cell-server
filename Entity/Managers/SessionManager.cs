using System.Collections.Concurrent;
using Entity.VOs.session;
using Fantasy.Network;

namespace Entity.Managers;

/// <summary>
/// 进程级 WebSocket Session 缓存。
/// 定义在 Entity 程序集，不随 Hotfix 热更卸载；仅进程退出时释放。
/// </summary>
public sealed class SessionManager
{
    private static readonly SessionManager _instance = new();
    public static SessionManager Instance => _instance;

    private readonly ConcurrentDictionary<uint, WsSession> _sessions = new();
    private readonly ConcurrentDictionary<uint, uint> _userIdToSessionId = new();
    private readonly ConcurrentDictionary<uint, uint> _sessionIdToUserId = new();

    private SessionManager()
    {
    }

    /// <summary>
    /// 添加或覆盖 Session（同 userId 重连时覆盖旧连接）。
    /// </summary>
    public void Add(WsSession session)
    {
        _sessions[session.GetId] = session;
    }

    /// <summary>
    /// 仅当不存在时添加；已存在则返回 false。
    /// </summary>
    public bool TryAdd(WsSession session)
    {
        return _sessions.TryAdd(session.GetId, session);
    }

    /// <summary>
    /// 绑定 userId 与 sessionId。当前两者相等，预留未来由网关独立分配 sessionId。
    /// 仅在 PlayerEntry 成功后调用。
    /// </summary>
    public void Bind(uint userId, uint sessionId)
    {
        _userIdToSessionId[userId] = sessionId;
        _sessionIdToUserId[sessionId] = userId;
    }

    public bool TryGet(uint sessionId, out WsSession? session)
    {
        return _sessions.TryGetValue(sessionId, out session);
    }

    /// <summary>
    /// 经 Fantasy Session 反查 userId（线性扫描匹配 WsSession.GetSession）。
    /// </summary>
    public bool TryGetUserIdBySession(Session session, out long userId)
    {
        userId = 0;
        foreach (var ws in _sessions.Values)
        {
            if (ws.GetSession == session)
            {
                userId = ws.GetId;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 经 userId -> sessionId 映射查找 Session。
    /// </summary>
    public bool TryGetByUserId(uint userId, out WsSession? session)
    {
        session = null;
        return _userIdToSessionId.TryGetValue(userId, out var sessionId)
               && _sessions.TryGetValue(sessionId, out session);
    }

    public bool TryGetSessionId(uint userId, out uint sessionId)
    {
        return _userIdToSessionId.TryGetValue(userId, out sessionId);
    }

    public bool Remove(uint sessionId)
    {
        if (!_sessions.TryRemove(sessionId, out _))
        {
            return false;
        }

        // 经反向映射清理 userId -> sessionId，session 自身不持有 userId。
        if (_sessionIdToUserId.TryRemove(sessionId, out var userId))
        {
            _userIdToSessionId.TryRemove(userId, out _);
        }
        return true;
    }

    public bool Contains(uint sessionId)
    {
        return _sessions.ContainsKey(sessionId);
    }

    public int Count => _sessions.Count;
}
