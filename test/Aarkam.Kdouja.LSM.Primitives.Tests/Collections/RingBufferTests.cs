using Aarkam.Kdouja.LSM.Primitives.Collections;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.Collections;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.Collections)]
public sealed class RingBufferTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void RingBuffer_Constructor_NonPositiveCapacity_ThrowsArgumentException(int invalidCapacity)
    {
        Assert.Throws<ArgumentException>(() => new RingBuffer<int>(invalidCapacity));
    }

    [Fact]
    public void RingBuffer_InitialState_IsEmptyAndNotFull()
    {
        var rb = new RingBuffer<string>(4);

        Assert.Equal(4, rb.Capacity);
        Assert.Equal(0, rb.Count);
        Assert.True(rb.IsEmpty);
        Assert.False(rb.IsFull);
        Assert.False(rb.TryPeek(out _));
        Assert.False(rb.TryDequeue(out _));
    }

    [Fact]
    public void RingBuffer_EnqueueDequeue_MaintainsFifoOrder()
    {
        var rb = new RingBuffer<int>(3);

        rb.Enqueue(10);
        rb.Enqueue(20);
        rb.Enqueue(30);

        Assert.True(rb.IsFull);
        Assert.Equal(3, rb.Count);
        Assert.True(rb.TryPeek(out int peeked));
        Assert.Equal(10, peeked);

        Assert.Equal(10, rb.Dequeue());
        Assert.Equal(20, rb.Dequeue());
        Assert.Equal(30, rb.Dequeue());
        Assert.True(rb.IsEmpty);
    }

    [Fact]
    public void RingBuffer_WhenFull_TryEnqueueReturnsFalseAndEnqueueThrows()
    {
        var rb = new RingBuffer<int>(2);
        Assert.True(rb.TryEnqueue(1));
        Assert.True(rb.TryEnqueue(2));

        Assert.False(rb.TryEnqueue(3));
        Assert.Throws<InvalidOperationException>(() => rb.Enqueue(3));
    }

    [Fact]
    public void RingBuffer_WhenEmpty_DequeueThrowsInvalidOperationException()
    {
        var rb = new RingBuffer<int>(2);
        Assert.Throws<InvalidOperationException>(() => rb.Dequeue());
    }

    [Fact]
    public void RingBuffer_WrapAround_PreservesOrderAcrossMultipleCycles()
    {
        var rb = new RingBuffer<int>(3);

        for (int cycle = 0; cycle < 5; cycle++)
        {
            int baseVal = cycle * 10;
            Assert.True(rb.TryEnqueue(baseVal + 1));
            Assert.True(rb.TryEnqueue(baseVal + 2));

            Assert.True(rb.TryDequeue(out int first));
            Assert.True(rb.TryDequeue(out int second));

            Assert.Equal(baseVal + 1, first);
            Assert.Equal(baseVal + 2, second);
            Assert.True(rb.IsEmpty);
        }
    }

    [Fact]
    public void RingBuffer_Clear_ResetsCountAndIndices()
    {
        var rb = new RingBuffer<string>(4);
        rb.Enqueue("a");
        rb.Enqueue("b");

        rb.Clear();

        Assert.Equal(0, rb.Count);
        Assert.True(rb.IsEmpty);
        Assert.False(rb.IsFull);
        Assert.False(rb.TryPeek(out _));

        rb.Enqueue("c");
        Assert.Equal("c", rb.Dequeue());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(100)]
    public void ConcurrentRingBuffer_NonPowerOfTwoOrLessThanTwoCapacity_ThrowsArgumentException(int invalidCapacity)
    {
        Assert.Throws<ArgumentException>(() => new ConcurrentRingBuffer<int>(invalidCapacity));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(64)]
    public void ConcurrentRingBuffer_ValidPowerOfTwoCapacity_Succeeds(int capacity)
    {
        var crb = new ConcurrentRingBuffer<int>(capacity);
        Assert.Equal(capacity, crb.Capacity);
    }

    [Fact]
    public void ConcurrentRingBuffer_SingleThreadFillAndDrain_RespectsCapacityBounds()
    {
        var crb = new ConcurrentRingBuffer<int>(4);

        Assert.True(crb.TryEnqueue(10));
        Assert.True(crb.TryEnqueue(20));
        Assert.True(crb.TryEnqueue(30));
        Assert.True(crb.TryEnqueue(40));
        Assert.False(crb.TryEnqueue(50));

        Assert.True(crb.TryDequeue(out int v1));
        Assert.True(crb.TryDequeue(out int v2));
        Assert.True(crb.TryDequeue(out int v3));
        Assert.True(crb.TryDequeue(out int v4));
        Assert.False(crb.TryDequeue(out _));

        Assert.Equal([10, 20, 30, 40], [v1, v2, v3, v4]);
    }

    [Fact]
    public async Task ConcurrentRingBuffer_MultiProducerMultiConsumer_TransfersAllItemsWithoutLoss()
    {
        const int totalItems = 2000;
        var crb = new ConcurrentRingBuffer<int>(64);
        int dequeuedSum = 0;
        int dequeuedCount = 0;

        Task[] producers = Enumerable.Range(0, 4).Select(p => Task.Run(() =>
        {
            int start = p * 500 + 1;
            for (int i = start; i < start + 500; i++)
            {
                while (!crb.TryEnqueue(i))
                {
                    Thread.SpinWait(1);
                }
            }
        })).ToArray();

        Task[] consumers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            while (Volatile.Read(ref dequeuedCount) < totalItems)
            {
                if (crb.TryDequeue(out int item))
                {
                    Interlocked.Add(ref dequeuedSum, item);
                    Interlocked.Increment(ref dequeuedCount);
                }
                else
                {
                    Thread.SpinWait(1);
                }
            }
        })).ToArray();

        await Task.WhenAll(producers);
        await Task.WhenAll(consumers);

        int expectedSum = totalItems * (totalItems + 1) / 2;
        Assert.Equal(totalItems, dequeuedCount);
        Assert.Equal(expectedSum, dequeuedSum);
    }
}
