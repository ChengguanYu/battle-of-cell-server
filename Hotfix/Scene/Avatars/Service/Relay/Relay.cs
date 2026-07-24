using System.Collections.Generic;
using Entity.DTOs;
using Entity.Domains;
using Entity.Utils;
using Fantasy;
using Fantasy.Async;
using Hotfix.Common.Abstract.Service;
using Hotfix.Utils;

namespace Hotfix.Scene.Avatars.Service;

/// <summary>
/// 跨 Scene 转发：门禁 + 转发，与本域 AvatarsService 分离。
/// 文件位于 Service/Relay。
/// </summary>
public sealed class Relay() : ServiceBase(), IRelay
{
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

        InnerMatchResp? resp = null;
        try
        {
            var req = InnerMatchReq.Create();
            req.user_id = userId;
            var address = Scene.GetSceneAddress(SceneType.Match);
            resp = await Call<InnerMatchReq, InnerMatchResp>(address, req);
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

    /// <remarks>
    /// FIXME: 依赖上游 Handler 手工转移 frames 所有权，待统一收口。
    /// </remarks>
    public void ClientFrame(long userId, ulong frameNumber, List<Frame>? frames)
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
