using Entity.Managers;
using Entity.VOs.session;
using Fantasy;
using Fantasy.Async;
using Hotfix.Scene.Http.Repositories;

namespace Hotfix.Scene.Gate.Service;

/// <summary>
/// Gate Scene 级会话服务（挂在 Scene 上，全 Handler 共享同一实例）。
/// 无请求级状态；在线 Session 仍由 SessionManager 持有。
/// </summary>
public sealed class SessionService : Fantasy.Entitas.Entity
{
    public async FTask<bool> EntryHome(long userId)
    {
        var user = await UserDao.FindByIdAsync(userId);
        if (user == null)
        {
            Log.Warning($"用户 {userId} 不存在，断开连接");
            return false;
        }

        var wsSession = new WsSession(user);
        SessionManager.Instance.Add(wsSession);
        return true;
    }
}
