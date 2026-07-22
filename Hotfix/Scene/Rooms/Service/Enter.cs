using Entity.DTOs;
using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;

namespace Hotfix.Scene.Rooms.Service;

/// <summary>
/// Rooms Scene 级服务（挂在 Scene 上，全 Handler 共享同一实例）。
/// 方法按 partial 文件拆分：Enter/Leave/Join/Create/GetRoomListSnap。
/// </summary>
public sealed partial class RoomsService : ServiceBase
{
    /// <summary>
    /// 旧进入链路：MatchOrCreate。成功时 Args[0] 为 roomId。
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

        Log.Info(
            $"玩家 {userId} 进入房间成功: roomId={room.RoomId}, memberCount={room.MemberCount}/{room.Capacity}, state={room.State}");
        return InnerResult.Ok(string.Empty, room.RoomId);
    }
}
