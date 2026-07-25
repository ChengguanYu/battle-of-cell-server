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
    /// 客户端匹配：选房/无房创建但不入房，成功后写 Redis。
    /// 房间可加入性由 Rooms.GetRoomListSnap 负责。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    FTask<InnerResult> ClientMatch(long userId, Fantasy.MatchType matchType);
}
