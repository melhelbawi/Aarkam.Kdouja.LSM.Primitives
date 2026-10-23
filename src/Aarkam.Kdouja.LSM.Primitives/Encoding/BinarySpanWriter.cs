using System.Buffers.Binary;

namespace Aarkam.Kdouja.LSM.Primitives.Encoding
{
    /// <summary>
    /// A high-performance, stack-allocated binary stream formatter providing zero-allocation encoding routines over raw byte spans.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> This utility serves as the underlying structural formatting driver across all write pathways. 
    /// Because it is defined as a <see langword="ref struct"/>, it is allocated exclusively within the thread's execution stack frame. 
    /// This guarantees that complex serialization serialization workflows do not generate garbage collection churn.
    /// </para>
    /// <para>
    /// This architecture is specifically engineered to handle:
    /// <list type="bullet">
    /// <item><description><b>WAL Generation:</b> Sequentially formatting transaction headers and payload boundaries before physical disk commits.</description></item>
    /// <item><description><b>SSTable Flush Workers:</b> Assembling key-value page data block sections, trailer indices, and bloom filter metrics into contiguous buffers.</description></item>
    /// <item><description><b>Manifest Commits:</b> Emitting transactional checkpoint states and tracking active storage nodes during system changes.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public ref struct BinarySpanWriter
    {
        /// <summary>
        /// The underlying target destination byte memory frame wrapped by this writer context.
        /// </summary>
        private Span<byte> _buffer;

        /// <summary>
        /// The current internal byte position pointer indicating the active writing coordinate within the destination buffer.
        /// </summary>
        private int _position;

        /// <summary>
        /// Initializes a new instance of the stack-allocated <see cref="BinarySpanWriter"/> struct over a target destination byte sequence.
        /// </summary>
        /// <param name="buffer">The mutable destination memory span assigned to receive serialized data outputs.</param>
        public BinarySpanWriter(Span<byte> buffer)
        {
            _buffer = buffer;
            _position = 0;
        }

        /// <summary>
        /// Gets the absolute numerical offset index representing the active pointer coordinate within the underlying data buffer.
        /// </summary>
        /// <value>The current byte offset position index.</value>
        public readonly int Position => _position;

        /// <summary>
        /// Gets the total number of bytes remaining to be processed from the active writing position to the trailing edge of the buffer.
        /// </summary>
        /// <value>The remaining payload capacity measuring in bytes.</value>
        public readonly int Remaining => _buffer.Length - _position;

        /// <summary>
        /// Gets a read-only view tracking the exact sub-slice of bytes committed to memory since writer initialization.
        /// </summary>
        /// <value>A zero-allocation <see cref="ReadOnlySpan{T}"/> containing the written payload footprint context.</value>
        public readonly ReadOnlySpan<byte> WrittenSpan => _buffer.Slice(0, _position);

        /// <summary>
        /// Gets the raw, unsegmented mutable memory workspace passed into this instance frame.
        /// </summary>
        /// <value>The underlying mutable <see cref="Span{T}"/> data block instance.</value>
        public readonly Span<byte> Buffer => _buffer;

        /// <summary>
        /// Encodes a single raw byte into the target stream destination and advances the cursor coordinate by exactly $1$ byte.
        /// </summary>
        /// <param name="value">The byte value to write.</param>
        /// <exception cref="InvalidOperationException">Thrown if the underlying memory buffer is too small to absorb the operation.</exception>
        public void WriteByte(byte value)
        {
            EnsureCapacity(sizeof(byte));
            _buffer[_position++] = value;
        }

        /// <summary>
        /// Encodes a Boolean flag state by serializing a single byte ($1$ for <see langword="true"/>, $0$ for <see langword="false"/>) 
        /// at the current stream pointer coordinate.
        /// </summary>
        /// <param name="value">The conditional state flag to serialize.</param>
        /// <exception cref="InvalidOperationException">Thrown if space allocation constraints break buffer tracking boundaries.</exception>
        public void WriteBoolean(bool value)
        {
            WriteByte(value ? (byte)1 : (byte)0);
        }

        /// <summary>
        /// Encodes a 16-bit signed short integer utilizing Little-Endian byte-ordering format configurations.
        /// </summary>
        /// <param name="value">The 16-bit signed short integer to serialize.</param>
        /// <exception cref="InvalidOperationException">Thrown if fewer than $2$ bytes are available for configuration within the target stream space.</exception>
        public void WriteInt16(short value)
        {
            EnsureCapacity(sizeof(short));
            BinaryPrimitives.WriteInt16LittleEndian(_buffer.Slice(_position), value);
            _position += sizeof(short);
        }

        /// <summary>
        /// Encodes a 16-bit unsigned short integer utilizing Little-Endian byte-ordering format configurations.
        /// </summary>
        /// <param name="value">The 16-bit unsigned short integer to serialize.</param>
        /// <exception cref="InvalidOperationException">Thrown if fewer than $2$ bytes are available for configuration within the target stream space.</exception>
        public void WriteUInt16(ushort value)
        {
            EnsureCapacity(sizeof(ushort));
            BinaryPrimitives.WriteUInt16LittleEndian(_buffer.Slice(_position), value);
            _position += sizeof(ushort);
        }

        /// <summary>
        /// Encodes a 32-bit signed integer utilizing Little-Endian byte-ordering format configurations.
        /// </summary>
        /// <param name="value">The 32-bit signed integer to serialize.</param>
        /// <exception cref="InvalidOperationException">Thrown if fewer than $4$ bytes are available for configuration within the target stream space.</exception>
        /// <remarks>
        /// Heavily used when formatting structural metadata headers to pack fields such as string layout lengths or record key counts.
        /// </remarks>
        public void WriteInt32(int value)
        {
            EnsureCapacity(sizeof(int));
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.Slice(_position), value);
            _position += sizeof(int);
        }

        /// <summary>
        /// Encodes a 32-bit unsigned integer utilizing Little-Endian byte-ordering format configurations.
        /// </summary>
        /// <param name="value">The 32-bit unsigned integer to serialize.</param>
        /// <exception cref="InvalidOperationException">Thrown if fewer than $4$ bytes are available for configuration within the target stream space.</exception>
        public void WriteUInt32(uint value)
        {
            EnsureCapacity(sizeof(uint));
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.Slice(_position), value);
            _position += sizeof(uint);
        }

        /// <summary>
        /// Encodes a 64-bit signed long integer utilizing Little-Endian byte-ordering format configurations.
        /// </summary>
        /// <param name="value">The 64-bit signed long integer to serialize.</param>
        /// <exception cref="InvalidOperationException">Thrown if fewer than $8$ bytes are available for configuration within the target stream space.</exception>
        /// <remarks>
        /// Crucial for serializing global coordination tracks such as transaction sequence numbers, physical log offsets, or system timestamp markers.
        /// </remarks>
        public void WriteInt64(long value)
        {
            EnsureCapacity(sizeof(long));
            BinaryPrimitives.WriteInt64LittleEndian(_buffer.Slice(_position), value);
            _position += sizeof(long);
        }

        /// <summary>
        /// Encodes a 64-bit unsigned long integer utilizing Little-Endian byte-ordering format configurations.
        /// </summary>
        /// <param name="value">The 64-bit unsigned long integer to serialize.</param>
        /// <exception cref="InvalidOperationException">Thrown if fewer than $8$ bytes are available for configuration within the target stream space.</exception>
        public void WriteUInt64(ulong value)
        {
            EnsureCapacity(sizeof(ulong));
            BinaryPrimitives.WriteUInt64LittleEndian(_buffer.Slice(_position), value);
            _position += sizeof(ulong);
        }

        /// <summary>
        /// Replicates a raw sequence of bytes directly into the current stream position, advancing the internal tracker.
        /// </summary>
        /// <param name="value">The read-only byte block sequence to clone into memory lines.</param>
        /// <exception cref="InvalidOperationException">Thrown if the target allocation framework cannot contain the length of the input payload sequence.</exception>
        /// <remarks>
        /// Used when pushing record payload data segments directly into pooled memory buffers or specialized cache targets.
        /// </remarks>
        public void WriteBytes(ReadOnlySpan<byte> value)
        {
            EnsureCapacity(value.Length);
            value.CopyTo(_buffer.Slice(_position));
            _position += value.Length;
        }

        /// <summary>
        /// Artificially forces the internal writing stream pointer offset index forward by a designated byte length.
        /// </summary>
        /// <param name="count">The structural number of bytes to advance past current allocations.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the provided count variable is a negative coordinate length.</exception>
        /// <exception cref="InvalidOperationException">Thrown if pushing the pointer forward breaks the configured allocation limits of the destination buffer.</exception>
        /// <remarks>
        /// Typically leveraged to reserve precise padding spaces, record footers, or block alignment zones destined to be updated at a later point in the serialization track.
        /// </remarks>
        public void Advance(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
            EnsureCapacity(count);
            _position += count;
        }

        /// <summary>
        /// Performs low-level capacity verification checks to guarantee the target execution space can absorb subsequent operations safely.
        /// </summary>
        /// <param name="size">The physical size parameter evaluating in bytes requested by incoming write callers.</param>
        /// <exception cref="InvalidOperationException">Thrown if executing the write operation would cause a buffer overflow error.</exception>
        private readonly void EnsureCapacity(int size)
        {
            if (_position + size > _buffer.Length)
            {
                throw new InvalidOperationException($"Destination buffer is too small. Position: {_position}, Requested: {size}, Total Length: {_buffer.Length}");
            }
        }
    }
}