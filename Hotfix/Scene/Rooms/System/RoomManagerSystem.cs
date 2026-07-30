using Entity.Config;
using Entity.Managers;
using Entity.Runtime.room;
using Entity.VOs.room;
using Fantasy;
using Hotfix.Simulation.Abstractions;
using Hotfix.Simulation.System;
using SimManager = Entity.Managers.SimulationManagerEntity;

namespace Hotfix.Scene.Rooms.System;

public static class RoomManagerSystem
{
    public static void SetTimerScene(
        this RoomManagerEntity self,
        Fantasy.Scene scene,
        int defaultTickRate = RoomTickerEntity.DefaultTickRate)
    {
        ArgumentNullException.ThrowIfNull(scene);

        if (self.TimerScene != null && !ReferenceEquals(self.TimerScene, scene))
        {
            Log.Warning(
                $"RoomManager 覆盖 TimerScene: oldRuntimeId={self.TimerScene.RuntimeId}, newRuntimeId={scene.RuntimeId}");
        }

        self.TimerScene = scene;
        if (defaultTickRate > 0)
        {
            self.DefaultTickRate = defaultTickRate;
        }

        Log.Info(
            $"RoomManager 绑定 TimerScene: sceneId={scene.SceneConfigId}, runtimeId={scene.RuntimeId}, defaultTickRate={self.DefaultTickRate}, intervalMs={Math.Max(1, 1000 / self.DefaultTickRate)}");
    }

    public static void SetHoldTimeoutHandler(this RoomManagerEntity self, Action<uint>? handler)
    {
        self.HoldTimeoutHandler = handler;
        Log.Info($"RoomManager 绑定 HoldTimeoutHandler: bound={handler != null}");
    }

    public static void SetEmptyRoomHandler(this RoomManagerEntity self, Action<uint, string?>? handler)
    {
        self.EmptyRoomHandler = handler;
        Log.Info($"RoomManager 绑定 EmptyRoomHandler: bound={handler != null}");
    }

    public static bool TryGetTimerHost(this RoomManagerEntity self, out Fantasy.Scene? scene, out int tickRate)
    {
        scene = self.TimerScene;
        tickRate = self.DefaultTickRate;
        return scene != null;
    }

    public static void NotifyHoldTimeout(this RoomManagerEntity self, uint roomId)
    {
        var handler = self.HoldTimeoutHandler;
        if (handler == null)
        {
            Log.Warning($"RoomManager HoldTimeout 无回调: roomId={roomId}");
            return;
        }

        try
        {
            handler(roomId);
        }
        catch (Exception ex)
        {
            Log.Error($"RoomManager HoldTimeout 回调异常: roomId={roomId}, ex={ex}");
        }
    }

    public static RoomEntity? CreateRoom(this RoomManagerEntity self, int capacity = RoomConfig.DefaultCapacity)
    {
        if (!self.RoomIdGenerator.TryAcquire(out var roomId))
        {
            Log.Warning($"RoomManager.Create 失败：无法分配 roomId, capacity={capacity}");
            return null;
        }

        Log.Debug($"RoomManager.Create 开始: roomId={roomId}, capacity={capacity}");
        var room = new RoomEntity();
        room.Initialize(self);
        if (!room.Open(roomId, capacity))
        {
            self.RoomIdGenerator.Release(roomId);
            Log.Debug($"RoomManager.Create Open 失败: roomId={roomId}, capacity={capacity}");
            return null;
        }

        self.RoomById[roomId] = room;

        // 房间与模拟器 1:1：创建成功后立即拉起对应模拟器
        var simManager = self.Scene?.GetComponent<SimManager>();
        if (simManager == null || !simManager.Create(roomId, out var sim) || sim == null)
        {
            Log.Error($"RoomManager.Create 模拟器创建失败，回滚房间: roomId={roomId}");
            self.RoomById.TryRemove(roomId, out _);
            room.Close("sim_create_failed");
            self.RoomIdGenerator.Release(roomId);
            return null;
        }

        // 状态转移：Created -> Running，由房间创建方显式驱动
        sim.Run();

        Log.Debug($"RoomManager.Create 成功: roomId={roomId}, capacity={capacity}");
        return room;
    }

    public static RoomEntity? EnterRoom(this RoomManagerEntity self, uint roomId, long userId)
    {
        Log.Debug($"RoomManager.Entry 开始: roomId={roomId}, userId={userId}");
        if (userId <= 0 || roomId == 0)
        {
            Log.Debug($"RoomManager.Entry 参数非法: roomId={roomId}, userId={userId}");
            return null;
        }

        if (!Join(self, roomId, userId))
        {
            Log.Debug($"RoomManager.Entry Join 失败: roomId={roomId}, userId={userId}");
            return null;
        }

        if (!self.RoomById.TryGetValue(roomId, out var room) || room == null)
        {
            Log.Debug($"RoomManager.Entry 房间丢失: roomId={roomId}, userId={userId}");
            return null;
        }

        Log.Debug(
            $"RoomManager.Entry 成功: roomId={room.RoomId}, userId={userId}, memberCount={room.MemberCount}/{room.Capacity}, state={room.State}");
        return room;
    }

    public static bool LeaveRoom(this RoomManagerEntity self, long userId, string? reason = null)
    {
        if (!self.RoomIdByUserId.TryRemove(userId, out var roomId))
        {
            return false;
        }

        if (!self.RoomById.TryGetValue(roomId, out var room) || room == null)
        {
            return false;
        }

        if (!room.TryRemoveMember(userId))
        {
            return false;
        }

        // 玩家离房时从模拟器清除（幂等：未注册时静默跳过）
        var simManager = self.Scene?.GetComponent<SimulationManagerEntity>();
        if (simManager != null && simManager.TryGet(roomId, out var sim) && sim is SimBase simBase)
        {
            simBase.RemovePlayer(userId);
        }

        if (room.MemberCount == 0 && room.IsOpened())
        {
            NotifyEmptyRoom(self, roomId, reason ?? "empty");
        }

        return true;
    }

    public static bool HoldRoom(this RoomManagerEntity self, uint roomId, int remainMs)
    {
        if (!self.RoomById.TryGetValue(roomId, out var room) || room == null)
        {
            Log.Warning($"RoomManager.Hold 失败：房间不存在, roomId={roomId}, remainMs={remainMs}");
            return false;
        }

        if (!room.Hold(remainMs))
        {
            Log.Warning($"RoomManager.Hold 失败：状态机拒绝, roomId={roomId}, state={room.State}, remainMs={remainMs}");
            return false;
        }

        Log.Info($"RoomManager.Hold 成功: roomId={roomId}, state={room.State}, remainMs={remainMs}");
        return true;
    }

    public static bool ResumeRoom(this RoomManagerEntity self, uint roomId)
    {
        if (!self.RoomById.TryGetValue(roomId, out var room) || room == null)
        {
            Log.Warning($"RoomManager.Resume 失败：房间不存在, roomId={roomId}");
            return false;
        }

        if (!room.IsHolding())
        {
            return true;
        }

        if (!room.Resume())
        {
            Log.Warning($"RoomManager.Resume 失败：状态机拒绝, roomId={roomId}, state={room.State}");
            return false;
        }

        Log.Info($"RoomManager.Resume 成功: roomId={roomId}, memberCount={room.MemberCount}");
        return true;
    }

    public static bool TryGetByUser(this RoomManagerEntity self, long userId, out RoomEntity? room)
    {
        room = null;
        if (!self.RoomIdByUserId.TryGetValue(userId, out var roomId))
        {
            return false;
        }

        return self.RoomById.TryGetValue(roomId, out room);
    }

    public static bool TryGetById(this RoomManagerEntity self, uint roomId, out RoomEntity? room)
    {
        room = null;
        if (roomId == 0)
        {
            return false;
        }

        return self.RoomById.TryGetValue(roomId, out room) && room != null;
    }

    public static bool RemoveRoom(this RoomManagerEntity self, uint roomId, string? reason = null)
    {
        if (!self.RoomById.TryRemove(roomId, out var room) || room == null)
        {
            return false;
        }

        foreach (var userId in room.MemberUserIds.ToArray())
        {
            if (self.RoomIdByUserId.TryGetValue(userId, out var mappedRoomId) && mappedRoomId == roomId)
            {
                self.RoomIdByUserId.TryRemove(userId, out _);
            }
        }

        room.Close(reason);
        self.RoomIdGenerator.Release(roomId);

        // 房间与模拟器 1:1：房间销毁时一并停止并移除对应模拟器
        self.Scene?.GetComponent<SimManager>()?.Remove(roomId);

        return true;
    }

    public static bool ContainsRoom(this RoomManagerEntity self, uint roomId)
    {
        return self.RoomById.ContainsKey(roomId);
    }

    public static List<RoomEntity> GetRoomsSnapshot(this RoomManagerEntity self)
    {
        var list = new List<RoomEntity>(self.RoomById.Count);
        foreach (var pair in self.RoomById)
        {
            if (pair.Value != null)
            {
                list.Add(pair.Value);
            }
        }

        return list;
    }

    private static bool Join(RoomManagerEntity self, uint roomId, long userId)
    {
        if (!self.RoomById.TryGetValue(roomId, out var room) || room == null)
        {
            return false;
        }

        if (self.RoomIdByUserId.TryGetValue(userId, out var oldRoomId) && oldRoomId != roomId)
        {
            self.LeaveRoom(userId, reason: "switch_room");
        }

        if (!room.TryAddMember(userId))
        {
            return false;
        }

        self.RoomIdByUserId[userId] = roomId;
        return true;
    }

    private static void NotifyEmptyRoom(RoomManagerEntity self, uint roomId, string? reason)
    {
        var handler = self.EmptyRoomHandler;
        if (handler == null)
        {
            Log.Warning($"RoomManager EmptyRoom 无回调，回退 Remove: roomId={roomId}, reason={reason}");
            self.RemoveRoom(roomId, reason: reason ?? "empty");
            return;
        }

        try
        {
            handler(roomId, reason);
        }
        catch (Exception ex)
        {
            Log.Error($"RoomManager EmptyRoom 回调异常，回退 Remove: roomId={roomId}, reason={reason}, ex={ex}");
            self.RemoveRoom(roomId, reason: reason ?? "empty_handler_error");
        }
    }
}
