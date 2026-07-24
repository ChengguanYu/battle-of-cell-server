using Entity.DTOs;
using Fantasy.Async;

namespace Hotfix.Scene.Avatars.Service;

/// <summary>
/// Avatars Scene 本域服务契约（加载/清理）。跨 Scene 转发见 Service.Relay。
/// </summary>
public interface IAvatarsService
{
    /// <summary>
    /// 将用户对应的 Avatar 加载到内存领域。
    /// </summary>
    FTask<InnerResult> LoadPlayer(long userId);

    /// <summary>
    /// 清理玩家：由 Gate 在 WsSession 清理后通知。
    /// </summary>
    FTask CleanupPlayer(long userId, string? reason);
}
