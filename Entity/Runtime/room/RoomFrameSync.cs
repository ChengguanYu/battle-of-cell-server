using Entity.Config;
using Entity.Managers;
using Entity.Utils;
using Fantasy;

namespace Entity.Runtime.room;

/// <summary>
/// 房间帧缓冲与延迟广播。
/// 由 Room 持有，配合 RoomTicker 在 OnTick 中驱动；不反向持有 Room。
/// </summary>
public sealed class RoomFrameSync
{
    private readonly Func<long> _getRoomId;
    private readonly RingBuffer<server_frame> _frameBuffer;

    public RoomFrameSync(Func<long> getRoomId)
    {
        _getRoomId = getRoomId ?? throw new ArgumentNullException(nameof(getRoomId));
        _frameBuffer = new RingBuffer<server_frame>(
            RoomConfig.FrameBufferCapacity,
            RingBufferFullPolicy.OverwriteOldest);
    }

    /// <summary>
    /// 每逻辑帧：写入空 server_frame；tickIndex &gt;= DelayFrame 时广播帧 tickIndex - DelayFrame。
    /// </summary>
    public void OnTick(long tickIndex, IReadOnlyCollection<long> memberUserIds)
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

        BroadcastFrame((ulong)(tickIndex - delayFrame), memberUserIds);
    }

    /// <summary>
    /// 开房/关房时释放缓冲内 server_frame 并清空。
    /// </summary>
    public void Clear()
    {
        if (_frameBuffer.IsEmpty)
        {
            return;
        }

        foreach (var frame in _frameBuffer)
        {
            // FIXME: 缓冲持有 Create(autoReturn:false) 的消息对象，是否继续在覆盖/清空时手动 Return 回 MessageObjectPool，待后续决策
            frame?.Return();
        }

        _frameBuffer.Clear();
    }

    private void WriteEmptyFrame(ulong frameNumber)
    {
        // FIXME: 缓冲持有 Create(autoReturn:false) 的消息对象，是否继续在覆盖/清空时手动 Return 回 MessageObjectPool，待后续决策
        if (_frameBuffer.IsFull && _frameBuffer.TryPeek(out var oldest) && oldest != null)
        {
            oldest.Return();
        }

        var frame = server_frame.Create(autoReturn: false);
        frame.frame_number = frameNumber;
        // frames 保持空列表，表示本帧无操作
        if (!_frameBuffer.Enqueue(frame))
        {
            // FIXME: 写失败路径同样依赖手动 Return，与上面缓冲生命周期策略一并决策
            frame.Return();
            Log.Warning($"RoomFrameSync 写帧失败: roomId={_getRoomId()}, frameNumber={frameNumber}");
        }
    }

    private void BroadcastFrame(ulong frameNumber, IReadOnlyCollection<long> memberUserIds)
    {
        if (memberUserIds == null || memberUserIds.Count == 0)
        {
            return;
        }

        if (!TryGetBufferedFrame(frameNumber, out var buffered) || buffered == null)
        {
            Log.Warning(
                $"RoomFrameSync 延迟广播找不到帧: roomId={_getRoomId()}, frameNumber={frameNumber}, bufferCount={_frameBuffer.Count}");
            return;
        }

        foreach (var userId in memberUserIds)
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
}
