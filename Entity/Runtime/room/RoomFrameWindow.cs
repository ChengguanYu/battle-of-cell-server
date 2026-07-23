using Fantasy;

namespace Entity.Runtime.room;

/// <summary>
/// 按帧号索引的预分配帧窗口。
/// 每个槽聚合消息对象与占用/可清标记；写帧只改槽内容，不新建消息对象。
/// 可写条件：未占用，或已标记可清（Clearable）；占用且未可清时拒绝异帧覆盖。
/// 非线程安全。
/// </summary>
public sealed class RoomFrameWindow
{
    private readonly Slot[] _slots;

    public RoomFrameWindow(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "容量必须大于 0。");
        }

        Capacity = capacity;
        _slots = new Slot[capacity];

        for (var i = 0; i < capacity; i++)
        {
            // 预分配常驻消息：autoReturn=false，生命周期由窗口持有，写路径禁止 Create。
            _slots[i] = new Slot(server_frame.Create(autoReturn: false));
        }
    }

    /// <summary>固定槽位数。</summary>
    public int Capacity { get; }

    /// <summary>
    /// 槽是否占用中（含已消费、待下次写入清空的 Clearable）。
    /// 帧号不匹配时视为未占用该帧。
    /// </summary>
    public bool IsOccupied(ulong frameNumber)
    {
        ref var slot = ref _slots[SlotIndex(frameNumber)];
        return slot.Occupied && slot.Matches(frameNumber);
    }

    /// <summary>
    /// 槽是否已标记可清（消费完成，下次写入前可清空）。
    /// 帧号不匹配时返回 false。
    /// </summary>
    public bool IsClearable(ulong frameNumber)
    {
        ref var slot = ref _slots[SlotIndex(frameNumber)];
        return slot.Occupied && slot.Clearable && slot.Matches(frameNumber);
    }

    /// <summary>
    /// 将指定帧号对应槽重置为空帧（仅改字段，不新建对象）。
    /// 可写：未占用，或 Clearable；占用且未可清时仅允许同帧幂等。
    /// </summary>
    public bool TryWriteEmpty(ulong frameNumber, out string? error)
    {
        var index = SlotIndex(frameNumber);
        ref var slot = ref _slots[index];

        if (slot.Occupied && !slot.Clearable)
        {
            if (slot.Matches(frameNumber))
            {
                // 同帧重复写：保持占用、未可清（空帧场景幂等）
                error = null;
                return true;
            }

            error =
                $"槽位未消费不可覆盖: index={index}, occupiedFrame={slot.Frame.frame_number}, writeFrame={frameNumber}";
            return false;
        }

        // 未占用，或 Clearable：写入前清空
        slot.ResetContent();
        slot.Frame.frame_number = frameNumber;
        slot.Occupied = true;
        slot.Clearable = false;
        error = null;
        return true;
    }

    /// <summary>
    /// 按帧号读取槽位。仅占用且未可清、帧号匹配时可读。
    /// </summary>
    public bool TryGet(ulong frameNumber, out server_frame? frame, out string? error)
    {
        frame = null;
        var index = SlotIndex(frameNumber);
        ref var slot = ref _slots[index];

        if (!slot.Occupied || slot.Clearable)
        {
            error =
                $"槽位不可读: index={index}, frameNumber={frameNumber}, occupied={slot.Occupied}, clearable={slot.Clearable}";
            return false;
        }

        if (!slot.Matches(frameNumber))
        {
            error =
                $"帧号不匹配: index={index}, expected={frameNumber}, actual={slot.Frame.frame_number}";
            return false;
        }

        frame = slot.Frame;
        error = null;
        return true;
    }

    /// <summary>
    /// 标记帧已消费：Clearable=true，下次写入前可清空复用。
    /// 仅占用、未可清、帧号匹配时成功。
    /// </summary>
    public bool TryMarkClearable(ulong frameNumber, out string? error)
    {
        var index = SlotIndex(frameNumber);
        ref var slot = ref _slots[index];

        if (!slot.Occupied || slot.Clearable)
        {
            error =
                $"标记可清空失败：槽不可标记, index={index}, frameNumber={frameNumber}, occupied={slot.Occupied}, clearable={slot.Clearable}";
            return false;
        }

        if (!slot.Matches(frameNumber))
        {
            error =
                $"标记可清空失败：帧号不匹配, index={index}, expected={frameNumber}, actual={slot.Frame.frame_number}";
            return false;
        }

        slot.Clearable = true;
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
            ref var slot = ref _slots[i];
            slot.ResetContent();
            slot.Occupied = false;
            slot.Clearable = false;
        }
    }

    private int SlotIndex(ulong frameNumber)
    {
        return (int)(frameNumber % (ulong)Capacity);
    }

    /// <summary>
    /// 单个环形槽。
    /// Occupied：是否持有一帧数据；Clearable：是否已消费、可在下次写入前清空。
    /// 未占用即空闲，不单独建 Empty 状态。
    /// </summary>
    private struct Slot
    {
        public readonly server_frame Frame;
        public bool Occupied;
        public bool Clearable;

        public Slot(server_frame frame)
        {
            Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            Occupied = false;
            Clearable = false;
        }

        public bool Matches(ulong frameNumber) => Frame.frame_number == frameNumber;

        /// <summary>
        /// 重置槽内容但不 Return 到消息池（预分配对象常驻窗口）。
        /// </summary>
        public void ResetContent()
        {
            // server_frame.Dispose 在非池对象上直接 return，不能用来清字段；手动清。
            if (Frame.frames is { Count: > 0 })
            {
                foreach (var op in Frame.frames)
                {
                    op?.Dispose();
                }

                Frame.frames.Clear();
            }

            Frame.frame_number = default;
            Frame.randomSeed = default;

            if (Frame.meta != null)
            {
                Frame.meta.Dispose();
                Frame.meta = null!;
            }
        }
    }
}
