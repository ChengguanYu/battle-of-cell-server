using System.Collections.Concurrent;
using Entity.Config;
using Entity.VOs.session;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;

namespace Entity.Managers;

/// <summary>
/// 进程级在线会话缓存。
/// 定义在 Entity 程序集，不随 Hotfix 热更卸载；仅进程退出时释放。
/// 索引：userId 与框架 Session（以 Session.Id 为键）双向关联；WsSession 经状态机迁移。
/// 重连策略未完整实现；同 user 再 Bind 时走 Online-&gt;Kicked-&gt;Closed 顶号路径。
/// TimedOut 后由绑定的 Gate Scene 启动宽限期定时器，到期迁移 Closed 并移除缓存。
/// </summary>
public sealed class SessionManager
{
    private static readonly SessionManager _instance = new();
    public static SessionManager Instance => _instance;

    private readonly ConcurrentDictionary<long, WsSession> _wsByUserId = new();
    private readonly ConcurrentDictionary<long, Session> _sessionByUserId = new();
    private readonly ConcurrentDictionary<long, long> _userIdBySessionId = new();
    private readonly ConcurrentDictionary<long, long> _timedOutCloseTimerByUserId = new();

    private Scene? _timerScene;

    private SessionManager()
    {
    }

    /// <summary>
    /// 绑定用于 TimedOut 宽限期计时的 Scene（通常为 Gate）。
    /// 重复绑定同一 Scene 视为幂等；绑定不同 Scene 时会覆盖并记录警告。
    /// </summary>
    public void BindTimerScene(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        if (_timerScene != null && !ReferenceEquals(_timerScene, scene))
        {
            Log.Warning($"SessionManager 覆盖 TimerScene: oldRuntimeId={_timerScene.RuntimeId}, newRuntimeId={scene.RuntimeId}");
        }

        _timerScene = scene;
        Log.Info($"SessionManager 绑定 TimerScene: sceneId={scene.SceneConfigId}, runtimeId={scene.RuntimeId}, timedOutCloseMs={SessionTimeoutConfig.TimedOutCloseDelayMs}");
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
            CancelTimedOutCloseTimer(userId);
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

        CancelTimedOutCloseTimer(userId);
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
            CancelTimedOutCloseTimer(userId);
            CloseWs(ws, kickIfOnline: false);
        }

        return true;
    }

    /// <summary>
    /// 连接断开：拆除 Session 索引，并将绑定的 WsSession 迁移到 TimedOut。
    /// 未绑定或非 Online 时返回 false；不主动 Closed，启动宽限期定时器后等待清理。
    /// </summary>
    public bool MarkTimedOutBySession(Session session)
    {
        if (!_userIdBySessionId.TryRemove(session.Id, out var userId))
        {
            return false;
        }

        _sessionByUserId.TryRemove(userId, out _);

        if (!_wsByUserId.TryGetValue(userId, out var ws) || ws == null)
        {
            return false;
        }

        if (!ws.TransitOnlineToTimedOut())
        {
            return false;
        }

        ScheduleTimedOutClose(userId);
        return true;
    }

    public bool ContainsUserId(long userId)
    {
        return _wsByUserId.TryGetValue(userId, out var ws) && ws.IsOnline;
    }

    public int Count => _wsByUserId.Count;

    /// <summary>
    /// TimedOut 宽限期到期：仅当仍是 TimedOut 时迁移 Closed 并移除缓存。
    /// </summary>
    private void CloseTimedOutIfStillTimedOut(long userId)
    {
        _timedOutCloseTimerByUserId.TryRemove(userId, out _);

        if (!_wsByUserId.TryGetValue(userId, out var ws) || ws == null)
        {
            return;
        }

        if (ws.State != WsSessionState.TimedOut)
        {
            Log.Info($"WsSession 宽限期到期但状态已变化，跳过 Closed: userId={userId}, state={ws.State}");
            return;
        }

        if (!_wsByUserId.TryRemove(userId, out ws) || ws == null)
        {
            return;
        }

        // 竞态：移除瞬间状态可能已变化，仍按当前状态收敛
        if (ws.State == WsSessionState.TimedOut)
        {
            ws.TransitTimedOutToClosed("timed_out_grace_expired");
            return;
        }

        CloseWs(ws, kickIfOnline: false);
    }

    private void ScheduleTimedOutClose(long userId)
    {
        CancelTimedOutCloseTimer(userId);

        var scene = _timerScene;
        if (scene == null)
        {
            Log.Warning($"SessionManager 未绑定 TimerScene，无法启动 TimedOut 宽限期: userId={userId}");
            return;
        }

        var delayMs = SessionTimeoutConfig.TimedOutCloseDelayMs;
        var timerId = FTask.OnceTimer(scene, delayMs, () =>
        {
            CloseTimedOutIfStillTimedOut(userId);
        });

        _timedOutCloseTimerByUserId[userId] = timerId;
        Log.Info($"WsSession TimedOut 宽限期计时启动: userId={userId}, delayMs={delayMs}, timerId={timerId}");
    }

    private void CancelTimedOutCloseTimer(long userId)
    {
        if (!_timedOutCloseTimerByUserId.TryRemove(userId, out var timerId) || timerId == 0)
        {
            return;
        }

        var scene = _timerScene;
        if (scene == null)
        {
            return;
        }

        FTask.RemoveTimer(scene, ref timerId);
        Log.Info($"WsSession TimedOut 宽限期计时取消: userId={userId}");
    }

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
            case WsSessionState.TimedOut:
                ws.TransitTimedOutToClosed(kickReason ?? "force_close");
                break;
            case WsSessionState.Closed:
            case WsSessionState.New:
                break;
        }
    }
}
