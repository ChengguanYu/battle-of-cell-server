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
            // TODO: 实现重连回房逻辑
            Log.Warning($"用户 {userId} 当前不可匹配，state={player.State}");
            return InnerResult.Fail("当前状态不可匹配", player.State);
        }

        MatchResp? resp = null;
        try
        {
            var req = MatchReq.Create();
            req.user_id = userId;
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

            return InnerResult.Ok(string.Empty, resp.room_id > 0 && resp.room_id <= uint.MaxValue ? (uint)resp.room_id : 0u);
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
    /// 主动退出房间：仅 InRoom 可退出；Rooms RPC 成功后再 InRoom -> Lobby。
    /// 成功时 Args[0] 为离开的 roomId。
    /// </summary>
    public async FTask<InnerResult> LeaveRoom(long userId)
    {
        if (!AvatarDomain.Inst.TryGet(userId, out var player) || player == null)
        {
            return InnerResult.Fail("Avatar 未加载", userId);
        }

        if (!player.IsInRoom)
        {
            Log.Warning($"用户 {userId} 当前不可退出房间，state={player.State}");
            return InnerResult.Fail("当前状态不可退出房间", player.State);
        }

        RoomsLeaveResp? resp = null;
        try
        {
            var req = RoomsLeaveReq.Create();
            req.user_id = userId;
            req.reason = "client_leave";
            var address = Scene.GetSceneAddress(SceneType.Rooms);
            resp = await Call<RoomsLeaveReq, RoomsLeaveResp>(address, req);
            if (!resp.IsOk())
            {
                Log.Warning($"用户 {userId} RoomsLeave 失败，status={resp.ToMessage()}");
                return InnerResult.Fail("RoomsLeave 失败", resp.ToMessage());
            }

            var roomId = resp.room_id > 0 && resp.room_id <= uint.MaxValue
                ? (uint)resp.room_id
                : 0u;
            if (!player.TransitInRoomToLobby("client_leave"))
            {
                // Rooms 已离房成功，状态迁移失败时仍按 Rooms 权威回成功，避免双侧卡死
                Log.Error(
                    $"[Avatar] Rooms 已离房但 InRoom->Lobby 失败: userId={userId}, roomId={roomId}, state={player.State}");
            }

            return InnerResult.Ok(string.Empty, roomId);
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Rooms Scene，用户 {userId} 退出房间失败");
            return InnerResult.Fail("未找到 Rooms Scene", userId);
        }
        finally
        {
            resp?.Dispose();
        }
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

    /// <summary>
    /// 校验 Avatar 状态后转发客户端帧到 Rooms（单向）。
    /// </summary>
    /// <remarks>
    /// FIXME: 依赖上游 Handler 手工转移 frames 所有权，待统一收口。
    /// 原因：入站 message 在 Handler finally 中 Dispose 会级联释放 frames；上游摘引用后由本方法接管。
    /// 当前写法：成功则 msg.frames=frames 后 Send（ServiceBase.Send finally 回收整包）；校验/失败路径本方法 DisposeFrames。
    /// 后续：边界深拷贝 / 明确单所有者 API，去掉每跳手写交接。
    /// </remarks>
    public void ForwardClientFrame(long userId, ulong frameNumber, List<Frame>? frames)
    {
        if (!AvatarDomain.Inst.TryGet(userId, out var player) || player == null)
        {
            FrameMessageUtil.DisposeFrames(frames);
            Log.Warning($"[Avatar] ClientFrame 丢弃：Avatar 未加载, userId={userId}, frame={frameNumber}");
            return;
        }

        if (!player.IsInRoom)
        {
            FrameMessageUtil.DisposeFrames(frames);
            Log.Warning(
                $"[Avatar] ClientFrame 丢弃：非 InRoom, userId={userId}, state={player.State}, frame={frameNumber}");
            return;
        }

        try
        {
            var address = Scene.GetSceneAddress(SceneType.Rooms);
            var msg = RoomsClientFrameNotify.Create();
            msg.user_id = userId;
            msg.frame_number = frameNumber;
            if (frames is { Count: > 0 })
            {
                msg.frames = frames;
            }
            Send(address, msg);
        }
        catch (InvalidOperationException)
        {
            FrameMessageUtil.DisposeFrames(frames);
            Log.Warning($"[Avatar] 未找到 Rooms Scene，ClientFrame 丢弃: userId={userId}, frame={frameNumber}");
        }
        catch (Exception ex)
        {
            FrameMessageUtil.DisposeFrames(frames);
            Log.Error($"[Avatar] 转发 ClientFrame 失败: userId={userId}, frame={frameNumber}, ex={ex}");
        }
    }

}
