using Entity.DTOs;
using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;
using Hotfix.Database;
using Hotfix.Scene.Rooms.System;

namespace Hotfix.Scene.Rooms.Service;
//TODO: 不要到处写 redis == null, 改为抛出错误
/// <summary>
/// Rooms Scene 级服务（挂在 Scene 上，全 Handler 共享同一实例）。
/// 方法按 partial 文件拆分：Entry/Leave/Join/Create/GetRoomListSnap。
/// </summary>
public sealed partial class RoomsService : ServiceBase
{
    /// <summary>
    /// 本 Scene 的独立 Redis 实例；Scene 创建时绑定，调用路径不再 GetComponent。
    /// </summary>
    private RedisComponent? _redis;

    private RoomManagerEntity Manager => Scene.GetComponent<RoomManagerEntity>();

    /// <summary>
    /// 绑定当前 Rooms Scene 的 Redis 实例。
    /// </summary>
    public void BindRedis(RedisComponent redis)
    {
        ArgumentNullException.ThrowIfNull(redis);
        _redis = redis;
    }

    /// <summary>
    /// 进入指定房间。成功时 Args[0] 为 roomId。
    /// 满员判断：逻辑人数 = 实际人数 + 匹配占位人数。
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

        if (!Manager.TryGetById(roomId, out var existingRoom) || existingRoom == null)
        {
            Log.Warning($"玩家 {userId} Entry 房间 {roomId} 失败：房间不存在");
            return InnerResult.Fail("Entry 失败：房间不存在", userId, roomId);
        }

        // 已在目标房：幂等成功，不再走加入路径。
        if (existingRoom.ContainsMember(userId))
        {
            // 清掉redis的匹配状态，防止逻辑出现空占用
            if (_redis == null)
            {
                Log.Warning($"RoomsService.Entry 清理占位跳过：Redis 实例缺失, userId={userId}, roomId={existingRoom.RoomId}");
            }
            else if (!MatchResultDao.Delete(_redis, existingRoom.RoomId, userId, out var clearError))
            {
                Log.Warning(
                    $"RoomsService.Entry 清理占位失败: userId={userId}, roomId={existingRoom.RoomId}, error={clearError}");
            }
            Log.Info(
                $"玩家 {userId} Entry 房间成功(已在房): roomId={existingRoom.RoomId}, memberCount={existingRoom.MemberCount}/{existingRoom.Capacity}, state={existingRoom.State}");
            return InnerResult.Ok(string.Empty, existingRoom.RoomId);
        }

        if (!TryGetLogicalMemberCount(existingRoom, out var logicalCount, out var countError)
            || logicalCount >= existingRoom.Capacity)
        {
            var reason = string.IsNullOrEmpty(countError) ? "逻辑人数已满" : countError;
            Log.Warning(
                $"玩家 {userId} Entry 房间 {roomId} 失败：{reason}, memberCount={existingRoom.MemberCount}, logicalCount={logicalCount}, capacity={existingRoom.Capacity}");
            return InnerResult.Fail(reason, userId, roomId);
        }

        var joined = Manager.EnterRoom(roomId, userId);
        if (joined == null)
        {
            Log.Warning($"玩家 {userId} Entry 房间 {roomId} 失败：无法加入");
            return InnerResult.Fail("Entry 失败：无法加入", userId, roomId);
        }

        // 先入房再删占位：避免低估导致超加；删失败只记日志，不回滚 Entry。
        if (_redis == null)
        {
            Log.Warning($"RoomsService.Entry 清理占位跳过：Redis 实例缺失, userId={userId}, roomId={joined.RoomId}");
        }
        else if (!MatchResultDao.Delete(_redis, joined.RoomId, userId, out var clearError))
        {
            Log.Warning(
                $"RoomsService.Entry 清理占位失败: userId={userId}, roomId={joined.RoomId}, error={clearError}");
        }
        Log.Info(
            $"玩家 {userId} Entry 房间成功: roomId={joined.RoomId}, memberCount={joined.MemberCount}/{joined.Capacity}, logicalCountBefore={logicalCount}, state={joined.State}");
        return InnerResult.Ok(string.Empty, joined.RoomId);
    }
}
