using Entity.DTOs;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.Service;

/// <summary>
/// Rooms Scene 房间领域服务契约。
/// 实现类作为 Scene 级 Entity 组件挂载；Handler 经 <c>GetComponent&lt;RoomsService&gt;()</c> 获取后按本接口调用。
/// </summary>
public interface IRoomsService
{
    /// <summary>
    /// 玩家进入房间。
    /// </summary>
    FTask<InnerResult> Enter(long userId);
}
