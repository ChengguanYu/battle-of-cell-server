using Entity.DTOs;
using Fantasy;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;
using Hotfix.Database;
using Hotfix.Utils;
//todo: 所有服务层错误应该使用抛出错误来传递，不需要通过返回值
namespace Hotfix.Scene.Match.Service;

/// <summary>
/// Match Scene 级服务（挂在 Scene 上，全 Handler 共享同一实例）。
/// 只负责编排：拉可加入房间列表 / Create；房间满员规则由 Rooms 负责。
/// </summary>
public sealed class MatchService() : ServiceBase(), IMatchService
{
    /// <summary>
    /// 本 Scene 的独立 Redis 实例；Scene 创建时绑定，调用路径不再 GetComponent。
    /// </summary>
    private RedisComponent? _redis;

    /// <summary>
    /// 绑定当前 Match Scene 的 Redis 实例。
    /// </summary>
    public void BindRedis(RedisComponent redis)
    {
        ArgumentNullException.ThrowIfNull(redis);
        _redis = redis;
    }

    /// <summary>
    /// 客户端匹配：选房/无房创建但不入房，成功后写 Redis。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> ClientMatch(long userId, MatchType matchType)
    {
        RoomsGetRoomListSnapResp? snapResp = null;
        try
        {
            var address = Scene.GetSceneAddress(SceneType.Rooms);
            var req = RoomsGetRoomListSnapReq.Create();
            snapResp = await Call<RoomsGetRoomListSnapReq, RoomsGetRoomListSnapResp>(address, req);
            if (!snapResp.IsOk())
            {
                Log.Warning($"用户 {userId} ClientMatch GetRoomListSnap 失败，status={snapResp.ToMessage()}");
                return InnerResult.Fail("GetRoomListSnap 失败", snapResp.ToMessage());
            }

            long roomId;
            if (snapResp.is_empty || snapResp.rooms is not { Count: > 0 })
            {
                Log.Debug($"用户 {userId} ClientMatch 无候选房，走 Create");
                var createResult = await Create(address, userId);
                if (!createResult.IsSuccess)
                {
                    return createResult;
                }

                roomId = TryGetRoomId(createResult);
            }
            else
            {
                var rooms = snapResp.rooms;
                var pick = rooms[Random.Shared.Next(rooms.Count)];
                roomId = pick.room_id;
                Log.Debug(
                    $"用户 {userId} ClientMatch 候选={rooms.Count}，随机选 room_id={roomId}（不入房）");
            }

            if (roomId <= 0)
            {
                Log.Warning($"用户 {userId} ClientMatch 得到非法 room_id={roomId}");
                return InnerResult.Fail("未得到有效 room_id", userId, roomId);
            }

            if (_redis == null)
            {
                Log.Warning($"用户 {userId} ClientMatch 失败：MatchService 未绑定 Redis");
                return InnerResult.Fail("Redis 实例缺失", userId, roomId);
            }

            if (!MatchResultDao.TrySave(_redis, userId, roomId, (int)matchType, out var error))
            {
                Log.Warning($"用户 {userId} ClientMatch 写匹配结果失败: roomId={roomId}, error={error}");
                return InnerResult.Fail(error, userId, roomId);
            }

            Log.Info(
                $"玩家 {userId} ClientMatch 成功: roomId={roomId}, matchType={matchType}");
            return InnerResult.Ok(string.Empty, roomId > 0 && roomId <= uint.MaxValue ? (uint)roomId : 0u);
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Rooms Scene，用户 {userId} ClientMatch 失败");
            return InnerResult.Fail("未找到 Rooms Scene", userId);
        }
        finally
        {
            snapResp?.Dispose();
        }
    }

    /// <summary>
    /// 无候选房时：仅 Create（Open/Start，不入房）。成功时 Args[0] 为 roomId。
    /// </summary>
    private async FTask<InnerResult> Create(long roomsAddress, long userId)
    {
        RoomsCreateResp? resp = null;
        try
        {
            var req = RoomsCreateReq.Create();
            req.user_id = userId;
            resp = await Call<RoomsCreateReq, RoomsCreateResp>(roomsAddress, req);
            if (!resp.IsOk())
            {
                Log.Warning($"用户 {userId} Create 房间失败，status={resp.ToMessage()}");
                return InnerResult.Fail("Create 失败", resp.ToMessage());
            }

            if (resp.room_id <= 0)
            {
                Log.Warning($"用户 {userId} Create 成功但 room_id 非法: {resp.room_id}");
                return InnerResult.Fail("Create 未返回有效 room_id", userId);
            }

            Log.Info($"玩家 {userId} Create 成功: roomId={resp.room_id}");
            return InnerResult.Ok(string.Empty, (uint)resp.room_id);
        }
        finally
        {
            resp?.Dispose();
        }
    }

    private static long TryGetRoomId(InnerResult result)
    {
        if (result.Args is { Count: > 0 } && result.Args[0] is uint roomId)
        {
            return roomId;
        }

        return 0;
    }
}
