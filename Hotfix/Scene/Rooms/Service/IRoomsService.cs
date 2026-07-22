using Entity.DTOs;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.Service;

/// <summary>
/// Rooms Scene 房间领域服务契约。
/// </summary>
public interface IRoomsService
{
    /// <summary>
    /// 旧进入链路：MatchOrCreate。成功时 Args[0] 为 roomId。
    /// </summary>
    FTask<InnerResult> Enter(long userId);

    /// <summary>
    /// 玩家离房检查：若不在房则忽略；若是最后一名成员则关闭房间。
    /// </summary>
    FTask Leave(long userId, string? reason);

    /// <summary>
    /// 房间列表快照（只读线索，非权威）。成功时 Args[0] 为 IReadOnlyList&lt;RoomSnapItem&gt; 或等价列表。
    /// </summary>
    FTask<InnerResult> GetRoomListSnap();

    /// <summary>
    /// 加入指定房间。成功时 Args[0] 为 roomId。
    /// </summary>
    FTask<InnerResult> Join(long userId, long roomId);

    /// <summary>
    /// 创建房间并加入首位成员。capacity &lt;= 0 时用默认容量。成功时 Args[0] 为 roomId。
    /// </summary>
    FTask<InnerResult> Create(long userId, int capacity);
}
