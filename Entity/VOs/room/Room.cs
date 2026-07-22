using Entity.Config;
using Entity.Managers;
using Entity.Runtime.room;
using Entity.Utils;
using Fantasy;

namespace Entity.VOs.room;

/// <summary>
/// 房间运行态 VO。
/// 业务状态：成员、容量、状态机、房间内 UID。
/// 运行时：组合 <see cref="RoomTicker"/> 与帧 RingBuffer，由状态迁移启停，不由外部直接驱动。
/// 状态机：Created -&gt; Opened -&gt; Closed。
/// 写路径约定由 Rooms Actor 串行执行。
/// </summary>
public sealed class Room
{
    /// <summary>
    /// UID 低位序号位数。同一毫秒最多 2^20 个 ID，Actor 串行下足够。
    /// 布局：高 44 位毫秒时间戳 | 低 20 位序号。
    /// </summary>
    private const int UidSeqBits = 20;

    private const int UidSeqMask = (1 << UidSeqBits) - 1;

    private readonly HashSet<long> _memberUserIds = new();
    private readonly RoomTicker _ticker;
    private readonly RingBuffer<server_frame> _frameBuffer;

    private RoomState _state = RoomState.Created;
    private long _roomId;
    private int _capacity = RoomConfig.DefaultCapacity;
    private long _createdAtUnixMs;
    private long _updatedAtUnixMs;

    /// <summary>上一次分配 UID 使用的毫秒时间戳。</summary>
    private long _lastUidMs;

    /// <summary>同一毫秒内的序号；Actor 串行，无需加锁。</summary>
    private int _uidSeqInMs;

    public Room()
    {
        _ticker = new RoomTicker(this);
        _frameBuffer = new RingBuffer<server_frame>(RoomConfig.FrameBufferCapacity, RingBufferFullPolicy.OverwriteOldest);
    }

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

    /// <summary>当前成员快照（只读拷贝）。</summary>
    public IReadOnlyCollection<long> MemberUserIds => _memberUserIds.ToArray();

    /// <summary>
    /// 状态迁移：Created -&gt; Opened。
    /// 成功后启动房间私有 tick（从帧 0 开始写空帧）。
    /// </summary>
    public bool TransitCreatedToOpened(long roomId, int capacity = RoomConfig.DefaultCapacity)
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
        ClearFrameBuffer();
        Touch();
        _createdAtUnixMs = _updatedAtUnixMs;
        Log.Info(
            $"Room 开启成功 Created->Opened: roomId={_roomId}, capacity={_capacity}, delayFrame={RoomConfig.DelayFrame}");

        StartTick();
        return true;
    }

    /// <summary>
    /// 关闭房间：Opened -&gt; Closed。
    /// 先停 tick，再清帧缓冲与业务态。
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

        StopTick();
        ClearFrameBuffer();

        _state = RoomState.Closed;
        _memberUserIds.Clear();
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
    /// 房间逻辑帧入口。由 <see cref="RoomTicker"/> 回调，仅在 Opened 时触发。
    /// 从帧 0 起写入空 <see cref="server_frame"/>；当 tickIndex &gt;= DelayFrame 时广播帧 tickIndex - DelayFrame。
    /// </summary>
    internal void OnTick(long tickIndex)
    {
        if (tickIndex < 0)
        {
            return;
        }

        WriteEmptyFrame((ulong)tickIndex);

        var delayFrame = RoomConfig.DelayFrame;
        if (tickIndex < delayFrame)
        {
            return;
        }

        BroadcastFrame((ulong)(tickIndex - delayFrame));
    }

    private void WriteEmptyFrame(ulong frameNumber)
    {
        // 覆盖写前释放即将被挤掉的旧帧，避免池对象泄漏。
        if (_frameBuffer.IsFull && _frameBuffer.TryPeek(out var oldest) && oldest != null)
        {
            oldest.Return();
        }

        var frame = server_frame.Create(autoReturn: false);
        frame.frame_number = frameNumber;
        // frames 保持空列表，表示本帧无操作
        if (!_frameBuffer.Enqueue(frame))
        {
            frame.Return();
            Log.Warning($"Room 写帧失败: roomId={_roomId}, frameNumber={frameNumber}");
        }
    }

    private void BroadcastFrame(ulong frameNumber)
    {
        if (_memberUserIds.Count == 0)
        {
            return;
        }

        if (!TryGetBufferedFrame(frameNumber, out var buffered) || buffered == null)
        {
            Log.Warning($"Room 延迟广播找不到帧: roomId={_roomId}, frameNumber={frameNumber}, bufferCount={_frameBuffer.Count}");
            return;
        }

        foreach (var userId in _memberUserIds)
        {
            if (!SessionManager.Instance.TryGetSession(userId, out var session) || session == null)
            {
                continue;
            }

            // 每连接独立消息，避免共享池对象。
            using var msg = server_frame.Create();
            msg.frame_number = buffered.frame_number;
            msg.randomSeed = buffered.randomSeed;
            session.Send(msg);
        }
    }

    private bool TryGetBufferedFrame(ulong frameNumber, out server_frame? frame)
    {
        frame = null;
        var count = _frameBuffer.Count;
        if (count == 0)
        {
            return false;
        }

        // 顺序写入下，目标帧逻辑下标 = Count - 1 - (newestNumber - targetNumber)
        if (!_frameBuffer.TryPeekNewest(out var newest) || newest == null)
        {
            return false;
        }

        if (frameNumber > newest.frame_number)
        {
            return false;
        }

        var distanceFromNewest = newest.frame_number - frameNumber;
        if (distanceFromNewest >= (ulong)count)
        {
            return false;
        }

        var index = count - 1 - (int)distanceFromNewest;
        frame = _frameBuffer[index];
        return frame != null && frame.frame_number == frameNumber;
    }

    private void ClearFrameBuffer()
    {
        if (_frameBuffer.IsEmpty)
        {
            return;
        }

        foreach (var frame in _frameBuffer)
        {
            frame?.Return();
        }

        _frameBuffer.Clear();
    }

    private void StartTick()
    {
        if (!RoomManager.Instance.TryGetTimerHost(out var timerScene, out var tickRate) || timerScene == null)
        {
            Log.Warning($"Room Opened 后无法启动 tick：未绑定 TimerScene, roomId={_roomId}");
            return;
        }

        if (!_ticker.Start(timerScene, tickRate))
        {
            Log.Warning($"Room Opened 后启动 tick 失败: roomId={_roomId}");
        }
    }

    private void StopTick()
    {
        _ticker.Stop();
    }

    private void Touch()
    {
        _updatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
