using Entity.DTOs;
using Entity.Domains;
using Fantasy;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;
using Hotfix.Scene.Http.Repositories;
using Hotfix.Utils;

namespace Hotfix.Scene.Avatars.Service;

/// <summary>
/// Avatars Scene 级服务（挂在 Scene 上，全 Handler 共享同一实例）。
/// </summary>
public sealed class AvatarsService() : ServiceBase(), IAvatarsService
{
    public async FTask<InnerResult> LoadPlayer(long userId)
    {
        var user = await UserDao.FindByIdAsync(userId);
        if (user == null)
        {
            return InnerResult.Fail("未找到用户");
        }
        var player = new AvatarDomainPrototype(user);
        AvatarDomain.Inst.Load(player);

        return InnerResult.Ok();
    }

    /// <summary>
    /// 纯转发匹配请求到 Match Scene，不做匹配业务校验。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> Match(long userId)
    {
        MatchResp? resp = null;
        try
        {
            var req = MatchReq.Create();
            req.userId = userId;
            var address = Scene.GetSceneAddress(SceneType.Match);
            resp = await Call<MatchReq, MatchResp>(address, req);
            if (!resp.IsOk())
            {
                Log.Warning($"用户 {userId} Match 失败，status={resp.ToMessage()}");
                return InnerResult.Fail("Match 失败", resp.ToMessage());
            }

            return InnerResult.Ok(string.Empty, resp.room_id);
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Match Scene，用户 {userId} 匹配失败");
            return InnerResult.Fail("未找到 Match Scene", userId);
        }
        finally
        {
            resp?.Dispose();
        }
    }
}
