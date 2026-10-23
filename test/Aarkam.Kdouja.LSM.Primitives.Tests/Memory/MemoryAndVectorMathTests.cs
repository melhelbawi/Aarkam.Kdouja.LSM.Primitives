using Aarkam.Kdouja.LSM.Primitives.Memory;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.Memory;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.Memory)]
public sealed class MemoryAndVectorMathTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void PooledBuffer_NegativeLength_ThrowsArgumentOutOfRangeException(int invalidLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PooledBuffer(invalidLength));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(37)]
    [InlineData(1024)]
    [InlineData(8192)]
    public void PooledBuffer_RentAndSlice_ExposesExactRequestedLength(int requestedLength)
    {
        using var buffer = new PooledBuffer(requestedLength);

        Assert.Equal(requestedLength, buffer.Memory.Length);
        Assert.Equal(requestedLength, buffer.Span.Length);

        if (requestedLength == 1)
        {
            buffer.Span[0] = 0x5A;
            Assert.Equal((byte)0x5A, buffer.Memory.Span[0]);
        }
        else if (requestedLength > 1)
        {
            buffer.Span[0] = 0x5A;
            buffer.Span[requestedLength - 1] = 0xA5;
            Assert.Equal((byte)0x5A, buffer.Memory.Span[0]);
            Assert.Equal((byte)0xA5, buffer.Memory.Span[requestedLength - 1]);
        }
    }

    [Fact]
    public void PooledBuffer_AfterDispose_ThrowsObjectDisposedExceptionAndDisposeIsIdempotent()
    {
        var buffer = new PooledBuffer(128);
        buffer.Dispose();
        buffer.Dispose(); // Idempotent second call

        Assert.Throws<ObjectDisposedException>(() => _ = buffer.Memory);
        Assert.Throws<ObjectDisposedException>(() => _ = buffer.Span.Length);
    }

    [Fact]
    public void VectorMath_CompareBlocks_DifferentLengths_ReturnsFalse()
    {
        Assert.False(VectorMath.CompareBlocks([1, 2, 3], [1, 2]));
        Assert.False(VectorMath.CompareBlocks([], [0]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(128)]
    [InlineData(513)]
    public void VectorMath_CompareBlocks_IdenticalAndModifiedBuffers_EvaluatesCorrectly(int length)
    {
        byte[] a = TestDataGenerator.CreateDeterministicBytes(length, seed: 500 + length);
        byte[] b = (byte[])a.Clone();

        Assert.True(VectorMath.CompareBlocks(a, b));

        if (length > 0)
        {
            b[0] ^= 0x01;
            Assert.False(VectorMath.CompareBlocks(a, b));
            b[0] ^= 0x01;

            int mid = length / 2;
            b[mid] ^= 0x80;
            Assert.False(VectorMath.CompareBlocks(a, b));
            b[mid] ^= 0x80;

            b[length - 1] ^= 0xFF;
            Assert.False(VectorMath.CompareBlocks(a, b));
        }
    }

    [Fact]
    public void VectorMath_IndexOf_EmptyOrNotFound_ReturnsMinusOne()
    {
        Assert.Equal(-1, VectorMath.IndexOf(ReadOnlySpan<byte>.Empty, 0x42));

        byte[] buffer = new byte[100];
        Array.Fill(buffer, (byte)0x11);
        Assert.Equal(-1, VectorMath.IndexOf(buffer, 0x99));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(15, 14)]
    [InlineData(16, 0)]
    [InlineData(16, 15)]
    [InlineData(17, 16)]
    [InlineData(31, 30)]
    [InlineData(32, 0)]
    [InlineData(32, 17)]
    [InlineData(32, 31)]
    [InlineData(33, 32)]
    [InlineData(64, 45)]
    [InlineData(65, 64)]
    [InlineData(130, 129)]
    public void VectorMath_IndexOf_FindsTargetAtExactOffsetAcrossVectorBoundaries(int bufferLength, int targetIndex)
    {
        byte[] buffer = new byte[bufferLength];
        Array.Fill(buffer, (byte)0x01);
        buffer[targetIndex] = 0xFE;

        int foundIndex = VectorMath.IndexOf(buffer, 0xFE);

        Assert.Equal(targetIndex, foundIndex);
    }

    [Fact]
    public void VectorMath_IndexOf_WhenMultipleMatchesExist_ReturnsFirstOccurrence()
    {
        byte[] buffer = new byte[80];
        buffer[19] = 0x77;
        buffer[45] = 0x77;
        buffer[79] = 0x77;

        Assert.Equal(19, VectorMath.IndexOf(buffer, 0x77));
    }
}
