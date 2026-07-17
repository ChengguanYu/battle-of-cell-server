using Entity.DTOs;
using Entity.Managers;
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
/// 无请求级状态；在线绑定由 SessionManager 持有。
/// </summary>
public sealed class SessionService() : ServiceBase(), ISessionService
{
    public async FTask<InnerResult> EntryHome(long userId, Session session)
    {
        var user = await UserDao.FindByIdAsync(userId);
        if (user == null)
        {
            Log.Warning($"用户 {userId} 不存在，断开连接");
            return InnerResult.Fail("用户不存在", userId);
        }

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

            // PlayerEntry 成功后才绑定：写入 WsSession 并建立 userId <-> Session 索引
            SessionManager.Instance.Bind(userId, session);

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
    /// 发起房间匹配请求：通过内部 RPC 转发到 Rooms Scene 处理。
    /// </summary>
    public async FTask<InnerResult> PlayerRooms(long userId)
    {
        RoomsMatchResp? resp = null;
        try
        {
            var req = RoomsMatchReq.Create();
            req.userId = userId;
            var address = Scene.GetSceneAddress(SceneType.Rooms);
            resp = await Call<RoomsMatchReq, RoomsMatchResp>(address, req);
            if (!resp.IsOk())
            {
                Log.Warning($"用户 {userId} RoomsMatch 失败，status={resp.ToMessage()}");
                return InnerResult.Fail("RoomsMatch 失败", resp.ToMessage());
            }

            return InnerResult.Ok();
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Rooms Scene，用户 {userId} 房间匹配失败");
            return InnerResult.Fail("未找到 Rooms Scene", userId);
        }
        finally
        {
            resp?.Dispose();
        }
    }
}
