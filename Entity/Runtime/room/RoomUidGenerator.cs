namespace Entity.Runtime.room;

/// <summary>
/// 毫秒时间戳 + 同毫秒序号的 UID 生成器。
/// 布局：高 44 位毫秒 | 低 20 位序号。依赖调用方串行。
/// </summary>
public sealed class RoomUidGenerator
{
    private const int UidSeqBits = 20;
    private const int UidSeqMask = (1 << UidSeqBits) - 1;

    private long _lastUidMs;
    private int _uidSeqInMs;

    public void Reset()
    {
        _lastUidMs = 0;
        _uidSeqInMs = 0;
    }

    public ulong Next()
    {
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (nowMs < _lastUidMs)
        {
            // 时钟回拨：沿用上次毫秒，避免回退撞号
            nowMs = _lastUidMs;
        }

        if (nowMs == _lastUidMs)
        {
            if (_uidSeqInMs >= UidSeqMask)
            {
                nowMs = _lastUidMs + 1;
                _lastUidMs = nowMs;
                _uidSeqInMs = 0;
            }
            else
            {
                _uidSeqInMs++;
            }
        }
        else
        {
            _lastUidMs = nowMs;
            _uidSeqInMs = 0;
        }

        var uid = ((ulong)nowMs << UidSeqBits) | (uint)_uidSeqInMs;
        if (uid == 0)
        {
            _uidSeqInMs = 1;
            uid = ((ulong)nowMs << UidSeqBits) | 1u;
        }

        return uid;
    }
}
