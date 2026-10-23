using System.Buffers;

namespace Aarkam.Kdouja.LSM.Primitives.Memory
{
    /// <summary>
    /// A thread-safe, deterministic resource wrapper implementing <see cref="IMemoryOwner{T}"/> 
    /// that handles renting and recycling byte array buffers using the shared system array pool.
    /// </summary>
    public sealed class PooledBuffer : IMemoryOwner<byte>
    {
        private readonly int _length;
        private readonly bool _clearOnDispose;
        private byte[]? _array;

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledBuffer"/> class, renting an internal tracking array 
        /// matching at least the specified minimum length.
        /// </summary>
        /// <param name="minimumLength">The baseline minimum capacity in bytes requested by the calling system component.</param>
        /// <param name="clearOnDispose"><see langword="true"/> to zero the contents of the array before returning it to the shared pool, preventing sensitive plaintext or key material from persisting in shared memory.</param>
        public PooledBuffer(int minimumLength, bool clearOnDispose = true)
        {
            if (minimumLength < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumLength));

            _array = ArrayPool<byte>.Shared.Rent(minimumLength);
            _length = minimumLength;
            _clearOnDispose = clearOnDispose;

            Diagnostics.PrimitiveMetrics.BufferRented.Add(1);
        }

        /// <inheritdoc/>
        public Memory<byte> Memory
        {
            get
            {
                var array = _array;
                if (array == null)
                    throw new ObjectDisposedException(nameof(PooledBuffer));

                return new Memory<byte>(array, 0, _length);
            }
        }

        /// <summary>
        /// Gets a high-performance mutable memory span mapped over the active rented data block segment, truncated precisely to the requested length.
        /// </summary>
        public Span<byte> Span
        {
            get
            {
                var array = _array;
                if (array == null)
                    throw new ObjectDisposedException(nameof(PooledBuffer));

                return new Span<byte>(array, 0, _length);
            }
        }

        /// <summary>
        /// Releases the underlying byte array back into the central shared pool resource repository and invalidates all active tracking states.
        /// Thread-safe and idempotent against duplicate or concurrent dispose calls.
        /// </summary>
        public void Dispose()
        {
            byte[]? array = Interlocked.Exchange(ref _array, null);
            if (array != null)
            {
                ArrayPool<byte>.Shared.Return(array, _clearOnDispose);
                Diagnostics.PrimitiveMetrics.BufferReturned.Add(1);
            }
        }
    }
}
