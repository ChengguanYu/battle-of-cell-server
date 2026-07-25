using Entity.Managers;
using Entity.VOs.room;
using Fantasy;
using Fantasy.Async;

namespace Entity.Runtime.room;

/// <summary>
/// 房间 Holding 延时关房计时器。
/// 与 <see cref="RoomTicker"/> 同模式：宿主 TimerScene + 独立 timerId；once/deadline 语义。
/// 不推帧、不删 Redis、不 Remove 房间；超时仅通知 RoomManager。
/// </summary>
public sealed class RoomHoldTimer
{
    private readonly Room _room;

    private Scene? _timerScene;
    private long _timerId;
    private int _delayMs;

    public RoomHoldTimer(Room room)
    {
        _room = room ?? throw new ArgumentNullException(nameof(room));
    }

    public bool IsScheduled => _timerId != 0;

    public int DelayMs => _delayMs;

    /// <summary>
    /// 按 remainMs 挂一次计时；重复调用会先取消旧计时再重挂（续命）。
    /// </summary>
    public bool Schedule(int delayMs)
    {
        if (delayMs <= 0)
        {
            Log.Warning($"RoomHoldTimer 启动失败：delayMs 非法, roomId={_room.RoomId}, delayMs={delayMs}");
            return false;
        }

        if (!RoomManager.Instance.TryGetTimerHost(out var timerScene, out _) || timerScene == null)
        {
            Log.Warning($"RoomHoldTimer 启动失败：未绑定 TimerScene, roomId={_room.RoomId}");
            return false;
        }

        Cancel();

        _timerScene = timerScene;
        _delayMs = delayMs;
        _timerId = FTask.OnceTimer(timerScene, delayMs, OnTimer);

        if (_timerId == 0)
        {
            _timerScene = null;
            _delayMs = 0;
            Log.Warning($"RoomHoldTimer 启动失败：OnceTimer 返回 0, roomId={_room.RoomId}, delayMs={delayMs}");
            return false;
        }

        Log.Info(
            $"RoomHoldTimer 启动: roomId={_room.RoomId}, delayMs={_delayMs}, timerId={_timerId}");
        return true;
    }

    /// <summary>
    /// 取消 hold 计时。可重复调用。
    /// </summary>
    public void Cancel()
    {
        if (_timerId == 0)
        {
            _timerScene = null;
            _delayMs = 0;
            return;
        }

        var scene = _timerScene;
        if (scene != null)
        {
            FTask.RemoveTimer(scene, ref _timerId);
        }
        else
        {
            _timerId = 0;
        }

        _timerScene = null;
        _delayMs = 0;
        Log.Info($"RoomHoldTimer 取消: roomId={_room.RoomId}");
    }

    private void OnTimer()
    {
        // OnceTimer 触发后句柄失效，本地清零避免重复 Remove。
        _timerId = 0;
        _timerScene = null;
        var delayMs = _delayMs;
        _delayMs = 0;

        if (!_room.IsHolding())
        {
            Log.Debug(
                $"RoomHoldTimer 超时忽略：房间非 Holding, roomId={_room.RoomId}, state={_room.State}");
            return;
        }

        Log.Info($"RoomHoldTimer 超时: roomId={_room.RoomId}, delayMs={delayMs}");
        RoomManager.Instance.NotifyHoldTimeout(_room.RoomId);
    }
}
