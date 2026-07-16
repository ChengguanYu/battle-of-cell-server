using Entity.DTOs;
using Entity.Domains;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;
using Hotfix.Scene.Http.Repositories;

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
}
