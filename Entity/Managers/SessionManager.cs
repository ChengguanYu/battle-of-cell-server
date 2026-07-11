using System.Collections.Concurrent;
using Entity.VOs.session;

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

    public bool TryGet(uint sessionId, out WsSession? session)
    {
        return _sessions.TryGetValue(sessionId, out session);
    }

    public bool Remove(uint sessionId)
    {
        return _sessions.TryRemove(sessionId, out _);
    }

    public bool Contains(uint sessionId)
    {
        return _sessions.ContainsKey(sessionId);
    }

    public int Count => _sessions.Count;
}
