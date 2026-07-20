namespace Entity.VOs.room;

/// <summary>
/// 房间生命周期状态。
/// 合法迁移：
/// New -> Waiting；
/// Waiting -> Ready | Closed；
/// Ready -> Fighting | Closed；
/// Fighting -> Settling | Closed；
/// Settling -> Closed。
/// </summary>
public enum RoomState
{
    /// <summary>刚创建，尚未进入可加入态。</summary>
    New = 0,

    /// <summary>等待玩家加入/补齐。</summary>
    Waiting = 1,

    /// <summary>人数已齐，等待开战。</summary>
    Ready = 2,

    /// <summary>对局进行中。</summary>
    Fighting = 3,

    /// <summary>对局结束，结算中。</summary>
    Settling = 4,

    /// <summary>终态，房间销毁。</summary>
    Closed = 5,
}