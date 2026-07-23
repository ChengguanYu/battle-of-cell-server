using Entity.DTOs;
using Fantasy.Async;
using Fantasy.Network;

namespace Hotfix.Scene.Gate.Service;

/// <summary>
/// Gate Scene 会话服务契约。
/// 实现类作为 Scene 级 Entity 组件挂载；Handler 经 <c>GetComponent&lt;SessionService&gt;()</c> 获取后按本接口调用。
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// 用户进入家园：校验用户、跨 Scene 加载玩家，成功后绑定在线会话。
    /// </summary>
    FTask<InnerResult> EntryHome(long userId, Session session);

    /// <summary>
    /// 发起匹配：通过内部 RPC 转发到 Avatars Scene。
    /// </summary>
    FTask<InnerResult> PlayerMatch(long userId);
    /// <summary>
    /// 主动退出房间：通过内部 RPC 转发到 Avatars Scene。
    /// </summary>
    FTask<InnerResult> PlayerLeaveRoom(long userId);

    /// <summary>
    /// 转发客户端帧到 Avatars Scene（单向，业务暂为日志骨架）。
    /// </summary>
    void ForwardClientFrame(long userId, ulong frameNumber, int framesCount);
}

