using System.Collections.Generic;
using Entity.DTOs;
using Fantasy;
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
    /// 代发匹配：Avatar 仅转发到 Match Scene，不做匹配业务校验。
    /// </summary>
    FTask<InnerResult> Match(long userId);

    /// <summary>
    /// 主动退出房间：仅 InRoom 可退出；Rooms 确认后再迁回 Lobby。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    FTask<InnerResult> LeaveRoom(long userId);

    /// <summary>
    /// 清理玩家：由 Gate 在 WsSession 清理后通知。
    /// </summary>
    FTask CleanupPlayer(long userId, string? reason);

    /// <summary>
    /// 转发客户端帧到 Rooms Scene（单向）。
    /// </summary>
    void ForwardClientFrame(long userId, ulong frameNumber, List<frame>? frames);
}
