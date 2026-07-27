using Entity.VOs.room;

namespace Entity.Runtime.room;

public sealed class RoomFrameSyncEntity
{
    public RoomEntity Room = null!;
    public RoomFrameWindowEntity FrameWindow = null!;
    public long CurrentTickIndex = -1;
}
