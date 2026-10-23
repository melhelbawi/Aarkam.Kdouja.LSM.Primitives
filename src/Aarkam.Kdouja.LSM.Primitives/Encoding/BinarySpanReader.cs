using System.Buffers.Binary;

namespace Aarkam.Kdouja.LSM.Primitives.Encoding
{
    /// <summary>
    /// A high-performance, stack-allocated binary stream parser providing zero-allocation decoding routines over raw byte spans.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> This utility serves as the underlying structural parsing driver across all read pathways. 
    /// Because it is defined as a <see langword="ref struct"/>, it is allocated exclusively within the thread's execution stack frame. 
    /// This guarantees that complex deserialization jobs do not generate garbage collection churn.
    /// </para>
    /// <para>
    /// This architecture is specifically engineered to handle:
    /// <list type="bullet">
    /// <item><description><b>WAL Parsing:</b> Sequentially streaming transactional record structures during system crash recovery phases.</description></item>
    /// <item><description><b>SSTable Block Decoding:</b> Navigating structured page records, data blocks, and index filters fetched from cold storage files.</description></item>
    /// <item><description><b>Manifest Deserialization:</b> Reconstituting system metadata snapshots and partition version states upon instance startup.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public ref struct BinarySpanReader
    {
        /// <summary>
        /// The underlying read-only byte source memory frame wrapped by this reader context.
        /// </summary>
        private readonly ReadOnlySpan<byte> _buffer;

        /// <summary>
        /// The current internal byte position pointer indicating the active reading coordinate within the source buffer.
        /// </summary>
        private int _position;

        /// <summary>
        /// Initializes a new instance of the stack-allocated <see cref="BinarySpanReader"/> struct over a target byte sequence.
        /// </summary>
        /// <param name="buffer">The target read-only binary memory span containing serialized transaction or record blocks.</param>
        public BinarySpanReader(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
            _position = 0;
        }

        /// <summary>
        /// Gets the total number of bytes remaining to be processed from the active reading position to the trailing edge of the buffer.
        /// </summary>
        /// <value>The remaining payload capacity measuring in bytes.</value>
        public readonly int Remaining => _buffer.Length - _position;

        /// <summary>
        /// Gets the absolute numerical offset index representing the active pointer coordinate within the underlying data buffer.
        /// </summary>
        /// <value>The current byte offset position index.</value>
        public readonly int Position => _position;

        /// <summary>
        /// Extracts a single raw byte from the current stream position and advances the cursor coordinate by exactly $1$ byte.
        /// </summary>
        /// <returns>The decoded byte value located at the current stream coordinate position.</returns>
        /// <exception cref="InvalidOperationException">Thrown if there are no remaining bytes left to process inside the source span boundary.</exception>
        public byte ReadByte()
        {
            if (Remaining < 1)
                throw new InvalidOperationException($"Cannot read 1 byte; only {Remaining} remain.");
            return _buffer[_position++];
        }

        /// <summary>
        /// Decodes a Boolean flag condition by evaluating a single byte at the current stream pointer coordinate.
        /// </summary>
        /// <returns><see langword="true"/> if the target byte evaluates to a non-zero coordinate value; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if buffer boundary tracking detects an underflow condition.</exception>
        public bool ReadBoolean()
        {
            return ReadByte() != 0;
        }

        /// <summary>
        /// Extracts a 16-bit signed integer value utilizing Little-Endian byte-ordering format configurations.
        /// </summary>
        /// <returns>The decoded 16-bit signed short integer value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if fewer than $2$ bytes are available for evaluation within the current stream scope.</exception>
        public short ReadInt16()
        {
            if (Remaining < 2)
                throw new InvalidOperationException($"Cannot read 2 bytes; only {Remaining} remain.");
            var value = BinaryPrimitives.ReadInt16LittleEndian(_buffer.Slice(_position, 2));
            _position += 2;
            return value;
        }

        /// <summary>
        /// Extracts a 16-bit unsigned integer value utilizing Little-Endian byte-ordering format configurations.
        /// </summary>
        /// <returns>The decoded 16-bit unsigned short integer value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if fewer than $2$ bytes are available for evaluation within the current stream scope.</exception>
        public ushort ReadUInt16()
        {
            if (Remaining < 2)
                throw new InvalidOperationException($"Cannot read 2 bytes; only {Remaining} remain.");
            var value = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.Slice(_position, 2));
            _position += 2;
            return value;
        }

        /// <summary>
        /// Extracts a 32-bit signed integer value utilizing Little-Endian byte-ordering format configurations.
        /// </summary>
        /// <returns>The decoded 32-bit signed integer value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if fewer than $4$ bytes are available for evaluation within the current stream scope.</exception>
        /// <remarks>
        /// Heavily used when parsing structural metadata headers to unpack value fields such as string layout lengths or record key counts.
        /// </remarks>
        public int ReadInt32()
        {
            if (Remaining < 4)
                throw new InvalidOperationException($"Cannot read 4 bytes; only {Remaining} remain.");
            var value = BinaryPrimitives.ReadInt32LittleEndian(_buffer.Slice(_position, 4));
            _position += 4;
            return value;
        }

        /// <summary>
        /// Extracts a 32-bit unsigned integer value utilizing Little-Endian byte-ordering format configurations.
        /// </summary>
        /// <returns>The decoded 32-bit unsigned integer value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if fewer than $4$ bytes are available for evaluation within the current stream scope.</exception>
        public uint ReadUInt32()
        {
            if (Remaining < 4)
                throw new InvalidOperationException($"Cannot read 4 bytes; only {Remaining} remain.");
            var value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.Slice(_position, 4));
            _position += 4;
            return value;
        }

        /// <summary>
        /// Extracts a 64-bit signed integer value utilizing Little-Endian byte-ordering format configurations.
        /// </summary>
        /// <returns>The decoded 64-bit signed long integer value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if fewer than $8$ bytes are available for evaluation within the current stream scope.</exception>
        /// <remarks>
        /// Crucial for parsing global coordination tracks such as transaction sequence numbers, physical log offsets, or system timestamp markers.
        /// </remarks>
        public long ReadInt64()
        {
            if (Remaining < 8)
                throw new InvalidOperationException($"Cannot read 8 bytes; only {Remaining} remain.");
            var value = BinaryPrimitives.ReadInt64LittleEndian(_buffer.Slice(_position, 8));
            _position += 8;
            return value;
        }

        /// <summary>
        /// Extracts a 64-bit unsigned integer value utilizing Little-Endian byte-ordering format configurations.
        /// </summary>
        /// <returns>The decoded 64-bit unsigned long integer value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if fewer than $8$ bytes are available for evaluation within the current stream scope.</exception>
        public ulong ReadUInt64()
        {
            if (Remaining < 8)
                throw new InvalidOperationException($"Cannot read 8 bytes; only {Remaining} remain.");
            var value = BinaryPrimitives.ReadUInt64LittleEndian(_buffer.Slice(_position, 8));
            _position += 8;
            return value;
        }

        /// <summary>
        /// Extracts a sub-slice block window of a designated byte length, moving the stream tracking pointer forward past the window.
        /// </summary>
        /// <param name="length">The physical number of sequential bytes to carve out from the source buffer.</param>
        /// <returns>A zero-allocation <see cref="ReadOnlySpan{T}"/> view pointing directly to the sub-slice sequence inside the primary buffer footprint.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the requested slice boundary length outstrips the available capacity limits.</exception>
        /// <remarks>
        /// <b>Performance Profile:</b> This operation relies completely on span slicing mechanics. It returns a direct view into 
        /// existing memory without copying data or triggering heap allocations, which is ideal for isolating raw record keys or value payloads.
        /// </remarks>
        public ReadOnlySpan<byte> ReadBytes(int length)
        {
            if (Remaining < length)
                throw new InvalidOperationException($"Cannot read {length} bytes; only {Remaining} remain.");

            var slice = _buffer.Slice(_position, length);
            _position += length;
            return slice;
        }

        /// <summary>
        /// Copies raw binary data directly from the current stream position into a pre-allocated destination memory span.
        /// </summary>
        /// <param name="destination">The pre-allocated mutable destination target span where the sequence will be replicated.</param>
        /// <exception cref="InvalidOperationException">Thrown if the target destination length parameter outstrips available stream boundary fields.</exception>
        /// <remarks>
        /// Used when pulling record data segments directly into pooled memory buffers or specialized cache targets.
        /// </remarks>
        public void ReadBytes(Span<byte> destination)
        {
            int length = destination.Length;
            if (Remaining < length)
                throw new InvalidOperationException($"Cannot read {length} bytes; only {Remaining} remain.");

            _buffer.Slice(_position, length).CopyTo(destination);
            _position += length;
        }
    }
}