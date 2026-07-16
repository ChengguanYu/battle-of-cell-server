using System.Collections.Concurrent;
using Entity.VOs.session;
using Fantasy.Network;

namespace Entity.Managers;

/// <summary>
/// 进程级在线会话缓存。
/// 定义在 Entity 程序集，不随 Hotfix 热更卸载；仅进程退出时释放。
/// 索引：userId 与框架 Session（以 Session.Id 为键）双向关联；WsSession 经 Bind 写入内部值。
/// 重连/顶号由本 Manager 负责，当前未实现完整重连策略。
/// </summary>
public sealed class SessionManager
{
    private static readonly SessionManager _instance = new();
    public static SessionManager Instance => _instance;

    /// <summary>userId 到 WsSession（在线态主存）。</summary>
    private readonly ConcurrentDictionary<long, WsSession> _wsByUserId = new();

    /// <summary>userId 到框架 Session（推送/踢人入口）。</summary>
    private readonly ConcurrentDictionary<long, Session> _sessionByUserId = new();

    /// <summary>框架 Session.Id 到 userId（Handler 解析已绑定用户）。</summary>
    private readonly ConcurrentDictionary<long, long> _userIdBySessionId = new();

    private SessionManager()
    {
    }

    /// <summary>
    /// 鉴权成功后绑定：写入 WsSession 内部值，并建立 userId 与 Session 双向索引。
    /// </summary>
    public void Bind(long userId, Session session)
    {
        //TODO:重连冲突逻辑 同 user 已有绑定：先摘掉旧索引（完整重连策略后续再做）
        if (_wsByUserId.TryRemove(userId, out var oldWs))
        {
            oldWs.ClearBind();
        }

        if (_sessionByUserId.TryRemove(userId, out var oldSession))
        {
            _userIdBySessionId.TryRemove(oldSession.Id, out _);
        }

        var ws = new WsSession();
        ws.ApplyBind(userId, session);

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

        ws.ClearBind();
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
            ws.ClearBind();
        }

        return true;
    }

    public bool ContainsUserId(long userId)
    {
        return _wsByUserId.ContainsKey(userId);
    }

    public int Count => _wsByUserId.Count;
}
