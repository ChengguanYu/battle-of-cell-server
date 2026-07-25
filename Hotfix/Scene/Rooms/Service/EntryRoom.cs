using Entity.DTOs;
using Fantasy;
using Fantasy.Async;
using Hotfix.Database;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 客户端进房：读 Redis 匹配结果后 Entry。成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> EntryRoom(long userId)
    {
        Log.Debug($"RoomsService.EntryRoom 开始: userId={userId}");
        if (userId <= 0)
        {
            Log.Debug($"RoomsService.EntryRoom 参数非法: userId={userId}");
            return InnerResult.Fail("userId 非法", userId);
        }

        if (_redis == null)
        {
            Log.Warning($"RoomsService.EntryRoom 失败：Redis 实例缺失, userId={userId}");
            return InnerResult.Fail("Redis 实例缺失", userId);
        }

        if (!MatchResultDao.TryFindByUserId(_redis, userId, out var matchedRoomId, out var findError))
        {
            Log.Warning($"RoomsService.EntryRoom 查匹配结果失败: userId={userId}, error={findError}");
            return InnerResult.Fail(findError, userId);
        }

        if (matchedRoomId <= 0 || matchedRoomId > uint.MaxValue)
        {
            Log.Warning($"RoomsService.EntryRoom 匹配 room_id 非法: userId={userId}, roomId={matchedRoomId}");
            return InnerResult.Fail("匹配 room_id 非法", userId, matchedRoomId);
        }

        var roomId = (uint)matchedRoomId;
        Log.Debug($"RoomsService.EntryRoom 命中匹配结果，继续 Entry: userId={userId}, roomId={roomId}");
        var entryResult = await Entry(userId, roomId);
        Log.Debug(
            $"RoomsService.EntryRoom 结束: userId={userId}, roomId={roomId}, ok={entryResult.IsSuccess}, reason={entryResult.Reason}");
        return entryResult;
    }
}
