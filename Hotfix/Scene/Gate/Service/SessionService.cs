using Entity.Models;
using Entity.VOs.session;
using Fantasy;
using Fantasy.Async;
using Hotfix.Scene.Http.Repositories;

namespace Hotfix.Scene.Gate.Service;

public static class SessionService
{
    public static async FTask<bool> EntryHome(long userId)
    {
        var user = await UserDao.FindByIdAsync(userId);
        if (user == null)
        {
            Log.Warning($"用户 {userId} 不存在，断开连接");
            return false;
        }
        var wsSession = new WsSession(user);
        SessionManger.Instance.Add(wsSession);
        return true;
    }
}
