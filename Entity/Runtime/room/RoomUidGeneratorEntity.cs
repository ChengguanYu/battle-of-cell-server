namespace Entity.Runtime.room;

public sealed class RoomUidGeneratorEntity
{
    public const int UidSeqBits = 20;
    public const int UidSeqMask = (1 << UidSeqBits) - 1;

    public long LastUidMs;
    public int UidSeqInMs;
}
