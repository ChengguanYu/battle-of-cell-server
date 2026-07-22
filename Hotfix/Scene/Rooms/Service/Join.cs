using Entity.DTOs;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 加入指定房间。成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> Join(long userId, long roomId)
    {
        await FTask.CompletedTask;
        if (userId <= 0 || roomId <= 0)
        {
            return InnerResult.Fail("参数非法", userId, roomId);
        }

        // TODO: RoomManager 指定房加入
        return InnerResult.Fail("Join 未实现", userId, roomId);
    }
}
