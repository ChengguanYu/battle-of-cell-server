
using Entity.Common;
using Entity.Domains;
using Fantasy;
using Hotfix.Scene.Http.Repositories;

namespace Hotfix.Scene.Avatars.Service;
public class AvatarsService : Fantasy.Entitas.Entity
{
    public async Task<InnerResult> LoadPlayer(long userId)
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