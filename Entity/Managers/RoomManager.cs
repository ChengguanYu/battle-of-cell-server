using System.Collections.Concurrent;
using Entity.VOs.room;

namespace Entity.Managers;

/// <summary>
/// 进程级房间缓存。
/// 定义在 Entity 程序集，不随 Hotfix 热更卸载；仅进程退出时释放。
/// 索引：roomId 与 userId 双向关联；Room 经状态机迁移。
/// 写路径约定仅由 Rooms Scene 串行访问。
/// </summary>
public sealed class RoomManager
{
    private static readonly RoomManager _instance = new();
    public static RoomManager Instance => _instance;

    private readonly ConcurrentDictionary<long, Room> _roomById = new();
    private readonly ConcurrentDictionary<long, long> _roomIdByUserId = new();
    private long _nextRoomId = 1;

    private RoomManager()
    {
    }

    /// <summary>
    /// 匹配入房：已在房则直接返回；有 Waiting 未满房则加入；否则创建并加入。
    /// 仅应由 Rooms Scene 串行调用（Actor 模型），不做跨线程锁。
    /// </summary>
    public Room? TryMatchOrCreate(long userId, int capacity = Room.DefaultCapacity)
    {
        if (userId <= 0)
        {
            return null;
        }

        if (TryGetByUserId(userId, out var existing) && existing != null)
        {
            return existing;
        }

        Room? candidate = null;
        foreach (var pair in _roomById)
        {
            var room = pair.Value;
            if (room == null || room.State != RoomState.Waiting || room.IsFull)
            {
                continue;
            }

            if (candidate == null || room.RoomId < candidate.RoomId)
            {
                candidate = room;
            }
        }

        if (candidate != null && TryJoin(candidate.RoomId, userId))
        {
            return candidate;
        }

        return CreateWithMember(userId, capacity);
    }

    /// <summary>
    /// 创建房间并进入 Waiting。
    /// </summary>
    public Room Create(int capacity = Room.DefaultCapacity)
    {
        var roomId = Interlocked.Increment(ref _nextRoomId) - 1;
        var room = new Room();
        if (!room.TransitNewToWaiting(roomId, capacity))
        {
            // 理论上不会失败；失败时不入索引
            return room;
        }

        _roomById[roomId] = room;
        return room;
    }

    /// <summary>
    /// 创建房间并加入首位成员。加人失败时关闭并移除房间，返回 null。
    /// </summary>
    public Room? CreateWithMember(long userId, int capacity = Room.DefaultCapacity)
    {
        var room = Create(capacity);
        if (!TryJoin(room.RoomId, userId))
        {
            Remove(room.RoomId, reason: "create_with_member_failed");
            return null;
        }

        return room;
    }

    /// <summary>
    /// 玩家加入房间。成功时建立 userId -> roomId 索引。
    /// 同一 user 已在其他房间时先离开旧房间。
    /// </summary>
    public bool TryJoin(long roomId, long userId)
    {
        if (!_roomById.TryGetValue(roomId, out var room) || room == null)
        {
            return false;
        }

        // 同 user 已在其他房间：先摘旧
        if (_roomIdByUserId.TryGetValue(userId, out var oldRoomId) && oldRoomId != roomId)
        {
            TryLeave(userId);
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
    public bool TryLeave(long userId)
    {
        if (!_roomIdByUserId.TryRemove(userId, out var roomId))
        {
            return false;
        }

        if (!_roomById.TryGetValue(roomId, out var room) || room == null)
        {
            return false;
        }

        room.TryRemoveMember(userId);

        if (room.MemberCount == 0 && room.State is RoomState.Waiting or RoomState.Ready)
        {
            Remove(roomId, reason: "empty");
        }

        return true;
    }

    /// <summary>经 roomId 取房间。</summary>
    public bool TryGetByRoomId(long roomId, out Room? room)
    {
        return _roomById.TryGetValue(roomId, out room);
    }

    /// <summary>经 userId 取所在房间。</summary>
    public bool TryGetByUserId(long userId, out Room? room)
    {
        room = null;
        if (!_roomIdByUserId.TryGetValue(userId, out var roomId))
        {
            return false;
        }

        return _roomById.TryGetValue(roomId, out room);
    }

    /// <summary>经 userId 取 roomId。</summary>
    public bool TryGetRoomIdByUserId(long userId, out long roomId)
    {
        return _roomIdByUserId.TryGetValue(userId, out roomId);
    }

    /// <summary>关闭并移除房间，同时拆除所有成员索引。</summary>
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

        room.TransitToClosed(reason);
        return true;
    }

    /// <summary>经 userId 关闭其所在房间。</summary>
    public bool RemoveByUserId(long userId, string? reason = null)
    {
        if (!_roomIdByUserId.TryGetValue(userId, out var roomId))
        {
            return false;
        }

        return Remove(roomId, reason);
    }

    public bool ContainsRoomId(long roomId)
    {
        return _roomById.ContainsKey(roomId);
    }

    public bool ContainsUserId(long userId)
    {
        return _roomIdByUserId.ContainsKey(userId);
    }

    public int Count => _roomById.Count;

    public int MemberIndexCount => _roomIdByUserId.Count;
}