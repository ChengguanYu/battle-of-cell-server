using System.Diagnostics.CodeAnalysis;
using Entity.Common;
using Entity.Models;
using Fantasy;

namespace Entity.Domains;

public class AvatarDomainPrototype : User
{
    private AvatarState _state = AvatarState.New;

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

    public AvatarState State => _state;

    public bool IsInLobby => _state == AvatarState.Lobby;

    public bool IsInRoom => _state == AvatarState.InRoom;

    /// <summary>
    /// 状态迁移：New -> Lobby（Entry 成功）。
    /// </summary>
    public bool TransitNewToLobby()
    {
        if (_state != AvatarState.New)
        {
            Log.Warning($"Avatar 非法迁移 New->Lobby：state={_state}, userId={Id}");
            return false;
        }

        _state = AvatarState.Lobby;
        Log.Info($"Avatar 进入大厅 New->Lobby: userId={Id}");
        return true;
    }

    /// <summary>
    /// 状态迁移：Lobby -> InRoom（进入房间）。
    /// </summary>
    public bool TransitLobbyToInRoom()
    {
        if (_state != AvatarState.Lobby)
        {
            Log.Warning($"Avatar 非法迁移 Lobby->InRoom：state={_state}, userId={Id}");
            return false;
        }

        _state = AvatarState.InRoom;
        Log.Info($"Avatar 进入房间 Lobby->InRoom: userId={Id}");
        return true;
    }

    /// <summary>
    /// 状态迁移：InRoom -> Lobby（离开房间回大厅）。
    /// </summary>
    public bool TransitInRoomToLobby(string? reason = null)
    {
        if (_state != AvatarState.InRoom)
        {
            Log.Warning($"Avatar 非法迁移 InRoom->Lobby：state={_state}, userId={Id}, reason={reason}");
            return false;
        }

        _state = AvatarState.Lobby;
        Log.Info($"Avatar 返回大厅 InRoom->Lobby: userId={Id}, reason={reason}");
        return true;
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

    public bool TryGet(long userId, [NotNullWhen(true)] out AvatarDomainPrototype? player)
    {
        return _memCache.TryGetValue(userId, out player);
    }
}
