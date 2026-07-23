using System.Collections.Generic;
using Fantasy;

namespace Entity.Utils;

/// <summary>
/// frame / server_frame 协议层固定逻辑：深拷贝、摘 list、Dispose、发送装配。
/// 无 Room 依赖；业务侧只编排调用，不散落手写树拷贝。
/// </summary>
public static class FrameMessageUtil
{
    /// <summary>
    /// 深拷贝一条 frame（含子对象），供窗口持有/发送路径独立生命周期。
    /// </summary>
    public static frame CloneFrame(frame src)
    {
        // 池化对象：ResetContent 里 Dispose 可正确回收
        var dst = frame.Create();
        dst.op = src.op;
        if (src.data != null)
        {
            dst.data = ClonePlayer(src.data);
        }

        return dst;
    }

    /// <summary>
    /// 将 source 中的 frame 深拷贝追加到 target.frames。
    /// source 为空或 count==0 时直接返回。
    /// </summary>
    public static void CopyFramesTo(server_frame target, List<frame>? source)
    {
        if (source == null || source.Count == 0)
        {
            return;
        }

        target.frames ??= new List<frame>();
        for (var i = 0; i < source.Count; i++)
        {
            var src = source[i];
            if (src == null)
            {
                continue;
            }

            target.frames.Add(CloneFrame(src));
        }
    }

    /// <summary>
    /// 由缓冲帧装配发送用 server_frame（池化 Create + 字段拷贝 + frames 深拷贝）。
    /// </summary>
    public static server_frame CreateServerFrameForSend(server_frame source)
    {
        var msg = server_frame.Create();
        msg.frame_number = source.frame_number;
        msg.randomSeed = source.randomSeed;
        CopyFramesTo(msg, source.frames);
        return msg;
    }

    /// <summary>
    /// 从 client_frame 摘下 frames 所有权，并挂上空 list，避免 Handler finally Dispose 级联释放。
    /// </summary>
    public static List<frame> DetachFrames(client_frame message)
    {
        var frames = message.frames;
        message.frames = new List<frame>();
        return frames;
    }

    /// <summary>
    /// 从 AvatarClientFrameNotify 摘下 frames 所有权，并挂上空 list。
    /// </summary>
    public static List<frame> DetachFrames(AvatarClientFrameNotify message)
    {
        var frames = message.frames;
        message.frames = new List<frame>();
        return frames;
    }

    /// <summary>
    /// 从 RoomsClientFrameNotify 摘下 frames 所有权，并挂上空 list。
    /// </summary>
    public static List<frame> DetachFrames(RoomsClientFrameNotify message)
    {
        var frames = message.frames;
        message.frames = new List<frame>();
        return frames;
    }

    /// <summary>
    /// 释放 frames 列表内池对象并 Clear；null 或空列表为 no-op。
    /// </summary>
    public static void DisposeFrames(List<frame>? frames)
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

    private static player ClonePlayer(player src)
    {
        var dst = player.Create();
        dst.speed = src.speed;
        dst.eid = src.eid;
        if (src.direction != null)
        {
            dst.direction = vec2d.Create();
            dst.direction.x = src.direction.x;
            dst.direction.y = src.direction.y;
        }

        if (src.position != null)
        {
            dst.position = position2d.Create();
            dst.position.x = src.position.x;
            dst.position.y = src.position.y;
        }

        return dst;
    }
}
