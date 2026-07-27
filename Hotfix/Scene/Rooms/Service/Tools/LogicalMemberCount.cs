using Entity.VOs.room;
using Fantasy;
using Hotfix.Database;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 逻辑人数 = 房间实际人数 + 房间匹配占位人数。
    /// </summary>
    /// <remarks>
    /// 匹配人数来自 Redis 占位 key 计数；Entry 成功后会清理本人占位，避免与成员双计。
    /// </remarks>
    private bool TryGetLogicalMemberCount(RoomEntity room, out int logicalCount, out string error)
    {
        logicalCount = 0;
        error = string.Empty;

        if (room == null || room.RoomId == 0)
        {
            error = "房间无效";
            return false;
        }

        if (_redis == null)
        {
            error = "Redis 实例缺失";
            return false;
        }

        if (!MatchResultDao.TryCountPlaceholders(_redis, room.RoomId, out var matchCount, out error))
        {
            return false;
        }

        logicalCount = room.MemberCount + matchCount;
        return true;
    }
}
