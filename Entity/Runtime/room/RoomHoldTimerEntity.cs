using Entity.VOs.room;
using Fantasy;

namespace Entity.Runtime.room;

public sealed class RoomHoldTimerEntity
{
    public RoomEntity Room = null!;
    public Scene? TimerScene;
    public long TimerId;
    public int DelayMs;

    public bool IsScheduled => TimerId != 0;
}
