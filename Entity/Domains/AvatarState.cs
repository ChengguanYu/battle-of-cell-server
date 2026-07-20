namespace Entity.Domains;

/// <summary>
/// Avatar 业务位置状态。
/// 合法迁移：
/// New -> Lobby；
/// Lobby -> InRoom；
/// InRoom -> Lobby。
/// </summary>
public enum AvatarState
{
    /// <summary>刚构造，尚未完成进入。</summary>
    New = 0,

    /// <summary>Entry 成功，位于大厅。</summary>
    Lobby = 1,

    /// <summary>已进入房间。</summary>
    InRoom = 2,
}
