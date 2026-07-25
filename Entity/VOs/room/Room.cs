using Entity.Config;
using Entity.Runtime.room;
using Fantasy;

namespace Entity.VOs.room;

/// <summary>
/// 房间运行态 VO。
/// 业务状态：成员、容量、状态机。
/// 运行时：组合 <see cref="RoomTicker"/>、<see cref="RoomHoldTimer"/>、<see cref="RoomFrameSync"/>、<see cref="RoomUidGenerator"/>，由状态迁移启停。
/// 状态机：Created -&gt; Opened -&gt; Holding -&gt; Closed；Opened 可直接 Closed；Holding 可回 Opened。
/// 写路径约定由 Rooms Actor 串行执行。
/// </summary>
public sealed class Room : IRoomStateMachine
{
    private readonly HashSet<long> _memberUserIds = new();
    private readonly RoomTicker _ticker;
    private readonly RoomHoldTimer _holdTimer;
    private readonly RoomFrameSync _frameSync;
    private readonly RoomUidGenerator _uidGenerator = new();

    private RoomState _state = RoomState.Created;
    private uint _roomId;
    private int _capacity = RoomConfig.DefaultCapacity;
    private long _createdAtUnixMs;
    private long _updatedAtUnixMs;

    public Room()
    {
        _ticker = new RoomTicker(this);
        _holdTimer = new RoomHoldTimer(this);
        _frameSync = new RoomFrameSync(() => _roomId);
    }

    public uint RoomId => _roomId;

    public RoomState State => _state;

    public int Capacity => _capacity;

    public int MemberCount => _memberUserIds.Count;

    public bool IsFull => _memberUserIds.Count >= _capacity;

    public long CreatedAtUnixMs => _createdAtUnixMs;

    public long UpdatedAtUnixMs => _updatedAtUnixMs;

    /// <summary>当前成员快照（只读拷贝）。</summary>
    public IReadOnlyCollection<long> MemberUserIds => _memberUserIds.ToArray();

    public bool Open(uint roomId, int capacity = RoomConfig.DefaultCapacity)
    {
        if (_state != RoomState.Created)
        {
            Log.Warning($"Room 非法迁移 Created->Opened：state={_state}, roomId={roomId}");
            return false;
        }

        if (roomId == 0)
        {
            Log.Warning($"Room 开启失败：roomId 非法, roomId={roomId}");
            return false;
        }

        if (capacity <= 0)
        {
            Log.Warning($"Room 开启失败：capacity 非法, roomId={roomId}, capacity={capacity}");
            return false;
        }

        CommitOpen(roomId, capacity);

        if (!_ticker.Start())
        {
            Log.Warning($"Room Open 失败：tick 启动失败, roomId={_roomId}");
            RollbackOpen();
            return false;
        }

        Log.Info(
            $"Room 开启成功 Created->Opened: roomId={_roomId}, capacity={_capacity}, delayFrame={RoomConfig.DelayFrame}");
        return true;
    }

    /// <summary>
    /// Opened -&gt; Holding，或 Holding 续命。持有房间但暂停推帧，等待占位玩家入场。
    /// 首次进入 Holding 时先挂计时再切状态；失败保持 Opened。
    /// </summary>
    public bool Hold(int remainMs)
    {
        if (remainMs <= 0)
        {
            Log.Warning($"Room Hold 失败：remainMs 非法, roomId={_roomId}, remainMs={remainMs}");
            return false;
        }

        if (_state == RoomState.Opened)
        {
            _ticker.Stop();
            _frameSync.Clear();

            if (!_holdTimer.Schedule(remainMs))
            {
                // 回滚：尽量恢复 tick，保持 Opened。
                if (!_ticker.Start())
                {
                    Log.Error($"Room Hold 回滚失败：tick 无法恢复, roomId={_roomId}");
                }

                Log.Warning($"Room Hold 失败：计时启动失败, roomId={_roomId}, state={_state}, remainMs={remainMs}");
                return false;
            }

            _state = RoomState.Holding;
            Touch();
            Log.Info(
                $"Room Hold 成功 Opened->Holding: roomId={_roomId}, remainMs={remainMs}, memberCount={MemberCount}");
            return true;
        }

        if (_state != RoomState.Holding)
        {
            Log.Warning($"Room 非法迁移 ->Holding：state={_state}, roomId={_roomId}, remainMs={remainMs}");
            return false;
        }

        if (!_holdTimer.Schedule(remainMs))
        {
            Log.Warning($"Room Hold 续命失败：计时启动失败, roomId={_roomId}, remainMs={remainMs}");
            return false;
        }

        Touch();
        Log.Info(
            $"Room Hold 续命成功: roomId={_roomId}, remainMs={remainMs}, memberCount={MemberCount}");
        return true;
    }

    /// <summary>
    /// Holding -&gt; Opened。停止 hold 计时并恢复推帧。
    /// </summary>
    public bool Resume()
    {
        if (_state != RoomState.Holding)
        {
            Log.Warning($"Room 非法迁移 Holding->Opened：state={_state}, roomId={_roomId}");
            return false;
        }

        // 先切 Opened 再启 tick（RoomTicker 要求 Opened）；失败回滚 Holding。
        _holdTimer.Cancel();
        _state = RoomState.Opened;

        if (!_ticker.Start())
        {
            _state = RoomState.Holding;
            Log.Warning($"Room Resume 失败：tick 启动失败, roomId={_roomId}");
            return false;
        }

        Touch();
        Log.Info($"Room Resume 成功 Holding->Opened: roomId={_roomId}, memberCount={MemberCount}");
        return true;
    }

    public bool Close(string? reason = null)
    {
        if (_state == RoomState.Closed)
        {
            Log.Info($"Room 关闭跳过: 已是 Closed, roomId={_roomId}");
            return true;
        }

        if (_state != RoomState.Opened && _state != RoomState.Holding)
        {
            Log.Warning($"Room 非法迁移 ->Closed：state={_state}, roomId={_roomId}, reason={reason}");
            return false;
        }

        var from = _state;
        _holdTimer.Cancel();
        _ticker.Stop();
        _frameSync.Clear();

        _state = RoomState.Closed;
        _memberUserIds.Clear();
        _uidGenerator.Reset();
        Touch();
        Log.Info($"Room 关闭完成 {from}->Closed: roomId={_roomId}, reason={reason}");
        return true;
    }

    /// <summary>
    /// Opened/Holding 态加入成员。已在房间返回 true；满员或非法状态返回 false。
    /// Holding 下成功加人后由状态机自动 Resume 回 Opened（Holding 不变量：空员等待入场）。
    /// </summary>
    public bool TryAddMember(long userId)
    {
        if (userId <= 0)
        {
            Log.Warning($"Room 加人失败：userId 非法, roomId={_roomId}, userId={userId}");
            return false;
        }

        if (_state != RoomState.Opened && _state != RoomState.Holding)
        {
            Log.Warning($"Room 加人失败：非 Opened/Holding, state={_state}, roomId={_roomId}, userId={userId}");
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

        var wasHolding = _state == RoomState.Holding;
        _memberUserIds.Add(userId);

        if (wasHolding && !Resume())
        {
            _memberUserIds.Remove(userId);
            Log.Warning(
                $"Room 加人失败：Holding Resume 失败已回滚, roomId={_roomId}, userId={userId}, state={_state}");
            return false;
        }

        Touch();
        Log.Info($"Room 加人成功: roomId={_roomId}, userId={userId}, memberCount={MemberCount}/{_capacity}, state={_state}");
        return true;
    }

    /// <summary>
    /// Opened 态移除成员。不在房间返回 false。
    /// Holding 时空员，不应再走移除路径。
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

    /// <summary>
    /// 将客户端帧操作写入房间帧窗口（Opened 且成员有效时）。
    /// </summary>
    public bool TryAppendClientOps(ulong frameNumber, IReadOnlyList<Frame>? ops, out string? error)
    {
        if (_state != RoomState.Opened)
        {
            error = $"房间非 Opened, state={_state}";
            return false;
        }

        return _frameSync.TryAppendClientOps(frameNumber, ops, out error);
    }

    private void CommitOpen(uint roomId, int capacity)
    {
        _roomId = roomId;
        _capacity = capacity;
        _state = RoomState.Opened;
        _uidGenerator.Reset();
        _frameSync.Clear();
        _holdTimer.Cancel();
        Touch();
        _createdAtUnixMs = _updatedAtUnixMs;
    }

    private void RollbackOpen()
    {
        _holdTimer.Cancel();
        _ticker.Stop();
        _frameSync.Clear();
        _uidGenerator.Reset();
        _memberUserIds.Clear();
        _roomId = 0;
        _capacity = RoomConfig.DefaultCapacity;
        _createdAtUnixMs = 0;
        _updatedAtUnixMs = 0;
        _state = RoomState.Created;
    }

    private void Touch()
    {
        _updatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
