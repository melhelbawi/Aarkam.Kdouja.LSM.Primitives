namespace Aarkam.Kdouja.LSM.Primitives.IO
{
    /// <summary>
    /// Provides a high-performance, forward-only buffered writer tailored for fast sequential file or stream formatting 
    /// within the storage engine execution layer.
    /// </summary>
    public sealed class SequentialFileWriter : IDisposable
    {
        private readonly Stream _stream;
        private readonly byte[] _buffer;
        private readonly bool _leaveOpen;
        private int _bufferOffset;
        private long _position;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SequentialFileWriter"/> class over a designated data channel using customized sizing properties.
        /// </summary>
        /// <param name="stream">The destination writable data stream handle (e.g., a <see cref="FileStream"/> referencing an active WAL segment on disk).</param>
        /// <param name="bufferSize">The physical size parameter in bytes allocated for the internal application staging layer; defaults to <c>65,536</c> (64 KB).</param>
        /// <param name="leaveOpen"><see langword="true"/> to leave the destination stream handle open after the writer context terminates; otherwise, <see langword="false"/>.</param>
        public SequentialFileWriter(Stream stream, int bufferSize = 65536, bool leaveOpen = false)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must be writable.", nameof(stream));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "Must be greater than zero.");

            _buffer = new byte[bufferSize];
            _leaveOpen = leaveOpen;
            _position = stream.CanSeek ? stream.Position : 0;
        }

        /// <summary>
        /// Gets the logical absolute location coordinate tracking index measuring the total byte volume written to the underlying storage layer and the active in-memory buffer.
        /// </summary>
        public long Position => _position;

        /// <summary>
        /// Writes a read-only memory span block sequence directly into the internal staging buffer, transparently flushing contents to the OS file channel when capacity runs full.
        /// Large writes exceeding buffer capacity bypass the buffer after draining.
        /// </summary>
        public void Write(ReadOnlySpan<byte> data)
        {
            ThrowIfDisposed();
            _position += data.Length;

            // Large payload: drain the buffer, then bypass it directly to the stream
            if (data.Length >= _buffer.Length)
            {
                FlushBuffer();
                _stream.Write(data);
                Diagnostics.PrimitiveMetrics.IoBytesWritten.Add(data.Length);
                return;
            }

            if (data.Length > _buffer.Length - _bufferOffset)
            {
                FlushBuffer();
            }

            data.CopyTo(_buffer.AsSpan(_bufferOffset));
            _bufferOffset += data.Length;
            Diagnostics.PrimitiveMetrics.IoBytesWritten.Add(data.Length);
        }

        /// <summary>
        /// Encodes a single standalone byte straight into the sequential staging pathway.
        /// </summary>
        public void WriteByte(byte value)
        {
            ThrowIfDisposed();
            if (_bufferOffset >= _buffer.Length)
            {
                FlushBuffer();
            }
            _buffer[_bufferOffset++] = value;
            _position++;

            Diagnostics.PrimitiveMetrics.IoBytesWritten.Add(1);
        }

        /// <summary>
        /// Forces an immediate structural flush of the active application staging cache to the underlying stream layer.
        /// </summary>
        /// <param name="flushToDisk"><see langword="true"/> to force an OS-level fsync / FlushFileBuffers barrier on disk streams.</param>
        public void Flush(bool flushToDisk = false)
        {
            ThrowIfDisposed();
            FlushBuffer();
            if (_stream is FileStream fs)
            {
                fs.Flush(flushToDisk);
            }
            else
            {
                _stream.Flush();
            }
        }

        private void FlushBuffer()
        {
            if (_bufferOffset > 0)
            {
                _stream.Write(_buffer, 0, _bufferOffset);
                _bufferOffset = 0;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SequentialFileWriter));
        }

        /// <summary>
        /// Releases all unmanaged system handles and flushes remaining buffered data to the underlying stream.
        /// Errors during final flush are propagated so failure is never hidden.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                FlushBuffer();
            }
            finally
            {
                if (!_leaveOpen)
                {
                    _stream.Dispose();
                }
            }
        }
    }
}
