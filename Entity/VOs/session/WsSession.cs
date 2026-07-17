using Fantasy;
using Fantasy.Network;

namespace Entity.VOs.session;

/// <summary>
/// 玩家在线态 VO。userId / Session 仅允许经状态机合法迁移写入。
/// 非法迁移不抛异常，记录警告并返回 false，内部数据不变。
/// </summary>
public sealed class WsSession
{
    private long _userId;
    private Session? _session;
    private long _lastHeartbeatUnixMs;
    private WsSessionState _state = WsSessionState.New;

    public WsSessionState State => _state;

    public bool IsOnline => _state == WsSessionState.Online;

    public long UserId => _userId;

    public Session? Session => _session;

    public long LastHeartbeatUnixMs => _lastHeartbeatUnixMs;

    /// <summary>
    /// 状态迁移：New -&gt; Online（完成绑定）。
    /// 非法迁移返回 false，不抛异常。
    /// </summary>
    public bool TransitNewToOnline(long userId, Session session)
    {
        if (_state != WsSessionState.New)
        {
            Log.Warning($"WsSession 非法迁移 New-&gt;Online：state={_state}, userId={userId}");
            return false;
        }

        _userId = userId;
        _session = session;
        _state = WsSessionState.Online;
        _lastHeartbeatUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Log.Info($"WsSession 绑定成功 New->Online: userId={_userId}");
        return true;
    }

    /// <summary>
    /// 状态迁移：Online -&gt; Kicked（顶号/踢下线）。
    /// 非法迁移返回 false，不抛异常。
    /// </summary>
    public bool TransitOnlineToKicked(string? reason = null)
    {
        if (_state != WsSessionState.Online)
        {
            Log.Warning($"WsSession 非法迁移 Online-&gt;Kicked：state={_state}, userId={_userId}, reason={reason}");
            return false;
        }

        _state = WsSessionState.Kicked;
        Log.Info($"WsSession 被踢下线 Online->Kicked: userId={_userId}, reason={reason}");
        return true;
    }

    /// <summary>
    /// 状态迁移：Online -&gt; TimedOut（连接断开/心跳超时未续）。
    /// 非法迁移返回 false，不抛异常。
    /// 进入超时态后解除 Session 引用，保留 userId 供后续清理/重连。
    /// </summary>
    public bool TransitOnlineToTimedOut()
    {
        if (_state != WsSessionState.Online)
        {
            Log.Warning($"WsSession 非法迁移 Online-&gt;TimedOut：state={_state}, userId={_userId}");
            return false;
        }

        _session = null;
        _state = WsSessionState.TimedOut;
        Log.Info($"WsSession 连接超时/断开 Online->TimedOut: userId={_userId}");
        return true;
    }

    /// <summary>
    /// 状态迁移：Online -&gt; Closed（正常解绑）。
    /// 非法迁移返回 false，不抛异常。
    /// </summary>
    public bool TransitOnlineToClosed()
    {
        if (_state != WsSessionState.Online)
        {
            Log.Warning($"WsSession 非法迁移 Online-&gt;Closed：state={_state}, userId={_userId}");
            return false;
        }

        ClearBoundData();
        _state = WsSessionState.Closed;
        Log.Info($"WsSession 正常关闭 Online->Closed: userId={_userId}");
        return true;
    }

    /// <summary>
    /// 状态迁移：Kicked -&gt; Closed（踢下线后清理）。
    /// 已 Closed 视为成功；其他非法迁移返回 false，不抛异常。
    /// </summary>
    public bool TransitKickedToClosed()
    {
        if (_state == WsSessionState.Closed)
        {
            Log.Info($"WsSession 关闭跳过: 已是 Closed 状态, userId={_userId}");
            return true;
        }

        if (_state != WsSessionState.Kicked)
        {
            Log.Warning($"WsSession 非法迁移 Kicked-&gt;Closed：state={_state}, userId={_userId}");
            return false;
        }

        ClearBoundData();
        _state = WsSessionState.Closed;
        Log.Info($"WsSession 踢下线清理完成 Kicked->Closed: userId={_userId}");
        return true;
    }

    /// <summary>
    /// 刷新心跳时间戳（仅 Online 有效）。
    /// 非 Online 返回 false，不抛异常。
    /// </summary>
    public bool UpdateHeartbeat()
    {
        Log.Info($"WsSession 心跳更新: userId={_userId}, 上次心跳={_lastHeartbeatUnixMs}, 当前={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        if (_state != WsSessionState.Online)
        {
            return false;
        }

        _lastHeartbeatUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return true;
    }

    private void ClearBoundData()
    {
        Log.Info($"WsSession 清空绑定数据: userId={_userId}, 有Session={_session != null}");
        _userId = 0;
        _session = null;
    }
}
