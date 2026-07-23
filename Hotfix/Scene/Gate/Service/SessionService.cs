using System.Collections.Generic;
using Entity.DTOs;
using Entity.Managers;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Hotfix.Common.Abstract.Service;
using Hotfix.Scene.Http.Repositories;
using Hotfix.Utils;
using FScene = Fantasy.Scene;

namespace Hotfix.Scene.Gate.Service;

/// <summary>
/// Gate Scene 级会话服务（挂在 Scene 上，全 Handler 共享同一实例）。
/// 无请求级状态；在线绑定由 SessionManager 持有。
/// </summary>
public sealed class SessionService() : ServiceBase(), ISessionService
{
    public async FTask<InnerResult> EntryHome(long userId, Session session)
    {
        var user = await UserDao.FindByIdAsync(userId);
        if (user == null)
        {
            Log.Warning($"用户 {userId} 不存在，断开连接");
            return InnerResult.Fail("用户不存在", userId);
        }

        PlayerEntryResp? resp = null;
        try
        {
            var req = PlayerEntryReq.Create();
            req.userId = userId;
            // PlayerEntryHandle.cs
            var address = Scene.GetSceneAddress(SceneType.Avatars);
            resp = await Call<PlayerEntryReq, PlayerEntryResp>(address, req);
            if (!resp.IsOk())
            {
                Log.Warning($"用户 {userId} PlayerEntry 失败，status={resp.ToMessage()}");
                return InnerResult.Fail("PlayerEntry 失败", resp.ToMessage());
            }

            // PlayerEntry 成功后才绑定：写入 WsSession 并建立 userId <-> Session 索引
            SessionManager.Instance.Bind(userId, session);

            return InnerResult.Ok();
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Avatars Scene，用户 {userId} 进入失败");
            return InnerResult.Fail("未找到 Avatars Scene", userId);
        }
        finally
        {
            resp?.Dispose();
        }
    }

    /// <summary>
    /// 发起匹配请求：通过内部 RPC 转发到 Avatars Scene，由 Avatar 再转 Match。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> PlayerMatch(long userId)
    {
        AvatarMatchResp? resp = null;
        try
        {
            var req = AvatarMatchReq.Create();
            req.userId = userId;
            var address = Scene.GetSceneAddress(SceneType.Avatars);
            resp = await Call<AvatarMatchReq, AvatarMatchResp>(address, req);
            if (!resp.IsOk())
            {
                Log.Warning($"用户 {userId} AvatarMatch 失败，status={resp.ToMessage()}");
                return InnerResult.Fail("AvatarMatch 失败", resp.ToMessage());
            }

            return InnerResult.Ok(string.Empty, resp.room_id > 0 && resp.room_id <= uint.MaxValue ? (uint)resp.room_id : 0u);
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Avatars Scene，用户 {userId} 匹配失败");
            return InnerResult.Fail("未找到 Avatars Scene", userId);
        }
        finally
        {
            resp?.Dispose();
        }
    }
    /// <summary>
    /// 主动退出房间：通过内部 RPC 转发到 Avatars Scene。
    /// 成功时 Args[0] 为 roomId。
    /// </summary>
    public async FTask<InnerResult> PlayerLeaveRoom(long userId)
    {
        AvatarLeaveRoomResp? resp = null;
        try
        {
            var req = AvatarLeaveRoomReq.Create();
            req.userId = userId;
            var address = Scene.GetSceneAddress(SceneType.Avatars);
            resp = await Call<AvatarLeaveRoomReq, AvatarLeaveRoomResp>(address, req);
            if (!resp.IsOk())
            {
                Log.Warning($"用户 {userId} AvatarLeaveRoom 失败，status={resp.ToMessage()}");
                return InnerResult.Fail("AvatarLeaveRoom 失败", resp.ToMessage());
            }

            return InnerResult.Ok(string.Empty, resp.room_id > 0 && resp.room_id <= uint.MaxValue ? (uint)resp.room_id : 0u);
        }
        catch (InvalidOperationException)
        {
            Log.Warning($"未找到 Avatars Scene，用户 {userId} 退出房间失败");
            return InnerResult.Fail("未找到 Avatars Scene", userId);
        }
        finally
        {
            resp?.Dispose();
        }
    }
    /// <summary>
    /// 转发客户端帧到 Avatars Scene（单向）。
    /// </summary>
    /// <remarks>
    /// FIXME: 依赖上游 Handler 手工转移 frames 所有权，待统一收口。
    /// 原因：入站 message 在 Handler finally 中 Dispose 会级联释放 frames；上游摘引用后由本方法接管。
    /// 当前写法：成功则 msg.frames=frames 后 Send（ServiceBase.Send finally 回收整包）；失败路径本方法 DisposeFrames。
    /// 后续：边界深拷贝 / 明确单所有者 API，去掉每跳手写交接。
    /// </remarks>
    public void ForwardClientFrame(long userId, ulong frameNumber, List<frame>? frames)
    {
        try
        {
            var address = Scene.GetSceneAddress(SceneType.Avatars);
            var msg = AvatarClientFrameNotify.Create();
            msg.userId = userId;
            msg.frame_number = frameNumber;
            if (frames is { Count: > 0 })
            {
                msg.frames = frames;
            }
            Send(address, msg);
        }
        catch (InvalidOperationException)
        {
            DisposeFrames(frames);
            Log.Warning($"[Gate] 未找到 Avatars Scene，client_frame 丢弃: userId={userId}, frame={frameNumber}");
        }
        catch (Exception ex)
        {
            DisposeFrames(frames);
            Log.Error($"[Gate] 转发 client_frame 失败: userId={userId}, frame={frameNumber}, ex={ex}");
        }
    }

    private static void DisposeFrames(List<frame>? frames)
    {
        if (frames == null || frames.Count == 0)
        {
            return;
        }

        foreach (var f in frames)
        {
            f?.Dispose();
        }

        frames.Clear();
    }
}

