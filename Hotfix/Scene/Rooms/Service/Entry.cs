using Entity.DTOs;
using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;

namespace Hotfix.Scene.Rooms.Service;

/// <summary>
/// Rooms Scene 级服务（挂在 Scene 上，全 Handler 共享同一实例）。
/// 方法按 partial 文件拆分：Entry/Leave/Join/Create/GetRoomListSnap。
/// </summary>
public sealed partial class RoomsService : ServiceBase
{
    /// <summary>
    /// 进入指定房间。成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> Entry(long userId, uint roomId)
    {
        await FTask.CompletedTask;
        Log.Debug($"RoomsService.Entry 开始: userId={userId}, roomId={roomId}");

        if (userId <= 0 || roomId == 0)
        {
            Log.Debug($"RoomsService.Entry 参数非法: userId={userId}, roomId={roomId}");
            return InnerResult.Fail("参数非法", userId, roomId);
        }

        var room = RoomManager.Instance.Entry(roomId, userId);
        if (room == null)
        {
            Log.Warning($"玩家 {userId} Entry 房间 {roomId} 失败：无法加入");
            return InnerResult.Fail("Entry 失败：无法加入", userId, roomId);
        }

        Log.Info(
            $"玩家 {userId} Entry 房间成功: roomId={room.RoomId}, memberCount={room.MemberCount}/{room.Capacity}, state={room.State}");
        return InnerResult.Ok(string.Empty, room.RoomId);
    }
}
