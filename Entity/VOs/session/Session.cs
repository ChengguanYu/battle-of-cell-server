
using Entity.Models;

namespace Entity.VOs.session;

public class Session
{
   
    private uint _id;
    private string _clientIp = string.Empty;
    private short _clientPort;
    
    private long _lastHeartbeat;

    public string GetAddress => $"{_clientIp}:{_clientPort}";

    public void SetAddress(string ip, short port)
    {
        _clientIp = ip;
        _clientPort = port;
    }

    public uint GetId => _id;
    
    public Session(User user)
    {
        _id = (uint)user.Id;
    }
}