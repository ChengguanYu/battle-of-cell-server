using System.Collections.Concurrent;
using Entity.Config;
using Entity.Runtime.room;
using Entity.Utils;
using Entity.VOs.room;
using Fantasy;

namespace Entity.Managers;

/// <summary>
/// 进程级房间缓存。
/// 定义在 Entity 程序集，不随 Hotfix 热更卸载；仅进程退出时释放。
/// 索引：roomId 与 userId 双向关联；Room 经状态机迁移。
/// 仅提供 TimerScene 宿主；房间 tick 由 Room 状态迁移自行启停。
/// 写路径约定仅由 Rooms Scene 串行访问。
/// </summary>
public sealed class RoomManager
{
    private static readonly RoomManager _instance = new();
    public static RoomManager Instance => _instance;

    private readonly ConcurrentDictionary<uint, Room> _roomById = new();
    private readonly ConcurrentDictionary<long, uint> _roomIdByUserId = new();
    private readonly RecyclableUIntIdGenerator _roomIdGenerator = new();

    /// <summary>
    /// 房间私有 tick 定时器宿主 Scene（通常为 Rooms Scene）。
    /// </summary>
    private Scene? _timerScene;

    /// <summary>
    /// 新建房间默认逻辑帧率（tick/秒）。
    /// </summary>
    private int _defaultTickRate = RoomTicker.DefaultTickRate;

    private RoomManager()
    {
    }

    /// <summary>
    /// 绑定 Rooms Scene 作为各房间私有 tick 的定时器宿主。
    /// 应在 Rooms Scene 创建时调用。
    /// </summary>
    public void SetTimerScene(Scene scene, int defaultTickRate = RoomTicker.DefaultTickRate)
    {
        ArgumentNullException.ThrowIfNull(scene);

        if (_timerScene != null && !ReferenceEquals(_timerScene, scene))
        {
            Log.Warning(
                $"RoomManager 覆盖 TimerScene: oldRuntimeId={_timerScene.RuntimeId}, newRuntimeId={scene.RuntimeId}");
        }

        _timerScene = scene;
        if (defaultTickRate > 0)
        {
            _defaultTickRate = defaultTickRate;
        }

        Log.Info(
            $"RoomManager 绑定 TimerScene: sceneId={scene.SceneConfigId}, runtimeId={scene.RuntimeId}, defaultTickRate={_defaultTickRate}, intervalMs={Math.Max(1, 1000 / _defaultTickRate)}");
    }

    /// <summary>
    /// 供 Room 状态迁移获取 tick 宿主与默认帧率。
    /// </summary>
    public bool TryGetTimerHost(out Scene? scene, out int tickRate)
    {
        scene = _timerScene;
        tickRate = _defaultTickRate;
        return scene != null;
    }

    /// <summary>
    /// 创建房间并开启。
    /// </summary>
    public Room? Create(int capacity = RoomConfig.DefaultCapacity)
    {
        if (!_roomIdGenerator.TryAcquire(out var roomId))
        {
            Log.Warning($"RoomManager.Create 失败：无法分配 roomId, capacity={capacity}");
            return null;
        }

        Log.Debug($"RoomManager.Create 开始: roomId={roomId}, capacity={capacity}");
        var room = new Room();
        if (!room.Open(roomId, capacity))
        {
            _roomIdGenerator.Release(roomId);
            Log.Debug($"RoomManager.Create Open 失败: roomId={roomId}, capacity={capacity}");
            return null;
        }

        _roomById[roomId] = room;
        Log.Debug($"RoomManager.Create 成功: roomId={roomId}, capacity={capacity}");
        return room;
    }

    /// <summary>
    /// 玩家进入指定房间。
    /// 成功返回房间；失败返回 null。
    /// </summary>
    public Room? Entry(uint roomId, long userId)
    {
        Log.Debug($"RoomManager.Entry 开始: roomId={roomId}, userId={userId}");
        if (userId <= 0 || roomId == 0)
        {
            Log.Debug($"RoomManager.Entry 参数非法: roomId={roomId}, userId={userId}");
            return null;
        }

        if (!Join(roomId, userId))
        {
            Log.Debug($"RoomManager.Entry Join 失败: roomId={roomId}, userId={userId}");
            return null;
        }

        if (!_roomById.TryGetValue(roomId, out var room) || room == null)
        {
            Log.Debug($"RoomManager.Entry 房间丢失: roomId={roomId}, userId={userId}");
            return null;
        }

        Log.Debug(
            $"RoomManager.Entry 成功: roomId={room.RoomId}, userId={userId}, memberCount={room.MemberCount}/{room.Capacity}");
        return room;
    }


    /// <summary>
    /// 玩家加入房间。
    /// </summary>
    private bool Join(uint roomId, long userId)
    {
        if (!_roomById.TryGetValue(roomId, out var room) || room == null)
        {
            return false;
        }

        if (_roomIdByUserId.TryGetValue(userId, out var oldRoomId) && oldRoomId != roomId)
        {
            Leave(userId);
        }

        if (!room.TryAddMember(userId))
        {
            return false;
        }

        _roomIdByUserId[userId] = roomId;
        return true;
    }

    /// <summary>
    /// 玩家离开当前房间。房间无人后自动关闭并移除。
    /// </summary>
    public bool Leave(long userId)
    {
        if (!_roomIdByUserId.TryRemove(userId, out var roomId))
        {
            return false;
        }

        if (!_roomById.TryGetValue(roomId, out var room) || room == null)
        {
            return false;
        }

        if (!room.TryRemoveMember(userId))
        {
            return false;
        }

        if (room.MemberCount == 0 && room.IsOpened())
        {
            Remove(roomId, reason: "empty");
        }

        return true;
    }

    /// <summary>
    /// 经 userId 取所在房间。
    /// </summary>
    public bool TryGetByUser(long userId, out Room? room)
    {
        room = null;
        if (!_roomIdByUserId.TryGetValue(userId, out var roomId))
        {
            return false;
        }

        return _roomById.TryGetValue(roomId, out room);
    }

    /// <summary>
    /// 关闭并移除房间。
    /// </summary>
    public bool Remove(uint roomId, string? reason = null)
    {
        if (!_roomById.TryRemove(roomId, out var room) || room == null)
        {
            return false;
        }

        foreach (var userId in room.MemberUserIds)
        {
            if (_roomIdByUserId.TryGetValue(userId, out var mappedRoomId) && mappedRoomId == roomId)
            {
                _roomIdByUserId.TryRemove(userId, out _);
            }
        }

        room.Close(reason);
        _roomIdGenerator.Release(roomId);
        return true;
    }

    public bool Contains(uint roomId)
    {
        return _roomById.ContainsKey(roomId);
    }


    /// <summary>
    /// 当前管理房间的瞬时快照（只读线索，非 Join 权威）。
    /// 无房间时返回空列表，不返回 null。
    /// </summary>
    public List<Room> GetRoomsSnapshot()
    {
        var list = new List<Room>(_roomById.Count);
        foreach (var pair in _roomById)
        {
            if (pair.Value != null)
            {
                list.Add(pair.Value);
            }
        }

        return list;
    }

}
