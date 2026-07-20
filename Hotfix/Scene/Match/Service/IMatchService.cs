using Entity.DTOs;
using Fantasy.Async;

namespace Hotfix.Scene.Match.Service;

/// <summary>
/// Match Scene 匹配领域服务契约。
/// 实现类作为 Scene 级 Entity 组件挂载；Handler 经 <c>GetComponent&lt;MatchService&gt;()</c> 获取后按本接口调用。
/// </summary>
public interface IMatchService
{
    /// <summary>
    /// 将玩家加入匹配，并转发到 Rooms 入房；已在房由 Rooms 处理。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    FTask<InnerResult> Match(long userId);
}
