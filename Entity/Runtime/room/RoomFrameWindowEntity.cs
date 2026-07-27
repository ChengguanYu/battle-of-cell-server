using Fantasy;

namespace Entity.Runtime.room;

public sealed class RoomFrameWindowEntity
{
    public Slot[] Slots = [];
    public int Capacity;

    public struct Slot
    {
        public ServerFrame Frame;
        public bool Occupied;
        public bool Clearable;
    }
}
