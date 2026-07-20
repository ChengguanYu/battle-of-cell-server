using Entity.DTOs;
using Fantasy;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;
using Hotfix.Utils;

namespace Hotfix.Scene.Match.Service;

/// <summary>
/// Match Scene 级服务（挂在 Scene 上，全 Handler 共享同一实例）。
/// 当前阶段直接转发到 Rooms Scene 完成入房；已在房语义由 Rooms 处理。
/// </summary>
public sealed class MatchService() : ServiceBase(), IMatchService
{
    /// <summary>
    /// 处理玩家匹配：转发到 Rooms 入房；不在此判断是否已在房间。
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
}
