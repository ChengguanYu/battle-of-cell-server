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
    /// 校验玩家已加载后，转发匹配请求到 Match Scene。
    /// </summary>
    public async FTask<InnerResult> Match(long userId)
    {
        if (!AvatarDomain.Inst.TryGet(userId, out _))
        {
            Log.Warning($"[MatchFlow] Avatar 玩家未加载 userId={userId}");
            return InnerResult.Fail("玩家未加载", userId);
        }

        MatchResp? resp = null;
        try
        {
            var req = MatchReq.Create();
            req.userId = userId;
            var address = Scene.GetSceneAddress(SceneType.Match);
            Log.Info($"[MatchFlow] Avatar->Match 转发匹配 userId={userId} address={address}");
            resp = await Call<MatchReq, MatchResp>(address, req);
            if (!resp.IsOk())
            {
                Log.Warning($"[MatchFlow] Avatar<-Match 匹配失败 userId={userId} status={resp.ToMessage()}");
                return InnerResult.Fail("Match 失败", resp.ToMessage());
            }

            Log.Info($"[MatchFlow] Avatar<-Match 匹配成功 userId={userId}");
            return InnerResult.Ok();
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"[MatchFlow] 未找到 Match Scene userId={userId}");
            return InnerResult.Fail("未找到 Match Scene", userId);
        }
        finally
        {
            resp?.Dispose();
        }
    }
}
