using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Hotfix.Scene.Rooms.System;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 可加入房间列表快照（只读线索，非权威）。
    /// 仅返回逻辑人数未满的房间；member_count 仍为真实成员数。
    /// </summary>
    public async FTask<List<RoomSnapItem>> GetRoomListSnap()
    {
        await FTask.CompletedTask;

        var rooms = Manager.GetRoomsSnapshot();
        var snaps = new List<RoomSnapItem>(rooms.Count);

        foreach (var room in rooms)
        {
            if (room == null || room.RoomId == 0 || room.Capacity <= 0)
            {
                continue;
            }

            if (!TryGetLogicalMemberCount(room, out var logicalCount, out var countError)
                || logicalCount >= room.Capacity)
            {
                Log.Debug(
                    $"GetRoomListSnap 逻辑人数已满跳过房间: roomId={room.RoomId}, memberCount={room.MemberCount}, logicalCount={logicalCount}, capacity={room.Capacity}, error={countError}");
                continue;
            }

            var item = RoomSnapItem.Create(autoReturn: false);
            item.room_id = room.RoomId;
            item.member_count = room.MemberCount;
            item.capacity = room.Capacity;
            item.state = (int)room.State;
            snaps.Add(item);
        }

        return snaps;
    }
}
