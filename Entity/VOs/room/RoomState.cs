namespace Entity.VOs.room;

/// <summary>
/// 房间生命周期状态。
/// 合法迁移：
/// Created -&gt; Opened；
/// Opened -&gt; Closed。
/// </summary>
public enum RoomState
{
    /// <summary>创建态：房间对象已建立，尚未开启。</summary>
    Created = 0,

    /// <summary>开启态：已启动，可加人、运行中。</summary>
    Opened = 1,

    /// <summary>关闭态：已关闭销毁。</summary>
    Closed = 2,
}
