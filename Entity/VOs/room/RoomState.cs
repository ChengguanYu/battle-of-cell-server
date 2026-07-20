namespace Entity.VOs.room;

/// <summary>
/// 房间生命周期状态。
/// 合法迁移：
/// New -&gt; Active；
/// Active -&gt; Closed。
/// </summary>
public enum RoomState
{
    /// <summary>初始态：刚创建，尚未建房完成。</summary>
    New = 0,

    /// <summary>活跃态：可加人、运行中。</summary>
    Active = 1,

    /// <summary>死亡态：已关闭销毁。</summary>
    Closed = 2,
}
