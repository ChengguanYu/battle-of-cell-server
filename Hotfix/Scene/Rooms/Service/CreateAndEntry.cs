using Entity.DTOs;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 创建并进入（纯聚合 Create + Enter，无额外业务）。成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> CreateAndEntry(long userId)
    {
        await FTask.CompletedTask;
        if (userId <= 0)
        {
            return InnerResult.Fail("userId 非法", userId);
        }

        // TODO: 聚合 Create + Enter
        return InnerResult.Fail("CreateAndEntry 未实现", userId);
    }
}
