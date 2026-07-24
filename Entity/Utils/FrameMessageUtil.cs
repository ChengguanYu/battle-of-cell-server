using System.Collections.Generic;
using Fantasy;

namespace Entity.Utils;

/// <summary>
/// frame / ServerFrame 协议层固定逻辑：深拷贝、摘 list、Dispose、发送装配。
/// 无 Room 依赖；业务侧只编排调用，不散落手写树拷贝。
/// </summary>
public static class FrameMessageUtil
{
    /// <summary>
    /// 深拷贝一条 frame（含子对象），供窗口持有/发送路径独立生命周期。
    /// </summary>
    public static Frame CloneFrame(Frame src)
    {
        // 池化对象：ResetContent 里 Dispose 可正确回收
        var dst = Frame.Create();
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
    public static void CopyFramesTo(ServerFrame target, List<Frame>? source)
    {
        if (source == null || source.Count == 0)
        {
            return;
        }

        target.frames ??= new List<Frame>();
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
    /// 由缓冲帧装配发送用 ServerFrame（池化 Create + 字段拷贝 + frames 深拷贝）。
    /// </summary>
    public static ServerFrame CreateServerFrameForSend(ServerFrame source)
    {
        var msg = ServerFrame.Create();
        msg.frame_number = source.frame_number;
        msg.random_seed = source.random_seed;
        CopyFramesTo(msg, source.frames);
        return msg;
    }

    /// <summary>
    /// 从 ClientFrame 摘下 frames 所有权，并挂上空 list，避免 Handler finally Dispose 级联释放。
    /// </summary>
    public static List<Frame> DetachFrames(ClientFrame message)
    {
        var frames = message.frames;
        message.frames = new List<Frame>();
        return frames;
    }

    /// <summary>
    /// 从 AvatarClientFrameNotify 摘下 frames 所有权，并挂上空 list。
    /// </summary>
    public static List<Frame> DetachFrames(AvatarClientFrameNotify message)
    {
        var frames = message.frames;
        message.frames = new List<Frame>();
        return frames;
    }

    /// <summary>
    /// 从 RoomsClientFrameNotify 摘下 frames 所有权，并挂上空 list。
    /// </summary>
    public static List<Frame> DetachFrames(RoomsClientFrameNotify message)
    {
        var frames = message.frames;
        message.frames = new List<Frame>();
        return frames;
    }

    /// <summary>
    /// 释放 frames 列表内池对象并 Clear；null 或空列表为 no-op。
    /// </summary>
    public static void DisposeFrames(List<Frame>? frames)
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

    private static Player ClonePlayer(Player src)
    {
        var dst = Player.Create();
        dst.speed = src.speed;
        dst.eid = src.eid;
        if (src.direction != null)
        {
            dst.direction = Vec2d.Create();
            dst.direction.x = src.direction.x;
            dst.direction.y = src.direction.y;
        }

        if (src.position != null)
        {
            dst.position = Position2d.Create();
            dst.position.x = src.position.x;
            dst.position.y = src.position.y;
        }

        return dst;
    }
}
