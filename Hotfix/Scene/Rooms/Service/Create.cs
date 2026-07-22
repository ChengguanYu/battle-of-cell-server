using Entity.DTOs;
using Entity.Managers;
using Fantasy;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 创建房间并开启。成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> Create(long userId)
    {
        await FTask.CompletedTask;
        if (userId <= 0)
        {
            return InnerResult.Fail("userId 非法", userId);
        }

        var room = RoomManager.Instance.Create();
        if (room == null)
        {
            Log.Warning($"玩家 {userId} Create 房间失败：无法创建");
            return InnerResult.Fail("Create 失败：无法创建", userId);
        }

        Log.Info(
            $"玩家 {userId} Create 房间成功: roomId={room.RoomId}, memberCount={room.MemberCount}/{room.Capacity}, state={room.State}");
        return InnerResult.Ok(string.Empty, room.RoomId);
    }
}
