namespace Entity.Utils;

/// <summary>
/// 可回收随机 uint ID 生成器。
/// 模块内维护占用集合与空闲列表：Acquire 优先从空闲中随机取回，否则生成新随机 ID；
/// Release 解除占用后可供再次分配。
/// 均摊 O(1)（空闲池交换删除 + HashSet）。非线程安全，由调用方串行访问。
/// </summary>
public sealed class RecyclableUIntIdGenerator
{
    private readonly HashSet<uint> _occupied = new();
    private readonly List<uint> _free = new();
    private readonly uint _minInclusive;
    private readonly uint _maxExclusive;
    private readonly int _maxRandomAttempts;

    /// <param name="minInclusive">可分配下界（含），默认 1（排除 0）。</param>
    /// <param name="maxExclusive">可分配上界（不含），默认 <see cref="uint.MaxValue"/>。</param>
    /// <param name="maxRandomAttempts">空闲池为空时，随机撞库最大尝试次数。</param>
    public RecyclableUIntIdGenerator(
        uint minInclusive = 1,
        uint maxExclusive = uint.MaxValue,
        int maxRandomAttempts = 64)
    {
        if (minInclusive >= maxExclusive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxExclusive),
                "maxExclusive 必须大于 minInclusive。");
        }

        if (maxRandomAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRandomAttempts));
        }

        _minInclusive = minInclusive;
        _maxExclusive = maxExclusive;
        _maxRandomAttempts = maxRandomAttempts;
    }

    /// <summary>当前已占用数量。</summary>
    public int OccupiedCount => _occupied.Count;

    /// <summary>可立即复用的空闲数量。</summary>
    public int FreeCount => _free.Count;

    /// <summary>
    /// 分配一个当前未占用的随机 ID。
    /// 优先从空闲池随机取；池空则在范围内随机生成。
    /// </summary>
    public bool TryAcquire(out uint id)
    {
        if (_free.Count > 0)
        {
            var idx = Random.Shared.Next(_free.Count);
            id = _free[idx];
            var last = _free.Count - 1;
            _free[idx] = _free[last];
            _free.RemoveAt(last);
            _occupied.Add(id);
            return true;
        }

        var capacity = (ulong)_maxExclusive - _minInclusive;
        if ((ulong)_occupied.Count >= capacity)
        {
            id = 0;
            return false;
        }

        for (var attempt = 0; attempt < _maxRandomAttempts; attempt++)
        {
            var candidate = NextRandomInRange();
            if (_occupied.Add(candidate))
            {
                id = candidate;
                return true;
            }
        }

        // 范围较小时线性兜底；大范围仅依赖随机，避免 O(range) 扫描。
        if (capacity <= 4096)
        {
            for (var candidate = _minInclusive; candidate < _maxExclusive; candidate++)
            {
                if (_occupied.Add(candidate))
                {
                    id = candidate;
                    return true;
                }
            }
        }

        id = 0;
        return false;
    }

    /// <summary>
    /// 解除 ID 占用，使其可被再次分配。
    /// 对未占用 ID 返回 false。
    /// </summary>
    public bool Release(uint id)
    {
        if (!_occupied.Remove(id))
        {
            return false;
        }

        _free.Add(id);
        return true;
    }

    /// <summary>是否处于占用中。</summary>
    public bool IsOccupied(uint id) => _occupied.Contains(id);

    private uint NextRandomInRange()
    {
        // Random.Shared.Next 仅支持 int 区间；对 uint 全量用 NextInt64 映射。
        var span = (ulong)_maxExclusive - _minInclusive;
        if (span <= (ulong)int.MaxValue)
        {
            return _minInclusive + (uint)Random.Shared.Next((int)span);
        }

        var offset = (ulong)Random.Shared.NextInt64(0, (long)span);
        return _minInclusive + (uint)offset;
    }
}
