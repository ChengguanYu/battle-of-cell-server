using Entity.Common;
using Entity.Managers;
using Entity.VOs.session;
using Fantasy;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;
using Hotfix.Scene.Http.Repositories;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Gate.Service;

/// <summary>
/// Gate Scene 级会话服务（挂在 Scene 上，全 Handler 共享同一实例）。
/// 无请求级状态；在线 Session 仍由 SessionManager 持有。
/// </summary>
public sealed class SessionService() : ServiceBase()
{
    public async FTask<bool> EntryHome(long userId)
    {
        //TODO: 是否有必要查出整个User？毕竟只是用ID
        var user = await UserDao.FindByIdAsync(userId);
        if (user == null)
        {
            Log.Warning($"用户 {userId} 不存在，断开连接");
            return false;
        }

        var wsSession = new WsSession(user);
        SessionManager.Instance.Add(wsSession);

        // 跨 Scene 调用 Avatars 的 PlayerEntry，加载玩家到内存。
        // 目标 Scene 的 Address = 其 RuntimeId，可从 SceneConfig 静态获得。
        // GetSceneAddress 在未配置 Avatars Scene 时抛 InvalidOperationException，这里捕获降级为业务失败（返回 false）。
        PlayerEntryReq? req = null;
        try
        {
            var address = Scene.GetSceneAddress(SceneType.Avatars);
            req = PlayerEntryReq.Create();
            req.userId = userId;

            var resp = (PlayerEntryResp)await Scene.Call(address, req);
            var ok = resp.status == (uint)StatusCode.Ok;
            if (!ok)
            {
                Log.Warning($"用户 {userId} PlayerEntry 失败，status={resp.status}");
            }
            resp.Dispose();
            return ok;
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Avatars Scene，用户 {userId} 进入失败");
            return false;
        }
        finally
        {
            req?.Dispose();
        }
    }
}
