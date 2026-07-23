using Entity.DTOs;
using Fantasy;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 加入指定房间。成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> Join(long userId, uint roomId)
    {
        return await Entry(userId, roomId);
    }
}
