using Fantasy;

namespace Hotfix.Utils;

/// <summary>
/// frame / ServerFrame 协议层固定逻辑：深拷贝、摘 list、Dispose、发送装配。
/// </summary>
public static class FrameMessageUtil
{
    public static Frame CloneFrame(Frame src)
    {
        var dst = Frame.Create();
        dst.op = src.op;
        if (src.data != null)
        {
            dst.data = ClonePlayer(src.data);
        }

        return dst;
    }

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

    public static ServerFrame CreateServerFrameForSend(ServerFrame source)
    {
        var msg = ServerFrame.Create();
        msg.frame_number = source.frame_number;
        msg.random_seed = source.random_seed;
        CopyFramesTo(msg, source.frames);
        return msg;
    }

    public static List<Frame> DetachFrames(ClientFrame message)
    {
        var frames = message.frames;
        message.frames = new List<Frame>();
        return frames;
    }

    public static List<Frame> DetachFrames(AvatarRelayClientFrameNotify message)
    {
        var frames = message.frames;
        message.frames = new List<Frame>();
        return frames;
    }

    public static List<Frame> DetachFrames(RoomsClientFrameNotify message)
    {
        var frames = message.frames;
        message.frames = new List<Frame>();
        return frames;
    }

    public static void DisposeFrames(List<Frame>? frames)
    {
        if (frames == null || frames.Count == 0)
        {
            return;
        }

        foreach (var frame in frames)
        {
            frame?.Dispose();
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
