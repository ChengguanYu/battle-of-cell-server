using System.Collections.Concurrent;
using Entity.Runtime.room;
using Entity.Utils;
using Entity.VOs.room;
using Fantasy;

namespace Entity.Managers;

public sealed class RoomManagerEntity : Fantasy.Entitas.Entity
{
    public readonly ConcurrentDictionary<uint, RoomEntity> RoomById = new();
    public readonly ConcurrentDictionary<long, uint> RoomIdByUserId = new();
    public readonly RecyclableUIntIdGeneratorEntity RoomIdGenerator = new();

    public Scene? TimerScene;
    public int DefaultTickRate = RoomTickerEntity.DefaultTickRate;
    public Action<uint>? HoldTimeoutHandler;
    public Action<uint, string?>? EmptyRoomHandler;
}
