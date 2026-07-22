namespace Entity.Config;

/// <summary>
/// 房间相关配置。
/// </summary>
public static class RoomConfig
{
    /// <summary>默认房间容量。</summary>
    public const int DefaultCapacity = 10;

    /// <summary>服务端帧环形缓冲容量（覆盖写）。</summary>
    public const int FrameBufferCapacity = 10;

    /// <summary>
    /// 延迟广播帧数。写入帧 N 后，广播帧 N - DelayFrame。
    /// 须满足 0 &lt;= DelayFrame &lt; FrameBufferCapacity。
    /// </summary>
    public const int DelayFrame = 3;
}
