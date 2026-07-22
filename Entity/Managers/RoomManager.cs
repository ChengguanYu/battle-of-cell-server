using System.Collections.Concurrent;
using Entity.Config;
using Entity.Runtime.room;
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

    private readonly ConcurrentDictionary<long, Room> _roomById = new();
    private readonly ConcurrentDictionary<long, long> _roomIdByUserId = new();
    private long _nextRoomId = 1;

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
    /// 匹配入房：已在房则返回；有 Opened 未满房则加入；否则创建并加入。
    /// </summary>
    public Room? MatchOrCreate(long userId, int capacity = RoomConfig.DefaultCapacity)
    {
        if (userId <= 0)
        {
            return null;
        }

        if (TryGetByUser(userId, out var existing) && existing != null)
        {
            return existing;
        }

        Room? candidate = null;
        foreach (var pair in _roomById)
        {
            var room = pair.Value;
            if (room == null || room.State != RoomState.Opened || room.IsFull)
            {
                continue;
            }

            if (candidate == null || room.RoomId < candidate.RoomId)
            {
                candidate = room;
            }
        }

        if (candidate != null && Join(candidate.RoomId, userId))
        {
            return candidate;
        }

        return CreateWithMember(userId, capacity);
    }

    /// <summary>
    /// 创建房间并进入 Opened。
    /// tick 由 Room.TransitCreatedToOpened 状态迁移启动。
    /// </summary>
    public Room Create(int capacity = RoomConfig.DefaultCapacity)
    {
        var roomId = Interlocked.Increment(ref _nextRoomId) - 1;
        var room = new Room();
        if (!room.TransitCreatedToOpened(roomId, capacity))
        {
            // 理论上不会失败；失败时不入索引
            return room;
        }

        _roomById[roomId] = room;
        return room;
    }

    /// <summary>
    /// 创建房间并加入首位成员。
    /// </summary>
    public Room? CreateWithMember(long userId, int capacity = RoomConfig.DefaultCapacity)
    {
        var room = Create(capacity);
        if (!Join(room.RoomId, userId))
        {
            Remove(room.RoomId, reason: "create_with_member_failed");
            return null;
        }

        return room;
    }

    /// <summary>
    /// 玩家加入房间。
    /// </summary>
    public bool Join(long roomId, long userId)
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

        if (room.MemberCount == 0 && room.State == RoomState.Opened)
        {
            Remove(roomId, reason: "empty");
        }

        return true;
    }

    /// <summary>
    /// 经 roomId 取房间。
    /// </summary>
    public bool TryGet(long roomId, out Room? room)
    {
        return _roomById.TryGetValue(roomId, out room);
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
    /// 经 userId 取 roomId。
    /// </summary>
    public bool TryGetRoomId(long userId, out long roomId)
    {
        return _roomIdByUserId.TryGetValue(userId, out roomId);
    }

    /// <summary>
    /// 关闭并移除房间。
    /// tick 由 Room.TransitOpenedToClosed 状态迁移停止。
    /// </summary>
    public bool Remove(long roomId, string? reason = null)
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

        room.TransitOpenedToClosed(reason);
        return true;
    }

    /// <summary>
    /// 经 userId 关闭其所在房间。
    /// </summary>
    public bool RemoveByUser(long userId, string? reason = null)
    {
        if (!_roomIdByUserId.TryGetValue(userId, out var roomId))
        {
            return false;
        }

        return Remove(roomId, reason);
    }

    public bool Contains(long roomId)
    {
        return _roomById.ContainsKey(roomId);
    }

    public bool ContainsUser(long userId)
    {
        return _roomIdByUserId.ContainsKey(userId);
    }

    public int Count => _roomById.Count;

    public int MemberIndexCount => _roomIdByUserId.Count;
}
