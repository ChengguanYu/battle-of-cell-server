using Entity.DTOs;
using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Hotfix.Scene.Rooms.System;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 玩家离房。空房后的 Hold/Close 由状态机路径（EmptyRoom 回调）处理，不在此决策。
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

        if (!Manager.TryGetByUser(userId, out var room) || room == null)
        {
            Log.Info($"Rooms 离房跳过：玩家不在房间, userId={userId}, reason={reason}");
            return InnerResult.Fail("玩家不在房间", userId);
        }

        var roomId = room.RoomId;
        var memberCountBefore = room.MemberCount;
        var stateBefore = room.State;

        if (!Manager.LeaveRoom(userId, reason: reason))
        {
            Log.Warning(
                $"Rooms 离房失败: userId={userId}, roomId={roomId}, memberBefore={memberCountBefore}, state={stateBefore}, reason={reason}");
            return InnerResult.Fail("离房失败", userId, roomId);
        }

        var stillExists = Manager.ContainsRoom(roomId);
        var stateAfter = stillExists && Manager.TryGetById(roomId, out var after) && after != null
            ? after.State.ToString()
            : "Removed";
        Log.Info(
            $"Rooms 离房完成: userId={userId}, roomId={roomId}, memberBefore={memberCountBefore}, stateBefore={stateBefore}, stateAfter={stateAfter}, roomExists={stillExists}, reason={reason}");
        return InnerResult.Ok(string.Empty, roomId);
    }
}
