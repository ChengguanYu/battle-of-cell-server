using Entity.DTOs;
using Fantasy.Async;

namespace Hotfix.Scene.Match.Service;

/// <summary>
/// Match Scene 匹配领域服务契约。
/// </summary>
public interface IMatchService
{
    /// <summary>
    /// 旧匹配链路：转发到 Rooms.Enter（MatchOrCreate）。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    FTask<InnerResult> Match(long userId);

    /// <summary>
    /// 新匹配链路：GetRoomListSnap；无房 CreateAndEntry，有房随机 Join。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    FTask<InnerResult> NewMatch(long userId);
}
