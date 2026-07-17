using Entity.DTOs;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;

namespace Hotfix.Scene.Rooms.Service;

/// <summary>
/// Rooms Scene 级服务（挂在 Scene 上，全 Handler 共享同一实例）。
/// </summary>
public sealed class RoomsService() : ServiceBase(), IRoomsService
{
    /// <summary>
    /// 处理玩家进入房间。
    /// 业务逻辑待实现，当前仅占位成功返回。
    /// </summary>
    public async FTask<InnerResult> Enter(long userId)
    {
        // TODO: 进入房间逻辑待实现
        await FTask.CompletedTask;
        return InnerResult.Ok();
    }
}
