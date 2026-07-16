using Entity.DTOs;
using Entity.Managers;
using Entity.VOs.session;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
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
    public async FTask<InnerResult> EntryHome(long userId, Session session)
    {
        var user = await UserDao.FindByIdAsync(userId);
        if (user == null)
        {
            Log.Warning($"用户 {userId} 不存在，断开连接");
            return InnerResult.Fail("用户不存在", userId);
        }

        var wsSession = new WsSession(user, session);
        SessionManager.Instance.Add(wsSession);

        PlayerEntryResp? resp = null;
        try
        {
            var req = PlayerEntryReq.Create();
            req.userId = userId;
            // PlayerEntryHandle.cs
            var address = Scene.GetSceneAddress(SceneType.Avatars);
            resp = await Call<PlayerEntryReq, PlayerEntryResp>(address, req);
            if (!resp.IsOk())
            {
                Log.Warning($"用户 {userId} PlayerEntry 失败，status={resp.ToMessage()}");
                return InnerResult.Fail("PlayerEntry 失败", resp.ToMessage());
            }

            // PlayerEntry 成功，绑定 userId 与 sessionId。userId 取自鉴权上下文，
            // sessionId 取自连接；当前两者相等，预留未来由网关独立分配 sessionId。
            SessionManager.Instance.Bind((uint)userId, wsSession.GetId);

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

    /// <summary>
    /// 发起匹配请求：通过内部 RPC 转发到 Avatars Scene 处理。
    /// </summary>
    public async FTask<InnerResult> PlayerMatch(long userId)
    {
        AvatarMatchResp? resp = null;
        try
        {
            var req = AvatarMatchReq.Create();
            req.userId = userId;
            var address = Scene.GetSceneAddress(SceneType.Avatars);
            resp = await Call<AvatarMatchReq, AvatarMatchResp>(address, req);
            if (!resp.IsOk())
            {
                Log.Warning($"用户 {userId} AvatarMatch 失败，status={resp.ToMessage()}");
                return InnerResult.Fail("AvatarMatch 失败", resp.ToMessage());
            }

            return InnerResult.Ok();
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Avatars Scene，用户 {userId} 匹配失败");
            return InnerResult.Fail("未找到 Avatars Scene", userId);
        }
        finally
        {
            resp?.Dispose();
        }
    }
}
