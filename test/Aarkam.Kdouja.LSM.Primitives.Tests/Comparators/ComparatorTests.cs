using Aarkam.Kdouja.LSM.Primitives.Comparators;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.Comparators;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.Comparators)]
public sealed class ComparatorTests
{
    [Fact]
    public void DefaultKeyComparator_SingletonAndName_AreExpected()
    {
        IKeyComparator comparator = DefaultKeyComparator.Instance;

        Assert.NotNull(comparator);
        Assert.Equal("kdouja.BytewiseComparator.v1", comparator.Name);
        Assert.Same(DefaultKeyComparator.Instance, comparator);
    }

    [Theory]
    [InlineData("", "", 0)]
    [InlineData("a", "a", 0)]
    [InlineData("a", "b", -1)]
    [InlineData("b", "a", 1)]
    [InlineData("abc", "abcd", -1)]
    [InlineData("abcd", "abc", 1)]
    [InlineData("user:0001", "user:0002", -1)]
    public void DefaultKeyComparator_Compare_OrdersLexicographically(string left, string right, int expectedSign)
    {
        byte[] x = TestDataGenerator.CreateUtf8Bytes(left);
        byte[] y = TestDataGenerator.CreateUtf8Bytes(right);

        int result = DefaultKeyComparator.Instance.Compare(x, y);

        Assert.Equal(expectedSign, Math.Sign(result));
    }

    [Fact]
    public void DefaultKeyComparator_Compare_HighBitBytes_UsesUnsignedByteComparison()
    {
        ReadOnlySpan<byte> low = [0x7F];
        ReadOnlySpan<byte> high = [0x80];

        Assert.True(DefaultKeyComparator.Instance.Compare(low, high) < 0);
        Assert.True(DefaultKeyComparator.Instance.Compare(high, low) > 0);
    }

    [Fact]
    public void DefaultKeyComparator_FindShortestSeparator_ShortensWhenPossible()
    {
        byte[] start = "abcdxyz"u8.ToArray();
        ReadOnlySpan<byte> limit = "abcf"u8;

        int len = DefaultKeyComparator.Instance.FindShortestSeparator(start, limit);
        Assert.Equal(4, len);
        Assert.Equal("abce"u8, start.AsSpan(0, len));
        Assert.True(DefaultKeyComparator.Instance.Compare(start.AsSpan(0, len), limit) < 0);
    }

    [Fact]
    public void DefaultKeyComparator_FindShortestSeparator_LeavesAsIsWhenPrefix()
    {
        byte[] start = "abc"u8.ToArray();
        ReadOnlySpan<byte> limit = "abcd"u8;

        int len = DefaultKeyComparator.Instance.FindShortestSeparator(start, limit);
        Assert.Equal(start.Length, len);
    }

    [Fact]
    public void DefaultKeyComparator_FindShortSuccessor_ShortensProperly()
    {
        byte[] key = [0x10, 0x20, 0x30];
        int len = DefaultKeyComparator.Instance.FindShortSuccessor(key);
        Assert.Equal(1, len);
        Assert.Equal(0x11, key[0]);

        byte[] allFF = [0xFF, 0xFF, 0xFF];
        int lenFF = DefaultKeyComparator.Instance.FindShortSuccessor(allFF);
        Assert.Equal(3, lenFF);
    }

    [Theory]
    [InlineData("", "", 0)]
    [InlineData("alpha", "alpha", 0)]
    [InlineData("alpha", "beta", -1)]
    [InlineData("beta", "alpha", 1)]
    [InlineData("prefix", "prefix_ext", -1)]
    public void SpanLexicographicalComparer_ReadOnlyMemory_OrdersLexicographically(string left, string right, int expectedSign)
    {
        ReadOnlyMemory<byte> x = TestDataGenerator.CreateUtf8Bytes(left);
        ReadOnlyMemory<byte> y = TestDataGenerator.CreateUtf8Bytes(right);

        int result = SpanLexicographicalComparer.Instance.Compare(x, y);

        Assert.Equal(expectedSign, Math.Sign(result));
    }

    [Fact]
    public void SpanLexicographicalComparer_ByteArray_HandlesNullAndNonNullBounds()
    {
        var comparer = SpanLexicographicalComparer.Instance;
        byte[] a = [1, 2, 3];
        byte[] b = [1, 2, 4];

        Assert.Equal(0, comparer.Compare((byte[]?)null, null));
        Assert.Equal(-1, comparer.Compare(null, a));
        Assert.Equal(1, comparer.Compare(a, null));
        Assert.True(comparer.Compare(a, b) < 0);
        Assert.True(comparer.Compare(b, a) > 0);
        Assert.Equal(0, comparer.Compare(a, [1, 2, 3]));
    }

    [Fact]
    public void SpanLexicographicalComparer_WorksWithSortedSetOfByteArrays()
    {
        var set = new SortedSet<byte[]>(SpanLexicographicalComparer.Instance)
        {
            new byte[] { 0x02, 0x00 },
            new byte[] { 0x01, 0xFF },
            new byte[] { 0x01 }
        };

        byte[][] ordered = set.ToArray();
        Assert.Equal(new byte[] { 0x01 }, ordered[0]);
        Assert.Equal(new byte[] { 0x01, 0xFF }, ordered[1]);
        Assert.Equal(new byte[] { 0x02, 0x00 }, ordered[2]);
    }
}
