using System.IO;
using Aarkam.Kdouja.LSM.Primitives.Collections;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.Collections;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.Collections)]
public sealed class BloomFilterTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_NonPositiveExpectedItems_ThrowsArgumentOutOfRangeException(int invalidExpectedItems)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BloomFilter(invalidExpectedItems));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-0.05)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public void Constructor_InvalidFalsePositiveRate_ThrowsArgumentOutOfRangeException(double invalidFpr)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BloomFilter(100, invalidFpr));
    }

    [Fact]
    public void Constructor_NullBitsArray_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BloomFilter(null!, 4));
    }

    [Fact]
    public void Constructor_EmptyBitsArray_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new BloomFilter(Array.Empty<byte>(), 4));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(31)]
    public void Constructor_InvalidHashCount_ThrowsArgumentOutOfRangeException(int invalidHashCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BloomFilter(new byte[8], invalidHashCount));
    }

    [Fact]
    public void MayContain_EmptyFilter_ReturnsFalse()
    {
        var filter = new BloomFilter(100, 0.01);

        Assert.False(filter.MayContain("missing-key-1"u8));
        Assert.False(filter.MayContain("missing-key-2"u8));
    }

    [Theory]
    [InlineData(10, 0.05)]
    [InlineData(100, 0.01)]
    [InlineData(500, 0.001)]
    [InlineData(5000, 0.01)]
    public void Add_MayContain_ReturnsTrueForAllInsertedKeys(int keyCount, double falsePositiveRate)
    {
        var filter = new BloomFilter(keyCount, falsePositiveRate);

        for (int i = 0; i < keyCount; i++)
        {
            byte[] key = TestDataGenerator.CreateKeyBytes("user", i);
            filter.Add(key);
        }

        for (int i = 0; i < keyCount; i++)
        {
            byte[] key = TestDataGenerator.CreateKeyBytes("user", i);
            Assert.True(filter.MayContain(key));
        }
    }

    [Fact]
    public void MayContain_FalsePositiveRate_StaysWithinReasonableBounds()
    {
        const int insertedCount = 1000;
        const int probeCount = 2000;
        var filter = new BloomFilter(insertedCount, 0.01);

        for (int i = 0; i < insertedCount; i++)
        {
            filter.Add(TestDataGenerator.CreateKeyBytes("present", i));
        }

        int falsePositives = 0;
        for (int i = 0; i < probeCount; i++)
        {
            if (filter.MayContain(TestDataGenerator.CreateKeyBytes("absent", i)))
            {
                falsePositives++;
            }
        }

        double observedRate = (double)falsePositives / probeCount;
        Assert.True(observedRate < 0.05, $"Observed false positive rate {observedRate:P2} exceeded 5% tolerance.");
    }

    [Fact]
    public void Serialize_Deserialize_PreservesMembershipState()
    {
        var original = new BloomFilter(200, 0.01);
        for (int i = 0; i < 50; i++)
        {
            original.Add(TestDataGenerator.CreateKeyBytes("persisted", i));
        }

        byte[] serialized = original.Serialize();
        BloomFilter rehydrated = BloomFilter.Deserialize(serialized);

        for (int i = 0; i < 50; i++)
        {
            Assert.True(rehydrated.MayContain(TestDataGenerator.CreateKeyBytes("persisted", i)));
        }

        Assert.Equal(serialized, rehydrated.Serialize());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)] // Exactly header size but no bit payload
    public void Deserialize_BufferShorterThanOrEqualToHeader_ThrowsInvalidDataException(int length)
    {
        byte[] invalidData = new byte[length];
        Assert.Throws<InvalidDataException>(() => BloomFilter.Deserialize(invalidData));
    }

    [Fact]
    public void Deserialize_InvalidVersionOrHashId_ThrowsInvalidDataException()
    {
        byte[] valid = new BloomFilter(100, 0.01).Serialize();
        byte[] badVersion = (byte[])valid.Clone();
        badVersion[0] = 99; // invalid version
        Assert.Throws<InvalidDataException>(() => BloomFilter.Deserialize(badVersion));

        byte[] badHash = (byte[])valid.Clone();
        badHash[1] = 99; // invalid hash algorithm id
        Assert.Throws<InvalidDataException>(() => BloomFilter.Deserialize(badHash));
    }
}
