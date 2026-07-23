namespace Entity.Utils;

/// <summary>
/// 可回收有序 uint ID 生成器。
/// 分配策略：优先复用已释放 ID（LIFO），否则按递增序号取新号。
/// Release 解除占用后可供再次分配。
/// 均摊 O(1)。非线程安全，由调用方串行访问。
/// </summary>
public sealed class RecyclableUIntIdGenerator
{
    private readonly HashSet<uint> _occupied = new();
    private readonly List<uint> _free = new();
    private readonly uint _minInclusive;
    private readonly uint _maxExclusive;

    /// <summary>
    /// 下一个尚未发放过的递增 ID（空闲池为空时使用）。
    /// </summary>
    private uint _nextId;

    /// <param name="minInclusive">可分配下界（含），默认 1（排除 0）。</param>
    /// <param name="maxExclusive">可分配上界（不含），默认 <see cref="uint.MaxValue"/>。</param>
    public RecyclableUIntIdGenerator(
        uint minInclusive = 1,
        uint maxExclusive = uint.MaxValue)
    {
        if (minInclusive >= maxExclusive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxExclusive),
                "maxExclusive 必须大于 minInclusive。");
        }

        _minInclusive = minInclusive;
        _maxExclusive = maxExclusive;
        _nextId = minInclusive;
    }

    /// <summary>当前已占用数量。</summary>
    public int OccupiedCount => _occupied.Count;

    /// <summary>可立即复用的空闲数量。</summary>
    public int FreeCount => _free.Count;

    /// <summary>下一个将新发放的递增 ID（尚未占用；空闲池非空时实际可能先复用）。</summary>
    public uint NextSequentialId => _nextId;

    /// <summary>
    /// 分配一个当前未占用的 ID。
    /// 优先从空闲栈弹出；否则发放递增新号。
    /// </summary>
    public bool TryAcquire(out uint id)
    {
        if (_free.Count > 0)
        {
            var last = _free.Count - 1;
            id = _free[last];
            _free.RemoveAt(last);
            _occupied.Add(id);
            return true;
        }

        if (_nextId >= _maxExclusive)
        {
            id = 0;
            return false;
        }

        id = _nextId;
        _nextId++;
        _occupied.Add(id);
        return true;
    }

    /// <summary>
    /// 解除 ID 占用，使其可被再次分配。
    /// 对未占用 ID 返回 false。
    /// </summary>
    public bool Release(uint id)
    {
        if (id < _minInclusive || id >= _maxExclusive)
        {
            return false;
        }

        if (!_occupied.Remove(id))
        {
            return false;
        }

        _free.Add(id);
        return true;
    }

    /// <summary>是否处于占用中。</summary>
    public bool IsOccupied(uint id) => _occupied.Contains(id);
}
