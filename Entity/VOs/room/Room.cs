using Entity.Config;
using Entity.Managers;
using Entity.Runtime.room;
using Fantasy;

namespace Entity.VOs.room;

/// <summary>
/// 房间运行态 VO。
/// 业务状态：成员、容量、状态机。
/// 运行时：组合 <see cref="RoomTicker"/>、<see cref="RoomFrameSync"/>、<see cref="RoomUidGenerator"/>，由状态迁移启停。
/// 状态机：Created -&gt; Opened -&gt; Closed。
/// 写路径约定由 Rooms Actor 串行执行。
/// </summary>
public sealed class Room : IRoomStateMachine
{
    private readonly HashSet<long> _memberUserIds = new();
    private readonly RoomTicker _ticker;
    private readonly RoomFrameSync _frameSync;
    private readonly RoomUidGenerator _uidGenerator = new();

    private RoomState _state = RoomState.Created;
    private long _roomId;
    private int _capacity = RoomConfig.DefaultCapacity;
    private long _createdAtUnixMs;
    private long _updatedAtUnixMs;

    public Room()
    {
        _ticker = new RoomTicker(this);
        _frameSync = new RoomFrameSync(() => _roomId);
    }

    public long RoomId => _roomId;

    public RoomState State => _state;

    public int Capacity => _capacity;

    public int MemberCount => _memberUserIds.Count;

    public bool IsFull => _memberUserIds.Count >= _capacity;

    public long CreatedAtUnixMs => _createdAtUnixMs;

    public long UpdatedAtUnixMs => _updatedAtUnixMs;

    /// <summary>当前成员快照（只读拷贝）。</summary>
    public IReadOnlyCollection<long> MemberUserIds => _memberUserIds.ToArray();

    public bool Open(long roomId, int capacity = RoomConfig.DefaultCapacity)
    {
        if (_state != RoomState.Created)
        {
            Log.Warning($"Room 非法迁移 Created->Opened：state={_state}, roomId={roomId}");
            return false;
        }

        if (roomId <= 0)
        {
            Log.Warning($"Room 开启失败：roomId 非法, roomId={roomId}");
            return false;
        }

        if (capacity <= 0)
        {
            Log.Warning($"Room 开启失败：capacity 非法, roomId={roomId}, capacity={capacity}");
            return false;
        }

        _roomId = roomId;
        _capacity = capacity;
        _state = RoomState.Opened;
        _uidGenerator.Reset();
        _frameSync.Clear();
        Touch();
        _createdAtUnixMs = _updatedAtUnixMs;
        Log.Info(
            $"Room 开启成功 Created->Opened: roomId={_roomId}, capacity={_capacity}, delayFrame={RoomConfig.DelayFrame}");

        if (!RoomManager.Instance.TryGetTimerHost(out var timerScene, out var tickRate) || timerScene == null)
        {
            Log.Warning($"Room Opened 后无法启动 tick：未绑定 TimerScene, roomId={_roomId}");
        }
        else if (!_ticker.Start(timerScene, tickRate))
        {
            Log.Warning($"Room Opened 后启动 tick 失败: roomId={_roomId}");
        }
        return true;
    }

    public bool Close(string? reason = null)
    {
        if (_state == RoomState.Closed)
        {
            Log.Info($"Room 关闭跳过: 已是 Closed, roomId={_roomId}");
            return true;
        }

        if (_state != RoomState.Opened)
        {
            Log.Warning($"Room 非法迁移 Opened->Closed：state={_state}, roomId={_roomId}, reason={reason}");
            return false;
        }

        _ticker.Stop();
        _frameSync.Clear();

        _state = RoomState.Closed;
        _memberUserIds.Clear();
        _uidGenerator.Reset();
        Touch();
        Log.Info($"Room 关闭完成 Opened->Closed: roomId={_roomId}, reason={reason}");
        return true;
    }

    /// <summary>
    /// Opened 态加入成员。已在房间返回 true；满员或非法状态返回 false。
    /// </summary>
    public bool TryAddMember(long userId)
    {
        if (userId <= 0)
        {
            Log.Warning($"Room 加人失败：userId 非法, roomId={_roomId}, userId={userId}");
            return false;
        }

        if (_state != RoomState.Opened)
        {
            Log.Warning($"Room 加人失败：非 Opened, state={_state}, roomId={_roomId}, userId={userId}");
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
        return true;
    }

    /// <summary>
    /// Opened 态移除成员。不在房间返回 false。
    /// </summary>
    public bool TryRemoveMember(long userId)
    {
        if (_state != RoomState.Opened)
        {
            Log.Warning($"Room 移除成员失败：非 Opened, state={_state}, roomId={_roomId}, userId={userId}");
            return false;
        }

        if (!_memberUserIds.Remove(userId))
        {
            return false;
        }

        Touch();
        Log.Info($"Room 移除成员: roomId={_roomId}, userId={userId}, memberCount={MemberCount}/{_capacity}, state={_state}");
        return true;
    }

    public bool ContainsMember(long userId)
    {
        return _memberUserIds.Contains(userId);
    }

    public bool TryNextUid(out ulong uid)
    {
        uid = 0;
        if (_state != RoomState.Opened)
        {
            Log.Warning($"Room 分配 UID 失败：非 Opened, state={_state}, roomId={_roomId}");
            return false;
        }

        uid = _uidGenerator.Next();
        return true;
    }

    /// <summary>
    /// 房间逻辑帧入口。由 <see cref="RoomTicker"/> 回调，仅在 Opened 时触发。
    /// 转发给 <see cref="RoomFrameSync"/>：写空帧并按 DelayFrame 延迟广播。
    /// </summary>
    internal void OnTick(long tickIndex)
    {
        _frameSync.OnTick(tickIndex, _memberUserIds);
    }



    private void Touch()
    {
        _updatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
