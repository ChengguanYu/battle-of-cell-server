using Fantasy;
using Fantasy.Async;
using Entity.Managers;
using Entity.VOs.room;

namespace Entity.Runtime.room;

/// <summary>
/// 房间私有 tick 运行时。
/// 持有 timerId / Scene / 帧率等非业务状态；由 Room 状态迁移启停。
/// 启动时自行从 RoomManager 取 TimerScene 宿主。
/// </summary>
public sealed class RoomTicker
{
    /// <summary>默认逻辑帧率（tick/秒）。周期 = max(1, 1000 / TickRate) ms。</summary>
    public const int DefaultTickRate = 20;

    private readonly Room _room;

    private Scene? _timerScene;
    private int _tickRate = DefaultTickRate;
    private int _intervalMs;
    private long _timerId;
    private long _tickIndex;

    public RoomTicker(Room room)
    {
        _room = room ?? throw new ArgumentNullException(nameof(room));
    }

    public long TickIndex => _tickIndex;

    public int TickRate => _tickRate;

    public int IntervalMs => _intervalMs;

    public bool IsRunning => _timerId != 0;

    /// <summary>
    /// 启动私有 tick。应在 Room 进入 Opened 后调用。
    /// 从 RoomManager 取 TimerScene 与默认帧率。
    /// </summary>
    public bool Start()
    {
        if (!_room.IsOpened())
        {
            Log.Warning($"RoomTicker 启动失败：房间非 Opened, state={_room.State}, roomId={_room.RoomId}");
            return false;
        }

        if (!RoomManager.Instance.TryGetTimerHost(out var timerScene, out var tickRate) || timerScene == null)
        {
            Log.Warning($"RoomTicker 启动失败：未绑定 TimerScene, roomId={_room.RoomId}");
            return false;
        }

        if (tickRate <= 0)
        {
            tickRate = DefaultTickRate;
        }

        Stop();

        _timerScene = timerScene;
        _tickRate = tickRate;
        _intervalMs = Math.Max(1, 1000 / tickRate);
        _tickIndex = -1;
        _timerId = FTask.RepeatedTimer(timerScene, _intervalMs, OnTimer);

        Log.Info(
            $"RoomTicker 启动: roomId={_room.RoomId}, tickRate={_tickRate}, intervalMs={_intervalMs}, timerId={_timerId}");
        return _timerId != 0;
    }

    /// <summary>
    /// 停止私有 tick。可重复调用。
    /// </summary>
    public void Stop()
    {
        if (_timerId == 0)
        {
            _timerScene = null;
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
        Log.Info($"RoomTicker 停止: roomId={_room.RoomId}, lastTickIndex={_tickIndex}");
    }

    private void OnTimer()
    {
        if (!_room.IsOpened())
        {
            Stop();
            return;
        }

        _tickIndex++;
        _room.OnTick(_tickIndex);
    }
}
