using Entity.Config;

namespace Entity.VOs.room;

/// <summary>
/// 房间生命周期状态。合法迁移：Created -&gt; Opened -&gt; Closed。
/// </summary>
public enum RoomState
{
    Created = 0,
    Opened = 1,
    Closed = 2,
}

/// <summary>
/// 房间生命周期状态机。合法迁移：Created -&gt; Opened -&gt; Closed。
/// </summary>
public interface IRoomStateMachine
{
    RoomState State { get; }

    bool Open(uint roomId, int capacity = RoomConfig.DefaultCapacity);

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

    public static bool IsClosed(this IRoomStateMachine sm)
    {
        ArgumentNullException.ThrowIfNull(sm);
        return sm.State == RoomState.Closed;
    }
}
