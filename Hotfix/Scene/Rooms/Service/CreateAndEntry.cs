using Entity.DTOs;
using Fantasy;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 创建并进入（纯聚合 Create + Entry，无额外业务）。成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> CreateAndEntry(long userId)
    {
        Log.Debug($"RoomsService.CreateAndEntry 开始: userId={userId}");
        var createResult = await Create(userId);
        if (!createResult.IsSuccess)
        {
            Log.Debug($"RoomsService.CreateAndEntry Create 失败: userId={userId}, reason={createResult.Reason}");
            return createResult;
        }

        if (createResult.Args is not { Count: > 0 } || createResult.Args[0] is not long roomId || roomId <= 0)
        {
            Log.Debug($"RoomsService.CreateAndEntry Create 未返回有效 roomId: userId={userId}");
            return InnerResult.Fail("Create 未返回有效 roomId", userId);
        }

        Log.Debug($"RoomsService.CreateAndEntry Create 成功，继续 Entry: userId={userId}, roomId={roomId}");
        var entryResult = await Entry(userId, roomId);
        Log.Debug(
            $"RoomsService.CreateAndEntry 结束: userId={userId}, roomId={roomId}, ok={entryResult.IsSuccess}, reason={entryResult.Reason}");
        return entryResult;
    }
}
