using Entity.Runtime.room;

namespace Hotfix.Scene.Rooms.System;

public static class RoomUidGeneratorSystem
{
    public static void Reset(this RoomUidGeneratorEntity self)
    {
        self.LastUidMs = 0;
        self.UidSeqInMs = 0;
    }

    public static ulong Next(this RoomUidGeneratorEntity self)
    {
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (nowMs < self.LastUidMs)
        {
            nowMs = self.LastUidMs;
        }

        if (nowMs == self.LastUidMs)
        {
            if (self.UidSeqInMs >= RoomUidGeneratorEntity.UidSeqMask)
            {
                nowMs = self.LastUidMs + 1;
                self.LastUidMs = nowMs;
                self.UidSeqInMs = 0;
            }
            else
            {
                self.UidSeqInMs++;
            }
        }
        else
        {
            self.LastUidMs = nowMs;
            self.UidSeqInMs = 0;
        }

        var uid = ((ulong)nowMs << RoomUidGeneratorEntity.UidSeqBits) | (uint)self.UidSeqInMs;
        if (uid == 0)
        {
            self.UidSeqInMs = 1;
            uid = ((ulong)nowMs << RoomUidGeneratorEntity.UidSeqBits) | 1u;
        }

        return uid;
    }
}
