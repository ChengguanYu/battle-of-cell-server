using Entity.DTOs;
using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;

namespace Hotfix.Scene.Rooms.Service;

/// <summary>
/// Rooms Scene 级服务（挂在 Scene 上，全 Handler 共享同一实例）。
/// 房间创建/加入/索引由本服务经 RoomManager 管理。
/// </summary>
public sealed class RoomsService() : ServiceBase(), IRoomsService
{
    /// <summary>
    /// 玩家进入房间：已在房则返回原房间；否则加入 Opened 未满房或创建新房。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> Enter(long userId)
    {
        await FTask.CompletedTask;

        if (userId <= 0)
        {
            return InnerResult.Fail("userId 非法", userId);
        }

        var room = RoomManager.Instance.MatchOrCreate(userId);
        if (room == null)
        {
            Log.Warning($"玩家 {userId} 进入房间失败：无法创建或加入房间");
            return InnerResult.Fail("进入房间失败：无法创建或加入房间", userId);
        }

        Log.Info($"玩家 {userId} 进入房间成功: roomId={room.RoomId}, memberCount={room.MemberCount}/{room.Capacity}, state={room.State}");
        return InnerResult.Ok(string.Empty, room.RoomId);
    }

    /// <summary>
    /// 玩家离房检查：不在房则忽略；若是最后一名成员则关闭房间，否则仅离开。
    /// </summary>
    public async FTask Leave(long userId, string? reason)
    {
        await FTask.CompletedTask;

        if (userId <= 0)
        {
            Log.Warning($"Rooms 离房忽略：userId 非法, userId={userId}, reason={reason}");
            return;
        }

        if (!RoomManager.Instance.TryGetByUser(userId, out var room) || room == null)
        {
            Log.Info($"Rooms 离房跳过：玩家不在房间, userId={userId}, reason={reason}");
            return;
        }

        var roomId = room.RoomId;
        var memberCountBefore = room.MemberCount;
        var stateBefore = room.State;

        // 最后一名玩家：直接关房
        if (memberCountBefore <= 1)
        {
            if (!RoomManager.Instance.Remove(roomId, reason: reason ?? "last_player_left"))
            {
                Log.Warning($"Rooms 关房失败: userId={userId}, roomId={roomId}, reason={reason}");
                return;
            }

            Log.Info(
                $"Rooms 最后玩家离房并关房: userId={userId}, roomId={roomId}, state={stateBefore}, reason={reason}");
            return;
        }

        if (!RoomManager.Instance.Leave(userId))
        {
            Log.Warning(
                $"Rooms 离房失败: userId={userId}, roomId={roomId}, memberBefore={memberCountBefore}, state={stateBefore}, reason={reason}");
            return;
        }

        var stillExists = RoomManager.Instance.Contains(roomId);
        Log.Info(
            $"Rooms 离房完成: userId={userId}, roomId={roomId}, memberBefore={memberCountBefore}, roomClosed={!stillExists}, reason={reason}");
    }

    public async FTask<InnerResult> GetRoomListSnap()
    {
        await FTask.CompletedTask;
        // TODO: 扫描 Opened 未满房，组装 RoomSnapItem 列表
        return InnerResult.Fail("GetRoomListSnap 未实现");
    }

    public async FTask<InnerResult> Join(long userId, long roomId)
    {
        await FTask.CompletedTask;
        if (userId <= 0 || roomId <= 0)
        {
            return InnerResult.Fail("参数非法", userId, roomId);
        }

        // TODO: RoomManager 指定房加入
        return InnerResult.Fail("Join 未实现", userId, roomId);
    }

    public async FTask<InnerResult> Create(long userId, int capacity)
    {
        await FTask.CompletedTask;
        if (userId <= 0)
        {
            return InnerResult.Fail("userId 非法", userId);
        }

        // TODO: RoomManager 创建并加入
        return InnerResult.Fail("Create 未实现", userId, capacity);
    }
}
