using Entity.Managers;
using Fantasy;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 房间列表快照（只读线索，非权威）。无房返回空列表，不返回 null。
    /// </summary>
    public async FTask<List<RoomSnapItem>> GetRoomListSnap()
    {
        await FTask.CompletedTask;

        var rooms = RoomManager.Instance.GetRoomsSnapshot();
        var snaps = new List<RoomSnapItem>(rooms.Count);
        foreach (var room in rooms)
        {
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
