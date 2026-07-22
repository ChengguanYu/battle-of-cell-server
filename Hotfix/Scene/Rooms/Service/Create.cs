using Entity.DTOs;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 创建房间并加入首位成员。capacity &lt;= 0 时用默认容量。成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> Create(long userId, int capacity)
    {
        await FTask.CompletedTask;
        if (userId <= 0)
        {
            return InnerResult.Fail("userId 非法", userId);
        }

        // TODO: RoomManager 创建并加入
        return InnerResult.Fail("Create 未实现", userId, capacity);
    }
}
