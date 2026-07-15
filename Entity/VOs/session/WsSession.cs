
using Entity.Models;
using Fantasy.Network;

namespace Entity.VOs.session;

public class WsSession
{
 
    private uint _id;
    private Session? _session;
    private string _clientIp = string.Empty;
    private short _clientPort;
    
    private long _lastHeartbeat;

    public string GetAddress => $"{_clientIp}:{_clientPort}";

    public void SetAddress(string ip, short port)
    {
        _clientIp = ip;
        _clientPort = port;
    }

    public Session? GetSession => _session;

    /// <summary>
    /// 绑定 EntryHome 建立的 Fantasy 网络连接（RPC session）。
    /// </summary>
    public void SetSession(Session session)
    {
        _session = session;
    }

    public uint GetId => _id;
    
    public WsSession(User user, Session session)
    {
        _id = (uint)user.Id;
        _session = session;
    }
    
}
