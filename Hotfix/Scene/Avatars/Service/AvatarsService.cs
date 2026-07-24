using System.Collections.Generic;
using Entity.DTOs;
using Entity.Domains;
using Entity.Utils;
using Fantasy;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;
using Hotfix.Scene.Http.Repositories;
using Hotfix.Utils;

namespace Hotfix.Scene.Avatars.Service;

/// <summary>
/// Avatars Scene 本域服务：玩家加载/下线编排。跨 Scene 转发见 Service/Relay 下的 Relay 服务。
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
    /// 玩家下线清理入口：仅编排调用各清理步骤。
    /// 后续新增步骤在此追加函数调用；若步骤异步则 await。
    /// </summary>
    public async FTask CleanupPlayer(long userId, string? reason)
    {
        Log.Info($"[Avatar] 准备清理玩家: userId={userId}, reason={reason}");

        LeaveRoomIfNeeded(userId, reason);
        UnloadAvatar(userId, reason);
        await FTask.CompletedTask;
    }

    /// <summary>
    /// 若玩家在房间中，通知 Rooms 离房检查，并把 Avatar 状态收回大厅。
    /// 下线清理路径用单向 Notify，不阻塞会话清理。
    /// </summary>
    private void LeaveRoomIfNeeded(long userId, string? reason)
    {
        if (!AvatarDomain.Inst.TryGet(userId, out var player) || player == null)
        {
            Log.Info($"[Avatar] 离房步骤跳过：玩家未加载, userId={userId}, reason={reason}");
            return;
        }

        if (!player.IsInRoom)
        {
            return;
        }

        NotifyRoomsPlayerLeave(userId, reason);
        player.TransitInRoomToLobby(reason ?? "cleanup");
    }

    /// <summary>
    /// 从 Avatar 内存领域卸载玩家。
    /// </summary>
    private static void UnloadAvatar(long userId, string? reason)
    {
        if (!AvatarDomain.Inst.TryGet(userId, out _))
        {
            Log.Info($"[Avatar] 卸载跳过：玩家未加载, userId={userId}, reason={reason}");
            return;
        }

        if (AvatarDomain.Inst.Remove(userId))
        {
            Log.Info($"[Avatar] 玩家已从内存卸载: userId={userId}, reason={reason}");
            return;
        }

        Log.Warning($"[Avatar] 玩家卸载失败（缓存中不存在）: userId={userId}, reason={reason}");
    }

    private void NotifyRoomsPlayerLeave(long userId, string? reason)
    {
        try
        {
            var address = Scene.GetSceneAddress(SceneType.Rooms);
            var msg = RoomsPlayerLeaveNotify.Create();
            msg.user_id = userId;
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
