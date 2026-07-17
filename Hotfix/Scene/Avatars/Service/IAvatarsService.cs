using Entity.DTOs;
using Fantasy.Async;

namespace Hotfix.Scene.Avatars.Service;

/// <summary>
/// Avatars Scene 玩家领域服务契约。
/// 实现类作为 Scene 级 Entity 组件挂载；Handler 经 <c>GetComponent&lt;AvatarsService&gt;()</c> 获取后按本接口调用。
/// </summary>
public interface IAvatarsService
{
    /// <summary>
    /// 将用户对应的 Avatar 加载到内存领域。
    /// </summary>
    FTask<InnerResult> LoadPlayer(long userId);

    /// <summary>
    /// 代发匹配：由 Avatar 校验玩家状态后，内部 RPC 到 Match Scene。
    /// </summary>
    FTask<InnerResult> Match(long userId);
}
