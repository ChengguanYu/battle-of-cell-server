using Entity.Runtime.room;
using Entity.VOs.room;
using Fantasy;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.System;

public static class RoomTickerSystem
{
    public static void Initialize(this RoomTickerEntity self, RoomEntity room)
    {
        self.Room = room;
        self.TickRate = RoomTickerEntity.DefaultTickRate;
    }

    public static bool Start(this RoomTickerEntity self)
    {
        if (!self.Room.IsOpened())
        {
            Log.Warning($"RoomTicker 启动失败：房间非 Opened, state={self.Room.State}, roomId={self.Room.RoomId}");
            return false;
        }

        if (!self.Room.Manager.TryGetTimerHost(out var timerScene, out var tickRate) || timerScene == null)
        {
            Log.Warning($"RoomTicker 启动失败：未绑定 TimerScene, roomId={self.Room.RoomId}");
            return false;
        }

        if (tickRate <= 0)
        {
            tickRate = RoomTickerEntity.DefaultTickRate;
        }

        self.Stop();

        self.TimerScene = timerScene;
        self.TickRate = tickRate;
        self.IntervalMs = Math.Max(1, 1000 / tickRate);
        self.TickIndex = -1;
        self.TimerId = FTask.RepeatedTimer(timerScene, self.IntervalMs, () => OnTimer(self));

        Log.Info(
            $"RoomTicker 启动: roomId={self.Room.RoomId}, tickRate={self.TickRate}, intervalMs={self.IntervalMs}, timerId={self.TimerId}");
        return self.TimerId != 0;
    }

    public static void Stop(this RoomTickerEntity self)
    {
        if (self.TimerId == 0)
        {
            self.TimerScene = null;
            return;
        }

        var scene = self.TimerScene;
        if (scene != null)
        {
            FTask.RemoveTimer(scene, ref self.TimerId);
        }
        else
        {
            self.TimerId = 0;
        }

        self.TimerScene = null;
        Log.Info($"RoomTicker 停止: roomId={self.Room.RoomId}, lastTickIndex={self.TickIndex}");
    }

    private static void OnTimer(RoomTickerEntity self)
    {
        if (!self.Room.IsOpened())
        {
            self.Stop();
            return;
        }

        self.TickIndex++;
        self.Room.OnTick(self.TickIndex);
    }
}
