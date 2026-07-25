using Entity.Config;

namespace Entity.VOs.room;

/// <summary>
/// 房间生命周期状态。合法迁移：Created -&gt; Opened -&gt; Holding -&gt; Closed；
/// Opened 可直接 Closed；Holding 可回到 Opened。
/// </summary>
public enum RoomState
{
    Created = 0,
    Opened = 1,
    Closed = 2,
    Holding = 3,
}

/// <summary>
/// 房间生命周期状态机。
/// </summary>
public interface IRoomStateMachine
{
    RoomState State { get; }

    bool Open(uint roomId, int capacity = RoomConfig.DefaultCapacity);

    /// <summary>Opened -&gt; Holding，或 Holding 续命。</summary>
    bool Hold(int remainMs);

    /// <summary>Holding -&gt; Opened。</summary>
    bool Resume();

    bool Close(string? reason = null);
}

public static class RoomStateMachineExtensions
{
    public static bool IsCreated(this IRoomStateMachine sm)
    {
        ArgumentNullException.ThrowIfNull(sm);
        return sm.State == RoomState.Created;
    }

    public static bool IsOpened(this IRoomStateMachine sm)
    {
        ArgumentNullException.ThrowIfNull(sm);
        return sm.State == RoomState.Opened;
    }

    public static bool IsHolding(this IRoomStateMachine sm)
    {
        ArgumentNullException.ThrowIfNull(sm);
        return sm.State == RoomState.Holding;
    }

    public static bool IsClosed(this IRoomStateMachine sm)
    {
        ArgumentNullException.ThrowIfNull(sm);
        return sm.State == RoomState.Closed;
    }
}
