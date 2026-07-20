using Fantasy;

namespace Entity.VOs.room;

/// <summary>
/// 房间运行态 VO。
/// 成员集合与状态仅允许经状态机合法迁移写入；非法迁移不抛异常，记录警告并返回 false。
/// </summary>
public sealed class Room
{
    /// <summary>默认房间容量</summary>
    public const int DefaultCapacity = 10;

    private readonly HashSet<long> _memberUserIds = new();
    private RoomState _state = RoomState.New;
    private long _roomId;
    private int _capacity = DefaultCapacity;
    private long _createdAtUnixMs;
    private long _updatedAtUnixMs;

    public long RoomId => _roomId;

    public RoomState State => _state;

    public int Capacity => _capacity;

    public int MemberCount => _memberUserIds.Count;

    public bool IsFull => _memberUserIds.Count >= _capacity;

    public bool IsClosed => _state == RoomState.Closed;

    public long CreatedAtUnixMs => _createdAtUnixMs;

    public long UpdatedAtUnixMs => _updatedAtUnixMs;

    /// <summary>当前成员快照（只读拷贝）。</summary>
    public IReadOnlyCollection<long> MemberUserIds => _memberUserIds.ToArray();

    /// <summary>
    /// 状态迁移：New -> Waiting（完成建房）。
    /// </summary>
    public bool TransitNewToWaiting(long roomId, int capacity = DefaultCapacity)
    {
        if (_state != RoomState.New)
        {
            Log.Warning($"Room 非法迁移 New->Waiting：state={_state}, roomId={roomId}");
            return false;
        }

        if (roomId <= 0)
        {
            Log.Warning($"Room 建房失败：roomId 非法, roomId={roomId}");
            return false;
        }

        if (capacity <= 0)
        {
            Log.Warning($"Room 建房失败：capacity 非法, roomId={roomId}, capacity={capacity}");
            return false;
        }

        _roomId = roomId;
        _capacity = capacity;
        _state = RoomState.Waiting;
        Touch();
        _createdAtUnixMs = _updatedAtUnixMs;
        Log.Info($"Room 建房成功 New->Waiting: roomId={_roomId}, capacity={_capacity}");
        return true;
    }

    /// <summary>
    /// 状态迁移：Waiting -> Ready（人数已齐）。
    /// </summary>
    public bool TransitWaitingToReady()
    {
        if (_state != RoomState.Waiting)
        {
            Log.Warning($"Room 非法迁移 Waiting->Ready：state={_state}, roomId={_roomId}");
            return false;
        }

        if (!IsFull)
        {
            Log.Warning($"Room 无法 Ready：人数未满, roomId={_roomId}, memberCount={MemberCount}, capacity={_capacity}");
            return false;
        }

        _state = RoomState.Ready;
        Touch();
        Log.Info($"Room 就绪 Waiting->Ready: roomId={_roomId}");
        return true;
    }

    /// <summary>
    /// 状态迁移：Ready -> Fighting（开战）。
    /// </summary>
    public bool TransitReadyToFighting()
    {
        if (_state != RoomState.Ready)
        {
            Log.Warning($"Room 非法迁移 Ready->Fighting：state={_state}, roomId={_roomId}");
            return false;
        }

        _state = RoomState.Fighting;
        Touch();
        Log.Info($"Room 开战 Ready->Fighting: roomId={_roomId}");
        return true;
    }

    /// <summary>
    /// 状态迁移：Fighting -> Settling（进入结算）。
    /// </summary>
    public bool TransitFightingToSettling()
    {
        if (_state != RoomState.Fighting)
        {
            Log.Warning($"Room 非法迁移 Fighting->Settling：state={_state}, roomId={_roomId}");
            return false;
        }

        _state = RoomState.Settling;
        Touch();
        Log.Info($"Room 进入结算 Fighting->Settling: roomId={_roomId}");
        return true;
    }

    /// <summary>
    /// 关闭房间：任意非 Closed 态均可进入 Closed。
    /// </summary>
    public bool TransitToClosed(string? reason = null)
    {
        if (_state == RoomState.Closed)
        {
            Log.Info($"Room 关闭跳过: 已是 Closed, roomId={_roomId}");
            return true;
        }

        if (_state == RoomState.New)
        {
            Log.Warning($"Room 非法迁移 New->Closed：尚未建房完成, roomId={_roomId}");
            return false;
        }

        _state = RoomState.Closed;
        _memberUserIds.Clear();
        Touch();
        Log.Info($"Room 关闭完成 ->Closed: roomId={_roomId}, reason={reason}");
        return true;
    }

    /// <summary>
    /// Waiting 态加入成员。已在房间返回 true；满员或非法状态返回 false。
    /// 加满后自动 Waiting -> Ready。
    /// </summary>
    public bool TryAddMember(long userId)
    {
        if (userId <= 0)
        {
            Log.Warning($"Room 加人失败：userId 非法, roomId={_roomId}, userId={userId}");
            return false;
        }

        if (_state != RoomState.Waiting)
        {
            Log.Warning($"Room 加人失败：非 Waiting, state={_state}, roomId={_roomId}, userId={userId}");
            return false;
        }

        if (_memberUserIds.Contains(userId))
        {
            return true;
        }

        if (IsFull)
        {
            Log.Warning($"Room 加人失败：已满, roomId={_roomId}, userId={userId}");
            return false;
        }

        _memberUserIds.Add(userId);
        Touch();
        Log.Info($"Room 加人成功: roomId={_roomId}, userId={userId}, memberCount={MemberCount}/{_capacity}");

        if (IsFull)
        {
            TransitWaitingToReady();
        }

        return true;
    }

    /// <summary>
    /// Waiting / Ready 态移除成员。不在房间返回 false。
    /// Ready 且移除后未满时回退到 Waiting。
    /// </summary>
    public bool TryRemoveMember(long userId)
    {
        if (_state is not (RoomState.Waiting or RoomState.Ready))
        {
            Log.Warning($"Room 移除成员失败：状态不允许, state={_state}, roomId={_roomId}, userId={userId}");
            return false;
        }

        if (!_memberUserIds.Remove(userId))
        {
            return false;
        }

        if (_state == RoomState.Ready && !IsFull)
        {
            _state = RoomState.Waiting;
        }

        Touch();
        Log.Info($"Room 移除成员: roomId={_roomId}, userId={userId}, memberCount={MemberCount}/{_capacity}, state={_state}");
        return true;
    }

    public bool ContainsMember(long userId)
    {
        return _memberUserIds.Contains(userId);
    }

    private void Touch()
    {
        _updatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}