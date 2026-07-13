using System.Runtime.InteropServices.JavaScript;
using Entity.Common;
using Entity.Models;

namespace Entity.Domains;

public class AvatarDomainPrototype : User
{
    // 使用基类实例化自己
    public AvatarDomainPrototype(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        Id = user.Id;
        Uuid = user.Uuid;
        Email = user.Email;
        Username = user.Username;
        PasswordHash = user.PasswordHash;
        Salt = user.Salt;
        LastLoginAt = user.LastLoginAt;
        CreatedAt = user.CreatedAt;
        IsDeleted = user.IsDeleted;
    }
}


// Avatar 只允许交给 AvatarsService 管理，切勿跨Scene调用
public class AvatarDomain : Singleton<AvatarDomain> , IDomainBase<AvatarDomainPrototype>
{
    private Dictionary<long ,AvatarDomainPrototype> _memCache = new();

    public void Load(AvatarDomainPrototype player)
    {
        _memCache[player.Id] =  player;
    }
    
}