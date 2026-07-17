using Entity.DTOs;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;

namespace Hotfix.Scene.Match.Service;

/// <summary>
/// Match Scene 级服务（挂在 Scene 上，全 Handler 共享同一实例）。
/// </summary>
public sealed class MatchService() : ServiceBase(), IMatchService
{
    /// <summary>
    /// 处理玩家进入匹配队列。
    /// 匹配逻辑待实现，当前仅占位成功返回。
    /// </summary>
    public async FTask<InnerResult> Match(long userId)
    {
        // TODO: 匹配逻辑待实现
        await FTask.CompletedTask;
        return InnerResult.Ok();
    }
}
