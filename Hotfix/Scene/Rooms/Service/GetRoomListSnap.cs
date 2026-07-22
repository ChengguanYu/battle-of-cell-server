using Entity.DTOs;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 房间列表快照（只读线索，非权威）。
    /// </summary>
    public async FTask<InnerResult> GetRoomListSnap()
    {
        await FTask.CompletedTask;
        // TODO: 扫描 Opened 未满房，组装 RoomSnapItem 列表
        return InnerResult.Fail("GetRoomListSnap 未实现");
    }
}
