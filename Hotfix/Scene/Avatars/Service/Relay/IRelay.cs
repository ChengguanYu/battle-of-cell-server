using System.Collections.Generic;
using Entity.DTOs;
using Fantasy;
using Fantasy.Async;

namespace Hotfix.Scene.Avatars.Service;

/// <summary>
/// 跨 Scene 转发契约（门禁后转发，不含本域加载/清理）。
/// 实现位于 Service/Relay。
/// </summary>
public interface IRelay
{
    /// <summary>
    /// 匹配转发到 Match。成功时 Args[0] 为 roomId。
    /// </summary>
    FTask<InnerResult> Match(long userId);

    /// <summary>
    /// 客户端进房转发到 Rooms。成功时 Args[0] 为 roomId。
    /// </summary>
    FTask<InnerResult> EntryRoom(long userId);

    /// <summary>
    /// 退出房间转发到 Rooms。成功时 Args[0] 为 roomId。
    /// </summary>
    FTask<InnerResult> LeaveRoom(long userId);

    /// <summary>
    /// 客户端帧转发到 Rooms（单向）。
    /// </summary>
    void ClientFrame(long userId, ulong frameNumber, List<Frame>? frames);
}
