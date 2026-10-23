using Aarkam.Kdouja.LSM.Primitives.Buffers;
using System.Runtime.CompilerServices;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.Buffers;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.Buffers)]
public sealed class FixedBuffersTests
{
    [Fact]
    public void BufferSizes_MatchExpectedInlineDimensions()
    {
        Assert.Equal(12, Unsafe.SizeOf<Buffer12>());
        Assert.Equal(16, Unsafe.SizeOf<Buffer16>());
        Assert.Equal(32, Unsafe.SizeOf<Buffer32>());
        Assert.Equal(64, Unsafe.SizeOf<Buffer64>());
    }

    [Fact]
    public void Buffer12_DefaultInitialization_IsAllZeros()
    {
        var buffer = new Buffer12();
        Assert.Equal(12, buffer.AsSpan().Length);
        Assert.Equal(12, buffer.AsReadOnlySpan().Length);
        Assert.Equal(new string('0', 24), buffer.ToString());
    }

    [Fact]
    public void Buffer16_DefaultInitialization_IsAllZeros()
    {
        var buffer = new Buffer16();
        Assert.Equal(16, buffer.AsSpan().Length);
        Assert.Equal(16, buffer.AsReadOnlySpan().Length);
        Assert.Equal(new string('0', 32), buffer.ToString());
    }

    [Fact]
    public void Buffer32_DefaultInitialization_IsAllZeros()
    {
        var buffer = new Buffer32();
        Assert.Equal(32, buffer.AsSpan().Length);
        Assert.Equal(32, buffer.AsReadOnlySpan().Length);
        Assert.Equal(new string('0', 64), buffer.ToString());
    }

    [Fact]
    public void Buffer64_DefaultInitialization_IsAllZeros()
    {
        var buffer = new Buffer64();
        Assert.Equal(64, buffer.AsSpan().Length);
        Assert.Equal(64, buffer.AsReadOnlySpan().Length);
        Assert.Equal(new string('0', 128), buffer.ToString());
    }

    [Fact]
    public void Buffer12_FromAndCopyFrom_RoundtripAndEquality_Succeeds()
    {
        byte[] source = TestDataGenerator.CreateDeterministicBytes(12, seed: 101);
        var fromBuffer = Buffer12.From(source);
        var copyBuffer = new Buffer12();
        copyBuffer.CopyFrom(source);

        Assert.True(fromBuffer.Equals(copyBuffer));
        Assert.True(fromBuffer.AsReadOnlySpan().SequenceEqual(source));
        Assert.Equal(Convert.ToHexString(source), fromBuffer.ToString());

        copyBuffer.AsSpan()[0] ^= 0xFF;
        Assert.False(fromBuffer.Equals(copyBuffer));
    }

    [Fact]
    public void Buffer16_FromAndCopyFrom_RoundtripAndEquality_Succeeds()
    {
        byte[] source = TestDataGenerator.CreateDeterministicBytes(16, seed: 102);
        var fromBuffer = Buffer16.From(source);
        var copyBuffer = new Buffer16();
        copyBuffer.CopyFrom(source);

        Assert.True(fromBuffer.Equals(copyBuffer));
        Assert.True(fromBuffer.AsReadOnlySpan().SequenceEqual(source));
        Assert.Equal(Convert.ToHexString(source), fromBuffer.ToString());

        copyBuffer.AsSpan()[15] ^= 0xFF;
        Assert.False(fromBuffer.Equals(copyBuffer));
    }

    [Fact]
    public void Buffer32_FromAndCopyFrom_RoundtripAndEquality_Succeeds()
    {
        byte[] source = TestDataGenerator.CreateDeterministicBytes(32, seed: 103);
        var fromBuffer = Buffer32.From(source);
        var copyBuffer = new Buffer32();
        copyBuffer.CopyFrom(source);

        Assert.True(fromBuffer.Equals(copyBuffer));
        Assert.True(fromBuffer.AsReadOnlySpan().SequenceEqual(source));
        Assert.Equal(Convert.ToHexString(source), fromBuffer.ToString());

        copyBuffer.AsSpan()[16] ^= 0xFF;
        Assert.False(fromBuffer.Equals(copyBuffer));
    }

    [Fact]
    public void Buffer64_FromAndCopyFrom_RoundtripAndEquality_Succeeds()
    {
        byte[] source = TestDataGenerator.CreateDeterministicBytes(64, seed: 104);
        var fromBuffer = Buffer64.From(source);
        var copyBuffer = new Buffer64();
        copyBuffer.CopyFrom(source);

        Assert.True(fromBuffer.Equals(copyBuffer));
        Assert.True(fromBuffer.AsReadOnlySpan().SequenceEqual(source));
        Assert.Equal(Convert.ToHexString(source), fromBuffer.ToString());

        copyBuffer.AsSpan()[63] ^= 0xFF;
        Assert.False(fromBuffer.Equals(copyBuffer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(24)]
    public void Buffer12_InvalidSourceLength_ThrowsArgumentException(int invalidLength)
    {
        byte[] source = new byte[invalidLength];
        var buffer = new Buffer12();

        Assert.Throws<ArgumentException>(() => buffer.CopyFrom(source));
        Assert.Throws<ArgumentException>(() => Buffer12.From(source));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    [InlineData(32)]
    public void Buffer16_InvalidSourceLength_ThrowsArgumentException(int invalidLength)
    {
        byte[] source = new byte[invalidLength];
        var buffer = new Buffer16();

        Assert.Throws<ArgumentException>(() => buffer.CopyFrom(source));
        Assert.Throws<ArgumentException>(() => Buffer16.From(source));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(33)]
    [InlineData(64)]
    public void Buffer32_InvalidSourceLength_ThrowsArgumentException(int invalidLength)
    {
        byte[] source = new byte[invalidLength];
        var buffer = new Buffer32();

        Assert.Throws<ArgumentException>(() => buffer.CopyFrom(source));
        Assert.Throws<ArgumentException>(() => Buffer32.From(source));
    }

    [Fact]
    public void Buffers_Clear_ZeroesMemory()
    {
        byte[] s12 = TestDataGenerator.CreateDeterministicBytes(12, seed: 201);
        var b12 = Buffer12.From(s12);
        b12.Clear();
        Assert.True(b12.AsReadOnlySpan().SequenceEqual(new byte[12]));

        byte[] s16 = TestDataGenerator.CreateDeterministicBytes(16, seed: 202);
        var b16 = Buffer16.From(s16);
        b16.Clear();
        Assert.True(b16.AsReadOnlySpan().SequenceEqual(new byte[16]));

        byte[] s32 = TestDataGenerator.CreateDeterministicBytes(32, seed: 203);
        var b32 = Buffer32.From(s32);
        b32.Clear();
        Assert.True(b32.AsReadOnlySpan().SequenceEqual(new byte[32]));

        byte[] s64 = TestDataGenerator.CreateDeterministicBytes(64, seed: 204);
        var b64 = Buffer64.From(s64);
        b64.Clear();
        Assert.True(b64.AsReadOnlySpan().SequenceEqual(new byte[64]));
    }

    [Fact]
    public void Buffers_ConstantTimeEquals_MatchesExpectedEquality()
    {
        byte[] s32 = TestDataGenerator.CreateDeterministicBytes(32, seed: 301);
        var b1 = Buffer32.From(s32);
        var b2 = Buffer32.From(s32);
        Assert.True(b1.ConstantTimeEquals(b2));

        b2.AsSpan()[0] ^= 0xFF;
        Assert.False(b1.ConstantTimeEquals(b2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(63)]
    [InlineData(65)]
    [InlineData(128)]
    public void Buffer64_InvalidSourceLength_ThrowsArgumentException(int invalidLength)
    {
        byte[] source = new byte[invalidLength];
        var buffer = new Buffer64();

        Assert.Throws<ArgumentException>(() => buffer.CopyFrom(source));
        Assert.Throws<ArgumentException>(() => Buffer64.From(source));
    }
}
