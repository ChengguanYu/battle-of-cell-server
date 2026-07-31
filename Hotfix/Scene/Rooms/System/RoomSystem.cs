using Entity.Config;
using Entity.Managers;
using Entity.Runtime.room;
using Entity.VOs.room;
using Fantasy;
using Hotfix.Simulation.System;

using Hotfix.Utils;

namespace Hotfix.Scene.Rooms.System;

public static class RoomSystem
{
    public static void Initialize(this RoomEntity self, RoomManagerEntity manager)
    {
        self.Manager = manager;
        self.Ticker = new RoomTickerEntity();
        self.Ticker.Initialize(self);
        self.HoldTimer = new RoomHoldTimerEntity();
        self.HoldTimer.Initialize(self);
        self.FrameSync = new RoomFrameSyncEntity();
        self.FrameSync.Initialize(self);
    }

    public static bool IsCreated(this RoomEntity self)
    {
        return self.State == RoomState.Created;
    }

    public static bool IsOpened(this RoomEntity self)
    {
        return self.State == RoomState.Opened;
    }

    public static bool IsHolding(this RoomEntity self)
    {
        return self.State == RoomState.Holding;
    }

    public static bool IsClosed(this RoomEntity self)
    {
        return self.State == RoomState.Closed;
    }

    public static bool Open(this RoomEntity self, uint roomId, int capacity = RoomConfig.DefaultCapacity)
    {
        if (self.State != RoomState.Created)
        {
            Log.Warning($"Room 非法迁移 Created->Opened：state={self.State}, roomId={roomId}");
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

        CommitOpen(self, roomId, capacity);

        if (!self.Ticker.Start())
        {
            Log.Warning($"Room Open 失败：tick 启动失败, roomId={self.RoomId}");
            RollbackOpen(self);
            return false;
        }

        Log.Info(
            $"Room 开启成功 Created->Opened: roomId={self.RoomId}, capacity={self.Capacity}, delayFrame={RoomConfig.DelayFrame}");
        return true;
    }

    public static bool Hold(this RoomEntity self, int remainMs)
    {
        if (remainMs <= 0)
        {
            Log.Warning($"Room Hold 失败：remainMs 非法, roomId={self.RoomId}, remainMs={remainMs}");
            return false;
        }

        if (self.State == RoomState.Opened)
        {
            self.Ticker.Stop();
            self.FrameSync.Clear();

            if (!self.HoldTimer.Schedule(remainMs))
            {
                if (!self.Ticker.Start())
                {
                    Log.Error($"Room Hold 回滚失败：tick 无法恢复, roomId={self.RoomId}");
                }

                Log.Warning($"Room Hold 失败：计时启动失败, roomId={self.RoomId}, state={self.State}, remainMs={remainMs}");
                return false;
            }

            self.State = RoomState.Holding;
            Touch(self);
            Log.Info(
                $"Room Hold 成功 Opened->Holding: roomId={self.RoomId}, remainMs={remainMs}, memberCount={self.MemberCount}");
            return true;
        }

        if (self.State != RoomState.Holding)
        {
            Log.Warning($"Room 非法迁移 ->Holding：state={self.State}, roomId={self.RoomId}, remainMs={remainMs}");
            return false;
        }

        if (!self.HoldTimer.Schedule(remainMs))
        {
            Log.Warning($"Room Hold 续命失败：计时启动失败, roomId={self.RoomId}, remainMs={remainMs}");
            return false;
        }

        Touch(self);
        Log.Info(
            $"Room Hold 续命成功: roomId={self.RoomId}, remainMs={remainMs}, memberCount={self.MemberCount}");
        return true;
    }

    public static bool Resume(this RoomEntity self)
    {
        if (self.State != RoomState.Holding)
        {
            Log.Warning($"Room 非法迁移 Holding->Opened：state={self.State}, roomId={self.RoomId}");
            return false;
        }

        self.HoldTimer.Cancel();
        self.State = RoomState.Opened;

        if (!self.Ticker.Start())
        {
            self.State = RoomState.Holding;
            Log.Warning($"Room Resume 失败：tick 启动失败, roomId={self.RoomId}");
            return false;
        }

        Touch(self);
        Log.Info($"Room Resume 成功 Holding->Opened: roomId={self.RoomId}, memberCount={self.MemberCount}");
        return true;
    }

    public static bool Close(this RoomEntity self, string? reason = null)
    {
        if (self.State == RoomState.Closed)
        {
            Log.Info($"Room 关闭跳过: 已是 Closed, roomId={self.RoomId}");
            return true;
        }

        if (self.State != RoomState.Opened && self.State != RoomState.Holding)
        {
            Log.Warning($"Room 非法迁移 ->Closed：state={self.State}, roomId={self.RoomId}, reason={reason}");
            return false;
        }

        var from = self.State;
        self.HoldTimer.Cancel();
        self.Ticker.Stop();
        self.FrameSync.Clear();

        self.State = RoomState.Closed;
        self.MemberUserIds.Clear();
        self.UidGenerator.Reset();
        Touch(self);
        Log.Info($"Room 关闭完成 {from}->Closed: roomId={self.RoomId}, reason={reason}");
        return true;
    }

    public static bool TryAddMember(this RoomEntity self, long userId)
    {
        if (userId <= 0)
        {
            Log.Warning($"Room 加人失败：userId 非法, roomId={self.RoomId}, userId={userId}");
            return false;
        }

        if (self.State != RoomState.Opened && self.State != RoomState.Holding)
        {
            Log.Warning($"Room 加人失败：非 Opened/Holding, state={self.State}, roomId={self.RoomId}, userId={userId}");
            return false;
        }

        if (self.MemberUserIds.Contains(userId))
        {
            return true;
        }

        if (self.IsFull)
        {
            Log.Warning($"Room 加人失败：已满, roomId={self.RoomId}, userId={userId}");
            return false;
        }

        var wasHolding = self.State == RoomState.Holding;
        self.MemberUserIds.Add(userId);

        if (wasHolding && !self.Resume())
        {
            self.MemberUserIds.Remove(userId);
            Log.Warning(
                $"Room 加人失败：Holding Resume 失败已回滚, roomId={self.RoomId}, userId={userId}, state={self.State}");
            return false;
        }

        Touch(self);
        Log.Info($"Room 加人成功: roomId={self.RoomId}, userId={userId}, memberCount={self.MemberCount}/{self.Capacity}, state={self.State}");
        return true;
    }

    public static bool TryRemoveMember(this RoomEntity self, long userId)
    {
        if (self.State != RoomState.Opened)
        {
            Log.Warning($"Room 移除成员失败：非 Opened, state={self.State}, roomId={self.RoomId}, userId={userId}");
            return false;
        }

        if (!self.MemberUserIds.Remove(userId))
        {
            return false;
        }

        Touch(self);
        Log.Info($"Room 移除成员: roomId={self.RoomId}, userId={userId}, memberCount={self.MemberCount}/{self.Capacity}, state={self.State}");
        return true;
    }

    public static bool ContainsMember(this RoomEntity self, long userId)
    {
        return self.MemberUserIds.Contains(userId);
    }

    public static bool TryNextUid(this RoomEntity self, out ulong uid)
    {
        uid = 0;
        if (self.State != RoomState.Opened)
        {
            Log.Warning($"Room 分配 UID 失败：非 Opened, state={self.State}, roomId={self.RoomId}");
            return false;
        }

        uid = self.UidGenerator.Next();
        return true;
    }

    public static void OnTick(this RoomEntity self, long tickIndex)
    {
        self.FrameSync.OnTick(tickIndex, self.MemberUserIds);
    }


    public static bool TryAppendClientOps(
        this RoomEntity self,
        ulong frameNumber,
        IReadOnlyList<Frame>? ops,
        out string? error)
    {
        if (self.State != RoomState.Opened)
        {
            error = $"房间非 Opened, state={self.State}";
            return false;
        }

        return self.FrameSync.TryAppendClientOps(frameNumber, ops, out error);
    }

    private static void CommitOpen(RoomEntity self, uint roomId, int capacity)
    {
        self.RoomId = roomId;
        self.Capacity = capacity;
        self.State = RoomState.Opened;
        self.UidGenerator.Reset();
        self.FrameSync.Clear();
        self.HoldTimer.Cancel();
        Touch(self);
        self.CreatedAtUnixMs = self.UpdatedAtUnixMs;
    }

    private static void RollbackOpen(RoomEntity self)
    {
        self.HoldTimer.Cancel();
        self.Ticker.Stop();
        self.FrameSync.Clear();
        self.UidGenerator.Reset();
        self.MemberUserIds.Clear();
        self.RoomId = 0;
        self.Capacity = RoomConfig.DefaultCapacity;
        self.CreatedAtUnixMs = 0;
        self.UpdatedAtUnixMs = 0;
        self.State = RoomState.Created;
    }

    private static void Touch(RoomEntity self)
    {
        self.UpdatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
