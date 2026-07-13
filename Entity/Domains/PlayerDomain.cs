using System.Runtime.InteropServices.JavaScript;
using Entity.Common;
using Entity.Models;

namespace Entity.Domains;

public class PlayerDomainPrototype : User
{
    // 使用基类实例化自己
    public PlayerDomainPrototype(User user)
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


// Player 只允许交给 PlayersService 管理，切勿跨Scene调用
public class PlayerDomain : Singleton<PlayerDomain> , IDomainBase<PlayerDomainPrototype>
{
    private Dictionary<long ,PlayerDomainPrototype> _memCache = new();

    public void Load(PlayerDomainPrototype player)
    {
        _memCache[player.Id] =  player;
    }
    
}