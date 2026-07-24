using Entity.DTOs;
using Fantasy;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;
using Hotfix.Database;
using Hotfix.Utils;

namespace Hotfix.Scene.Match.Service;

/// <summary>
/// Match Scene 级服务（挂在 Scene 上，全 Handler 共享同一实例）。
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
    /// 匹配：拉取 Rooms 列表快照；无房 CreateAndEntry，有房随机 Join。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> Match(long userId)
    {
        if (userId <= 0)
        {
            return InnerResult.Fail("userId 非法", userId);
        }

        RoomsGetRoomListSnapResp? snapResp = null;
        try
        {
            var address = Scene.GetSceneAddress(SceneType.Rooms);
            var req = RoomsGetRoomListSnapReq.Create();
            snapResp = await Call<RoomsGetRoomListSnapReq, RoomsGetRoomListSnapResp>(address, req);
            if (!snapResp.IsOk())
            {
                Log.Warning($"用户 {userId} GetRoomListSnap 失败，status={snapResp.ToMessage()}");
                return InnerResult.Fail("GetRoomListSnap 失败", snapResp.ToMessage());
            }

            if (snapResp.is_empty || snapResp.rooms is not { Count: > 0 })
            {
                Log.Debug($"用户 {userId} GetRoomListSnap 为空，走 CreateAndEntry");
                return await CreateAndEntry(address, userId);
            }

            var rooms = snapResp.rooms;
            var pick = rooms[Random.Shared.Next(rooms.Count)];
            Log.Debug(
                $"用户 {userId} GetRoomListSnap 候选={rooms.Count}，随机选 room_id={pick.room_id} 走 Join");
            return await Join(address, userId, pick.room_id);
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Rooms Scene，用户 {userId} 匹配失败");
            return InnerResult.Fail("未找到 Rooms Scene", userId);
        }
        finally
        {
            snapResp?.Dispose();
        }
    }

    /// <summary>
    /// 客户端匹配：与旧逻辑一致选房/无房创建，但不入房；成功后写 Redis 匹配结果。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> ClientMatch(long userId, Fantasy.MatchType matchType)
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
    /// 有候选房时：Rooms Join（Entry 指定房间）。成功时 Args[0] 为 roomId。
    /// </summary>
    private async FTask<InnerResult> Join(long roomsAddress, long userId, long roomId)
    {
        RoomsJoinResp? resp = null;
        try
        {
            var req = RoomsJoinReq.Create();
            req.user_id = userId;
            req.room_id = roomId;
            resp = await Call<RoomsJoinReq, RoomsJoinResp>(roomsAddress, req);
            if (!resp.IsOk())
            {
                Log.Warning($"用户 {userId} Join 房间 {roomId} 失败，status={resp.ToMessage()}");
                return InnerResult.Fail("Join 失败", resp.ToMessage());
            }

            if (resp.room_id <= 0)
            {
                Log.Warning($"用户 {userId} Join 成功但 room_id 非法: {resp.room_id}");
                return InnerResult.Fail("Join 未返回有效 room_id", userId, roomId);
            }

            Log.Info($"玩家 {userId} Join 成功: roomId={resp.room_id}");
            return InnerResult.Ok(string.Empty, (uint)resp.room_id);
        }
        finally
        {
            resp?.Dispose();
        }
    }

    /// <summary>
    /// 无候选房时：Rooms CreateAndEntry（创建并进入）。成功时 Args[0] 为 roomId。
    /// </summary>
    private async FTask<InnerResult> CreateAndEntry(long roomsAddress, long userId)
    {
        RoomsCreateAndEntryResp? resp = null;
        try
        {
            var req = RoomsCreateAndEntryReq.Create();
            req.user_id = userId;
            resp = await Call<RoomsCreateAndEntryReq, RoomsCreateAndEntryResp>(roomsAddress, req);
            if (!resp.IsOk())
            {
                Log.Warning($"用户 {userId} CreateAndEntry 失败，status={resp.ToMessage()}");
                return InnerResult.Fail("CreateAndEntry 失败", resp.ToMessage());
            }

            if (resp.room_id <= 0)
            {
                Log.Warning($"用户 {userId} CreateAndEntry 成功但 room_id 非法: {resp.room_id}");
                return InnerResult.Fail("CreateAndEntry 未返回有效 room_id", userId);
            }

            Log.Info($"玩家 {userId} CreateAndEntry 成功: roomId={resp.room_id}");
            return InnerResult.Ok(string.Empty, (uint)resp.room_id);
        }
        finally
        {
            resp?.Dispose();
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
