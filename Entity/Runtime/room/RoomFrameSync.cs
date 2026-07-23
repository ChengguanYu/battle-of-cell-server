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

    public RoomFrameSync(Func<uint> getRoomId)
    {
        _getRoomId = getRoomId ?? throw new ArgumentNullException(nameof(getRoomId));
        _frameWindow = new RoomFrameWindow(RoomConfig.FrameBufferCapacity);
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

        var frameNumber = (ulong)tickIndex;
        if (!_frameWindow.TryWriteEmpty(frameNumber, out var writeError))
        {
            Log.Warning(
                $"RoomFrameSync 写帧失败: roomId={_getRoomId()}, frameNumber={frameNumber}, error={writeError}");
            return;
        }

        var delayFrame = RoomConfig.DelayFrame;
        if (tickIndex < delayFrame)
        {
            return;
        }

        BroadcastFrame((ulong)(tickIndex - delayFrame), memberUserIds);
    }

    /// <summary>
    /// 开房/关房时清空窗口槽内容（保留预分配对象）。
    /// </summary>
    public void Clear()
    {
        _frameWindow.Clear();
    }

    private void BroadcastFrame(ulong frameNumber, IReadOnlyCollection<long> memberUserIds)
    {
        if (!_frameWindow.TryGet(frameNumber, out var buffered, out var getError) || buffered == null)
        {
            Log.Warning(
                $"RoomFrameSync 延迟广播找不到帧: roomId={_getRoomId()}, frameNumber={frameNumber}, capacity={_frameWindow.Capacity}, error={getError}");
            return;
        }

        if (memberUserIds is { Count: > 0 })
        { 
            foreach (var userId in memberUserIds)
            {
                if (!SessionManager.Instance.TryGetSession(userId, out var session) || session == null)
                {
                    continue;
                }
                // 框架对象天然池化，发送后会销毁，故此需要拷贝多份给每一个客户端
                var msg = server_frame.Create();
                msg.frame_number = buffered.frame_number;
                msg.randomSeed = buffered.randomSeed;

                if (session.IsDisposed)
                {
                    msg.Dispose();
                    continue;
                }

                session.Send(msg);
            }
        }

        // 广播意图完成后标记可清空（无人可发也算消费完成，避免槽永久占用）
        if (!_frameWindow.TryMarkClearable(frameNumber, out var markError))
        {
            Log.Warning(
                $"RoomFrameSync 标记可清空失败: roomId={_getRoomId()}, frameNumber={frameNumber}, error={markError}");
        }
    }
}
