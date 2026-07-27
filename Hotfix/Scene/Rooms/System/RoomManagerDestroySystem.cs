using Entity.Managers;
using Entity.Runtime.room;
using Fantasy;
using Fantasy.Entitas.Interface;

namespace Hotfix.Scene.Rooms.System;

public sealed class RoomManagerDestroySystem : DestroySystem<RoomManagerEntity>
{
    protected override void Destroy(RoomManagerEntity self)
    {
        foreach (var roomId in self.RoomById.Keys.ToArray())
        {
            self.RemoveRoom(roomId, "rooms_scene_destroy");
        }

        self.RoomById.Clear();
        self.RoomIdByUserId.Clear();
        self.RoomIdGenerator.Occupied.Clear();
        self.RoomIdGenerator.Free.Clear();
        self.RoomIdGenerator.NextId = self.RoomIdGenerator.MinInclusive;
        self.TimerScene = null;
        self.DefaultTickRate = RoomTickerEntity.DefaultTickRate;
        self.HoldTimeoutHandler = null;
        self.EmptyRoomHandler = null;
    }
}
