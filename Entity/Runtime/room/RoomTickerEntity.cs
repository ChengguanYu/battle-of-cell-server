using Entity.VOs.room;
using Fantasy;

namespace Entity.Runtime.room;

public sealed class RoomTickerEntity
{
    public const int DefaultTickRate = 1;

    public RoomEntity Room = null!;
    public Scene? TimerScene;
    public int TickRate = DefaultTickRate;
    public int IntervalMs;
    public long TimerId;
    public long TickIndex;

    public bool IsRunning => TimerId != 0;
}
