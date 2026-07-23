using Fantasy;

namespace Entity.Runtime.room;

/// <summary>
/// 按帧号索引的预分配帧窗口。
/// 构造时固定分配 <see cref="server_frame"/> 槽位；写帧只修改槽内容，不新建消息对象。
/// 非线程安全。
/// </summary>
public sealed class RoomFrameWindow
{
    private readonly server_frame[] _slots;
    private readonly bool[] _occupied;

    public RoomFrameWindow(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "容量必须大于 0。");
        }

        Capacity = capacity;
        _slots = new server_frame[capacity];
        _occupied = new bool[capacity];

        for (var i = 0; i < capacity; i++)
        {
            // 预分配常驻槽：autoReturn=false，生命周期由窗口持有，写路径禁止 Create。
            _slots[i] = server_frame.Create(autoReturn: false);
            _occupied[i] = false;
        }
    }

    /// <summary>固定槽位数。</summary>
    public int Capacity { get; }

    /// <summary>
    /// 将指定帧号对应槽重置为空帧（仅改字段，不新建对象）。
    /// </summary>
    /// <param name="frameNumber">目标帧号。</param>
    /// <param name="error">失败原因；成功为 null。</param>
    /// <returns>是否写入成功。</returns>
    public bool TryWriteEmpty(ulong frameNumber, out string? error)
    {
        var index = SlotIndex(frameNumber);
        var slot = _slots[index];
        if (slot == null)
        {
            error = $"槽位为空: index={index}, frameNumber={frameNumber}";
            return false;
        }

        ResetSlotContent(slot);
        slot.frame_number = frameNumber;
        _occupied[index] = true;
        error = null;
        return true;
    }

    /// <summary>
    /// 按帧号读取槽位。槽未写入或帧号不匹配时失败。
    /// </summary>
    /// <param name="frameNumber">目标帧号。</param>
    /// <param name="frame">命中时返回槽内对象（只读使用，调用方不得归还池）。</param>
    /// <param name="error">失败原因；成功为 null。</param>
    public bool TryGet(ulong frameNumber, out server_frame? frame, out string? error)
    {
        frame = null;
        var index = SlotIndex(frameNumber);
        if (!_occupied[index])
        {
            error = $"槽位未写入: index={index}, frameNumber={frameNumber}";
            return false;
        }

        var slot = _slots[index];
        if (slot == null)
        {
            error = $"槽位为空: index={index}, frameNumber={frameNumber}";
            return false;
        }

        if (slot.frame_number != frameNumber)
        {
            error =
                $"帧号不匹配: index={index}, expected={frameNumber}, actual={slot.frame_number}";
            return false;
        }

        frame = slot;
        error = null;
        return true;
    }

    /// <summary>
    /// 清空所有槽的占用与内容，保留预分配对象。
    /// </summary>
    public void Clear()
    {
        for (var i = 0; i < Capacity; i++)
        {
            var slot = _slots[i];
            if (slot != null)
            {
                ResetSlotContent(slot);
            }

            _occupied[i] = false;
        }
    }

    private int SlotIndex(ulong frameNumber)
    {
        return (int)(frameNumber % (ulong)Capacity);
    }

    /// <summary>
    /// 重置槽内容但不 Return 到消息池（预分配对象常驻窗口）。
    /// </summary>
    private static void ResetSlotContent(server_frame slot)
    {
        // server_frame.Dispose 在非池对象上直接 return，不能用来清字段；手动清。
        if (slot.frames is { Count: > 0 })
        {
            foreach (var op in slot.frames)
            {
                op?.Dispose();
            }

            slot.frames.Clear();
        }

        slot.frame_number = default;
        slot.randomSeed = default;

        if (slot.meta != null)
        {
            slot.meta.Dispose();
            slot.meta = null!;
        }
    }
}
