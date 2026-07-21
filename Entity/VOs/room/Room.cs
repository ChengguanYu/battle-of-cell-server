using Fantasy;
using Fantasy.Async;
using Entity.Managers;

namespace Entity.VOs.room;

/// <summary>
/// 房间运行态 VO。
/// 成员集合与状态仅允许经状态机合法迁移写入；非法迁移不抛异常，记录警告并返回 false。
/// 状态机：Created -&gt; Opened -&gt; Closed。
/// 进入 Opened 时触发 <see cref="Start"/>（在 Start 内开启房间私有 tick）。
/// 写路径约定由 Rooms Actor 串行执行。
/// </summary>
public sealed class Room
{
    /// <summary>默认房间容量</summary>
    public const int DefaultCapacity = 10;

    /// <summary>
    /// 默认逻辑帧率（tick/秒）。周期 = max(1, 1000 / TickRate) ms。
    /// </summary>
    public const int DefaultTickRate = 20;

    /// <summary>
    /// UID 低位序号位数。同一毫秒最多 2^20 个 ID，Actor 串行下足够。
    /// 布局：高 44 位毫秒时间戳 | 低 20 位序号。
    /// </summary>
    private const int UidSeqBits = 20;

    private const int UidSeqMask = (1 << UidSeqBits) - 1;

    private readonly HashSet<long> _memberUserIds = new();
    private RoomState _state = RoomState.Created;
    private long _roomId;
    private int _capacity = DefaultCapacity;
    private long _createdAtUnixMs;
    private long _updatedAtUnixMs;

    /// <summary>上一次分配 UID 使用的毫秒时间戳。</summary>
    private long _lastUidMs;

    /// <summary>同一毫秒内的序号；Actor 串行，无需加锁。</summary>
    private int _uidSeqInMs;

    /// <summary>房间私有 tick 定时器宿主 Scene（通常为 Rooms Scene）。</summary>
    private Scene? _timerScene;

    /// <summary>房间私有 RepeatedTimer Id；0 表示未启动。</summary>
    private long _tickTimerId;

    /// <summary>逻辑帧率（tick/秒）。</summary>
    private int _tickRate;

    /// <summary>tick 周期（毫秒）= max(1, 1000 / TickRate)。</summary>
    private int _tickIntervalMs;

    /// <summary>已执行 tick 次数（从 1 起算；未启动为 0）。</summary>
    private long _tickIndex;

    public long RoomId => _roomId;

    public RoomState State => _state;

    public int Capacity => _capacity;

    public int MemberCount => _memberUserIds.Count;

    public bool IsFull => _memberUserIds.Count >= _capacity;

    public bool IsCreated => _state == RoomState.Created;

    public bool IsOpened => _state == RoomState.Opened;

    public bool IsClosed => _state == RoomState.Closed;

    public long CreatedAtUnixMs => _createdAtUnixMs;

    public long UpdatedAtUnixMs => _updatedAtUnixMs;

    /// <summary>逻辑帧率（tick/秒）。未启动时为 0。</summary>
    public int TickRate => _tickRate;

    /// <summary>tick 周期毫秒。未启动时为 0。</summary>
    public int TickIntervalMs => _tickIntervalMs;

    /// <summary>已执行 tick 次数。</summary>
    public long TickIndex => _tickIndex;

    /// <summary>房间私有 tick 定时器是否在跑。</summary>
    public bool IsTickRunning => _tickTimerId != 0;

    /// <summary>当前成员快照（只读拷贝）。</summary>
    public IReadOnlyCollection<long> MemberUserIds => _memberUserIds.ToArray();

    /// <summary>
    /// 状态迁移：Created -&gt; Opened（开启房间并触发 Start）。
    /// </summary>
    public bool TransitCreatedToOpened(long roomId, int capacity = DefaultCapacity)
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
        _lastUidMs = 0;
        _uidSeqInMs = 0;
        _tickIndex = 0;
        _tickRate = 0;
        _tickIntervalMs = 0;
        Touch();
        _createdAtUnixMs = _updatedAtUnixMs;
        Log.Info($"Room 开启成功 Created->Opened: roomId={_roomId}, capacity={_capacity}");
        Start();
        return true;
    }

    /// <summary>
    /// 关闭房间：Opened -&gt; Closed。
    /// </summary>
    public bool TransitOpenedToClosed(string? reason = null)
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

        _state = RoomState.Closed;
        _memberUserIds.Clear();
        // 关房时先停掉房间私有 tick，避免回调访问已关闭房间
        StopTickLoop();
        _lastUidMs = 0;
        _uidSeqInMs = 0;
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

    /// <summary>
    /// 在房间生命周期内生成 UID。
    /// 依赖 Rooms Actor 串行：同一时刻只会进入一次。
    /// 映射：高 44 位 = 毫秒时间戳，低 20 位 = 同毫秒序号。
    /// O(1) 时间，仅 2 个标量状态，无占用表。
    /// </summary>
    public bool TryNextUid(out ulong uid)
    {
        uid = 0;
        if (_state != RoomState.Opened)
        {
            Log.Warning($"Room 分配 UID 失败：非 Opened, state={_state}, roomId={_roomId}");
            return false;
        }

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (nowMs < _lastUidMs)
        {
            // 时钟回拨：继续沿用上次毫秒，避免回退撞号
            nowMs = _lastUidMs;
        }

        if (nowMs == _lastUidMs)
        {
            if (_uidSeqInMs >= UidSeqMask)
            {
                // 同一毫秒序号耗尽：推进到下一毫秒
                nowMs = _lastUidMs + 1;
                _lastUidMs = nowMs;
                _uidSeqInMs = 0;
            }
            else
            {
                _uidSeqInMs++;
            }
        }
        else
        {
            _lastUidMs = nowMs;
            _uidSeqInMs = 0;
        }

        uid = ((ulong)nowMs << UidSeqBits) | (uint)_uidSeqInMs;
        if (uid == 0)
        {
            // 理论上 nowMs>0 时不会发生；保底跳过 0
            _uidSeqInMs = 1;
            uid = ((ulong)nowMs << UidSeqBits) | 1u;
        }

        return true;
    }

    /// <summary>
    /// 启动房间私有 tick 循环。周期 = max(1, 1000 / tickRate) ms。
    /// 每个 Room 独立持有 timerId，互不影响；写路径仍约定由 Rooms Actor 串行。
    /// </summary>
    /// <param name="timerScene">定时器宿主 Scene（通常为 Rooms Scene）。</param>
    /// <param name="tickRate">逻辑帧率（tick/秒），&lt;=0 时回落 <see cref="DefaultTickRate"/>。</param>
    public bool StartTickLoop(Scene timerScene, int tickRate = DefaultTickRate)
    {
        if (timerScene == null)
        {
            Log.Warning($"Room 启动 tick 失败：timerScene 为空, roomId={_roomId}");
            return false;
        }

        if (_state != RoomState.Opened)
        {
            Log.Warning($"Room 启动 tick 失败：非 Opened, state={_state}, roomId={_roomId}");
            return false;
        }

        if (tickRate <= 0)
        {
            tickRate = DefaultTickRate;
        }

        // 重入时先停旧定时器，保证一房一 timer
        StopTickLoop();

        _timerScene = timerScene;
        _tickRate = tickRate;
        _tickIntervalMs = Math.Max(1, 1000 / tickRate);
        _tickIndex = 0;

        var roomId = _roomId;
        _tickTimerId = FTask.RepeatedTimer(timerScene, _tickIntervalMs, OnTick);
        Log.Info(
            $"Room tick 启动: roomId={roomId}, tickRate={_tickRate}, intervalMs={_tickIntervalMs}, timerId={_tickTimerId}");
        return _tickTimerId != 0;
    }

    /// <summary>
    /// 停止房间私有 tick 循环。可重复调用。
    /// </summary>
    public void StopTickLoop()
    {
        if (_tickTimerId == 0)
        {
            _timerScene = null;
            return;
        }

        var scene = _timerScene;
        if (scene != null)
        {
            FTask.RemoveTimer(scene, ref _tickTimerId);
        }
        else
        {
            _tickTimerId = 0;
        }

        _timerScene = null;
        Log.Info($"Room tick 停止: roomId={_roomId}, lastTickIndex={_tickIndex}");
    }

    /// <summary>
    /// 房间开启后的内部启动入口。
    /// 由 <see cref="TransitCreatedToOpened"/> 在进入 Opened 后调用；此处开启房间私有 tick。
    /// </summary>
    private void Start()
    {
        if (!RoomManager.Instance.TryGetTimerHost(out var timerScene, out var tickRate) || timerScene == null)
        {
            Log.Warning($"Room Start 跳过 tick：未绑定 TimerScene, roomId={_roomId}");
            return;
        }

        if (!StartTickLoop(timerScene, tickRate))
        {
            Log.Warning($"Room Start 启动 tick 失败: roomId={_roomId}");
        }
    }

    /// <summary>
    /// 房间私有 tick 回调：每 intervalMs 执行一次。
    /// </summary>
    private void OnTick()
    {
        if (_state != RoomState.Opened)
        {
            StopTickLoop();
            return;
        }

        _tickIndex++;

        // TODO: 房间逻辑帧（同步/结算/AI/广播等）
        // 高频路径默认不打日志；需要排查时可临时打开。
        // Log.Debug($"Room tick: roomId={_roomId}, tick={_tickIndex}, members={MemberCount}");
    }

    private void Touch()
    {
        _updatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
