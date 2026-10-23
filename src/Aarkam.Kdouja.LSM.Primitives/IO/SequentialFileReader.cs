namespace Aarkam.Kdouja.LSM.Primitives.IO
{
    /// <summary>
    /// Provides a high-performance, forward-only buffered reader tailored for fast sequential file or stream access 
    /// within the storage engine execution layer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> When the storage subsystem needs to read extensive linear file assets—such as parsing 
    /// a Write-Ahead Log (WAL) during recovery or executing a table scan across immutable SSTables—invoking countless micro-reads 
    /// directly against the OS layer causes severe I/O degradation. This reader wraps access inside a pre-allocated application memory buffer, 
    /// consolidating unmanaged disk block calls into massive, single-pass memory operations.
    /// </para>
    /// <para>
    /// <b>Performance Profile:</b> Designed intentionally for sequential operations, it handles variable-length fields efficiently via 
    /// <see cref="Span{T}"/> slices, keeping managed garbage collection allocations at zero on the critical read path.
    /// </para>
    /// </remarks>
    public sealed class SequentialFileReader : IDisposable
    {
        /// <summary>
        /// The raw unmanaged stream tracking handle driving the underlying data source channel.
        /// </summary>
        private readonly Stream _stream;

        /// <summary>
        /// The pre-allocated application-managed block buffer byte array tracking active page chunks.
        /// </summary>
        private readonly byte[] _buffer;

        /// <summary>
        /// The active numerical array index offset pinpointing the current reading head inside the application <see cref="_buffer"/>.
        /// </summary>
        private int _bufferOffset;

        /// <summary>
        /// The physical payload length boundary representing valid, populated data currently residing within the application <see cref="_buffer"/>.
        /// </summary>
        private int _bufferLength;

        /// <summary>
        /// A conditional flag dictating whether the target <see cref="_stream"/> should remain open following object disposal loops.
        /// </summary>
        private readonly bool _leaveOpen;

        /// <summary>
        /// The logical absolute stream coordinate tracking current global positions across the storage asset timeline.
        /// </summary>
        private long _position;

        /// <summary>
        /// Initializes a new instance of the <see cref="SequentialFileReader"/> class over a designated data channel using customized sizing properties.
        /// </summary>
        /// <param name="stream">The source readable data stream handle (e.g., a <see cref="FileStream"/> referencing an SSTable segment on disk).</param>
        /// <param name="bufferSize">The physical size parameter in bytes allocated for the internal application cache layer; defaults to <c>65,536</c> ($64$ KB).</param>
        /// <param name="leaveOpen"><see langword="true"/> to leave the source stream handle open after the reader context terminates; otherwise, <see langword="false"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided input <paramref name="stream"/> context evaluates to null.</exception>
        /// <exception cref="ArgumentException">Thrown if the provided input <paramref name="stream"/> fails to support structural read flags.</exception>
        public SequentialFileReader(Stream stream, int bufferSize = 65536, bool leaveOpen = false)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable.", nameof(stream));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "Must be greater than zero.");

            _buffer = new byte[bufferSize];
            _leaveOpen = leaveOpen;
            _position = stream.CanSeek ? stream.Position : 0;
        }

        /// <summary>
        /// Gets the logical absolute location coordinate tracking index tracking positions inside the active database asset layer.
        /// </summary>
        /// <value>A 64-bit integer index matching the current logical file stream coordinate position.</value>
        public long Position => _position;

        /// <summary>
        /// Pulls raw binary sequences from the buffered channel directly into a target destination memory span layout, advancing internal pointer indexes.
        /// </summary>
        /// <param name="destination">The pre-allocated mutable target memory span receiving the parsed data output sequence.</param>
        /// <returns>The total number of valid bytes successfully extracted and copied into target memory fields.</returns>
        /// <remarks>
        /// <b>Allocation Free Profile:</b> Leverages underlying array slicing to copy chunks directly out of the internal 
        /// cache window without generating heap garbage collection artifacts, which is critical for parsing transaction block headers.
        /// </remarks>
        public int Read(Span<byte> destination)
        {
            int bytesRead = 0;
            int remaining = destination.Length;

            while (remaining > 0)
            {
                int available = _bufferLength - _bufferOffset;
                if (available == 0)
                {
                    if (!FillBuffer())
                    {
                        break;
                    }
                    available = _bufferLength - _bufferOffset;
                }

                int toCopy = Math.Min(available, remaining);
                _buffer.AsSpan(_bufferOffset, toCopy).CopyTo(destination.Slice(bytesRead));
                _bufferOffset += toCopy;
                bytesRead += toCopy;
                remaining -= toCopy;
                _position += toCopy;
            }

            Diagnostics.PrimitiveMetrics.IoBytesRead.Add(bytesRead);
            return bytesRead;
        }

        /// <summary>
        /// Extracts a single standalone byte out of the sequential stream path.
        /// </summary>
        /// <returns>The byte value read parsed as a 32-bit signed integer format loop, or <c>-1</c> if stream capacity invariants indicate an EOF marker.</returns>
        /// <remarks>
        /// Extensively applied inside low-level deserialization token parsers to evaluate field descriptor types or state configuration byte indicators.
        /// </remarks>
        public int ReadByte()
        {
            int available = _bufferLength - _bufferOffset;
            if (available == 0)
            {
                if (!FillBuffer())
                {
                    return -1;
                }
            }

            byte val = _buffer[_bufferOffset++];
            _position++;

            Diagnostics.PrimitiveMetrics.IoBytesRead.Add(1);
            return val;
        }

        /// <summary>
        /// Skips the specified number of bytes forward in the stream, optimizing through seek coordinates or iterative buffer discarding where necessary.
        /// </summary>
        /// <param name="bytesToSkip">The physical count indicating how many bytes to skip over in the data line.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the provided skip count parameter evaluates to a negative numerical coordinate.</exception>
        /// <exception cref="EndOfStreamException">Thrown if an unseekable stream terminates prematurely before completing the skip request.</exception>
        /// <remarks>
        /// <b>Hybrid Seeking Optimization:</b> If the underlying source channel natively supports tracking coordinates (<see cref="Stream.CanSeek"/>), 
        /// the reader flushes cache windows and uses a fast hardware disk seek instruction. For unseekable streams (like network streams), it falls back to 
        /// looping block discards to keep execution stable.
        /// </remarks>
        public void Skip(long bytesToSkip)
        {
            if (bytesToSkip < 0)
                throw new ArgumentOutOfRangeException(nameof(bytesToSkip));

            long remaining = bytesToSkip;
            while (remaining > 0)
            {
                int available = _bufferLength - _bufferOffset;
                if (available == 0)
                {
                    if (_stream.CanSeek)
                    {
                        _stream.Seek(remaining, SeekOrigin.Current);
                        _bufferOffset = 0;
                        _bufferLength = 0;
                        _position += remaining;
                        break;
                    }

                    if (!FillBuffer())
                    {
                        throw new EndOfStreamException("Reached end of stream while skipping bytes.");
                    }
                    available = _bufferLength - _bufferOffset;
                }

                int toSkip = (int)Math.Min(available, remaining);
                _bufferOffset += toSkip;
                remaining -= toSkip;
                _position += toSkip;
            }
        }

        /// <summary>
        /// Executes a low-level physical read payload flush operation, filling the internal cache buffer straight from the source stream handle.
        /// </summary>
        /// <returns><see langword="true"/> if data was successfully fetched from the stream; <see langword="false"/> if the stream is exhausted.</returns>
        private bool FillBuffer()
        {
            _bufferOffset = 0;
            _bufferLength = _stream.Read(_buffer, 0, _buffer.Length);
            return _bufferLength > 0;
        }

        /// <summary>
        /// Releases all unmanaged system handles, file streams, and tracking resources utilized by this reader context.
        /// </summary>
        /// <remarks>
        /// If the initial setup parameter <see cref="_leaveOpen"/> was configured to <see langword="false"/>, the underlying stream 
        /// connection closes deterministically upon invocation.
        /// </remarks>
        public void Dispose()
        {
            if (!_leaveOpen)
            {
                _stream.Dispose();
            }
        }
    }
}
