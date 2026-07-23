using Entity.DTOs;
using Entity.Managers;
using Fantasy;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 玩家离房：最后一名成员则关房，否则仅离开。
    /// 成功时 Args[0] 为离开前的 roomId。
    /// </summary>
    public async FTask<InnerResult> Leave(long userId, string? reason)
    {
        await FTask.CompletedTask;

        if (userId <= 0)
        {
            Log.Warning($"Rooms 离房忽略：userId 非法, userId={userId}, reason={reason}");
            return InnerResult.Fail("userId 非法", userId);
        }

        if (!RoomManager.Instance.TryGetByUser(userId, out var room) || room == null)
        {
            Log.Info($"Rooms 离房跳过：玩家不在房间, userId={userId}, reason={reason}");
            return InnerResult.Fail("玩家不在房间", userId);
        }

        var roomId = room.RoomId;
        var memberCountBefore = room.MemberCount;
        var stateBefore = room.State;

        if (memberCountBefore <= 1)
        {
            if (!RoomManager.Instance.Remove(roomId, reason: reason ?? "last_player_left"))
            {
                Log.Warning($"Rooms 关房失败: userId={userId}, roomId={roomId}, reason={reason}");
                return InnerResult.Fail("关房失败", userId, roomId);
            }

            Log.Info(
                $"Rooms 最后玩家离房并关房: userId={userId}, roomId={roomId}, state={stateBefore}, reason={reason}");
            return InnerResult.Ok(string.Empty, roomId);
        }

        if (!RoomManager.Instance.Leave(userId))
        {
            Log.Warning(
                $"Rooms 离房失败: userId={userId}, roomId={roomId}, memberBefore={memberCountBefore}, state={stateBefore}, reason={reason}");
            return InnerResult.Fail("离房失败", userId, roomId);
        }

        var stillExists = RoomManager.Instance.Contains(roomId);
        Log.Info(
            $"Rooms 离房完成: userId={userId}, roomId={roomId}, memberBefore={memberCountBefore}, roomClosed={!stillExists}, reason={reason}");
        return InnerResult.Ok(string.Empty, roomId);
    }
}
