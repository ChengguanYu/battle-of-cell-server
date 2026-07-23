using Entity.Managers;
using Fantasy;
using Fantasy.Async;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 接收客户端帧并写入房间帧窗口。
    /// </summary>
    public async FTask OnClientFrame(long userId, ulong frameNumber, List<frame>? frames)
    {
        await FTask.CompletedTask;

        if (userId <= 0)
        {
            Log.Warning($"[Rooms] client_frame 忽略：userId 非法, userId={userId}, frame={frameNumber}");
            return;
        }

        if (!RoomManager.Instance.TryGetByUser(userId, out var room) || room == null)
        {
            Log.Warning(
                $"[Rooms] client_frame 跳过：玩家不在房间, userId={userId}, frame={frameNumber}, ops={frames?.Count ?? 0}");
            return;
        }

        if (!room.TryAppendClientOps(frameNumber, frames, out var error))
        {
            Log.Warning(
                $"[Rooms] client_frame 入窗失败: userId={userId}, roomId={room.RoomId}, frame={frameNumber}, ops={frames?.Count ?? 0}, state={room.State}, error={error}");
        }
    }
}
