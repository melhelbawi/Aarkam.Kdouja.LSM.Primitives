using Aarkam.Kdouja.LSM.Primitives.Cryptography;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.Cryptography;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.Cryptography)]
public sealed class KeyDerivationAndRandomTests
{
    [Fact]
    public void CryptographicConstants_ExposeExpectedDimensions()
    {
        Assert.Equal(100_000, CryptographicConstants.DefaultPbkdf2Iterations);
        Assert.Equal(32, CryptographicConstants.Aes256KeySize);
        Assert.Equal(12, CryptographicConstants.AesGcmNonceSize);
        Assert.Equal(16, CryptographicConstants.AesGcmTagSize);
        Assert.Equal(32, CryptographicConstants.ChaCha20KeySize);
        Assert.Equal(12, CryptographicConstants.ChaCha20Poly1305NonceSize);
        Assert.Equal(16, CryptographicConstants.ChaCha20Poly1305TagSize);
        Assert.Equal(12, NonceGenerator.AesGcmNonceSize);
    }

    [Fact]
    public void KeyDerivation_SameInputs_ProducesIdenticalDerivedKey()
    {
        IKeyDerivation kdf = new KeyDerivation();
        ReadOnlySpan<byte> password = "correct-horse-battery-staple"u8;
        byte[] salt = TestDataGenerator.CreateDeterministicBytes(16, seed: 42);
        byte[] key1 = new byte[32];
        byte[] key2 = new byte[32];

        kdf.DeriveKey(password, salt, 1000, key1);
        kdf.DeriveKey(password, salt, 1000, key2);

        Assert.Equal(key1, key2);
        Assert.Contains(key1, b => b != 0);
    }

    [Fact]
    public void KeyDerivation_DifferentPasswordSaltOrIterations_ProducesDistinctKeys()
    {
        var kdf = new KeyDerivation();
        byte[] salt1 = TestDataGenerator.CreateDeterministicBytes(16, seed: 1);
        byte[] salt2 = TestDataGenerator.CreateDeterministicBytes(16, seed: 2);
        byte[] baseKey = new byte[32];
        byte[] diffPassKey = new byte[32];
        byte[] diffSaltKey = new byte[32];
        byte[] diffIterKey = new byte[32];

        kdf.DeriveKey("pass-1"u8, salt1, 1000, baseKey);
        kdf.DeriveKey("pass-2"u8, salt1, 1000, diffPassKey);
        kdf.DeriveKey("pass-1"u8, salt2, 1000, diffSaltKey);
        kdf.DeriveKey("pass-1"u8, salt1, 2000, diffIterKey);

        Assert.NotEqual(baseKey, diffPassKey);
        Assert.NotEqual(baseKey, diffSaltKey);
        Assert.NotEqual(baseKey, diffIterKey);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void KeyDerivation_NonPositiveIterations_ThrowsArgumentOutOfRangeException(int invalidIterations)
    {
        var kdf = new KeyDerivation();
        byte[] salt = new byte[16];
        byte[] dest = new byte[32];

        Assert.Throws<ArgumentOutOfRangeException>(() => kdf.DeriveKey("secret"u8, salt, invalidIterations, dest));
    }

    [Fact]
    public void NonceGenerator_Fill_PopulatesNonZeroRandomBytes()
    {
        Span<byte> n1 = stackalloc byte[NonceGenerator.AesGcmNonceSize];
        Span<byte> n2 = stackalloc byte[NonceGenerator.AesGcmNonceSize];

        NonceGenerator.Fill(n1);
        NonceGenerator.Fill(n2);

        Assert.False(n1.SequenceEqual(n2));
    }

    [Theory]
    [InlineData(0L, 1L)]
    [InlineData(10L, 20L)]
    [InlineData(-500L, 500L)]
    [InlineData(0L, (1L << 40) + 12345L)]
    [InlineData(long.MinValue / 2, long.MaxValue / 2)]
    public void NonceGenerator_GetInt64_ReturnsValueWithinRequestedRange(long fromInclusive, long toExclusive)
    {
        for (int i = 0; i < 10; i++)
        {
            long value = NonceGenerator.GetInt64(fromInclusive, toExclusive);
            Assert.InRange(value, fromInclusive, toExclusive - 1);
        }
    }

    [Theory]
    [InlineData(5L, 5L)]
    [InlineData(10L, 5L)]
    [InlineData(0L, -1L)]
    public void NonceGenerator_GetInt64_WhenLowerBoundNotLessThanUpperBound_ThrowsArgumentOutOfRangeException(long from, long to)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NonceGenerator.GetInt64(from, to));
    }

    [Fact]
    public void NonceGenerator_LegacyObsoleteApis_WorkAsExpected()
    {
#pragma warning disable CS0618
        byte[] nonce = NonceGenerator.GenerateAesGcmNonce();
        long randomVal = NonceGenerator.SecureRandomNextInt64(100, 200);
#pragma warning restore CS0618

        Assert.Equal(NonceGenerator.AesGcmNonceSize, nonce.Length);
        Assert.InRange(randomVal, 100, 199);
    }

    [Fact]
    public void SecureRandom_FillAndNextInt32AndNextInt64_GenerateHighEntropyValues()
    {
        byte[] b1 = new byte[32];
        byte[] b2 = new byte[32];
        SecureRandom.Fill(b1);
        SecureRandom.Fill(b2);

        Assert.False(b1.AsSpan().SequenceEqual(b2));

        int int32Val = SecureRandom.NextInt32();
        Assert.InRange(int32Val, 0, int.MaxValue - 1);

        long l1 = SecureRandom.NextInt64();
        long l2 = SecureRandom.NextInt64();
        Assert.NotEqual(l1, l2);
    }
}
