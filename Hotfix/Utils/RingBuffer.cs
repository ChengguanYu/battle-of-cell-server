using System.Collections;

namespace Hotfix.Utils;

/// <summary>
/// 环形缓冲区满时的写入策略。
/// </summary>
public enum RingBufferFullPolicy
{
    /// <summary>覆盖最旧元素（默认）。</summary>
    OverwriteOldest = 0,

    /// <summary>丢弃本次写入，保留已有数据。</summary>
    DropNewest = 1,

    /// <summary>抛出 <see cref="InvalidOperationException"/>。</summary>
    Throw = 2,
}

/// <summary>
/// 可配置的泛型环形缓冲区。
/// 固定容量、O(1) 入队/出队；满缓冲行为由 <see cref="RingBufferFullPolicy"/> 决定。
/// 非线程安全。
/// </summary>
/// <typeparam name="T">元素类型。</typeparam>
public sealed class RingBuffer<T> : IReadOnlyCollection<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _tail;
    private int _count;

    /// <summary>
    /// 创建指定容量的环形缓冲区。
    /// </summary>
    /// <param name="capacity">容量，必须大于 0。</param>
    /// <param name="fullPolicy">写满时的策略，默认覆盖最旧元素。</param>
    public RingBuffer(int capacity, RingBufferFullPolicy fullPolicy = RingBufferFullPolicy.OverwriteOldest)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "容量必须大于 0。");
        }

        if (!Enum.IsDefined(fullPolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(fullPolicy), fullPolicy, "未知的满缓冲策略。");
        }

        Capacity = capacity;
        FullPolicy = fullPolicy;
        _buffer = new T[capacity];
    }

    /// <summary>固定容量。</summary>
    public int Capacity { get; }

    /// <summary>当前元素数量。</summary>
    public int Count => _count;

    /// <summary>写满时的策略。</summary>
    public RingBufferFullPolicy FullPolicy { get; }

    /// <summary>是否为空。</summary>
    public bool IsEmpty => _count == 0;

    /// <summary>是否已满。</summary>
    public bool IsFull => _count == Capacity;

    /// <summary>
    /// 写入一个元素。
    /// 满时按 <see cref="FullPolicy"/> 处理；
    /// <see cref="RingBufferFullPolicy.DropNewest"/> 时返回 <c>false</c>，其余成功返回 <c>true</c>。
    /// </summary>
    public bool Enqueue(T item)
    {
        if (_count == Capacity)
        {
            switch (FullPolicy)
            {
                case RingBufferFullPolicy.OverwriteOldest:
                    _buffer[_tail] = item;
                    _tail = Next(_tail);
                    _head = _tail;
                    return true;

                case RingBufferFullPolicy.DropNewest:
                    return false;

                case RingBufferFullPolicy.Throw:
                    throw new InvalidOperationException($"环形缓冲区已满，容量={Capacity}。");

                default:
                    throw new InvalidOperationException($"未处理的满缓冲策略: {FullPolicy}");
            }
        }

        _buffer[_tail] = item;
        _tail = Next(_tail);
        _count++;
        return true;
    }

    /// <summary>
    /// 尝试写入；满且策略为 <see cref="RingBufferFullPolicy.Throw"/> 时返回 <c>false</c> 而不抛异常。
    /// </summary>
    public bool TryEnqueue(T item)
    {
        if (_count == Capacity && FullPolicy == RingBufferFullPolicy.Throw)
        {
            return false;
        }

        return Enqueue(item);
    }

    /// <summary>
    /// 弹出最旧元素。
    /// </summary>
    /// <exception cref="InvalidOperationException">缓冲区为空。</exception>
    public T Dequeue()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("环形缓冲区为空。");
        }

        var item = _buffer[_head];
        _buffer[_head] = default!;
        _head = Next(_head);
        _count--;
        return item;
    }

    /// <summary>
    /// 尝试弹出最旧元素。
    /// </summary>
    public bool TryDequeue(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        item = Dequeue();
        return true;
    }

    /// <summary>
    /// 查看最旧元素，不移除。
    /// </summary>
    /// <exception cref="InvalidOperationException">缓冲区为空。</exception>
    public T Peek()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("环形缓冲区为空。");
        }

        return _buffer[_head];
    }

    /// <summary>
    /// 尝试查看最旧元素，不移除。
    /// </summary>
    public bool TryPeek(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        item = _buffer[_head];
        return true;
    }

    /// <summary>
    /// 查看最新元素，不移除。
    /// </summary>
    /// <exception cref="InvalidOperationException">缓冲区为空。</exception>
    public T PeekNewest()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("环形缓冲区为空。");
        }

        var index = _tail == 0 ? Capacity - 1 : _tail - 1;
        return _buffer[index];
    }

    /// <summary>
    /// 尝试查看最新元素，不移除。
    /// </summary>
    public bool TryPeekNewest(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        item = PeekNewest();
        return true;
    }

    /// <summary>
    /// 按从旧到新的逻辑下标访问。0 为最旧，Count-1 为最新。
    /// </summary>
    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"下标越界，Count={_count}。");
            }

            return _buffer[PhysicalIndex(index)];
        }
        set
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"下标越界，Count={_count}。");
            }

            _buffer[PhysicalIndex(index)] = value;
        }
    }

    /// <summary>
    /// 清空缓冲区；引用类型元素会被置为 default，便于 GC。
    /// </summary>
    public void Clear()
    {
        if (_count == 0)
        {
            _head = 0;
            _tail = 0;
            return;
        }

        if (_head < _tail)
        {
            Array.Clear(_buffer, _head, _count);
        }
        else
        {
            Array.Clear(_buffer, _head, Capacity - _head);
            Array.Clear(_buffer, 0, _tail);
        }

        _head = 0;
        _tail = 0;
        _count = 0;
    }

    /// <summary>
    /// 复制到新数组，顺序从旧到新。
    /// </summary>
    public T[] ToArray()
    {
        var result = new T[_count];
        CopyTo(result, 0);
        return result;
    }

    /// <summary>
    /// 复制到目标数组，顺序从旧到新。
    /// </summary>
    public void CopyTo(T[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        if (arrayIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, "arrayIndex 不能为负数。");
        }

        if (array.Length - arrayIndex < _count)
        {
            throw new ArgumentException("目标数组剩余空间不足。", nameof(array));
        }

        if (_count == 0)
        {
            return;
        }

        if (_head < _tail)
        {
            Array.Copy(_buffer, _head, array, arrayIndex, _count);
            return;
        }

        var first = Capacity - _head;
        Array.Copy(_buffer, _head, array, arrayIndex, first);
        Array.Copy(_buffer, 0, array, arrayIndex + first, _tail);
    }

    /// <summary>
    /// 从旧到新遍历。
    /// </summary>
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private int Next(int index)
    {
        var next = index + 1;
        return next == Capacity ? 0 : next;
    }

    private int PhysicalIndex(int logicalIndex)
    {
        var index = _head + logicalIndex;
        return index >= Capacity ? index - Capacity : index;
    }

    /// <summary>
    /// 值类型枚举器，避免遍历时的堆分配。
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly RingBuffer<T> _buffer;
        private readonly int _count;
        private int _index;
        private T? _current;

        internal Enumerator(RingBuffer<T> buffer)
        {
            _buffer = buffer;
            _count = buffer._count;
            _index = -1;
            _current = default;
        }

        public T Current => _current!;

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            var next = _index + 1;
            if (next >= _count)
            {
                _index = _count;
                _current = default;
                return false;
            }

            _index = next;
            _current = _buffer[next];
            return true;
        }

        public void Reset()
        {
            _index = -1;
            _current = default;
        }

        public void Dispose()
        {
        }
    }
}
