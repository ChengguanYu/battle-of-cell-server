using Entity.Config;
using Entity.Managers;
using Entity.Runtime.room;
using Fantasy;
using Hotfix.Utils;

namespace Hotfix.Scene.Rooms.System;

public static class RoomFrameSyncSystem
{
    public static void Initialize(this RoomFrameSyncEntity self, Entity.VOs.room.RoomEntity room)
    {
        self.Room = room;
        self.FrameWindow = new RoomFrameWindowEntity();
        self.FrameWindow.Initialize(RoomConfig.FrameBufferCapacity);
        self.CurrentTickIndex = -1;
    }

    public static void OnTick(this RoomFrameSyncEntity self, long tickIndex, IReadOnlyCollection<long> memberUserIds)
    {
        if (tickIndex < 0)
        {
            return;
        }

        self.CurrentTickIndex = tickIndex;
        var frameNumber = (ulong)tickIndex;
        if (!self.FrameWindow.TryWriteEmpty(frameNumber, out var writeError))
        {
            Log.Warning(
                $"RoomFrameSync 写帧失败: roomId={self.Room.RoomId}, frameNumber={frameNumber}, error={writeError}");
        }

        var delayFrame = RoomConfig.DelayFrame;
        if (tickIndex < delayFrame)
        {
            return;
        }

        BroadcastFrame(self, (ulong)(tickIndex - delayFrame), memberUserIds);
        FeedSimulatorFrame(self, (ulong)(tickIndex - delayFrame));
    }

    public static bool TryAppendClientOps(
        this RoomFrameSyncEntity self,
        ulong clientFrameNumber,
        IReadOnlyList<Frame>? ops,
        out string? error)
    {
        if (ops == null || ops.Count == 0)
        {
            error = null;
            return true;
        }

        if (self.CurrentTickIndex < 0)
        {
            error = "房间尚未产生逻辑帧";
            return false;
        }

        if (!TryResolveOpenTarget(clientFrameNumber, self.CurrentTickIndex, out var target, out error))
        {
            return false;
        }

        if (!self.FrameWindow.TryEnsureOpen(target, out error))
        {
            return false;
        }

        if (!self.FrameWindow.TryAppendOps(target, ops, out error))
        {
            return false;
        }

        error = null;
        return true;
    }

    public static void Clear(this RoomFrameSyncEntity self)
    {
        self.CurrentTickIndex = -1;
        self.FrameWindow.Clear();
    }

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

    private static void BroadcastFrame(
        RoomFrameSyncEntity self,
        ulong frameNumber,
        IReadOnlyCollection<long> memberUserIds)
    {
        if (!self.FrameWindow.TryGet(frameNumber, out var buffered, out var getError) || buffered == null)
        {
            Log.Warning(
                $"RoomFrameSync 延迟广播找不到帧: roomId={self.Room.RoomId}, frameNumber={frameNumber}, capacity={self.FrameWindow.Capacity}, error={getError}");
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

                    var msg = FrameMessageUtil.CreateServerFrameForSend(buffered);

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
            if (!self.FrameWindow.TryMarkClearable(frameNumber, out var markError))
            {
                Log.Warning(
                    $"RoomFrameSync 标记可清空失败: roomId={self.Room.RoomId}, frameNumber={frameNumber}, error={markError}");
            }
        }
    }
    private static void FeedSimulatorFrame(RoomFrameSyncEntity self, ulong frameNumber)
    {
        var simManager = self.Room.Manager.Scene?.GetComponent<SimulationManagerEntity>();
        if (simManager == null)
            return;

        if (!simManager.StateByRoomId.TryGetValue(self.Room.RoomId, out var simState) || simState == null)
            return;

        if (!self.FrameWindow.TryPeek(frameNumber, out var buffered) || buffered == null)
            return;

        // 归还上次没被模拟器消费的帧
        if (simState.PendingSimFrame != null)
        {
            simState.PendingSimFrame.Dispose();
            simState.PendingSimFrame = null;
        }

        simState.PendingSimFrame = FrameMessageUtil.CreateServerFrameForSend(buffered);
    }

}
