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
        return true;
    }

    /// <summary>
    /// 状态迁移：Online -&gt; TimedOut（心跳超时未续）。
    /// TODO: 后续补实现。
    /// </summary>
    public bool TransitOnlineToTimedOut()
    {
        return false;
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
            return true;
        }

        if (_state != WsSessionState.Kicked)
        {
            Log.Warning($"WsSession 非法迁移 Kicked-&gt;Closed：state={_state}, userId={_userId}");
            return false;
        }

        ClearBoundData();
        _state = WsSessionState.Closed;
        return true;
    }

    /// <summary>
    /// 刷新心跳时间戳（仅 Online 有效）。
    /// 非 Online 返回 false，不抛异常。
    /// </summary>
    public bool UpdateHeartbeat()
    {
        if (_state != WsSessionState.Online)
        {
            return false;
        }

        _lastHeartbeatUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return true;
    }

    private void ClearBoundData()
    {
        _userId = 0;
        _session = null;
    }
}
