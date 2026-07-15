using Entity.DTOs;
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
    public async FTask<InnerResult> EntryHome(long userId)
    {
        var user = await UserDao.FindByIdAsync(userId);
        if (user == null)
        {
            Log.Warning($"用户 {userId} 不存在，断开连接");
            return InnerResult.Fail("用户不存在", userId);
        }

        var wsSession = new WsSession(user);
        SessionManager.Instance.Add(wsSession);

        PlayerEntryResp? resp = null;
        try
        {
            var req = PlayerEntryReq.Create();
            req.userId = userId;
            // PlayerEntryHandle.cs
            var address = Scene.GetSceneAddress(SceneType.Avatars);
            resp = await Call<PlayerEntryReq, PlayerEntryResp>(address, req);

            if (resp.ErrorCode != (uint)StatusCode.Ok)
            {
                Log.Warning($"用户 {userId} PlayerEntry 失败，status={resp.ToMessage()}");
                return InnerResult.Fail("PlayerEntry 失败", resp.ErrorCode);
            }

            return InnerResult.Ok();
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Avatars Scene，用户 {userId} 进入失败");
            return InnerResult.Fail("未找到 Avatars Scene", userId);
        }
        finally
        {
            resp?.Dispose();
        }
    }
}
