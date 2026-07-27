using Entity.Runtime.room;
using Fantasy;
using Hotfix.Utils;

namespace Hotfix.Scene.Rooms.System;

public static class RoomFrameWindowSystem
{
    public static void Initialize(this RoomFrameWindowEntity self, int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "容量必须大于 0。");
        }

        self.Capacity = capacity;
        self.Slots = new RoomFrameWindowEntity.Slot[capacity];
        for (var i = 0; i < capacity; i++)
        {
            self.Slots[i].Frame = ServerFrame.Create(autoReturn: false);
        }
    }

    public static bool IsOccupied(this RoomFrameWindowEntity self, ulong frameNumber)
    {
        ref var slot = ref self.Slots[SlotIndex(self, frameNumber)];
        return slot.Occupied && Matches(slot, frameNumber);
    }

    public static bool IsClearable(this RoomFrameWindowEntity self, ulong frameNumber)
    {
        ref var slot = ref self.Slots[SlotIndex(self, frameNumber)];
        return slot.Occupied && slot.Clearable && Matches(slot, frameNumber);
    }

    public static bool TryWriteEmpty(this RoomFrameWindowEntity self, ulong frameNumber, out string? error)
    {
        var index = SlotIndex(self, frameNumber);
        ref var slot = ref self.Slots[index];

        if (slot.Occupied && !slot.Clearable)
        {
            if (Matches(slot, frameNumber))
            {
                error = null;
                return true;
            }

            if (slot.Frame.frame_number >= frameNumber)
            {
                error =
                    $"槽位不可回写更旧帧: index={index}, occupiedFrame={slot.Frame.frame_number}, writeFrame={frameNumber}";
                return false;
            }
        }

        ResetContent(ref slot);
        slot.Frame.frame_number = frameNumber;
        slot.Occupied = true;
        slot.Clearable = false;
        error = null;
        return true;
    }

    public static bool TryEnsureOpen(this RoomFrameWindowEntity self, ulong frameNumber, out string? error)
    {
        var index = SlotIndex(self, frameNumber);
        ref var slot = ref self.Slots[index];

        if (slot.Occupied && !slot.Clearable && Matches(slot, frameNumber))
        {
            error = null;
            return true;
        }

        return self.TryWriteEmpty(frameNumber, out error);
    }

    public static bool TryAppendOps(
        this RoomFrameWindowEntity self,
        ulong frameNumber,
        IReadOnlyList<Frame>? ops,
        out string? error)
    {
        if (ops == null || ops.Count == 0)
        {
            error = null;
            return true;
        }

        var index = SlotIndex(self, frameNumber);
        ref var slot = ref self.Slots[index];

        if (!slot.Occupied || slot.Clearable)
        {
            error =
                $"槽位不可追加: index={index}, frameNumber={frameNumber}, occupied={slot.Occupied}, clearable={slot.Clearable}";
            return false;
        }

        if (!Matches(slot, frameNumber))
        {
            error =
                $"追加失败：帧号不匹配, index={index}, expected={frameNumber}, actual={slot.Frame.frame_number}";
            return false;
        }

        slot.Frame.frames ??= new List<Frame>();
        for (var i = 0; i < ops.Count; i++)
        {
            var src = ops[i];
            if (src == null)
            {
                continue;
            }

            slot.Frame.frames.Add(FrameMessageUtil.CloneFrame(src));
        }

        error = null;
        return true;
    }

    public static bool TryGet(
        this RoomFrameWindowEntity self,
        ulong frameNumber,
        out ServerFrame? frame,
        out string? error)
    {
        frame = null;
        var index = SlotIndex(self, frameNumber);
        ref var slot = ref self.Slots[index];

        if (!slot.Occupied || slot.Clearable)
        {
            error =
                $"槽位不可读: index={index}, frameNumber={frameNumber}, occupied={slot.Occupied}, clearable={slot.Clearable}";
            return false;
        }

        if (!Matches(slot, frameNumber))
        {
            error =
                $"帧号不匹配: index={index}, expected={frameNumber}, actual={slot.Frame.frame_number}";
            return false;
        }

        frame = slot.Frame;
        error = null;
        return true;
    }

    public static bool TryMarkClearable(this RoomFrameWindowEntity self, ulong frameNumber, out string? error)
    {
        var index = SlotIndex(self, frameNumber);
        ref var slot = ref self.Slots[index];

        if (!slot.Occupied || slot.Clearable)
        {
            error =
                $"标记可清空失败：槽不可标记, index={index}, frameNumber={frameNumber}, occupied={slot.Occupied}, clearable={slot.Clearable}";
            return false;
        }

        if (!Matches(slot, frameNumber))
        {
            error =
                $"标记可清空失败：帧号不匹配, index={index}, expected={frameNumber}, actual={slot.Frame.frame_number}";
            return false;
        }

        slot.Clearable = true;
        error = null;
        return true;
    }

    public static void Clear(this RoomFrameWindowEntity self)
    {
        for (var i = 0; i < self.Capacity; i++)
        {
            ref var slot = ref self.Slots[i];
            ResetContent(ref slot);
            slot.Occupied = false;
            slot.Clearable = false;
        }
    }

    private static int SlotIndex(RoomFrameWindowEntity self, ulong frameNumber)
    {
        return (int)(frameNumber % (ulong)self.Capacity);
    }

    private static bool Matches(RoomFrameWindowEntity.Slot slot, ulong frameNumber)
    {
        return slot.Frame.frame_number == frameNumber;
    }

    private static void ResetContent(ref RoomFrameWindowEntity.Slot slot)
    {
        if (slot.Frame.frames is { Count: > 0 })
        {
            foreach (var op in slot.Frame.frames)
            {
                op?.Dispose();
            }

            slot.Frame.frames.Clear();
        }

        slot.Frame.frame_number = default;
        slot.Frame.random_seed = default;

        if (slot.Frame.meta != null)
        {
            slot.Frame.meta.Dispose();
            slot.Frame.meta = null!;
        }
    }
}
