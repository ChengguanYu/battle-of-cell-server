namespace Entity.VOs.room;

/// <summary>
/// 房间生命周期状态。合法迁移：Created -&gt; Opened -&gt; Holding -&gt; Closed；
/// Opened 可直接 Closed；Holding 可回到 Opened。
/// </summary>
public enum RoomState
{
    Created = 0,
    Opened = 1,
    Closed = 2,
    Holding = 3,
}
