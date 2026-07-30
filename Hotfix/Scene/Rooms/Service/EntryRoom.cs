using Entity.DTOs;
using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Hotfix.Database;
using Hotfix.Simulation;
using Hotfix.Simulation.Abstractions;
using Hotfix.Scene.Rooms.System;
using Hotfix.Simulation.System;

namespace Hotfix.Scene.Rooms.Service;

public sealed partial class RoomsService
{
    /// <summary>
    /// 客户端进房：读 Redis 匹配结果后 Entry。成功时一并从模拟器读取世界参数。
    /// </summary>
    public async FTask<InnerResult> EntryRoom(long userId, RoomsEntryRoomResp resp)
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

            if (entryResult.IsSuccess)
            {
                resp.room_id = roomId;

                var simManager = Scene.GetComponent<SimulationManagerEntity>();
                if (simManager.TryGet(roomId, out var sim) && sim is SimBase simBase)
                {
                    resp.world = WorldInit.Create();
                    resp.world.x_size = simBase.Config.World.Map.X;
                    resp.world.y_size = simBase.Config.World.Map.Y;
                    resp.world.shapes = ShapeDataBuilder.Build(simBase.SimState.ShapeView);
                    Log.Info($"RoomsService.EntryRoom 下发世界形状: room={roomId}, shapes={resp.world.shapes.Count}, userId={userId}");

                    // 生成实体 uid 并注册到模拟器
                    if (Manager.TryGetById(roomId, out var room) && room != null && room.TryNextUid(out var uid)
                        && simBase.AddPlayer((uint)uid, userId, out var coord))
                    {
                        resp.position = Position2d.Create();
                        resp.position.x = (int)coord.X;
                        resp.position.y = (int)coord.Y;
                        Log.Info($"RoomsService.EntryRoom 玩家注册模拟器: userId={userId}, uid={uid}, x={coord.X}, y={coord.Y}");
                    }
                    else
                    {
                        Log.Warning($"RoomsService.EntryRoom 注册玩家失败，回卷入房: userId={userId}, roomId={roomId}");
                        Manager.LeaveRoom(userId, reason: "add_player_failed");
                        entryResult = InnerResult.Fail("注册玩家失败", userId, roomId);
                    }
                }
                // TODO: simManager.TryGet 失败时（极低概率，仅当房间在 Entry 后被外部队列抢先删掉），
                //       响应 ok=true 但 world/position 为 null。此时应回卷入房并返回失败。
            }

        Log.Debug(
            $"RoomsService.EntryRoom 结束: userId={userId}, roomId={roomId}, ok={entryResult.IsSuccess}, reason={entryResult.Reason}");
        return entryResult;
    }
}
