using System.Collections.Concurrent;
using Entity.Config;
using Entity.Runtime.room;
using Entity.Utils;
using Entity.VOs.room;
using Fantasy;

namespace Entity.Managers;

/// <summary>
/// 进程级房间缓存。
/// 定义在 Entity 程序集，不随 Hotfix 热更卸载；仅进程退出时释放。
/// 索引：roomId 与 userId 双向关联；Room 经状态机迁移。
/// 仅提供 TimerScene 宿主；房间 tick / hold 计时由 Room 状态迁移自行启停。
/// 空房决策与 hold 超时由 Rooms Scene 绑定回调，Manager 只负责转发。
/// 写路径约定仅由 Rooms Scene 串行访问。
/// </summary>
public sealed class RoomManager
{
    private static readonly RoomManager _instance = new();
    public static RoomManager Instance => _instance;

    private readonly ConcurrentDictionary<uint, Room> _roomById = new();
    private readonly ConcurrentDictionary<long, uint> _roomIdByUserId = new();
    private readonly RecyclableUIntIdGenerator _roomIdGenerator = new();

    /// <summary>
    /// 房间私有 tick / hold 定时器宿主 Scene（通常为 Rooms Scene）。
    /// </summary>
    private Scene? _timerScene;

    /// <summary>
    /// 新建房间默认逻辑帧率（tick/秒）。
    /// </summary>
    private int _defaultTickRate = RoomTicker.DefaultTickRate;

    /// <summary>
    /// Holding 超时回调（由 Rooms 侧绑定；Entity 不感知 Match/Redis）。
    /// </summary>
    private Action<uint>? _holdTimeoutHandler;

    /// <summary>
    /// Opened 空房回调：由 Rooms 侧按占位决定 Hold 或 Close。
    /// 未绑定则回退为立即 Remove。
    /// </summary>
    private Action<uint, string?>? _emptyRoomHandler;

    private RoomManager()
    {
    }

    /// <summary>
    /// 绑定 Rooms Scene 作为各房间私有 tick / hold 的定时器宿主。
    /// 应在 Rooms Scene 创建时调用。
    /// </summary>
    public void SetTimerScene(Scene scene, int defaultTickRate = RoomTicker.DefaultTickRate)
    {
        ArgumentNullException.ThrowIfNull(scene);

        if (_timerScene != null && !ReferenceEquals(_timerScene, scene))
        {
            Log.Warning(
                $"RoomManager 覆盖 TimerScene: oldRuntimeId={_timerScene.RuntimeId}, newRuntimeId={scene.RuntimeId}");
        }

        _timerScene = scene;
        if (defaultTickRate > 0)
        {
            _defaultTickRate = defaultTickRate;
        }

        Log.Info(
            $"RoomManager 绑定 TimerScene: sceneId={scene.SceneConfigId}, runtimeId={scene.RuntimeId}, defaultTickRate={_defaultTickRate}, intervalMs={Math.Max(1, 1000 / _defaultTickRate)}");
    }

    /// <summary>
    /// 绑定 Holding 超时处理（通常为 RoomsService.OnHoldTimeout）。
    /// </summary>
    public void SetHoldTimeoutHandler(Action<uint>? handler)
    {
        _holdTimeoutHandler = handler;
        Log.Info($"RoomManager 绑定 HoldTimeoutHandler: bound={handler != null}");
    }

    /// <summary>
    /// 绑定 Opened 空房处理（通常为 RoomsService.OnRoomEmpty）。
    /// </summary>
    public void SetEmptyRoomHandler(Action<uint, string?>? handler)
    {
        _emptyRoomHandler = handler;
        Log.Info($"RoomManager 绑定 EmptyRoomHandler: bound={handler != null}");
    }

    /// <summary>
    /// 供 Room 状态迁移获取 tick/hold 宿主与默认帧率。
    /// </summary>
    public bool TryGetTimerHost(out Scene? scene, out int tickRate)
    {
        scene = _timerScene;
        tickRate = _defaultTickRate;
        return scene != null;
    }

    /// <summary>
    /// 由 <see cref="RoomHoldTimer"/> 触发，转发到 Rooms 侧回调。
    /// </summary>
    public void NotifyHoldTimeout(uint roomId)
    {
        var handler = _holdTimeoutHandler;
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

    /// <summary>
    /// 创建房间并开启。
    /// </summary>
    public Room? Create(int capacity = RoomConfig.DefaultCapacity)
    {
        if (!_roomIdGenerator.TryAcquire(out var roomId))
        {
            Log.Warning($"RoomManager.Create 失败：无法分配 roomId, capacity={capacity}");
            return null;
        }

        Log.Debug($"RoomManager.Create 开始: roomId={roomId}, capacity={capacity}");
        var room = new Room();
        if (!room.Open(roomId, capacity))
        {
            _roomIdGenerator.Release(roomId);
            Log.Debug($"RoomManager.Create Open 失败: roomId={roomId}, capacity={capacity}");
            return null;
        }

        _roomById[roomId] = room;
        Log.Debug($"RoomManager.Create 成功: roomId={roomId}, capacity={capacity}");
        return room;
    }

    /// <summary>
    /// 玩家进入指定房间。
    /// 成功返回房间；失败返回 null。
    /// </summary>
    public Room? Entry(uint roomId, long userId)
    {
        Log.Debug($"RoomManager.Entry 开始: roomId={roomId}, userId={userId}");
        if (userId <= 0 || roomId == 0)
        {
            Log.Debug($"RoomManager.Entry 参数非法: roomId={roomId}, userId={userId}");
            return null;
        }

        if (!Join(roomId, userId))
        {
            Log.Debug($"RoomManager.Entry Join 失败: roomId={roomId}, userId={userId}");
            return null;
        }

        if (!_roomById.TryGetValue(roomId, out var room) || room == null)
        {
            Log.Debug($"RoomManager.Entry 房间丢失: roomId={roomId}, userId={userId}");
            return null;
        }

        Log.Debug(
            $"RoomManager.Entry 成功: roomId={room.RoomId}, userId={userId}, memberCount={room.MemberCount}/{room.Capacity}, state={room.State}");
        return room;
    }

    /// <summary>
    /// 玩家加入房间。
    /// </summary>
    private bool Join(uint roomId, long userId)
    {
        if (!_roomById.TryGetValue(roomId, out var room) || room == null)
        {
            return false;
        }

        if (_roomIdByUserId.TryGetValue(userId, out var oldRoomId) && oldRoomId != roomId)
        {
            Leave(userId, reason: "switch_room");
        }

        if (!room.TryAddMember(userId))
        {
            return false;
        }

        _roomIdByUserId[userId] = roomId;
        return true;
    }

    /// <summary>
    /// 玩家离开当前房间。
    /// Opened 空房不在此直接关房，转交 EmptyRoomHandler 按占位走 Hold/Close。
    /// </summary>
    public bool Leave(long userId, string? reason = null)
    {
        if (!_roomIdByUserId.TryRemove(userId, out var roomId))
        {
            return false;
        }

        if (!_roomById.TryGetValue(roomId, out var room) || room == null)
        {
            return false;
        }

        if (!room.TryRemoveMember(userId))
        {
            return false;
        }

        if (room.MemberCount == 0 && room.IsOpened())
        {
            NotifyEmptyRoom(roomId, reason ?? "empty");
        }

        return true;
    }

    /// <summary>
    /// Opened 空房：转发到 Rooms 侧决策；无回调则立即 Remove。
    /// </summary>
    private void NotifyEmptyRoom(uint roomId, string? reason)
    {
        var handler = _emptyRoomHandler;
        if (handler == null)
        {
            Log.Warning($"RoomManager EmptyRoom 无回调，回退 Remove: roomId={roomId}, reason={reason}");
            Remove(roomId, reason: reason ?? "empty");
            return;
        }

        try
        {
            handler(roomId, reason);
        }
        catch (Exception ex)
        {
            Log.Error($"RoomManager EmptyRoom 回调异常，回退 Remove: roomId={roomId}, reason={reason}, ex={ex}");
            Remove(roomId, reason: reason ?? "empty_handler_error");
        }
    }

    /// <summary>
    /// Opened/Holding -&gt; Holding，挂/续 hold 计时。
    /// </summary>
    public bool Hold(uint roomId, int remainMs)
    {
        if (!_roomById.TryGetValue(roomId, out var room) || room == null)
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

    /// <summary>
    /// Holding -&gt; Opened，取消 hold 计时并恢复 tick。
    /// 非 Holding 视为幂等成功。
    /// </summary>
    public bool Resume(uint roomId)
    {
        if (!_roomById.TryGetValue(roomId, out var room) || room == null)
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

    /// <summary>
    /// 经 userId 取所在房间。
    /// </summary>
    public bool TryGetByUser(long userId, out Room? room)
    {
        room = null;
        if (!_roomIdByUserId.TryGetValue(userId, out var roomId))
        {
            return false;
        }

        return _roomById.TryGetValue(roomId, out room);
    }

    /// <summary>
    /// 经 roomId 取房间。
    /// </summary>
    public bool TryGetById(uint roomId, out Room? room)
    {
        room = null;
        if (roomId == 0)
        {
            return false;
        }

        return _roomById.TryGetValue(roomId, out room) && room != null;
    }

    /// <summary>
    /// 关闭并移除房间。
    /// </summary>
    public bool Remove(uint roomId, string? reason = null)
    {
        if (!_roomById.TryRemove(roomId, out var room) || room == null)
        {
            return false;
        }

        foreach (var userId in room.MemberUserIds)
        {
            if (_roomIdByUserId.TryGetValue(userId, out var mappedRoomId) && mappedRoomId == roomId)
            {
                _roomIdByUserId.TryRemove(userId, out _);
            }
        }

        room.Close(reason);
        _roomIdGenerator.Release(roomId);
        return true;
    }

    public bool Contains(uint roomId)
    {
        return _roomById.ContainsKey(roomId);
    }

    /// <summary>
    /// 当前管理房间的瞬时快照（只读线索，非 Join 权威）。
    /// 无房间时返回空列表，不返回 null。
    /// </summary>
    public List<Room> GetRoomsSnapshot()
    {
        var list = new List<Room>(_roomById.Count);
        foreach (var pair in _roomById)
        {
            if (pair.Value != null)
            {
                list.Add(pair.Value);
            }
        }

        return list;
    }
}
