using Entity.Runtime.room;
using Entity.VOs.room;
using Fantasy;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.System;

public static class RoomHoldTimerSystem
{
    public static void Initialize(this RoomHoldTimerEntity self, RoomEntity room)
    {
        self.Room = room;
    }

    public static bool Schedule(this RoomHoldTimerEntity self, int delayMs)
    {
        if (delayMs <= 0)
        {
            Log.Warning($"RoomHoldTimer 启动失败：delayMs 非法, roomId={self.Room.RoomId}, delayMs={delayMs}");
            return false;
        }

        if (!self.Room.Manager.TryGetTimerHost(out var timerScene, out _) || timerScene == null)
        {
            Log.Warning($"RoomHoldTimer 启动失败：未绑定 TimerScene, roomId={self.Room.RoomId}");
            return false;
        }

        self.Cancel();

        self.TimerScene = timerScene;
        self.DelayMs = delayMs;
        self.TimerId = FTask.OnceTimer(timerScene, delayMs, () => OnTimer(self));

        if (self.TimerId == 0)
        {
            self.TimerScene = null;
            self.DelayMs = 0;
            Log.Warning($"RoomHoldTimer 启动失败：OnceTimer 返回 0, roomId={self.Room.RoomId}, delayMs={delayMs}");
            return false;
        }

        Log.Info(
            $"RoomHoldTimer 启动: roomId={self.Room.RoomId}, delayMs={self.DelayMs}, timerId={self.TimerId}");
        return true;
    }

    public static void Cancel(this RoomHoldTimerEntity self)
    {
        if (self.TimerId == 0)
        {
            self.TimerScene = null;
            self.DelayMs = 0;
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
        self.DelayMs = 0;
        Log.Info($"RoomHoldTimer 取消: roomId={self.Room.RoomId}");
    }

    private static void OnTimer(RoomHoldTimerEntity self)
    {
        self.TimerId = 0;
        self.TimerScene = null;
        var delayMs = self.DelayMs;
        self.DelayMs = 0;

        if (!self.Room.IsHolding())
        {
            Log.Debug(
                $"RoomHoldTimer 超时忽略：房间非 Holding, roomId={self.Room.RoomId}, state={self.Room.State}");
            return;
        }

        Log.Info($"RoomHoldTimer 超时: roomId={self.Room.RoomId}, delayMs={delayMs}");
        self.Room.Manager.NotifyHoldTimeout(self.Room.RoomId);
    }
}
