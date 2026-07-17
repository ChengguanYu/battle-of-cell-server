namespace Hotfix.Utils;

/// <summary>
/// 网络协议响应错误码。
/// 各模块分段分配以防冲突：
/// - Gate / 通用: 1-999
/// - Players: 1000-1999
/// - Battle: 2000-2999
/// 0 固定为成功（框架 IResponse 约定），不在此定义。
/// </summary>
public enum StatusCode : uint
{
    Ok = 0,
    // ===== Gate / 通用 (1-999) =====
    TokenInvalid = 1,
    SessionEntryFailed = 2,
    NotAuthenticated = 3,

    // ===== Players (1000-1999) =====
    PlayerNotFound = 1000,
    LoadPlayerFailed = 1001,
    MatchFailed = 1002,
    RoomsEnterFailed = 1003,
}
