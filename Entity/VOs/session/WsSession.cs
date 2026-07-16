using Fantasy.Network;

namespace Entity.VOs.session;

/// <summary>
/// 玩家在线态。userId 与框架 Session 仅允许经 ApplyBind 写入。
/// </summary>
public class WsSession
{
    private long _userId;
    private Session? _session;
    private string _clientIp = string.Empty;
    private short _clientPort;
    private long _lastHeartbeat;

    public long GetUserId => _userId;

    public Session? GetSession => _session;

    public string GetAddress => $"{_clientIp}:{_clientPort}";

    public void SetAddress(string ip, short port)
    {
        _clientIp = ip;
        _clientPort = port;
    }

    /// <summary>
    /// 由 SessionManager.Bind 调用，写入绑定结果。
    /// </summary>
    public void ApplyBind(long userId, Session session)
    {
        _userId = userId;
        _session = session;
    }

    /// <summary>
    /// 由 SessionManager 解绑时调用，清空内部绑定。
    /// </summary>
    public void ClearBind()
    {
        _userId = 0;
        _session = null;
    }
}
