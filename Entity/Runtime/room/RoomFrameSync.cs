using Entity.Config;
using Entity.Managers;
using Fantasy;
using Fantasy.Network;

namespace Entity.Runtime.room;

/// <summary>
/// 房间帧缓冲与延迟广播。
/// 由 Room 持有，配合 RoomTicker 在 OnTick 中驱动；不反向持有 Room。
/// 帧槽位由 <see cref="RoomFrameWindow"/> 预分配管理，本类只负责编排写帧/读帧/广播。
/// </summary>
public sealed class RoomFrameSync
{
    private readonly Func<uint> _getRoomId;
    private readonly RoomFrameWindow _frameWindow;
    private long _currentTickIndex = -1;

    public RoomFrameSync(Func<uint> getRoomId)
    {
        _getRoomId = getRoomId ?? throw new ArgumentNullException(nameof(getRoomId));
        _frameWindow = new RoomFrameWindow(RoomConfig.FrameBufferCapacity);
    }

    /// <summary>最近一次 OnTick 的 tickIndex；未 tick 时为 -1。</summary>
    public long CurrentTickIndex => _currentTickIndex;

    /// <summary>
    /// 每逻辑帧：写入空 server_frame；tickIndex &gt;= DelayFrame 时广播帧 tickIndex - DelayFrame。
    /// 写路径约定：当前 tick 之前的槽内容可覆盖。
    /// </summary>
    public void OnTick(long tickIndex, IReadOnlyCollection<long> memberUserIds)
    {
        if (tickIndex < 0)
        {
            return;
        }

        _currentTickIndex = tickIndex;
        var frameNumber = (ulong)tickIndex;
        if (!_frameWindow.TryWriteEmpty(frameNumber, out var writeError))
        {
            Log.Warning(
                $"RoomFrameSync 写帧失败: roomId={_getRoomId()}, frameNumber={frameNumber}, error={writeError}");
            // 不 return：仍推进延迟广播
        }

        var delayFrame = RoomConfig.DelayFrame;
        if (tickIndex < delayFrame)
        {
            return;
        }

        BroadcastFrame((ulong)(tickIndex - delayFrame), memberUserIds);
    }

    /// <summary>
    /// 将客户端操作写入开放延迟窗口（深拷贝）。
    /// 仅接受 (currentTick - DelayFrame, currentTick] 内的帧号：
    /// - 已广播/过旧：拒绝
    /// - 超前于服务端当前 tick：拒绝
    /// - 目标槽已 Clearable：拒绝
    /// 目标槽未打开时按写空帧规则打开后再追加。
    /// </summary>
    public bool TryAppendClientOps(ulong clientFrameNumber, IReadOnlyList<frame>? ops, out string? error)
    {
        if (ops == null || ops.Count == 0)
        {
            error = null;
            return true;
        }

        if (_currentTickIndex < 0)
        {
            error = "房间尚未产生逻辑帧";
            return false;
        }

        if (!TryResolveOpenTarget(clientFrameNumber, _currentTickIndex, out var target, out error))
        {
            return false;
        }

        if (!_frameWindow.TryEnsureOpen(target, out error))
        {
            return false;
        }

        if (!_frameWindow.TryAppendOps(target, ops, out error))
        {
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// 开房/关房时清空窗口槽内容（保留预分配对象）。
    /// </summary>
    public void Clear()
    {
        _currentTickIndex = -1;
        _frameWindow.Clear();
    }

    /// <summary>
    /// 校验客户端帧号是否仍落在开放窗口，不做重映射。
    /// 开放区间：(T - DelayFrame, T]，T 为当前 tick。
    /// </summary>
    private static bool TryResolveOpenTarget(
        ulong clientFrame,
        long currentTick,
        out ulong target,
        out string? error)
    {
        target = 0;
        var delay = RoomConfig.DelayFrame;
        var high = (ulong)currentTick;
        var low = currentTick >= delay ? (ulong)(currentTick - delay + 1) : 0UL;

        if (clientFrame < low)
        {
            error =
                $"帧已过期(已广播或离开窗口): clientFrame={clientFrame}, open=[{low},{high}], currentTick={currentTick}";
            return false;
        }

        if (clientFrame > high)
        {
            error =
                $"帧超前于服务端: clientFrame={clientFrame}, open=[{low},{high}], currentTick={currentTick}";
            return false;
        }

        target = clientFrame;
        error = null;
        return true;
    }

    private void BroadcastFrame(ulong frameNumber, IReadOnlyCollection<long> memberUserIds)
    {
        if (!_frameWindow.TryGet(frameNumber, out var buffered, out var getError) || buffered == null)
        {
            Log.Warning(
                $"RoomFrameSync 延迟广播找不到帧: roomId={_getRoomId()}, frameNumber={frameNumber}, capacity={_frameWindow.Capacity}, error={getError}");
            return;
        }

        try
        {
            if (memberUserIds is { Count: > 0 })
            {
                foreach (var userId in memberUserIds)
                {
                    if (!SessionManager.Instance.TryGetSession(userId, out var session) || session == null)
                    {
                        continue;
                    }

                    // 框架对象天然池化，发送后会 Dispose，故每连接独立拷贝
                    var msg = server_frame.Create();
                    msg.frame_number = buffered.frame_number;
                    msg.randomSeed = buffered.randomSeed;
                    CopyFrames(buffered.frames, msg);

                    if (session.IsDisposed)
                    {
                        msg.Dispose();
                        continue;
                    }

                    session.Send(msg);
                }
            }
        }
        finally
        {
            // 广播意图完成后标记可清空（无人可发 / 发送异常也算消费完成）
            if (!_frameWindow.TryMarkClearable(frameNumber, out var markError))
            {
                Log.Warning(
                    $"RoomFrameSync 标记可清空失败: roomId={_getRoomId()}, frameNumber={frameNumber}, error={markError}");
            }
        }
    }

    private static void CopyFrames(List<frame>? source, server_frame target)
    {
        if (source == null || source.Count == 0)
        {
            return;
        }

        target.frames ??= new List<frame>();
        for (var i = 0; i < source.Count; i++)
        {
            var src = source[i];
            if (src == null)
            {
                continue;
            }

            target.frames.Add(CloneFrameForSend(src));
        }
    }

    private static frame CloneFrameForSend(frame src)
    {
        var dst = frame.Create();
        dst.op = src.op;
        if (src.data != null)
        {
            var p = player.Create();
            p.speed = src.data.speed;
            p.eid = src.data.eid;
            if (src.data.direction != null)
            {
                p.direction = vec2d.Create();
                p.direction.x = src.data.direction.x;
                p.direction.y = src.data.direction.y;
            }

            if (src.data.position != null)
            {
                p.position = position2d.Create();
                p.position.x = src.data.position.x;
                p.position.y = src.data.position.y;
            }

            dst.data = p;
        }

        return dst;
    }
}
