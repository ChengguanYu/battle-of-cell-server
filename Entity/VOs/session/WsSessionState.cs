namespace Entity.VOs.session;

/// <summary>
/// WsSession 生命周期状态。
/// 合法迁移：
/// New -&gt; Online；
/// Online -&gt; Kicked | TimedOut | Closed；
/// Kicked -&gt; Closed；
/// TimedOut -&gt; Closed。
/// </summary>
public enum WsSessionState
{
    /// <summary>刚创建，尚未绑定。</summary>
    New = 0,

    /// <summary>已绑定 userId 与框架 Session，可处理业务。</summary>
    Online = 1,

    /// <summary>被顶号/踢下线，等待清理。</summary>
    Kicked = 2,

    /// <summary>心跳超时未续，等待清理。</summary>
    TimedOut = 3,

    /// <summary>终态，不可再迁移。</summary>
    Closed = 4,
}
