using Entity.Managers;
using Fantasy;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 接收客户端帧（业务暂为日志骨架）。
    /// </summary>
    public async FTask OnClientFrame(long userId, ulong frameNumber, int framesCount)
    {
        await FTask.CompletedTask;

        if (userId <= 0)
        {
            Log.Warning($"[Rooms] client_frame 忽略：userId 非法, userId={userId}, frame={frameNumber}");
            return;
        }

        if (!RoomManager.Instance.TryGetByUser(userId, out var room) || room == null)
        {
            Log.Info(
                $"[Rooms] client_frame 跳过：玩家不在房间, userId={userId}, frame={frameNumber}, ops={framesCount}");
            return;
        }

        // TODO: 后续将 frames 合并进房间当前逻辑帧缓冲，再由 RoomFrameSync 延迟广播
        Log.Info(
            $"[Rooms] client_frame 收到: userId={userId}, roomId={room.RoomId}, frame={frameNumber}, ops={framesCount}, memberCount={room.MemberCount}/{room.Capacity}, state={room.State}");
    }
}