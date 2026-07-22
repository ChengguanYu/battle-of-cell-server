using Entity.DTOs;
using Fantasy;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;
using Hotfix.Utils;

namespace Hotfix.Scene.Match.Service;

/// <summary>
/// Match Scene 级服务（挂在 Scene 上，全 Handler 共享同一实例）。
/// </summary>
public sealed class MatchService() : ServiceBase(), IMatchService
{
    /// <summary>
    /// 旧匹配：转发到 Rooms.Enter（MatchOrCreate）。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> Match(long userId)
    {
        if (userId <= 0)
        {
            return InnerResult.Fail("userId 非法", userId);
        }

        RoomsEnterResp? resp = null;
        try
        {
            var req = RoomsEnterReq.Create();
            req.userId = userId;
            var address = Scene.GetSceneAddress(SceneType.Rooms);
            resp = await Call<RoomsEnterReq, RoomsEnterResp>(address, req);
            if (!resp.IsOk())
            {
                Log.Warning($"用户 {userId} RoomsEnter 失败，status={resp.ToMessage()}");
                return InnerResult.Fail("RoomsEnter 失败", resp.ToMessage());
            }

            if (resp.room_id <= 0)
            {
                Log.Warning($"用户 {userId} RoomsEnter 成功但 room_id 非法: {resp.room_id}");
                return InnerResult.Fail("RoomsEnter 未返回有效 room_id", userId);
            }

            Log.Info($"玩家 {userId} 匹配成功: roomId={resp.room_id}");
            return InnerResult.Ok(string.Empty, resp.room_id);
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Rooms Scene，用户 {userId} 匹配失败");
            return InnerResult.Fail("未找到 Rooms Scene", userId);
        }
        finally
        {
            resp?.Dispose();
        }
    }

    /// <summary>
    /// 新匹配：先拉取 Rooms 列表快照，按 IsEmpty 分支；Join/Create 待实现。
    /// </summary>
    public async FTask<InnerResult> NewMatch(long userId)
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

            if (snapResp.IsEmpty)
            {
                // TODO: 无候选房 → Create
                Log.Info($"用户 {userId} GetRoomListSnap 为空，待 Create");
                return InnerResult.Fail("NewMatch 未实现：无房待 Create", userId);
            }

            // TODO: 基于 snapResp.rooms 尝试 Join → 失败则 Create
            var rooms = snapResp.rooms;
            var roomCount = rooms?.Count ?? 0;
            Log.Info($"用户 {userId} GetRoomListSnap 完成，候选房数量={roomCount}");
            if (rooms is { Count: > 0 })
            {
                foreach (var room in rooms)
                {
                    Log.Info(
                        $"用户 {userId} 候选房: room_id={room.room_id}, member_count={room.member_count}, capacity={room.capacity}, state={room.state}");
                }
            }

            return InnerResult.Fail("NewMatch 未实现：有房待 Join", userId);
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Rooms Scene，用户 {userId} NewMatch 失败");
            return InnerResult.Fail("未找到 Rooms Scene", userId);
        }
        finally
        {
            snapResp?.Dispose();
        }
    }
}
