using Aarkam.Kdouja.LSM.Primitives.Compression;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.Compression;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.Compression)]
public sealed class CompressionCodecsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(256)]
    [InlineData(4096)]
    public void IdentityCompressionCodec_CompressAndDecompress_RoundtripsExactBytes(int length)
    {
        var codec = new IdentityCompressionCodec();
        byte[] source = TestDataGenerator.CreateDeterministicBytes(length, seed: 50 + length);
        byte[] compressed = new byte[length + 8];
        byte[] decompressed = new byte[length + 8];

        int writtenCompressed = codec.Compress(source, compressed);
        int writtenDecompressed = codec.Decompress(compressed.AsSpan(0, writtenCompressed), decompressed);

        Assert.Equal(length, writtenCompressed);
        Assert.Equal(length, writtenDecompressed);
        Assert.True(source.AsSpan().SequenceEqual(decompressed.AsSpan(0, writtenDecompressed)));
        Assert.Equal(length, codec.GetMaxCompressedLength(length));
    }

    [Fact]
    public void IdentityCompressionCodec_WhenDestinationTooSmall_ThrowsArgumentException()
    {
        var codec = new IdentityCompressionCodec();
        byte[] source = [1, 2, 3, 4];
        byte[] tooSmall = new byte[3];

        Assert.Throws<ArgumentException>(() => codec.Compress(source, tooSmall));
        Assert.Throws<ArgumentException>(() => codec.Decompress(source, tooSmall));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(512)]
    [InlineData(4096)]
    public void DeflateCompressionCodec_CompressAndDecompress_RoundtripsData(int length)
    {
        var codec = new DeflateCompressionCodec();
        byte[] source = TestDataGenerator.CreateRepeatingPattern(length, "KdoujaKV-Block-Pattern-"u8);
        byte[] compressed = new byte[codec.GetMaxCompressedLength(length)];
        byte[] decompressed = new byte[length + 16];

        int compressedLength = codec.Compress(source, compressed);
        int decompressedLength = codec.Decompress(compressed.AsSpan(0, compressedLength), decompressed);

        Assert.Equal(length, decompressedLength);
        Assert.True(source.AsSpan().SequenceEqual(decompressed.AsSpan(0, decompressedLength)));
    }

    [Fact]
    public void DeflateCompressionCodec_RepetitivePayload_ReducesSizeSignificantly()
    {
        var codec = new DeflateCompressionCodec();
        byte[] repetitive = TestDataGenerator.CreateRepeatingPattern(4096, "AAAA-BBBB-CCCC-DDDD-"u8);
        byte[] compressed = new byte[codec.GetMaxCompressedLength(4096)];

        int compressedLength = codec.Compress(repetitive, compressed);

        Assert.True(compressedLength < repetitive.Length / 4);
    }

    [Fact]
    public void DeflateCompressionCodec_WhenDestinationTooSmall_ThrowsArgumentException()
    {
        var codec = new DeflateCompressionCodec();
        byte[] source = TestDataGenerator.CreateDeterministicBytes(128, seed: 77);
        byte[] validCompressed = new byte[codec.GetMaxCompressedLength(128)];
        int compressedLen = codec.Compress(source, validCompressed);

        Assert.Throws<ArgumentException>(() => codec.Compress(source, new byte[1]));
        Assert.Throws<ArgumentException>(() => codec.Decompress(validCompressed.AsSpan(0, compressedLen), new byte[10]));
    }

    [Fact]
    public void CompressionCodecFactory_DefaultRegistrations_ReturnExpectedCodecTypes()
    {
        Assert.IsType<IdentityCompressionCodec>(CompressionCodecFactory.GetCodec(CompressionType.None));
        Assert.IsType<DeflateCompressionCodec>(CompressionCodecFactory.GetCodec(CompressionType.Deflate));
        Assert.True(CompressionCodecFactory.IsAvailable(CompressionType.None));
        Assert.True(CompressionCodecFactory.IsAvailable(CompressionType.Deflate));
    }

    [Theory]
    [InlineData(CompressionType.Lz4)]
    [InlineData(CompressionType.Zstd)]
    [InlineData(CompressionType.Snappy)]
    [InlineData((CompressionType)254)]
    public void CompressionCodecFactory_UnregisteredCompressionType_ThrowsNotSupportedException(CompressionType unreg)
    {
        Assert.Throws<NotSupportedException>(() => CompressionCodecFactory.GetCodec(unreg));
        Assert.False(CompressionCodecFactory.IsAvailable(unreg));
    }

    [Fact]
    public void CompressionCodecFactory_RegisterCodec_NullCodec_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CompressionCodecFactory.RegisterCodec(CompressionType.Lz4, null!));
    }

    [Fact]
    public void CompressionCodecFactory_RegisterCodec_CustomCodec_CanBeResolvedAndRestored()
    {
        const CompressionType customType = (CompressionType)250;
        var customCodec = new IdentityCompressionCodec();

        CompressionCodecFactory.RegisterCodec(customType, customCodec);
        ICompressionCodec resolved = CompressionCodecFactory.GetCodec(customType);

        Assert.Same(customCodec, resolved);
        Assert.True(CompressionCodecFactory.IsAvailable(customType));
    }
}
