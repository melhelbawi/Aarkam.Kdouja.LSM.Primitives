using System.Runtime.InteropServices;

namespace Aarkam.Kdouja.LSM.Primitives.Collections
{
    /// <summary>
    /// A lightweight, allocation-free, and bounded circular queue (Ring Buffer) implementation optimized for sequential processing.
    /// </summary>
    /// <typeparam name="T">The type of structural elements or reference parameters managed within the circular layout.</typeparam>
    public sealed class RingBuffer<T>
    {
        private readonly T[] _buffer;
        private int _head;
        private int _tail;
        private int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="RingBuffer{T}"/> class with a fixed allocation ceiling.
        /// </summary>
        public RingBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be greater than zero.", nameof(capacity));
            _buffer = new T[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        /// <summary>
        /// Gets the exact total number of elements currently stored within the ring buffer bounds.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Gets the absolute capacity ceiling of the pre-allocated tracking vector.
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// Gets a value indicating whether the container contains zero active storage items.
        /// </summary>
        public bool IsEmpty => _count == 0;

        /// <summary>
        /// Gets a value indicating whether the internal element counter has reached the pre-allocated capacity boundary.
        /// </summary>
        public bool IsFull => _count == _buffer.Length;

        /// <summary>
        /// Attempts to push an item onto the back of the circular array without inducing execution blocks or memory reallocations.
        /// </summary>
        public bool TryEnqueue(T item)
        {
            if (IsFull)
                return false;

            _buffer[_tail] = item;
            _tail = (_tail + 1) % _buffer.Length;
            _count++;
            return true;
        }

        /// <summary>
        /// Enqueues an item onto the back of the circular array, throwing an exception if the allocation block is full.
        /// </summary>
        public void Enqueue(T item)
        {
            if (!TryEnqueue(item))
                throw new InvalidOperationException("Ring buffer is full.");
        }

        /// <summary>
        /// Attempts to extract and return the item staged at the front of the circular queue layout.
        /// </summary>
        public bool TryDequeue(out T item)
        {
            if (IsEmpty)
            {
                item = default!;
                return false;
            }

            item = _buffer[_head];
            _buffer[_head] = default!;
            _head = (_head + 1) % _buffer.Length;
            _count--;
            return true;
        }

        /// <summary>
        /// Extracts and returns the item positioned at the front of the circular structure, throwing an exception if empty.
        /// </summary>
        public T Dequeue()
        {
            if (!TryDequeue(out var item))
                throw new InvalidOperationException("Ring buffer is empty.");
            return item;
        }

        /// <summary>
        /// Attempts to view the item currently resting at the head position without altering internal state parameters or indices.
        /// </summary>
        public bool TryPeek(out T item)
        {
            if (IsEmpty)
            {
                item = default!;
                return false;
            }

            item = _buffer[_head];
            return true;
        }

        /// <summary>
        /// Resets all internal array offsets and counters, safely zeroing out references across the backing allocation span.
        /// </summary>
        public void Clear()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 192)]
    internal struct RingBufferCursors
    {
        [FieldOffset(64)] public long Enqueue;
        [FieldOffset(128)] public long Dequeue;
    }

    /// <summary>
    /// An ultra-high-performance, thread-safe bounded MPMC ring queue implementing Vyukov's array-based lock-free queue algorithm.
    /// </summary>
    /// <typeparam name="T">The transactional data type or tracking payload element stored inside the concurrent array matrix.</typeparam>
    public sealed class ConcurrentRingBuffer<T>
    {
        private struct Node
        {
            public T Item;
            public long Sequence;
        }

        private readonly Node[] _buffer;
        private readonly int _mask;
        private RingBufferCursors _cursors;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentRingBuffer{T}"/> class.
        /// </summary>
        /// <param name="capacity">The absolute size bounds of the circular vector. Must evaluate to an exact power of two and at least 2.</param>
        public ConcurrentRingBuffer(int capacity)
        {
            if (capacity < 2 || (capacity & (capacity - 1)) != 0)
            {
                throw new ArgumentException("Capacity must be a power of two and at least 2.", nameof(capacity));
            }

            _mask = capacity - 1;
            _buffer = new Node[capacity];

            for (int i = 0; i < capacity; i++)
            {
                _buffer[i].Sequence = i;
            }
        }

        /// <summary>
        /// Gets the absolute maximum structural capacity constraint of the concurrent lock-free buffer array.
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// Attempts to securely assign an element item into the ring buffer sequence across atomic boundaries.
        /// </summary>
        public bool TryEnqueue(T item)
        {
            Node[] buffer = _buffer;
            int mask = _mask;

            while (true)
            {
                long currentPos = Volatile.Read(ref _cursors.Enqueue);
                int index = (int)(currentPos & mask);
                long seq = Volatile.Read(ref buffer[index].Sequence);
                long diff = seq - currentPos;

                if (diff == 0)
                {
                    if (Interlocked.CompareExchange(ref _cursors.Enqueue, currentPos + 1, currentPos) == currentPos)
                    {
                        buffer[index].Item = item;
                        Volatile.Write(ref buffer[index].Sequence, currentPos + 1);
                        return true;
                    }
                }
                else if (diff < 0)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Attempts to atomically dequeue and clear an entry from the front of the buffer matrix without blocking concurrent writing pipelines.
        /// </summary>
        public bool TryDequeue(out T item)
        {
            Node[] buffer = _buffer;
            int mask = _mask;

            while (true)
            {
                long currentPos = Volatile.Read(ref _cursors.Dequeue);
                int index = (int)(currentPos & mask);
                long seq = Volatile.Read(ref buffer[index].Sequence);
                long diff = seq - (currentPos + 1);

                if (diff == 0)
                {
                    if (Interlocked.CompareExchange(ref _cursors.Dequeue, currentPos + 1, currentPos) == currentPos)
                    {
                        item = buffer[index].Item;
                        buffer[index].Item = default!;
                        Volatile.Write(ref buffer[index].Sequence, currentPos + mask + 1);
                        return true;
                    }
                }
                else if (diff < 0)
                {
                    item = default!;
                    return false;
                }
            }
        }
    }
}
