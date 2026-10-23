using Aarkam.Kdouja.LSM.Primitives.Checksums;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.Checksums;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.Checksums)]
public sealed class Crc32AlgorithmTests
{
    [Fact]
    public void Instance_ImplementsIChecksumAndHasExpectedName()
    {
        IChecksum checksum = Crc32Algorithm.Instance;

        Assert.NotNull(checksum);
        Assert.Equal("CRC32-C", checksum.Name);
        Assert.Same(Crc32Algorithm.Instance, checksum);
    }

    [Fact]
    public void Compute_EmptySpan_ReturnsZero()
    {
        uint crc = Crc32Algorithm.Instance.Compute(ReadOnlySpan<byte>.Empty);
        Assert.Equal(0u, crc);
    }

    [Fact]
    public void Compute_StandardAsciiTestVector_ReturnsExpectedCastagnoliChecksum()
    {
        ReadOnlySpan<byte> payload = "123456789"u8;
        uint crc = Crc32Algorithm.Instance.Compute(payload);

        Assert.Equal(0xE3069283u, crc);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(255)]
    [InlineData(1024)]
    public void Compute_VariousPayloadLengths_MatchesReferenceCastagnoliCalculation(int length)
    {
        byte[] payload = TestDataGenerator.CreateDeterministicBytes(length, seed: length * 7);

        uint actual = Crc32Algorithm.Instance.Compute(payload);
        uint expected = ComputeReferenceCrc32C(payload);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Compute_SingleBitFlip_ProducesDifferentChecksum()
    {
        byte[] original = TestDataGenerator.CreateDeterministicBytes(128, seed: 99);
        byte[] mutated = (byte[])original.Clone();
        mutated[73] ^= 0x04;

        uint crcOriginal = Crc32Algorithm.Instance.Compute(original);
        uint crcMutated = Crc32Algorithm.Instance.Compute(mutated);

        Assert.NotEqual(crcOriginal, crcMutated);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 7)]
    [InlineData(8, 8)]
    [InlineData(10, 25)]
    [InlineData(64, 64)]
    [InlineData(128, 7)]
    public void Extend_MatchesSingleShotCompute(int lenA, int lenB)
    {
        byte[] partA = TestDataGenerator.CreateDeterministicBytes(lenA, seed: 101);
        byte[] partB = TestDataGenerator.CreateDeterministicBytes(lenB, seed: 202);

        byte[] combined = new byte[lenA + lenB];
        partA.CopyTo(combined, 0);
        partB.CopyTo(combined, lenA);

        uint fullCrc = Crc32Algorithm.Instance.Compute(combined);
        uint crcA = Crc32Algorithm.Instance.Compute(partA);
        uint extendedCrc = Crc32Algorithm.Instance.Extend(crcA, partB);

        Assert.Equal(fullCrc, extendedCrc);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(0xE3069283u)]
    [InlineData(0xFFFFFFFFu)]
    [InlineData(0x12345678u)]
    public void Mask_Unmask_RoundTripsAccurately(uint originalCrc)
    {
        uint masked = Crc32Algorithm.Mask(originalCrc);
        Assert.NotEqual(originalCrc, masked);

        uint unmasked = Crc32Algorithm.Unmask(masked);
        Assert.Equal(originalCrc, unmasked);
    }

    private static uint ComputeReferenceCrc32C(ReadOnlySpan<byte> data)
    {
        const uint poly = 0x82F63B78u;
        uint crc = 0xFFFFFFFFu;

        for (int i = 0; i < data.Length; i++)
        {
            crc ^= data[i];
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ poly : crc >> 1;
            }
        }

        return crc ^ 0xFFFFFFFFu;
    }
}
