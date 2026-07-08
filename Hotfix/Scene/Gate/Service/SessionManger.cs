using Entity.VOs.session;

namespace Hotfix.Scene.Gate.Service;

public class SessionManger
{
    private static readonly SessionManger _instance = new();
    public static SessionManger Instance => _instance;

    private Dictionary<uint, WsSession> _sessions = new();

    private SessionManger()
    {
    }
    
    public void Add(WsSession session)
    {
        _sessions[session.GetId] = session;
    }
}