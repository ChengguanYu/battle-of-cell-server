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
    /// 玩家进入房间（已在房返回原房间；否则加入 Waiting 未满房或创建）。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    FTask<InnerResult> Enter(long userId);

    /// <summary>
    /// 玩家离房检查：若不在房则忽略；若是最后一名成员则关闭房间。
    /// </summary>
    FTask Leave(long userId, string? reason);
}
