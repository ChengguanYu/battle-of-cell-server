using Entity.DTOs;
using Fantasy;
using Fantasy.Async;

namespace Hotfix.Scene.Match.Service;

/// <summary>
/// Match Scene 匹配领域服务契约。
/// </summary>
public interface IMatchService
{
    /// <summary>
    /// 匹配：GetRoomListSnap（Rooms 已过滤逻辑已满房）；无房 CreateAndEntry，有房随机 Join。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    FTask<InnerResult> Match(long userId);

    /// <summary>
    /// 客户端匹配：选房/无房创建但不入房，成功后写 Redis。
    /// 房间可加入性由 Rooms.GetRoomListSnap 负责。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    FTask<InnerResult> ClientMatch(long userId, Fantasy.MatchType matchType);
}
