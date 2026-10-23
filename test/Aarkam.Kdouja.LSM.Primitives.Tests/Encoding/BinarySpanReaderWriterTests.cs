using Aarkam.Kdouja.LSM.Primitives.Encoding;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.Encoding;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.Encoding)]
public sealed class BinarySpanReaderWriterTests
{
    [Fact]
    public void WriteAndRead_AllPrimitiveTypes_RoundtripPreservesValuesAndPositions()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new BinarySpanWriter(buffer);

        Assert.Equal(0, writer.Position);
        Assert.Equal(64, writer.Remaining);
        Assert.Equal(64, writer.Buffer.Length);

        writer.WriteByte(0xAB);
        writer.WriteBoolean(true);
        writer.WriteBoolean(false);
        writer.WriteInt16(-12345);
        writer.WriteUInt16(54321);
        writer.WriteInt32(-1_234_567_890);
        writer.WriteUInt32(3_456_789_012u);
        writer.WriteInt64(-123_456_789_012_345_678L);
        writer.WriteUInt64(12_345_678_901_234_567_890UL);
        writer.WriteBytes([10, 20, 30, 40]);

        Assert.Equal(35, writer.Position);
        Assert.Equal(29, writer.Remaining);
        Assert.Equal(35, writer.WrittenSpan.Length);

        var reader = new BinarySpanReader(writer.WrittenSpan);
        Assert.Equal(0, reader.Position);
        Assert.Equal(35, reader.Remaining);

        Assert.Equal((byte)0xAB, reader.ReadByte());
        Assert.True(reader.ReadBoolean());
        Assert.False(reader.ReadBoolean());
        Assert.Equal((short)-12345, reader.ReadInt16());
        Assert.Equal((ushort)54321, reader.ReadUInt16());
        Assert.Equal(-1_234_567_890, reader.ReadInt32());
        Assert.Equal(3_456_789_012u, reader.ReadUInt32());
        Assert.Equal(-123_456_789_012_345_678L, reader.ReadInt64());
        Assert.Equal(12_345_678_901_234_567_890UL, reader.ReadUInt64());

        ReadOnlySpan<byte> sliced = reader.ReadBytes(2);
        Assert.True(sliced.SequenceEqual(new byte[] { 10, 20 }));

        Span<byte> copied = stackalloc byte[2];
        reader.ReadBytes(copied);
        Assert.True(copied.SequenceEqual(new byte[] { 30, 40 }));

        Assert.Equal(35, reader.Position);
        Assert.Equal(0, reader.Remaining);
    }

    [Theory]
    [InlineData(short.MinValue)]
    [InlineData((short)0)]
    [InlineData(short.MaxValue)]
    public void WriteInt16_ReadInt16_BoundaryValues_Roundtrip(short value)
    {
        Span<byte> buf = stackalloc byte[2];
        var writer = new BinarySpanWriter(buf);
        writer.WriteInt16(value);

        var reader = new BinarySpanReader(buf);
        Assert.Equal(value, reader.ReadInt16());
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(0)]
    [InlineData(int.MaxValue)]
    public void WriteInt32_ReadInt32_BoundaryValues_Roundtrip(int value)
    {
        Span<byte> buf = stackalloc byte[4];
        var writer = new BinarySpanWriter(buf);
        writer.WriteInt32(value);

        var reader = new BinarySpanReader(buf);
        Assert.Equal(value, reader.ReadInt32());
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(0L)]
    [InlineData(long.MaxValue)]
    public void WriteInt64_ReadInt64_BoundaryValues_Roundtrip(long value)
    {
        Span<byte> buf = stackalloc byte[8];
        var writer = new BinarySpanWriter(buf);
        writer.WriteInt64(value);

        var reader = new BinarySpanReader(buf);
        Assert.Equal(value, reader.ReadInt64());
    }

    [Fact]
    public void BinarySpanWriter_Advance_MovesPositionOrThrowsOnInvalidCount()
    {
        byte[] backing = new byte[10];
        var writer = new BinarySpanWriter(backing);

        writer.Advance(4);
        Assert.Equal(4, writer.Position);
        Assert.Equal(6, writer.Remaining);

        writer.Advance(0);
        Assert.Equal(4, writer.Position);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var w = new BinarySpanWriter(new byte[10]);
            w.Advance(-1);
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var w = new BinarySpanWriter(new byte[4]);
            w.Advance(5);
        });
    }

    [Fact]
    public void BinarySpanWriter_WhenBufferTooSmall_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            var w = new BinarySpanWriter(Span<byte>.Empty);
            w.WriteByte(1);
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var w = new BinarySpanWriter(Span<byte>.Empty);
            w.WriteBoolean(true);
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var w = new BinarySpanWriter(new byte[1]);
            w.WriteInt16(1);
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var w = new BinarySpanWriter(new byte[1]);
            w.WriteUInt16(1);
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var w = new BinarySpanWriter(new byte[3]);
            w.WriteInt32(1);
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var w = new BinarySpanWriter(new byte[3]);
            w.WriteUInt32(1u);
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var w = new BinarySpanWriter(new byte[7]);
            w.WriteInt64(1L);
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var w = new BinarySpanWriter(new byte[7]);
            w.WriteUInt64(1UL);
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var w = new BinarySpanWriter(new byte[2]);
            w.WriteBytes([1, 2, 3]);
        });
    }

    [Fact]
    public void BinarySpanReader_WhenInsufficientRemainingBytes_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            var r = new BinarySpanReader(ReadOnlySpan<byte>.Empty);
            _ = r.ReadByte();
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var r = new BinarySpanReader(ReadOnlySpan<byte>.Empty);
            _ = r.ReadBoolean();
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var r = new BinarySpanReader(new byte[1]);
            _ = r.ReadInt16();
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var r = new BinarySpanReader(new byte[1]);
            _ = r.ReadUInt16();
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var r = new BinarySpanReader(new byte[3]);
            _ = r.ReadInt32();
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var r = new BinarySpanReader(new byte[3]);
            _ = r.ReadUInt32();
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var r = new BinarySpanReader(new byte[7]);
            _ = r.ReadInt64();
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var r = new BinarySpanReader(new byte[7]);
            _ = r.ReadUInt64();
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var r = new BinarySpanReader(new byte[2]);
            _ = r.ReadBytes(3);
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var r = new BinarySpanReader(new byte[2]);
            r.ReadBytes(new byte[3]);
        });
    }
}
