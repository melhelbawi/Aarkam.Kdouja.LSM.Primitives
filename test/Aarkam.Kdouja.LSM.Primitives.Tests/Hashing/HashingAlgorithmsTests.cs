using Aarkam.Kdouja.LSM.Primitives.Hashing;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.Hashing;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.Hashing)]
public sealed class HashingAlgorithmsTests
{
    [Fact]
    public void Fnv1aHash32_SingletonAndName_MatchExpected()
    {
        IHashFunction hashFn = Fnv1aHash32.Instance;

        Assert.NotNull(hashFn);
        Assert.Equal("FNV-1a-32", hashFn.Name);
        Assert.Same(Fnv1aHash32.Instance, hashFn);
    }

    [Theory]
    [InlineData("", 0x811C9DC5u)]
    [InlineData("a", 0xE40C292Cu)]
    [InlineData("foobar", 0xBF9CF968u)]
    public void Fnv1aHash32_KnownTestVectors_ReturnExpectedHash(string input, uint expectedHash)
    {
        ReadOnlySpan<byte> bytes = TestDataGenerator.CreateUtf8Bytes(input);
        uint actual = Fnv1aHash32.Instance.Hash(bytes);

        Assert.Equal(expectedHash, actual);
    }

    [Fact]
    public void Fnv1aHash32_SeedZero_UsesStandardOffsetBasis()
    {
        byte[] payload = TestDataGenerator.CreateDeterministicBytes(32, seed: 12);

        uint defaultHash = Fnv1aHash32.Instance.Hash(payload);
        uint zeroSeedHash = Fnv1aHash32.Instance.Hash(payload, seed: 0);
        uint customSeedHash = Fnv1aHash32.Instance.Hash(payload, seed: 12345u);

        Assert.Equal(defaultHash, zeroSeedHash);
        Assert.NotEqual(defaultHash, customSeedHash);
    }

    [Fact]
    public void XxHash32Algorithm_SingletonAndName_MatchExpected()
    {
        IHashFunction hashFn = XxHash32Algorithm.Instance;

        Assert.NotNull(hashFn);
        Assert.Equal("XXHash32", hashFn.Name);
        Assert.Same(XxHash32Algorithm.Instance, hashFn);
    }

    [Theory]
    [InlineData("", 0u, 90033154u)]
    [InlineData("", 2654435761u, 3884627766u)]
    [InlineData("123456789", 0u, 1739422611u)]
    public void XxHash32Algorithm_StandardVectors_ReturnExpectedHash(string text, uint seed, uint expected)
    {
        byte[] data = TestDataGenerator.CreateUtf8Bytes(text);
        uint actual = XxHash32Algorithm.Instance.Hash(data, seed);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(64)]
    [InlineData(512)]
    [InlineData(4096)]
    public void XxHash32Algorithm_SeedVariation_ProducesDistinctDeterministicHashes(int length)
    {
        byte[] data = TestDataGenerator.CreateDeterministicBytes(length, seed: length);

        uint h0 = XxHash32Algorithm.Instance.Hash(data);
        uint h0Explicit = XxHash32Algorithm.Instance.Hash(data, 0);
        uint h1 = XxHash32Algorithm.Instance.Hash(data, 1);
        uint h2 = XxHash32Algorithm.Instance.Hash(data, 2);

        Assert.Equal(h0, h0Explicit);
        Assert.NotEqual(h0, h1);
        Assert.NotEqual(h1, h2);
    }
}
