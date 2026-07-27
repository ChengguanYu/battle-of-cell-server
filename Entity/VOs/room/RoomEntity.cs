using Entity.Config;
using Entity.Managers;
using Entity.Runtime.room;

namespace Entity.VOs.room;

public sealed class RoomEntity
{
    public RoomManagerEntity Manager = null!;
    public readonly HashSet<long> MemberUserIds = new();
    public RoomTickerEntity Ticker = null!;
    public RoomHoldTimerEntity HoldTimer = null!;
    public RoomFrameSyncEntity FrameSync = null!;
    public readonly RoomUidGeneratorEntity UidGenerator = new();

    public RoomState State = RoomState.Created;
    public uint RoomId;
    public int Capacity = RoomConfig.DefaultCapacity;
    public long CreatedAtUnixMs;
    public long UpdatedAtUnixMs;

    public int MemberCount => MemberUserIds.Count;
    public bool IsFull => MemberUserIds.Count >= Capacity;
}
