using Aarkam.Kdouja.LSM.Primitives.IO;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.IO;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.IO)]
public sealed class SequentialMemoryIoAndRateLimiterTests
{
    [Fact]
    public void SequentialFileWriter_NullOrNonWritableStream_ThrowsExpectedException()
    {
        Assert.Throws<ArgumentNullException>(() => new SequentialFileWriter(null!));

        using var readOnlyStream = new MemoryStream(new byte[16], writable: false);
        Assert.Throws<ArgumentException>(() => new SequentialFileWriter(readOnlyStream));
    }

    [Fact]
    public void SequentialFileReader_NullOrNonReadableStream_ThrowsExpectedException()
    {
        Assert.Throws<ArgumentNullException>(() => new SequentialFileReader(null!));

        using var nonReadable = new NonReadableOrSeekableStream(canRead: false, canWrite: true, canSeek: false);
        Assert.Throws<ArgumentException>(() => new SequentialFileReader(nonReadable));
    }

    [Theory]
    [InlineData(8, 100)]
    [InlineData(32, 256)]
    [InlineData(64, 64)]
    [InlineData(128, 1000)]
    public void SequentialFileWriter_And_Reader_RoundtripAcrossSmallBufferBoundaries(int bufferSize, int payloadLength)
    {
        byte[] payload = TestDataGenerator.CreateDeterministicBytes(payloadLength, seed: bufferSize + payloadLength);
        using var stream = new MemoryStream();

        using (var writer = new SequentialFileWriter(stream, bufferSize: bufferSize, leaveOpen: true))
        {
            Assert.Equal(0L, writer.Position);
            writer.WriteByte(0xFE);
            writer.Write(payload);
            writer.Flush();
            Assert.Equal(payloadLength + 1, writer.Position);
        }

        stream.Position = 0;

        using (var reader = new SequentialFileReader(stream, bufferSize: bufferSize, leaveOpen: true))
        {
            Assert.Equal(0L, reader.Position);
            Assert.Equal(0xFE, reader.ReadByte());

            byte[] readBack = new byte[payloadLength];
            int totalRead = reader.Read(readBack);

            Assert.Equal(payloadLength, totalRead);
            Assert.Equal(payloadLength + 1, reader.Position);
            Assert.Equal(payload, readBack);
            Assert.Equal(-1, reader.ReadByte());
        }
    }

    [Fact]
    public void SequentialFileWriter_Dispose_FlushesRemainingBytesAndRespectsLeaveOpen()
    {
        var openStream = new MemoryStream();
        using (var writer = new SequentialFileWriter(openStream, bufferSize: 1024, leaveOpen: true))
        {
            writer.Write([10, 20, 30]);
        }

        Assert.Equal([10, 20, 30], openStream.ToArray());
        openStream.Position = 0; // Still usable because leaveOpen = true

        var closedStream = new MemoryStream();
        using (var writer = new SequentialFileWriter(closedStream, bufferSize: 1024, leaveOpen: false))
        {
            writer.WriteByte(99);
        }

        Assert.Throws<ObjectDisposedException>(() => closedStream.WriteByte(1));
    }

    [Fact]
    public void SequentialFileReader_Skip_SeekableAndNonSeekableStreams_AdvancesCorrectly()
    {
        byte[] data = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

        using (var seekable = new MemoryStream(data))
        using (var reader = new SequentialFileReader(seekable, bufferSize: 4, leaveOpen: true))
        {
            Assert.Equal(0, reader.ReadByte()); // Loads 4 bytes into buffer, consumes 1
            reader.Skip(6); // Consumes remaining 3 in buffer + seeks 3 on stream
            Assert.Equal(7L, reader.Position);
            Assert.Equal(7, reader.ReadByte());
        }

        using (var nonSeekable = new NonReadableOrSeekableStream(new MemoryStream(data), canSeek: false))
        using (var reader = new SequentialFileReader(nonSeekable, bufferSize: 4))
        {
            reader.Skip(5);
            Assert.Equal(5L, reader.Position);
            Assert.Equal(5, reader.ReadByte());

            Assert.Throws<ArgumentOutOfRangeException>(() => reader.Skip(-1));
            Assert.Throws<EndOfStreamException>(() => reader.Skip(100));
        }
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(-1024L)]
    public void TokenBucketRateLimiter_NonPositiveRate_ThrowsArgumentOutOfRangeException(long invalidRate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenBucketRateLimiter(invalidRate));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-50L)]
    public async Task TokenBucketRateLimiter_NonPositiveBytes_ReturnsImmediately(long nonPositiveBytes)
    {
        var limiter = new TokenBucketRateLimiter(1024);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Non-positive requests short-circuit before checking cancellation or acquiring lock
        await limiter.AcquireAsync(nonPositiveBytes, cts.Token);
    }

    [Fact]
    public async Task TokenBucketRateLimiter_AcquireWithinCapacity_CompletesWithoutDelay()
    {
        var limiter = new TokenBucketRateLimiter(1_000_000);

        await limiter.AcquireAsync(500_000);
        await limiter.AcquireAsync(500_000);
    }

    [Fact]
    public async Task TokenBucketRateLimiter_CancelledToken_ThrowsOperationCanceledException()
    {
        var limiter = new TokenBucketRateLimiter(100);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await limiter.AcquireAsync(10, cts.Token));
    }

    [Fact]
    public async Task TokenBucketRateLimiter_WhenRequestExceedsCapacity_DelaysAndHonorsCancellation()
    {
        var limiter = new TokenBucketRateLimiter(100);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(25));

        // Requesting 1000 bytes on a 100 B/s bucket incurs 900 bytes debt (~9000 ms delay), which will be cancelled by cts
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await limiter.AcquireAsync(1000, cts.Token));
    }

    private sealed class NonReadableOrSeekableStream : Stream
    {
        private readonly Stream _inner;
        private readonly bool _canRead;
        private readonly bool _canWrite;
        private readonly bool _canSeek;

        public NonReadableOrSeekableStream(bool canRead, bool canWrite, bool canSeek)
        {
            _inner = new MemoryStream();
            _canRead = canRead;
            _canWrite = canWrite;
            _canSeek = canSeek;
        }

        public NonReadableOrSeekableStream(Stream inner, bool canSeek)
        {
            _inner = inner;
            _canRead = inner.CanRead;
            _canWrite = inner.CanWrite;
            _canSeek = canSeek;
        }

        public override bool CanRead => _canRead;
        public override bool CanSeek => _canSeek;
        public override bool CanWrite => _canWrite;
        public override long Length => _inner.Length;
        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) =>
            _canSeek ? _inner.Seek(offset, origin) : throw new NotSupportedException();
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
    }
}
