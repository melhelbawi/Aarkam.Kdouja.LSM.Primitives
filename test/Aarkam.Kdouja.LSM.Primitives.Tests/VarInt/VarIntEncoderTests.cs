using Aarkam.Kdouja.LSM.Primitives.VarInt;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.VarInt;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.VarInt)]
public sealed class VarIntEncoderTests
{
    [Theory]
    [InlineData(0u, 1)]
    [InlineData(1u, 1)]
    [InlineData(127u, 1)]
    [InlineData(128u, 2)]
    [InlineData(16383u, 2)]
    [InlineData(16384u, 3)]
    [InlineData(2097151u, 3)]
    [InlineData(2097152u, 4)]
    [InlineData(268435455u, 4)]
    [InlineData(268435456u, 5)]
    [InlineData(uint.MaxValue, 5)]
    public void WriteUInt32_ReadUInt32_Roundtrip_PreservesValueAndExpectedSize(uint value, int expectedSize)
    {
        Span<byte> buffer = stackalloc byte[10];

        int computedSize = VarIntEncoder.GetUInt32Size(value);
        int written = VarIntEncoder.WriteUInt32(buffer, value);
        int read = VarIntEncoder.ReadUInt32(buffer[..written], out uint decoded);

        Assert.Equal(expectedSize, computedSize);
        Assert.Equal(expectedSize, written);
        Assert.Equal(written, read);
        Assert.Equal(value, decoded);
    }

    [Theory]
    [InlineData(0UL, 1)]
    [InlineData(1UL, 1)]
    [InlineData(127UL, 1)]
    [InlineData(128UL, 2)]
    [InlineData(16383UL, 2)]
    [InlineData(16384UL, 3)]
    [InlineData(uint.MaxValue, 5)]
    [InlineData(1UL << 35, 6)]
    [InlineData(1UL << 42, 7)]
    [InlineData(1UL << 49, 8)]
    [InlineData(1UL << 56, 9)]
    [InlineData(1UL << 63, 10)]
    [InlineData(ulong.MaxValue, 10)]
    public void WriteUInt64_ReadUInt64_Roundtrip_PreservesValueAndExpectedSize(ulong value, int expectedSize)
    {
        Span<byte> buffer = stackalloc byte[12];

        int computedSize = VarIntEncoder.GetUInt64Size(value);
        int written = VarIntEncoder.WriteUInt64(buffer, value);
        int read = VarIntEncoder.ReadUInt64(buffer[..written], out ulong decoded);

        Assert.Equal(expectedSize, computedSize);
        Assert.Equal(expectedSize, written);
        Assert.Equal(written, read);
        Assert.Equal(value, decoded);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 1)]
    [InlineData(-64, 1)]
    [InlineData(63, 1)]
    [InlineData(-65, 2)]
    [InlineData(64, 2)]
    [InlineData(int.MinValue, 5)]
    [InlineData(int.MaxValue, 5)]
    public void WriteInt32_ReadInt32_ZigZagRoundtrip_PreservesSignedValue(int value, int expectedSize)
    {
        Span<byte> buffer = stackalloc byte[10];

        int computedSize = VarIntEncoder.GetInt32Size(value);
        int written = VarIntEncoder.WriteInt32(buffer, value);
        int read = VarIntEncoder.ReadInt32(buffer[..written], out int decoded);

        Assert.Equal(expectedSize, computedSize);
        Assert.Equal(expectedSize, written);
        Assert.Equal(written, read);
        Assert.Equal(value, decoded);
    }

    [Theory]
    [InlineData(0L, 1)]
    [InlineData(-1L, 1)]
    [InlineData(1L, 1)]
    [InlineData(-64L, 1)]
    [InlineData(63L, 1)]
    [InlineData(-65L, 2)]
    [InlineData(64L, 2)]
    [InlineData(long.MinValue, 10)]
    [InlineData(long.MaxValue, 10)]
    public void WriteInt64_ReadInt64_ZigZagRoundtrip_PreservesSignedValue(long value, int expectedSize)
    {
        Span<byte> buffer = stackalloc byte[12];

        int computedSize = VarIntEncoder.GetInt64Size(value);
        int written = VarIntEncoder.WriteInt64(buffer, value);
        int read = VarIntEncoder.ReadInt64(buffer[..written], out long decoded);

        Assert.Equal(expectedSize, computedSize);
        Assert.Equal(expectedSize, written);
        Assert.Equal(written, read);
        Assert.Equal(value, decoded);
    }

    [Fact]
    public void WriteUInt32_WhenDestinationBufferTooSmall_ThrowsArgumentException()
    {
        byte[] destination = new byte[1];
        Assert.Throws<ArgumentException>(() => VarIntEncoder.WriteUInt32(destination, 128u));
    }

    [Fact]
    public void WriteUInt64_WhenDestinationBufferTooSmall_ThrowsArgumentException()
    {
        byte[] destination = new byte[1];
        Assert.Throws<ArgumentException>(() => VarIntEncoder.WriteUInt64(destination, 128UL));
    }

    [Fact]
    public void ReadUInt32_WhenSourceTruncated_ThrowsFormatException()
    {
        byte[] truncated = [0x80];
        Assert.Throws<FormatException>(() => VarIntEncoder.ReadUInt32(truncated, out _));
    }

    [Fact]
    public void ReadUInt64_WhenSourceTruncated_ThrowsFormatException()
    {
        byte[] truncated = [0x80, 0x80];
        Assert.Throws<FormatException>(() => VarIntEncoder.ReadUInt64(truncated, out _));
    }

    [Fact]
    public void ReadUInt32_WhenContinuationBitsExceed32BitLimit_ThrowsFormatException()
    {
        byte[] malformed = [0x80, 0x80, 0x80, 0x80, 0x80, 0x01];
        Assert.Throws<FormatException>(() => VarIntEncoder.ReadUInt32(malformed, out _));
    }

    [Fact]
    public void ReadUInt64_WhenContinuationBitsExceed64BitLimit_ThrowsFormatException()
    {
        byte[] malformed = [0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01];
        Assert.Throws<FormatException>(() => VarIntEncoder.ReadUInt64(malformed, out _));
    }

    [Fact]
    public void TryReadUInt32_WhenValidData_Succeeds()
    {
        Span<byte> buffer = stackalloc byte[5];
        int written = VarIntEncoder.WriteUInt32(buffer, 300u);
        bool success = VarIntEncoder.TryReadUInt32(buffer[..written], out uint decoded, out int read);

        Assert.True(success);
        Assert.Equal(300u, decoded);
        Assert.Equal(written, read);
    }

    [Fact]
    public void TryReadUInt64_WhenValidData_Succeeds()
    {
        Span<byte> buffer = stackalloc byte[10];
        int written = VarIntEncoder.WriteUInt64(buffer, 123456789012345UL);
        bool success = VarIntEncoder.TryReadUInt64(buffer[..written], out ulong decoded, out int read);

        Assert.True(success);
        Assert.Equal(123456789012345UL, decoded);
        Assert.Equal(written, read);
    }

    [Fact]
    public void TryReadUInt32_WhenFifthByteOverflowsOrHasContinuation_ReturnsFalse()
    {
        byte[] overflow = [0x80, 0x80, 0x80, 0x80, 0x10]; // Bit 32 set in 5th byte (allowed max is 0x0F)
        Assert.False(VarIntEncoder.TryReadUInt32(overflow, out _, out _));

        byte[] continuationOnFifth = [0x80, 0x80, 0x80, 0x80, 0x80]; // Continuation bit set on 5th byte
        Assert.False(VarIntEncoder.TryReadUInt32(continuationOnFifth, out _, out _));
    }

    [Fact]
    public void TryReadUInt64_WhenTenthByteOverflowsOrHasContinuation_ReturnsFalse()
    {
        byte[] overflow = [0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x02]; // Bit 64 set in 10th byte (allowed max is 0x01)
        Assert.False(VarIntEncoder.TryReadUInt64(overflow, out _, out _));

        byte[] continuationOnTenth = [0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80]; // Continuation bit set on 10th byte
        Assert.False(VarIntEncoder.TryReadUInt64(continuationOnTenth, out _, out _));
    }

    [Fact]
    public void TryRead_WhenNonCanonicalOverlongEncoding_ReturnsFalse()
    {
        byte[] nonCanonicalZero = [0x80, 0x00]; // Zero encoded over 2 bytes instead of 1
        Assert.False(VarIntEncoder.TryReadUInt32(nonCanonicalZero, out _, out _));
        Assert.False(VarIntEncoder.TryReadUInt64(nonCanonicalZero, out _, out _));
    }
}
