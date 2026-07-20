using Entity.DTOs;
using Entity.Domains;
using Fantasy;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;
using Hotfix.Scene.Http.Repositories;
using Hotfix.Utils;

namespace Hotfix.Scene.Avatars.Service;

/// <summary>
/// Avatars Scene 级服务（挂在 Scene 上，全 Handler 共享同一实例）。
/// </summary>
public sealed class AvatarsService() : ServiceBase(), IAvatarsService
{
    public async FTask<InnerResult> LoadPlayer(long userId)
    {
        if (AvatarDomain.Inst.TryGet(userId, out var existing) && existing != null)
        {
            // 已在大厅或房间中：视为进入成功（幂等）
            if (existing.State is AvatarState.Lobby or AvatarState.InRoom)
            {
                return InnerResult.Ok();
            }

            // 残留 New 态时补一次进入大厅
            if (!existing.TransitNewToLobby())
            {
                return InnerResult.Fail("Avatar 进入大厅失败", existing.State);
            }

            return InnerResult.Ok();
        }

        var user = await UserDao.FindByIdAsync(userId);
        if (user == null)
        {
            return InnerResult.Fail("未找到用户");
        }

        var player = new AvatarDomainPrototype(user);
        if (!player.TransitNewToLobby())
        {
            return InnerResult.Fail("Avatar 进入大厅失败");
        }

        AvatarDomain.Inst.Load(player);
        return InnerResult.Ok();
    }

    /// <summary>
    /// 纯转发匹配请求到 Match Scene，不做匹配业务校验。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> Match(long userId)
    {
        if (!AvatarDomain.Inst.TryGet(userId, out var player) || player == null)
        {
            return InnerResult.Fail("Avatar 未加载", userId);
        }

        if (!player.IsInLobby)
        {
            Log.Warning($"用户 {userId} 当前不可匹配，state={player.State}");
            return InnerResult.Fail("当前状态不可匹配", player.State);
        }

        MatchResp? resp = null;
        try
        {
            var req = MatchReq.Create();
            req.userId = userId;
            var address = Scene.GetSceneAddress(SceneType.Match);
            resp = await Call<MatchReq, MatchResp>(address, req);
            if (!resp.IsOk())
            {
                Log.Warning($"用户 {userId} Match 失败，status={resp.ToMessage()}");
                return InnerResult.Fail("Match 失败", resp.ToMessage());
            }

            if (!player.TransitLobbyToInRoom())
            {
                return InnerResult.Fail("Avatar 进入房间失败", player.State);
            }

            return InnerResult.Ok(string.Empty, resp.room_id);
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Match Scene，用户 {userId} 匹配失败");
            return InnerResult.Fail("未找到 Match Scene", userId);
        }
        finally
        {
            resp?.Dispose();
        }
    }

    /// <summary>
    /// Gate 通知清理玩家。
    /// 若玩家在房间中，先通知 Rooms 做离房检查（最后一人则关房），再卸载 Avatar。
    /// </summary>
    public async FTask CleanupPlayer(long userId, string? reason)
    {
        await FTask.CompletedTask;

        Log.Info($"[Avatar] 准备清理玩家: userId={userId}, reason={reason}");

        if (!AvatarDomain.Inst.TryGet(userId, out var player) || player == null)
        {
            Log.Info($"[Avatar] 清理跳过：玩家未加载, userId={userId}, reason={reason}");
            return;
        }

        if (player.IsInRoom)
        {
            NotifyRoomsPlayerLeave(userId, reason);
            player.TransitInRoomToLobby(reason ?? "cleanup");
        }

        if (AvatarDomain.Inst.Remove(userId))
        {
            Log.Info($"[Avatar] 玩家已从内存卸载: userId={userId}, reason={reason}");
        }
        else
        {
            Log.Warning($"[Avatar] 玩家卸载失败（缓存中不存在）: userId={userId}, reason={reason}");
        }
    }

    private void NotifyRoomsPlayerLeave(long userId, string? reason)
    {
        try
        {
            var address = Scene.GetSceneAddress(SceneType.Rooms);
            var msg = RoomsPlayerLeaveNotify.Create();
            msg.userId = userId;
            msg.reason = reason ?? string.Empty;
            Send(address, msg);
            Log.Info($"[Avatar] 已通知 Rooms 离房检查: userId={userId}, reason={reason}, address={address}");
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"[Avatar] 未找到 Rooms Scene，无法通知离房: userId={userId}, reason={reason}");
        }
        catch (Exception ex)
        {
            Log.Error($"[Avatar] 通知 Rooms 离房失败: userId={userId}, reason={reason}, ex={ex}");
        }
    }
}
